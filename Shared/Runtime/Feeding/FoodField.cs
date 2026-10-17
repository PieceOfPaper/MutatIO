using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;

namespace MutatIO.Shared.Feeding
{
    public readonly struct FoodConsumption
    {
        public FoodConsumption(FoodItem food, Creature creature)
        {
            Food = food;
            Creature = creature;
        }

        public FoodItem Food { get; }

        public Creature Creature { get; }
    }

    /// <summary>
    /// 필드의 먹이 생성, 자석 흡수, 섭취 판정입니다.
    /// 흡수 연출(끌려가는 위치)과 보상(섭취)을 분리하며, 섭취한 먹이는 즉시 목록에서 제거되므로 보상이 한 번만 지급됩니다.
    /// </summary>
    public sealed class FoodField
    {
        private const int SpawnAttempts = 8;

        private readonly FoodSettings settings;
        private readonly WorldBounds bounds;
        private readonly Random random;
        private readonly List<FoodItem> items = new List<FoodItem>();
        private int nextId = 1;
        private int plantCount;
        private float regenTimer;

        public FoodField(FoodSettings settings, WorldBounds bounds, Random random)
        {
            this.settings = settings;
            this.bounds = bounds;
            this.random = random;
        }

        public event Action<FoodItem> Spawned;

        public IReadOnlyList<FoodItem> Items => items;

        public int PlantCount => plantCount;

        /// <summary>초식 먹이를 최대 개수까지 채웁니다. 간격 조건 때문에 일부를 채우지 못할 수 있습니다.</summary>
        public void FillPlants()
        {
            var attempts = settings.maxPlantCount * 2;
            for (var i = 0; i < attempts && plantCount < settings.maxPlantCount; i++)
            {
                TrySpawnPlant();
            }
        }

        public FoodItem Spawn(FoodKind kind, Vector2 position, float intake)
        {
            var food = new FoodItem(nextId++, kind, position, intake);
            items.Add(food);
            if (kind == FoodKind.Plant)
            {
                plantCount++;
            }

            Spawned?.Invoke(food);
            return food;
        }

        /// <summary>
        /// 리젠, 흡수 대상 지정, 끌어당김, 섭취를 dt만큼 진행합니다. 이번 스텝에 섭취된 먹이를 consumed에 추가합니다.
        /// </summary>
        public void Step(float deltaTime, IReadOnlyList<Creature> creatures, List<FoodConsumption> consumed)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateRegen(deltaTime);

            for (var i = items.Count - 1; i >= 0; i--)
            {
                var food = items[i];
                var attractor = UpdateAttractor(food, creatures);
                if (attractor == null)
                {
                    continue;
                }

                food.Position = MoveTowards(food.Position, attractor.Movement.Position, settings.magnetPullSpeed * deltaTime);
                var consumeRange = attractor.Radius + settings.consumeDistance;
                if (Vector2.DistanceSquared(food.Position, attractor.Movement.Position) <= consumeRange * consumeRange)
                {
                    Remove(i);
                    consumed.Add(new FoodConsumption(food, attractor));
                }
            }
        }

        private void UpdateRegen(float deltaTime)
        {
            if (plantCount >= settings.maxPlantCount || settings.plantRegenInterval <= 0f)
            {
                regenTimer = 0f;
                return;
            }

            regenTimer += deltaTime;
            while (regenTimer >= settings.plantRegenInterval && plantCount < settings.maxPlantCount)
            {
                regenTimer -= settings.plantRegenInterval;
                TrySpawnPlant();
            }
        }

        private bool TrySpawnPlant()
        {
            // 임시: 배치 방식은 미결정이며 현재는 맵 안 랜덤 위치 + 최소 간격입니다.
            var spacingSquared = settings.plantSpacing * settings.plantSpacing;
            for (var attempt = 0; attempt < SpawnAttempts; attempt++)
            {
                var position = new Vector2(
                    (float)(random.NextDouble() * 2.0 - 1.0) * bounds.HalfWidth,
                    (float)(random.NextDouble() * 2.0 - 1.0) * bounds.HalfHeight);
                if (IsFarFromOthers(position, spacingSquared))
                {
                    Spawn(FoodKind.Plant, position, settings.plantIntake);
                    return true;
                }
            }

            return false;
        }

        private bool IsFarFromOthers(Vector2 position, float spacingSquared)
        {
            foreach (var other in items)
            {
                if (Vector2.DistanceSquared(position, other.Position) < spacingSquared)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 임시: 여러 개체가 같은 먹이에 접근할 때의 소유권은 미결정입니다.
        /// 현재는 흡수 반경 안에서 가장 가까운 개체가 먼저 끌어당기고, 그 개체가 사라지거나 충분히 멀어질 때까지 유지합니다.
        /// </summary>
        private Creature UpdateAttractor(FoodItem food, IReadOnlyList<Creature> creatures)
        {
            if (food.AttractorId != FoodItem.NoAttractor)
            {
                var current = Find(creatures, food.AttractorId);
                if (current != null)
                {
                    var releaseRange = GetMagnetRadius(current) + settings.magnetReleaseMargin;
                    if (Vector2.DistanceSquared(food.Position, current.Movement.Position) <= releaseRange * releaseRange)
                    {
                        return current;
                    }
                }

                food.AttractorId = FoodItem.NoAttractor;
            }

            Creature nearest = null;
            var nearestDistanceSquared = float.MaxValue;
            foreach (var creature in creatures)
            {
                var range = GetMagnetRadius(creature);
                var distanceSquared = Vector2.DistanceSquared(food.Position, creature.Movement.Position);
                if (distanceSquared <= range * range && distanceSquared < nearestDistanceSquared)
                {
                    nearest = creature;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            if (nearest != null)
            {
                food.AttractorId = nearest.Id;
            }

            return nearest;
        }

        private float GetMagnetRadius(Creature creature)
        {
            return creature.Radius + settings.magnetRange;
        }

        private void Remove(int index)
        {
            if (items[index].Kind == FoodKind.Plant)
            {
                plantCount--;
            }

            items.RemoveAt(index);
        }

        private static Creature Find(IReadOnlyList<Creature> creatures, int id)
        {
            foreach (var creature in creatures)
            {
                if (creature.Id == id)
                {
                    return creature;
                }
            }

            return null;
        }

        private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
        {
            var difference = target - current;
            var distance = difference.Length();
            if (distance <= maxDelta || distance <= float.Epsilon)
            {
                return target;
            }

            return current + difference / distance * maxDelta;
        }
    }
}
