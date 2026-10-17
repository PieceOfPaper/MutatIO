using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Hunger;
using MutatIO.Shared.Movement;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 생명체 이동, 맵 경계, 먹이 흡수와 성장, 포만 감소와 사망을 한 번에 진행하는 월드 시뮬레이션입니다.
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

        /// <summary>생명체가 사망했을 때 발생합니다. 생명체는 이미 월드에서 제거되어 있습니다.</summary>
        public event Action<CreatureDeath> CreatureDied;

        public WorldSettings Settings { get; }

        public WorldBounds Bounds { get; }

        public FoodField Food { get; }

        public IReadOnlyList<Creature> Creatures => creatures;

        public Creature SpawnCreature(Vector2 position)
        {
            var creature = new Creature(nextCreatureId++, MovementState.At(position));
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

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            foreach (var creature in creatures)
            {
                MovementSystem.Step(ref creature.Movement, creature.Command, creature.Size, Settings.movement, deltaTime);
                Bounds.ClampCircle(ref creature.Movement, creature.Radius);
                // 대시 입력은 누른 순간 한 번만 적용합니다. 같은 명령으로 여러 스텝을 진행해도 반복하지 않습니다.
                creature.Command = new PlayerCommand(creature.Command.Move, false);
            }

            consumedThisStep.Clear();
            Food.Step(deltaTime, creatures, consumedThisStep);
            foreach (var consumption in consumedThisStep)
            {
                var eater = consumption.Creature;
                eater.AddIntake(consumption.Food.Intake, Settings.growth);
                var gain = HungerSystem.GetSatietyGain(consumption.Food.Kind, Settings.hunger);
                eater.SetSatiety(HungerSystem.Restore(eater.Satiety, gain, Settings.hunger));
                FoodConsumed?.Invoke(consumption.Food, eater);
            }

            // 같은 스텝에 먹은 회복을 먼저 반영한 뒤 포만을 줄입니다. 굶주림은 점수·몸집을 깎지 않습니다.
            deathsThisStep.Clear();
            foreach (var creature in creatures)
            {
                creature.SetSatiety(HungerSystem.Drain(creature.Satiety, Settings.hunger, deltaTime));
                if (creature.Satiety <= 0f)
                {
                    deathsThisStep.Add(new CreatureDeath(creature, DeathCause.Starvation));
                }
            }

            foreach (var death in deathsThisStep)
            {
                // 흡수 중이던 먹이는 다음 스텝에 대상이 없어 현재 위치에서 해제됩니다.
                creatures.Remove(death.Creature);
                death.Creature.MarkDead();
                CreatureDied?.Invoke(death);
            }
        }
    }
}
