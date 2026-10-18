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
    /// <remarks>
    /// 초식 먹이는 맵을 한 변 <see cref="FoodSettings.tileSize"/>인 타일로 나누어 타일별로 리젠합니다.
    /// - 타일 안의 랜덤 위치에 생성하고, 그 타일을 먹이의 귀속 타일(<see cref="FoodItem.OriginTile"/>)로 기록합니다.
    /// - 타일 상한은 현재 위치가 아니라 귀속 타일이 생성해 아직 남아 있는 개수로 셉니다. 섭취·제거될 때만 줄어듭니다.
    /// - 생명체 원이 덮고 있는 타일은 리젠하지 않습니다. 이미 있는 먹이는 제거하지 않습니다.
    /// 같은 타일 격자를 먹이 탐색용 공간 분할로도 사용합니다(현재 위치 기준).
    /// 타일 크기와 맵 크기는 생성 시에만 적용합니다.
    /// </remarks>
    public sealed class FoodField
    {
        private const int MeatSpawnAttempts = 8;
        private const float MinMeatSpawnInterval = 0.001f;

        private readonly FoodSettings settings;
        private readonly WorldBounds bounds;
        private readonly Random random;
        private readonly List<FoodItem> items = new List<FoodItem>();
        private readonly List<FoodItem> attracted = new List<FoodItem>();
        private readonly List<FoodItem> pushed = new List<FoodItem>();
        private readonly Dictionary<int, Creature> creaturesById = new Dictionary<int, Creature>();
        private readonly List<FoodItem>[] cells;
        private readonly int[] ownedCounts;
        private readonly float[] regenTimers;
        private readonly int[] occupiedStamps;
        private int occupiedStamp;
        private float meatSpawnTimer;
        private int nextId = 1;
        private int plantCount;

        public FoodField(FoodSettings settings, WorldBounds bounds, Random random)
        {
            this.settings = settings;
            this.bounds = bounds;
            this.random = random;

            TileSize = Math.Max(0.01f, settings.tileSize);
            Columns = Math.Max(1, (int)Math.Ceiling(bounds.HalfWidth * 2f / TileSize));
            Rows = Math.Max(1, (int)Math.Ceiling(bounds.HalfHeight * 2f / TileSize));
            cells = new List<FoodItem>[TileCount];
            ownedCounts = new int[TileCount];
            regenTimers = new float[TileCount];
            occupiedStamps = new int[TileCount];
            meatSpawnTimer = NextMeatSpawnInterval();
        }

        public event Action<FoodItem> Spawned;

        public IReadOnlyList<FoodItem> Items => items;

        public int PlantCount => plantCount;

        public float TileSize { get; }

        public int Columns { get; }

        public int Rows { get; }

        public int TileCount => Columns * Rows;

        /// <summary>위치가 속한 타일 인덱스입니다. 맵 밖 위치는 가장 가까운 가장자리 타일로 봅니다.</summary>
        public int GetTileIndex(Vector2 position)
        {
            var column = Clamp((int)Math.Floor((position.X + bounds.HalfWidth) / TileSize), 0, Columns - 1);
            var row = Clamp((int)Math.Floor((position.Y + bounds.HalfHeight) / TileSize), 0, Rows - 1);
            return row * Columns + column;
        }

        /// <summary>타일 영역입니다. 맵 크기가 타일 크기로 나누어떨어지지 않으면 마지막 타일은 맵 경계에서 잘립니다.</summary>
        public void GetTileBounds(int tile, out Vector2 min, out Vector2 max)
        {
            var column = tile % Columns;
            var row = tile / Columns;
            min = new Vector2(-bounds.HalfWidth + column * TileSize, -bounds.HalfHeight + row * TileSize);
            max = new Vector2(Math.Min(min.X + TileSize, bounds.HalfWidth), Math.Min(min.Y + TileSize, bounds.HalfHeight));
        }

        /// <summary>타일이 생성해 아직 남아 있는 초식 먹이 개수입니다. 다른 타일로 이동한 먹이도 포함합니다.</summary>
        public int GetOwnedPlantCount(int tile)
        {
            return ownedCounts[tile];
        }

        /// <summary>
        /// 모든 타일을 상한까지 채웁니다. 월드 시작 시 생명체가 없을 때 호출하므로 점유 차단을 검사하지 않습니다.
        /// 임시: 시작 시 가득 채우는 방식은 미결정이며 확인용 임시안입니다.
        /// </summary>
        public void FillPlants()
        {
            for (var tile = 0; tile < TileCount; tile++)
            {
                while (ownedCounts[tile] < settings.maxPlantsPerTile)
                {
                    SpawnPlantInTile(tile);
                }

                regenTimers[tile] = 0f;
            }
        }

        /// <summary>
        /// 필드 고기(FOOD-08)를 정상 상태 개수(유지시간 ÷ 평균 생성 간격)만큼 맵 전체에 미리 깝니다.
        /// 남은 시간을 0~유지시간 사이로 나누어 한꺼번에 썩지 않게 합니다. 필드 생성이 꺼져 있거나 고기가 썩지 않으면 아무것도 하지 않습니다.
        /// 월드 시작 시 생명체가 없을 때 호출하므로 점유 차단을 검사하지 않습니다.
        /// </summary>
        public void FillFieldMeat()
        {
            var count = GetFieldMeatSteadyCount(settings);
            for (var i = 0; i < count; i++)
            {
                var meat = Spawn(FoodKind.Meat, RandomPositionInBounds());
                meat.TimeLeft = Math.Max(MinMeatSpawnInterval, (float)random.NextDouble() * meat.Lifetime);
            }
        }

        /// <summary>필드 고기가 먹히지 않을 때 유지되는 평균 개수입니다. 유지시간 ÷ 평균 생성 간격(반올림).</summary>
        public static int GetFieldMeatSteadyCount(FoodSettings settings)
        {
            var min = Math.Max(0f, settings.meatSpawnIntervalMin);
            var max = Math.Max(min, settings.meatSpawnIntervalMax);
            var average = (min + max) * 0.5f;
            if (settings.meatSpawnIntervalMax <= 0f || settings.meatLifetime <= 0f || average <= 0f)
            {
                return 0;
            }

            return (int)Math.Round(settings.meatLifetime / average);
        }

        /// <summary>먹이가 아직 필드에 남아 있는지입니다. 섭취·제거된 먹이는 false입니다.</summary>
        public bool Contains(FoodItem food)
        {
            return food != null && food.ListIndex < items.Count && items[food.ListIndex] == food;
        }

        /// <summary>
        /// 타일 리젠과 무관한 먹이를 직접 생성합니다. 어느 타일의 상한에도 포함하지 않습니다.
        /// 고기는 <see cref="FoodSettings.meatLifetime"/>이 지나면 썩어서 사라집니다.
        /// </summary>
        public FoodItem Spawn(FoodKind kind, Vector2 position)
        {
            return Add(new FoodItem(nextId++, kind, position, FoodItem.NoTile, GetLifetime(kind)));
        }

        /// <summary>
        /// 죽은 초식 주변(몸 반경 + 범위 안)에 고기를 count개 흩뿌립니다. 맵 밖 위치는 경계 안으로 당깁니다.
        /// 임시: 원 안 균등 무작위 배치는 미결정 분배 방식의 임시안입니다.
        /// </summary>
        public void DropMeat(Vector2 center, float radius, int count)
        {
            var spread = Math.Max(0f, radius) + Math.Max(0f, settings.meatDropSpread);
            for (var i = 0; i < count; i++)
            {
                var angle = random.NextDouble() * Math.PI * 2.0;
                var distance = spread * (float)Math.Sqrt(random.NextDouble());
                var position = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
                Spawn(FoodKind.Meat, ClampToBounds(position));
            }
        }

        /// <summary>
        /// 생명체가 흡수 중인 먹이를 모두 현재 위치에 남기고 선점을 해제합니다. 변이로 역할이 바뀔 때 사용합니다.
        /// 아직 먹은 것이 아니므로 보상은 없습니다.
        /// </summary>
        public void ReleaseAttractedBy(int creatureId)
        {
            for (var i = attracted.Count - 1; i >= 0; i--)
            {
                var food = attracted[i];
                if (food.AttractorId == creatureId)
                {
                    food.AttractorId = FoodItem.NoAttractor;
                    RemoveAttractedAt(i);
                }
            }
        }

        /// <summary>
        /// 육식의 몸에 닿은 초식 먹이를 몸 가장자리 밖으로 밀어냅니다. 먹이를 없애거나 보상으로 바꾸지 않으며 귀속 타일은 그대로입니다.
        /// 임시: 밀림 반응(PUSH-01)은 몸 가장자리 + 여유 거리로 즉시 옮기는 방식입니다. 여러 육식이 동시에 밀면 생명체 목록 순서대로 처리하고,
        /// 맵 밖으로 밀린 먹이는 경계 안으로 당깁니다.
        /// </summary>
        public void PushPlants(IReadOnlyList<Creature> creatures, float margin)
        {
            foreach (var creature in creatures)
            {
                if (creature.Role != CreatureRole.Carnivore)
                {
                    continue;
                }

                var center = creature.Movement.Position;
                var radius = creature.Radius;
                var extent = new Vector2(radius, radius);
                GetCellRange(center - extent, center + extent, out var minColumn, out var minRow, out var maxColumn, out var maxRow);
                pushed.Clear();
                for (var row = minRow; row <= maxRow; row++)
                {
                    for (var column = minColumn; column <= maxColumn; column++)
                    {
                        var cell = cells[row * Columns + column];
                        if (cell == null)
                        {
                            continue;
                        }

                        foreach (var food in cell)
                        {
                            if (food.Kind == FoodKind.Plant && Vector2.DistanceSquared(food.Position, center) < radius * radius)
                            {
                                pushed.Add(food);
                            }
                        }
                    }
                }

                foreach (var food in pushed)
                {
                    var offset = food.Position - center;
                    var distance = offset.Length();
                    var direction = distance > 0.0001f ? offset / distance : Vector2.UnitY;
                    food.Position = ClampToBounds(center + direction * (radius + Math.Max(0f, margin)));
                    UpdateCell(food);
                }
            }
        }

        /// <summary>사각 영역(현재 위치 기준) 안의 먹이를 results에 추가합니다. 표시 컬링 등에 사용합니다.</summary>
        public void QueryRect(Vector2 min, Vector2 max, List<FoodItem> results)
        {
            GetCellRange(min, max, out var minColumn, out var minRow, out var maxColumn, out var maxRow);
            for (var row = minRow; row <= maxRow; row++)
            {
                for (var column = minColumn; column <= maxColumn; column++)
                {
                    var cell = cells[row * Columns + column];
                    if (cell == null)
                    {
                        continue;
                    }

                    foreach (var food in cell)
                    {
                        var position = food.Position;
                        if (position.X >= min.X && position.X <= max.X && position.Y >= min.Y && position.Y <= max.Y)
                        {
                            results.Add(food);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 리젠, 흡수 해제·대상 지정, 끌어당김, 섭취를 dt만큼 진행합니다. 이번 스텝에 섭취된 먹이를 consumed에 추가합니다.
        /// </summary>
        public void Step(float deltaTime, IReadOnlyList<Creature> creatures, List<FoodConsumption> consumed)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            creaturesById.Clear();
            foreach (var creature in creatures)
            {
                creaturesById[creature.Id] = creature;
            }

            MarkOccupiedTiles(creatures);
            UpdateRegen(deltaTime);
            UpdateMeatSpawn(deltaTime);
            ReleaseOrphans();
            UpdateDecay(deltaTime);
            AssignAttractors(creatures);
            PullAndConsume(deltaTime, consumed);
        }

        private void MarkOccupiedTiles(IReadOnlyList<Creature> creatures)
        {
            occupiedStamp++;
            foreach (var creature in creatures)
            {
                var center = creature.Movement.Position;
                var radius = creature.Radius;
                var extent = new Vector2(radius, radius);
                GetCellRange(center - extent, center + extent, out var minColumn, out var minRow, out var maxColumn, out var maxRow);
                for (var row = minRow; row <= maxRow; row++)
                {
                    for (var column = minColumn; column <= maxColumn; column++)
                    {
                        var tile = row * Columns + column;
                        GetTileBounds(tile, out var min, out var max);
                        // 중심 소속 타일만이 아니라 몸 원이 조금이라도 덮는 모든 타일을 점유로 봅니다.
                        var closest = Vector2.Clamp(center, min, max);
                        if (Vector2.DistanceSquared(center, closest) < radius * radius)
                        {
                            occupiedStamps[tile] = occupiedStamp;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 타일별 리젠입니다. 상한과 점유 조건을 모두 만족할 때만 생성합니다.
        /// 임시: 리젠 시계 규칙은 미결정입니다. 현재는 잔존 개수가 상한보다 적을 때만 시계가 진행하고(상한에 닿은 동안은 0),
        /// 주기가 찼을 때 점유 중이면 생성을 미루어 점유가 풀린 첫 스텝에 생성합니다.
        /// </summary>
        private void UpdateRegen(float deltaTime)
        {
            var limit = settings.maxPlantsPerTile;
            if (limit <= 0)
            {
                return;
            }

            var interval = Math.Max(0f, settings.plantRegenInterval);
            for (var tile = 0; tile < ownedCounts.Length; tile++)
            {
                if (ownedCounts[tile] >= limit)
                {
                    regenTimers[tile] = 0f;
                    continue;
                }

                var timer = regenTimers[tile] + deltaTime;
                if (timer < interval)
                {
                    regenTimers[tile] = timer;
                    continue;
                }

                if (occupiedStamps[tile] == occupiedStamp)
                {
                    regenTimers[tile] = interval;
                    continue;
                }

                SpawnPlantInTile(tile);
                regenTimers[tile] = 0f;
            }
        }

        /// <summary>
        /// 가끔 필드에 고기를 1개 만듭니다. 생명체가 덮은 타일은 피하며, 후보가 모두 덮여 있으면 다음 스텝에 다시 시도합니다.
        /// 임시: 생성 간격·개수·위치 규칙은 프로토타입용 임시안입니다.
        /// </summary>
        private void UpdateMeatSpawn(float deltaTime)
        {
            if (settings.meatSpawnIntervalMax <= 0f)
            {
                return;
            }

            meatSpawnTimer -= deltaTime;
            // 간격이 스텝보다 짧으면 한 스텝에 여러 개를 만듭니다.
            while (meatSpawnTimer <= 0f)
            {
                if (!TrySpawnFieldMeat())
                {
                    return;
                }

                meatSpawnTimer += Math.Max(MinMeatSpawnInterval, NextMeatSpawnInterval());
            }
        }

        private bool TrySpawnFieldMeat()
        {
            for (var attempt = 0; attempt < MeatSpawnAttempts; attempt++)
            {
                var position = RandomPositionInBounds();
                if (occupiedStamps[GetTileIndex(position)] != occupiedStamp)
                {
                    Spawn(FoodKind.Meat, position);
                    return true;
                }
            }

            return false;
        }

        private Vector2 RandomPositionInBounds()
        {
            return new Vector2(
                ((float)random.NextDouble() * 2f - 1f) * bounds.HalfWidth,
                ((float)random.NextDouble() * 2f - 1f) * bounds.HalfHeight);
        }

        private float NextMeatSpawnInterval()
        {
            var min = Math.Max(0f, settings.meatSpawnIntervalMin);
            var max = Math.Max(min, settings.meatSpawnIntervalMax);
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>흡수 대상이 월드에서 사라졌으면(사망·이탈) 현재 위치에 남기고 선점을 해제합니다.</summary>
        private void ReleaseOrphans()
        {
            for (var i = attracted.Count - 1; i >= 0; i--)
            {
                var food = attracted[i];
                if (!creaturesById.ContainsKey(food.AttractorId))
                {
                    food.AttractorId = FoodItem.NoAttractor;
                    RemoveAttractedAt(i);
                }
            }
        }

        /// <summary>
        /// 유지시간이 있는 먹이(고기)의 남은 시간을 줄이고, 다 된 먹이를 제거합니다. 보상은 없습니다.
        /// 흡수 중인 먹이는 시간이 다 되어도 남겨 두어 섭취를 마치게 하고, 선점이 해제되면 다음 스텝에 사라집니다.
        /// </summary>
        private void UpdateDecay(float deltaTime)
        {
            // 뒤에서부터 돌면 제거 시 끝에서 옮겨 오는 먹이는 이미 처리한 먹이입니다.
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var food = items[i];
                if (!food.Decays)
                {
                    continue;
                }

                food.TimeLeft = Math.Max(0f, food.TimeLeft - deltaTime);
                if (food.TimeLeft <= 0f && food.AttractorId == FoodItem.NoAttractor)
                {
                    Remove(food);
                }
            }
        }

        /// <summary>
        /// 흡수 반경(몸 반경 + 고정 거리)에 들어온, 역할상 먹을 수 있는 비어 있는 먹이를 선점시킵니다. 육식은 초식 먹이를 끌어당기지 않습니다.
        /// 먼저 반경에 들어온 생명체가 선점하며, 선점은 대상이 사라질 때까지 유지합니다(거리로 해제하지 않음).
        /// 같은 스텝에 여러 생명체가 들어오면 생명체 목록 순서상 먼저 판정된 하나에 배정합니다.
        /// </summary>
        private void AssignAttractors(IReadOnlyList<Creature> creatures)
        {
            foreach (var creature in creatures)
            {
                var center = creature.Movement.Position;
                var range = GetMagnetRadius(creature);
                var rangeSquared = range * range;
                var extent = new Vector2(range, range);
                GetCellRange(center - extent, center + extent, out var minColumn, out var minRow, out var maxColumn, out var maxRow);
                for (var row = minRow; row <= maxRow; row++)
                {
                    for (var column = minColumn; column <= maxColumn; column++)
                    {
                        var cell = cells[row * Columns + column];
                        if (cell == null)
                        {
                            continue;
                        }

                        foreach (var food in cell)
                        {
                            if (food.AttractorId == FoodItem.NoAttractor
                                && EcologyRules.CanEat(creature.Role, food.Kind)
                                && Vector2.DistanceSquared(food.Position, center) <= rangeSquared)
                            {
                                food.AttractorId = creature.Id;
                                attracted.Add(food);
                            }
                        }
                    }
                }
            }
        }

        private void PullAndConsume(float deltaTime, List<FoodConsumption> consumed)
        {
            var maxDelta = settings.magnetPullSpeed * deltaTime;
            for (var i = attracted.Count - 1; i >= 0; i--)
            {
                var food = attracted[i];
                var attractor = creaturesById[food.AttractorId];
                var target = attractor.Movement.Position;
                food.Position = MoveTowards(food.Position, target, maxDelta);
                UpdateCell(food);

                var consumeRange = attractor.Radius + settings.consumeDistance;
                if (Vector2.DistanceSquared(food.Position, target) <= consumeRange * consumeRange)
                {
                    RemoveAttractedAt(i);
                    Remove(food);
                    consumed.Add(new FoodConsumption(food, attractor));
                }
            }
        }

        private void SpawnPlantInTile(int tile)
        {
            GetTileBounds(tile, out var min, out var max);
            var position = new Vector2(
                min.X + (float)random.NextDouble() * (max.X - min.X),
                min.Y + (float)random.NextDouble() * (max.Y - min.Y));
            ownedCounts[tile]++;
            Add(new FoodItem(nextId++, FoodKind.Plant, position, tile, GetLifetime(FoodKind.Plant)));
        }

        private FoodItem Add(FoodItem food)
        {
            food.ListIndex = items.Count;
            items.Add(food);
            food.Cell = GetTileIndex(food.Position);
            GetOrCreateCell(food.Cell).Add(food);
            if (food.Kind == FoodKind.Plant)
            {
                plantCount++;
            }

            Spawned?.Invoke(food);
            return food;
        }

        private void Remove(FoodItem food)
        {
            var last = items.Count - 1;
            var moved = items[last];
            items[food.ListIndex] = moved;
            moved.ListIndex = food.ListIndex;
            items.RemoveAt(last);

            cells[food.Cell].Remove(food);
            if (food.Kind == FoodKind.Plant)
            {
                plantCount--;
            }

            if (food.OriginTile != FoodItem.NoTile)
            {
                ownedCounts[food.OriginTile]--;
            }
        }

        private void RemoveAttractedAt(int index)
        {
            var last = attracted.Count - 1;
            attracted[index] = attracted[last];
            attracted.RemoveAt(last);
        }

        private void UpdateCell(FoodItem food)
        {
            var cell = GetTileIndex(food.Position);
            if (cell == food.Cell)
            {
                return;
            }

            cells[food.Cell].Remove(food);
            food.Cell = cell;
            GetOrCreateCell(cell).Add(food);
        }

        private List<FoodItem> GetOrCreateCell(int cell)
        {
            return cells[cell] ?? (cells[cell] = new List<FoodItem>());
        }

        private void GetCellRange(Vector2 min, Vector2 max, out int minColumn, out int minRow, out int maxColumn, out int maxRow)
        {
            minColumn = Clamp((int)Math.Floor((min.X + bounds.HalfWidth) / TileSize), 0, Columns - 1);
            maxColumn = Clamp((int)Math.Floor((max.X + bounds.HalfWidth) / TileSize), 0, Columns - 1);
            minRow = Clamp((int)Math.Floor((min.Y + bounds.HalfHeight) / TileSize), 0, Rows - 1);
            maxRow = Clamp((int)Math.Floor((max.Y + bounds.HalfHeight) / TileSize), 0, Rows - 1);
        }

        private float GetLifetime(FoodKind kind)
        {
            return kind == FoodKind.Meat && settings.meatLifetime > 0f ? settings.meatLifetime : float.PositiveInfinity;
        }

        private float GetMagnetRadius(Creature creature)
        {
            return creature.Radius + settings.magnetRange;
        }

        private Vector2 ClampToBounds(Vector2 position)
        {
            var half = new Vector2(bounds.HalfWidth, bounds.HalfHeight);
            return Vector2.Clamp(position, -half, half);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
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
