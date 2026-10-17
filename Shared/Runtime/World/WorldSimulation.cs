using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Movement;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 생명체 이동, 맵 경계, 먹이 흡수와 성장을 한 번에 진행하는 월드 시뮬레이션입니다.
    /// 로컬 프로토타입에서는 클라이언트가 직접 실행하고, 온라인 단계에서는 서버가 같은 코드로 권위 판정을 합니다.
    /// 고정 틱을 가정하지 않고 호출자가 넘긴 dt만큼 진행합니다.
    /// </summary>
    public sealed class WorldSimulation
    {
        private readonly List<Creature> creatures = new List<Creature>();
        private readonly List<FoodConsumption> consumedThisStep = new List<FoodConsumption>();
        private int nextCreatureId = 1;

        public WorldSimulation(WorldSettings settings, int seed)
        {
            Settings = settings;
            Bounds = new WorldBounds(settings.width, settings.height);
            Food = new FoodField(settings.food, Bounds, new Random(seed));
        }

        /// <summary>먹이가 섭취되어 필드에서 사라졌을 때 발생합니다. 생명체의 섭취량은 이미 반영되어 있습니다.</summary>
        public event Action<FoodItem, Creature> FoodConsumed;

        public WorldSettings Settings { get; }

        public WorldBounds Bounds { get; }

        public FoodField Food { get; }

        public IReadOnlyList<Creature> Creatures => creatures;

        public Creature SpawnCreature(Vector2 position)
        {
            var creature = new Creature(nextCreatureId++, MovementState.At(position));
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
                consumption.Creature.AddIntake(consumption.Food.Intake, Settings.growth);
                FoodConsumed?.Invoke(consumption.Food, consumption.Creature);
            }
        }
    }
}
