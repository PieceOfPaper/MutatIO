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
        var world = CreateWorld(maxPlants: 0);
        var creature = world.SpawnCreature(Vector2.Zero);

        for (var i = 0; i < 60 * 30; i++)
        {
            creature.Command = new PlayerCommand(Vector2.UnitX, false);
            world.Step(Dt);
        }

        Assert.Equal(world.Bounds.HalfWidth - creature.Radius, creature.Movement.Position.X, 4);
    }

    [Fact]
    public void Plants_FillToMaxWithMinimumSpacing()
    {
        var world = CreateWorld(maxPlants: 100);

        world.Food.FillPlants();

        Assert.Equal(100, world.Food.PlantCount);
        var items = world.Food.Items;
        var spacing = world.Settings.food.plantSpacing;
        for (var i = 0; i < items.Count; i++)
        {
            Assert.True(world.Bounds.Contains(items[i].Position));
            for (var j = i + 1; j < items.Count; j++)
            {
                Assert.True(Vector2.Distance(items[i].Position, items[j].Position) >= spacing);
            }
        }
    }

    [Fact]
    public void Plants_RegenerateAfterBeingEaten()
    {
        var world = CreateWorld(maxPlants: 10);
        world.Food.FillPlants();
        var eater = world.SpawnCreature(world.Food.Items[0].Position);
        world.Step(Dt);
        Assert.True(world.Food.PlantCount < 10);

        world.RemoveCreature(eater.Id);
        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(10, world.Food.PlantCount);
    }

    [Fact]
    public void Magnet_PullsNearbyFoodAndRewardsOnce()
    {
        var world = CreateWorld(maxPlants: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var consumedCount = 0;
        world.FoodConsumed += (_, eater) =>
        {
            consumedCount++;
            Assert.Same(creature, eater);
        };
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(1.5f, 0f), 1f);

        world.Step(Dt);
        Assert.Equal(creature.Id, food.AttractorId);
        Assert.True(food.Position.X < 1.5f);

        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(1, consumedCount);
        Assert.Equal(1f, creature.Intake);
        Assert.Empty(world.Food.Items);
        Assert.True(creature.Size > 1f);
    }

    [Fact]
    public void Magnet_IgnoresFoodOutsideRange()
    {
        var world = CreateWorld(maxPlants: 0);
        world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(5f, 0f), 1f);

        world.Step(Dt);

        Assert.Equal(FoodItem.NoAttractor, food.AttractorId);
        Assert.Equal(new Vector2(5f, 0f), food.Position);
    }

    [Fact]
    public void Magnet_NearestCreatureGetsContestedFood()
    {
        var world = CreateWorld(maxPlants: 0);
        var near = world.SpawnCreature(new Vector2(-1f, 0f));
        var far = world.SpawnCreature(new Vector2(1.8f, 0f));
        world.Food.Spawn(FoodKind.Plant, new Vector2(0f, 0f), 1f);

        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(1f, near.Intake);
        Assert.Equal(0f, far.Intake);
    }

    [Fact]
    public void Magnet_ReleasesFoodWhenAttractorIsRemoved()
    {
        var world = CreateWorld(maxPlants: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(2f, 0f), 1f);
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
        var world = CreateWorld(maxPlants: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var smallSpeed = MovementSystem.GetMaxSpeed(creature.Size, world.Settings.movement);

        creature.AddIntake(200f, world.Settings.growth);

        Assert.True(MovementSystem.GetMaxSpeed(creature.Size, world.Settings.movement) < smallSpeed);
    }

    [Fact]
    public void Dash_AppliesOnlyOnceForRepeatedSteps()
    {
        var world = CreateWorld(maxPlants: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        creature.Command = new PlayerCommand(Vector2.UnitX, true);

        world.Step(Dt);

        Assert.True(creature.Movement.IsDashing);
        Assert.False(creature.Command.Dash);
    }

    [Fact]
    public void Hunger_StartsAtStartSatietyAndDrainsOverTime()
    {
        var world = CreateWorld(maxPlants: 0);
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

        Assert.Equal(80f, HungerSystem.Restore(50f, HungerSystem.GetSatietyGain(FoodKind.Plant, hunger), hunger), 4);
        Assert.Equal(100f, HungerSystem.Restore(90f, HungerSystem.GetSatietyGain(FoodKind.Plant, hunger), hunger), 4);
    }

    [Fact]
    public void Hunger_EatingInWorldRestoresSatiety()
    {
        var world = CreateWorld(maxPlants: 0);
        world.Settings.hunger.decayPerSecond = 0f;
        world.Settings.hunger.plantSatiety = 5f;
        var creature = world.SpawnCreature(Vector2.Zero);
        creature.SetSatiety(10f);
        world.Food.Spawn(FoodKind.Plant, new Vector2(1f, 0f), 1f);

        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(15f, creature.Satiety, 4);
    }

    [Fact]
    public void Hunger_StarvationKillsWithoutReducingScoreOrSize()
    {
        var world = CreateWorld(maxPlants: 0);
        world.Settings.hunger.decayPerSecond = 10f;
        var creature = world.SpawnCreature(Vector2.Zero);
        creature.AddIntake(40f, world.Settings.growth);
        var sizeBefore = creature.Size;
        creature.SetSatiety(5f);
        var deaths = new List<CreatureDeath>();
        world.CreatureDied += deaths.Add;

        world.Step(0.4f);
        Assert.True(creature.IsAlive);
        Assert.Equal(40f, creature.Intake);
        Assert.Equal(sizeBefore, creature.Size);

        world.Step(0.2f);

        var death = Assert.Single(deaths);
        Assert.Same(creature, death.Creature);
        Assert.Equal(DeathCause.Starvation, death.Cause);
        Assert.False(creature.IsAlive);
        Assert.Empty(world.Creatures);
        Assert.Equal(40f, creature.Intake);
        Assert.Equal(sizeBefore, creature.Size);
    }

    [Fact]
    public void Hunger_DeathReleasesAttractedFoodAtCurrentPosition()
    {
        var world = CreateWorld(maxPlants: 0);
        var creature = world.SpawnCreature(Vector2.Zero);
        var food = world.Food.Spawn(FoodKind.Plant, new Vector2(2f, 0f), 1f);
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

    private static WorldSimulation CreateWorld(int maxPlants)
    {
        var settings = new WorldSettings();
        settings.food.maxPlantCount = maxPlants;
        return new WorldSimulation(settings, seed: 1234);
    }
}
