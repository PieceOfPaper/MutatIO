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
        private readonly FoodSettings settings;
        private readonly WorldBounds bounds;
        private readonly Random random;
        private readonly List<FoodItem> items = new List<FoodItem>();
        private readonly List<FoodItem> attracted = new List<FoodItem>();
        private readonly Dictionary<int, Creature> creaturesById = new Dictionary<int, Creature>();
        private readonly List<FoodItem>[] cells;
        private readonly int[] ownedCounts;
        private readonly float[] regenTimers;
        private readonly int[] occupiedStamps;
        private int occupiedStamp;
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

        /// <summary>먹이가 아직 필드에 남아 있는지입니다. 섭취·제거된 먹이는 false입니다.</summary>
        public bool Contains(FoodItem food)
        {
            return food != null && food.ListIndex < items.Count && items[food.ListIndex] == food;
        }

        /// <summary>타일 리젠과 무관한 먹이를 직접 생성합니다. 어느 타일의 상한에도 포함하지 않습니다.</summary>
        public FoodItem Spawn(FoodKind kind, Vector2 position)
        {
            return Add(new FoodItem(nextId++, kind, position, FoodItem.NoTile));
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
            ReleaseOrphans();
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
        /// 흡수 반경(몸 반경 + 고정 거리)에 들어온 비어 있는 먹이를 선점시킵니다.
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
            Add(new FoodItem(nextId++, FoodKind.Plant, position, tile));
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

        private float GetMagnetRadius(Creature creature)
        {
            return creature.Radius + settings.magnetRange;
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
