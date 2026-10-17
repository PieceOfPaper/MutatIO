using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Growth;
using MutatIO.Shared.Hunger;
using MutatIO.Shared.Movement;
using MutatIO.Shared.World;

namespace MutatIO.Server.Tests;

public sealed class WorldSimulationTests
{
    private const float Dt = 1f / 60f;

    [Fact]
    public void Growth_SizeIncreasesWithIntakeByArea()
    {
        var growth = new GrowthSettings { areaPerIntake = 0.05f };

        Assert.Equal(1f, GrowthSystem.GetSize(0f, growth), 5);
        Assert.Equal(MathF.Sqrt(2f), GrowthSystem.GetSize(20f, growth), 4);
        Assert.True(GrowthSystem.GetSize(100f, growth) > GrowthSystem.GetSize(50f, growth));
    }

    [Fact]
    public void Growth_RespectsMaxSize()
    {
        var growth = new GrowthSettings { areaPerIntake = 0.05f, maxSize = 2f };

        Assert.Equal(2f, GrowthSystem.GetSize(100000f, growth), 5);
    }

    [Fact]
    public void Bounds_KeepCircleInsideAndStopOutwardVelocity()
    {
        var bounds = new WorldBounds(10f, 10f);
        var state = new MovementState { Position = new Vector2(7f, -9f), Velocity = new Vector2(3f, -2f) };

        bounds.ClampCircle(ref state, 0.5f);

        Assert.Equal(new Vector2(4.5f, -4.5f), state.Position);
        Assert.Equal(Vector2.Zero, state.Velocity);
    }

    [Fact]
    public void Bounds_KeepInwardVelocity()
    {
        var bounds = new WorldBounds(10f, 10f);
        var state = new MovementState { Position = new Vector2(6f, 0f), Velocity = new Vector2(-3f, 1f) };

        bounds.ClampCircle(ref state, 0.5f);

        Assert.Equal(-3f, state.Velocity.X);
    }

    [Fact]
    public void Creature_StaysInsideMapWhileMovingOutward()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var creature = world.SpawnCreature(Vector2.Zero);

        for (var i = 0; i < 60 * 30; i++)
        {
            creature.Command = new PlayerCommand(Vector2.UnitX, false);
            world.Step(Dt);
        }

        Assert.Equal(world.Bounds.HalfWidth - creature.Radius, creature.Movement.Position.X, 4);
    }

    [Fact]
    public void Tiles_CoverMapWithFixedSize()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);

        Assert.Equal(8, world.Food.Columns);
        Assert.Equal(8, world.Food.Rows);
        Assert.Equal(0, world.Food.GetTileIndex(new Vector2(-20f, -20f)));
        Assert.Equal(63, world.Food.GetTileIndex(new Vector2(19.9f, 19.9f)));
        world.Food.GetTileBounds(9, out var min, out var max);
        Assert.Equal(new Vector2(-15f, -15f), min);
        Assert.Equal(new Vector2(-10f, -10f), max);
    }

    [Fact]
    public void Plants_FillEachTileUpToLimitInsideTile()
    {
        var world = CreateWorld(maxPlantsPerTile: 2);

        world.Food.FillPlants();

        Assert.Equal(world.Food.TileCount * 2, world.Food.PlantCount);
        foreach (var food in world.Food.Items)
        {
            world.Food.GetTileBounds(food.OriginTile, out var min, out var max);
            Assert.InRange(food.Position.X, min.X, max.X);
            Assert.InRange(food.Position.Y, min.Y, max.Y);
        }

        for (var tile = 0; tile < world.Food.TileCount; tile++)
        {
            Assert.Equal(2, world.Food.GetOwnedPlantCount(tile));
        }
    }

    [Fact]
    public void Plants_RegenerateInOriginTileAfterInterval()
    {
        var world = CreateWorld(maxPlantsPerTile: 1);
        world.Settings.food.plantRegenInterval = 1f;
        world.Food.FillPlants();
        var food = world.Food.Items[0];
        var tile = food.OriginTile;
        var eater = world.SpawnCreature(food.Position);
        world.Step(Dt);
        Assert.Equal(0, world.Food.GetOwnedPlantCount(tile));

        world.RemoveCreature(eater.Id);
        world.Step(0.5f);
        Assert.Equal(0, world.Food.GetOwnedPlantCount(tile));
        world.Step(0.6f);

        Assert.Equal(1, world.Food.GetOwnedPlantCount(tile));
        Assert.Equal(world.Food.TileCount, world.Food.PlantCount);
    }

    [Fact]
    public void Plants_DoNotRegenerateInTilesCoveredByBody()
    {
        var world = CreateWorld(maxPlantsPerTile: 1);
        world.Settings.food.plantRegenInterval = 0.1f;
        world.Settings.hunger.decayPerSecond = 0f;
        // 반경 3인 몸 중심을 타일 모서리 근처에 두어 중심 소속 타일 외의 이웃 타일 세 곳도 덮게 합니다.
        var creature = world.SpawnCreature(new Vector2(0.1f, 0.1f));
        creature.AddIntake(700, world.Settings.growth);
        Assert.Equal(3f, creature.Radius, 3);
        var covered = new[]
        {
            world.Food.GetTileIndex(new Vector2(2f, 2f)),
            world.Food.GetTileIndex(new Vector2(-2f, 2f)),
            world.Food.GetTileIndex(new Vector2(2f, -2f)),
            world.Food.GetTileIndex(new Vector2(-2f, -2f)),
        };
        var far = world.Food.GetTileIndex(new Vector2(15f, 15f));

        for (var i = 0; i < 10; i++)
        {
            world.Step(0.2f);
        }

        foreach (var tile in covered)
        {
            Assert.Equal(0, world.Food.GetOwnedPlantCount(tile));
        }

        Assert.Equal(1, world.Food.GetOwnedPlantCount(far));

        world.RemoveCreature(creature.Id);
        world.Step(Dt);
        foreach (var tile in covered)
        {
            Assert.Equal(1, world.Food.GetOwnedPlantCount(tile));
        }
    }

    [Fact]
    public void Plants_MovedOutOfTileStillCountForOriginTile()
    {
        var world = CreateWorld(maxPlantsPerTile: 1);
        world.Settings.food.plantRegenInterval = 0.1f;
        world.Food.FillPlants();
        var tileA = world.Food.GetTileIndex(new Vector2(-12f, -12f));
        world.Food.GetTileBounds(tileA, out var minA, out var maxA);
        var food = FindByOrigin(world, tileA);
        // A 타일 오른쪽 이웃 타일 깊숙이 서서 먹이를 끌어온 뒤, 먹기 전에 사라집니다.
        var creature = world.SpawnCreature(new Vector2(maxA.X + 3f, food.Position.Y));
        world.Settings.food.magnetRange = 10f;
        while (food.Position.X <= maxA.X + 0.5f)
        {
            world.Step(Dt);
        }

        world.RemoveCreature(creature.Id);
        world.Step(Dt);
        var tileB = world.Food.GetTileIndex(food.Position);
        Assert.NotEqual(tileA, tileB);
        Assert.Equal(FoodItem.NoAttractor, food.AttractorId);
        Assert.Equal(tileA, food.OriginTile);

        for (var i = 0; i < 20; i++)
        {
            world.Step(0.2f);
        }

        // A는 밀려난 먹이를 잔존으로 세어 새로 만들지 않고, B는 자신이 만든 먹이만 셉니다.
        Assert.Equal(1, world.Food.GetOwnedPlantCount(tileA));
        Assert.Equal(1, world.Food.GetOwnedPlantCount(tileB));
        Assert.Single(world.Food.Items, item => item.OriginTile == tileA);
    }

    [Fact]
    public void Magnet_PullsNearbyFoodAndRewardsOnce()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var consumedCount = 0;
        world.FoodConsumed += (_, eater) =>
        {
            consumedCount++;
            Assert.Same(creature, eater);
        };
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(1.5f, 0f));

        world.Step(Dt);
        Assert.Equal(creature.Id, food.AttractorId);
        Assert.True(food.Position.X < 1.5f);

        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(1, consumedCount);
        Assert.Equal(1, creature.Intake);
        Assert.Empty(world.Food.Items);
        Assert.True(creature.Size > 1f);
    }

    [Fact]
    public void Magnet_IgnoresFoodOutsideRange()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(5f, 0f));

        world.Step(Dt);

        Assert.Equal(FoodItem.NoAttractor, food.AttractorId);
        Assert.Equal(new Vector2(5f, 0f), food.Position);
    }

    [Fact]
    public void Magnet_FirstCreatureInRangeKeepsFoodWhenAnotherComesCloser()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.Settings.food.magnetPullSpeed = 0.5f;
        var first = world.SpawnCreature(new Vector2(-1.9f, 0f));
        var food = world.Food.Spawn(FoodKind.Plant, Vector2.Zero);
        world.Step(Dt);
        Assert.Equal(first.Id, food.AttractorId);

        var second = world.SpawnCreature(new Vector2(0.3f, 0f));
        for (var i = 0; i < 300; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(1, first.Intake);
        Assert.Equal(0, second.Intake);
    }

    [Fact]
    public void Magnet_SameStepGoesToFirstProcessedCreature()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var a = world.SpawnCreature(new Vector2(-1.5f, 0f));
        var b = world.SpawnCreature(new Vector2(1f, 0f));
        var food = world.Food.Spawn(FoodKind.Plant, Vector2.Zero);

        world.Step(Dt);

        Assert.Equal(a.Id, food.AttractorId);
        Assert.NotEqual(b.Id, food.AttractorId);
    }

    [Fact]
    public void Magnet_KeepsTargetWhenAttractorMovesAway()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.Settings.food.magnetPullSpeed = 1f;
        var creature = world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(-1.9f, 0f));
        world.Step(Dt);

        // 흡수 반경보다 훨씬 멀리 달아나도 사망·이탈 전에는 선점을 풀지 않습니다.
        for (var i = 0; i < 120; i++)
        {
            creature.Command = new PlayerCommand(Vector2.UnitX, false);
            world.Step(Dt);
        }

        Assert.True(Vector2.Distance(food.Position, creature.Movement.Position) > creature.Radius + world.Settings.food.magnetRange);
        Assert.Equal(creature.Id, food.AttractorId);
    }

    [Fact]
    public void Magnet_ReleasesFoodWhenAttractorIsRemoved()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(2f, 0f));
        world.Step(Dt);
        Assert.Equal(creature.Id, food.AttractorId);

        world.RemoveCreature(creature.Id);
        world.Step(Dt);

        Assert.Equal(FoodItem.NoAttractor, food.AttractorId);
        Assert.Single(world.Food.Items);
    }

    [Fact]
    public void Growth_SlowsMovement()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var smallSpeed = MovementSystem.GetMaxSpeed(creature.Size, world.Settings.movement);

        creature.AddIntake(200, world.Settings.growth);

        Assert.True(MovementSystem.GetMaxSpeed(creature.Size, world.Settings.movement) < smallSpeed);
    }

    [Fact]
    public void Dash_AppliesOnlyOnceForRepeatedSteps()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        creature.Command = new PlayerCommand(Vector2.UnitX, true);

        world.Step(Dt);

        Assert.True(creature.Movement.IsDashing);
        Assert.False(creature.Command.Dash);
    }

    [Fact]
    public void Hunger_StartsAtStartSatietyAndDrainsOverTime()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.Settings.hunger.startSatiety = 50f;
        world.Settings.hunger.decayPerSecond = 10f;
        var creature = world.SpawnCreature(Vector2.Zero);
        Assert.Equal(50f, creature.Satiety);

        world.Step(1f);

        Assert.Equal(40f, creature.Satiety, 4);
    }

    [Fact]
    public void Hunger_EatingRestoresSatietyUpToMax()
    {
        var hunger = new HungerSettings { maxSatiety = 100f, plantSatiety = 30f };

        Assert.Equal(80f, HungerSystem.Restore(50f, HungerSystem.GetSatietyGain(FoodKind.Plant, 1f, hunger), hunger), 4);
        Assert.Equal(100f, HungerSystem.Restore(90f, HungerSystem.GetSatietyGain(FoodKind.Plant, 1f, hunger), hunger), 4);
    }

    [Fact]
    public void Hunger_BiggerBodyRecoversLessPerFoodDownToFloor()
    {
        var hunger = new HungerSettings { plantSatiety = 8f, sizeSatietyExponent = 1f, minSatietyMultiplier = 0.25f };

        Assert.Equal(8f, HungerSystem.GetSatietyGain(FoodKind.Plant, 1f, hunger), 4);
        Assert.Equal(4f, HungerSystem.GetSatietyGain(FoodKind.Plant, 2f, hunger), 4);
        Assert.Equal(2f, HungerSystem.GetSatietyGain(FoodKind.Plant, 100f, hunger), 4);
    }

    [Fact]
    public void Hunger_DecayIsSameForAnySize()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.Settings.hunger.decayPerSecond = 10f;
        var small = world.SpawnCreature(new Vector2(-10f, 0f));
        var big = world.SpawnCreature(new Vector2(10f, 0f));
        big.AddIntake(500, world.Settings.growth);

        world.Step(1f);

        Assert.Equal(small.Satiety, big.Satiety, 4);
    }

    [Fact]
    public void Hunger_EatingInWorldRestoresSatiety()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.Settings.hunger.decayPerSecond = 0f;
        world.Settings.hunger.plantSatiety = 5f;
        var creature = world.SpawnCreature(Vector2.Zero);
        creature.SetSatiety(10f);
        world.Food.Spawn(FoodKind.Plant, new Vector2(1f, 0f));

        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(15f, creature.Satiety, 4);
    }

    [Fact]
    public void Hunger_StarvationKillsWithoutReducingScoreOrSize()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        world.Settings.hunger.decayPerSecond = 10f;
        var creature = world.SpawnCreature(Vector2.Zero);
        creature.AddIntake(40, world.Settings.growth);
        var sizeBefore = creature.Size;
        creature.SetSatiety(5f);
        var deaths = new List<CreatureDeath>();
        world.CreatureDied += deaths.Add;

        world.Step(0.4f);
        Assert.True(creature.IsAlive);
        Assert.Equal(40, creature.Intake);
        Assert.Equal(sizeBefore, creature.Size);

        world.Step(0.2f);

        var death = Assert.Single(deaths);
        Assert.Same(creature, death.Creature);
        Assert.Equal(DeathCause.Starvation, death.Cause);
        Assert.False(creature.IsAlive);
        Assert.Empty(world.Creatures);
        Assert.Equal(40, creature.Intake);
        Assert.Equal(sizeBefore, creature.Size);
    }

    [Fact]
    public void Hunger_DeathReleasesAttractedFoodAtCurrentPosition()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(2f, 0f));
        world.Step(Dt);
        Assert.Equal(creature.Id, food.AttractorId);

        creature.SetSatiety(0f);
        world.Step(Dt);
        var stoppedAt = food.Position;
        world.Step(Dt);

        Assert.False(creature.IsAlive);
        Assert.Equal(FoodItem.NoAttractor, food.AttractorId);
        Assert.Equal(stoppedAt, food.Position);
        Assert.Single(world.Food.Items);
    }

    /// <summary>테스트용 40×40 맵(5×5 타일 8×8개)입니다.</summary>
    private static WorldSimulation CreateWorld(int maxPlantsPerTile)
    {
        var settings = new WorldSettings { width = 40f, height = 40f };
        settings.food.tileSize = 5f;
        settings.food.maxPlantsPerTile = maxPlantsPerTile;
        return new WorldSimulation(settings, seed: 1234);
    }

    private static FoodItem FindByOrigin(WorldSimulation world, int tile)
    {
        foreach (var item in world.Food.Items)
        {
            if (item.OriginTile == tile)
            {
                return item;
            }
        }

        throw new InvalidOperationException($"No food from tile {tile}.");
    }
}
