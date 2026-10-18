using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Combat;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Hunger;
using MutatIO.Shared.Movement;
using MutatIO.Shared.Mutation;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 생명체 이동, 맵 경계, 접촉 밀침, 먹이 흡수와 성장, 변이, 접촉 처치, 포만 감소와 사망을 한 번에 진행하는 월드 시뮬레이션입니다.
    /// 로컬 프로토타입에서는 클라이언트가 직접 실행하고, 온라인 단계에서는 서버가 같은 코드로 권위 판정을 합니다.
    /// 고정 틱을 가정하지 않고 호출자가 넘긴 dt만큼 진행합니다.
    /// </summary>
    public sealed class WorldSimulation
    {
        private readonly List<Creature> creatures = new List<Creature>();
        private readonly List<FoodConsumption> consumedThisStep = new List<FoodConsumption>();
        private readonly List<CreatureDeath> deathsThisStep = new List<CreatureDeath>();
        private int nextCreatureId = 1;

        public WorldSimulation(WorldSettings settings, int seed)
        {
            Settings = settings;
            Bounds = new WorldBounds(settings.width, settings.height);
            Food = new FoodField(settings.food, Bounds, new Random(seed));
        }

        /// <summary>먹이가 섭취되어 필드에서 사라졌을 때 발생합니다. 생명체의 섭취량은 이미 반영되어 있습니다.</summary>
        public event Action<FoodItem, Creature> FoodConsumed;

        /// <summary>초식이 변이해 육식이 되는 등 역할이 바뀌었을 때 발생합니다.</summary>
        public event Action<Creature> CreatureMutated;

        /// <summary>생명체가 사망했을 때 발생합니다. 생명체는 이미 월드에서 제거되어 있고, 초식이면 고기가 이미 생성되어 있습니다.</summary>
        public event Action<CreatureDeath> CreatureDied;

        public WorldSettings Settings { get; }

        public WorldBounds Bounds { get; }

        public FoodField Food { get; }

        public IReadOnlyList<Creature> Creatures => creatures;

        public Creature SpawnCreature(Vector2 position, string name = null)
        {
            var creature = new Creature(nextCreatureId++, MovementState.At(position)) { Name = name ?? string.Empty };
            creature.SetSatiety(Math.Min(Settings.hunger.startSatiety, Settings.hunger.maxSatiety));
            Bounds.ClampCircle(ref creature.Movement, creature.Radius);
            creatures.Add(creature);
            return creature;
        }

        public bool RemoveCreature(int id)
        {
            return creatures.RemoveAll(creature => creature.Id == id) > 0;
        }

        public Creature FindCreature(int id)
        {
            return creatures.Find(creature => creature.Id == id);
        }

        /// <summary>
        /// 역할을 바꿉니다. 진행 중인 흡수는 현재 위치에 남기고 취소하며 변이 수치는 0으로 둡니다.
        /// 변이 판정과 개발용 확인에서 사용합니다.
        /// </summary>
        public void ChangeRole(Creature creature, CreatureRole role)
        {
            if (creature.Role == role)
            {
                return;
            }

            creature.SetRole(role);
            creature.SetMutation(0f);
            Food.ReleaseAttractedBy(creature.Id);
            CreatureMutated?.Invoke(creature);
        }

        /// <summary>
        /// dt만큼 진행합니다. 순서: 이동·경계 → 비처치 쌍 밀침·육식의 초식 먹이 밀기 → 먹이 흡수·섭취(점수·몸집·포만·변이 갱신, 변이 전환)
        /// → 갱신된 상태로 접촉 처치 판정 → 포만·변이 감소와 굶주림 판정 → 사망 일괄 반영(초식·육식 모두 고기를 남김).
        /// 섭취·성장·변이를 접촉보다 먼저 처리하는 것은 확정 규칙입니다.
        /// 임시: 처치와 굶주림을 같은 스텝에 일괄 반영하는 것, 처치당한 개체는 포만 감소를 건너뛰는 것은 미결정 순서의 임시안입니다.
        /// </summary>
        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            foreach (var creature in creatures)
            {
                MovementSystem.Step(ref creature.Movement, creature.Command, Settings.movement, deltaTime);
                Bounds.ClampCircle(ref creature.Movement, creature.Radius);
                // 대시 입력은 누른 순간 한 번만 적용합니다. 같은 명령으로 여러 스텝을 진행해도 반복하지 않습니다.
                creature.Command = new PlayerCommand(creature.Command.Move, false);
            }

            ContactSystem.SeparateNonLethal(creatures, Bounds, Settings.contact, deltaTime);
            Food.PushPlants(creatures, Settings.contact.plantPushMargin);

            consumedThisStep.Clear();
            Food.Step(deltaTime, creatures, consumedThisStep);
            foreach (var consumption in consumedThisStep)
            {
                Consume(consumption.Food, consumption.Creature);
            }

            deathsThisStep.Clear();
            ContactSystem.FindKills(creatures, deathsThisStep);

            // 같은 스텝에 먹은 회복을 먼저 반영한 뒤 포만을 줄입니다. 굶주림은 점수·몸집을 깎지 않습니다.
            foreach (var creature in creatures)
            {
                if (IsDying(creature))
                {
                    continue;
                }

                if (creature.Role == CreatureRole.Herbivore)
                {
                    creature.SetMutation(MutationSystem.Decay(creature.Mutation, Settings.mutation, deltaTime));
                }

                creature.SetSatiety(HungerSystem.Drain(creature.Satiety, Settings.hunger, deltaTime));
                if (creature.Satiety <= 0f)
                {
                    deathsThisStep.Add(new CreatureDeath(creature, DeathCause.Starvation));
                }
            }

            foreach (var death in deathsThisStep)
            {
                // 흡수 중이던 먹이는 다음 스텝에 대상이 없어 현재 위치에서 해제됩니다.
                var dead = death.Creature;
                creatures.Remove(dead);
                dead.MarkDead();
                // 초식·육식 모두 사망 시 고기를 남깁니다.
                Food.DropMeat(dead.Movement.Position, dead.Radius, GetMeatDropCount(dead.Intake, Settings.food));

                CreatureDied?.Invoke(death);
            }
        }

        /// <summary>(DROP-01) 사망 시(초식·육식) 남기는 고기 개수. 임시: 반올림 변환은 임시안입니다.</summary>
        public static int GetMeatDropCount(int intake, FoodSettings settings)
        {
            return (int)Math.Round(Math.Max(0, intake) * Math.Max(0f, settings.meatDropRatio), MidpointRounding.AwayFromZero);
        }

        private void Consume(FoodItem food, Creature eater)
        {
            // 회복 배율은 이 먹이를 먹기 전 몸집으로 계산합니다.
            var gain = HungerSystem.GetSatietyGain(food.Kind, eater.Role, eater.Size, Settings.hunger);
            eater.AddIntake(1, Settings.growth);
            eater.SetSatiety(HungerSystem.Restore(eater.Satiety, gain, Settings.hunger));

            var mutated = false;
            if (food.Kind == FoodKind.Meat && eater.Role == CreatureRole.Herbivore)
            {
                // 임계치는 이 먹이로 성장한 뒤의 몸집으로 계산합니다.
                eater.SetMutation(eater.Mutation + Math.Max(0f, Settings.mutation.gainPerMeat));
                mutated = eater.Mutation >= MutationSystem.GetThreshold(eater.Size, Settings.mutation);
            }

            FoodConsumed?.Invoke(food, eater);
            if (mutated)
            {
                ChangeRole(eater, CreatureRole.Carnivore);
            }
        }

        private bool IsDying(Creature creature)
        {
            foreach (var death in deathsThisStep)
            {
                if (death.Creature == creature)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
