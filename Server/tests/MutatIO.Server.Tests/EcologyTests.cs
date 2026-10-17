using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Hunger;
using MutatIO.Shared.Mutation;
using MutatIO.Shared.World;

namespace MutatIO.Server.Tests;

public sealed class EcologyTests
{
    private const float Dt = 1f / 60f;

    [Fact]
    public void HerbivoreDeath_DropsMeatProportionalToScore()
    {
        var world = CreateWorld();
        var herbivore = world.SpawnCreature(new Vector2(3f, 2f));
        herbivore.AddIntake(10, world.Settings.growth);
        herbivore.SetSatiety(0.001f);

        world.Step(Dt);

        Assert.Equal(WorldSimulation.GetMeatDropCount(10, world.Settings.food), world.Food.Items.Count);
        Assert.Equal(5, world.Food.Items.Count);
        var spread = herbivore.Radius + world.Settings.food.meatDropSpread;
        Assert.All(world.Food.Items, food =>
        {
            Assert.Equal(FoodKind.Meat, food.Kind);
            Assert.Equal(FoodItem.NoTile, food.OriginTile);
            Assert.True(Vector2.Distance(food.Position, new Vector2(3f, 2f)) <= spread + 0.001f);
        });
    }

    [Fact]
    public void CarnivoreDeath_DropsNoMeat()
    {
        var world = CreateWorld();
        var carnivore = world.SpawnCreature(Vector2.Zero);
        world.ChangeRole(carnivore, CreatureRole.Carnivore);
        carnivore.AddIntake(10, world.Settings.growth);
        carnivore.SetSatiety(0.001f);

        world.Step(Dt);

        Assert.Empty(world.Food.Items);
    }

    [Fact]
    public void LargerCarnivore_KillsTouchedHerbivore()
    {
        var world = CreateWorld();
        var hunter = SpawnCarnivore(world, Vector2.Zero, 10);
        var prey = world.SpawnCreature(new Vector2(1f, 0f));
        prey.AddIntake(4, world.Settings.growth);
        var deaths = new List<CreatureDeath>();
        world.CreatureDied += deaths.Add;

        world.Step(Dt);

        var death = Assert.Single(deaths);
        Assert.Same(prey, death.Creature);
        Assert.Equal(DeathCause.Killed, death.Cause);
        Assert.Same(hunter, death.Killer);
        Assert.False(prey.IsAlive);
        Assert.Equal(2, world.Food.Items.Count(food => food.Kind == FoodKind.Meat));
    }

    [Fact]
    public void LargerCarnivore_KillsSmallerCarnivore()
    {
        var world = CreateWorld();
        SpawnCarnivore(world, Vector2.Zero, 10);
        var small = SpawnCarnivore(world, new Vector2(1f, 0f), 9);

        world.Step(Dt);

        Assert.False(small.IsAlive);
        Assert.Empty(world.Food.Items);
    }

    [Theory]
    [InlineData(CreatureRole.Carnivore, 10, CreatureRole.Carnivore, 10)]
    [InlineData(CreatureRole.Carnivore, 5, CreatureRole.Herbivore, 20)]
    [InlineData(CreatureRole.Herbivore, 30, CreatureRole.Herbivore, 2)]
    public void NonKillingContacts_BothSurvive(CreatureRole roleA, int scoreA, CreatureRole roleB, int scoreB)
    {
        var world = CreateWorld();
        var a = world.SpawnCreature(Vector2.Zero);
        world.ChangeRole(a, roleA);
        a.AddIntake(scoreA, world.Settings.growth);
        var b = world.SpawnCreature(new Vector2(0.5f, 0f));
        world.ChangeRole(b, roleB);
        b.AddIntake(scoreB, world.Settings.growth);

        world.Step(Dt);

        Assert.True(a.IsAlive);
        Assert.True(b.IsAlive);
    }

    [Fact]
    public void EatingIsAppliedBeforeContact()
    {
        var world = CreateWorld();
        SpawnCarnivore(world, Vector2.Zero, 10);
        var herbivore = world.SpawnCreature(new Vector2(1f, 0f));
        herbivore.AddIntake(9, world.Settings.growth);
        // 이 스텝에 먹어 점수가 10이 되면 동점이므로 처치되지 않습니다.
        world.Food.Spawn(FoodKind.Plant, new Vector2(1f, 0f));

        world.Step(Dt);

        Assert.True(herbivore.IsAlive);
        Assert.Equal(10, herbivore.Intake);
    }

    [Fact]
    public void Satiety_MeatDependsOnRole()
    {
        var hunger = new HungerSettings { meatSatiety = 8f, herbivoreMeatEfficiency = 0.5f, plantSatiety = 4f };

        Assert.Equal(8f, HungerSystem.GetSatietyGain(FoodKind.Meat, CreatureRole.Carnivore, 1f, hunger), 4);
        Assert.Equal(4f, HungerSystem.GetSatietyGain(FoodKind.Meat, CreatureRole.Herbivore, 1f, hunger), 4);
        Assert.Equal(0f, HungerSystem.GetSatietyGain(FoodKind.Plant, CreatureRole.Carnivore, 1f, hunger), 4);
    }

    [Fact]
    public void Mutation_ThresholdGrowsWithSize()
    {
        var mutation = new MutationSettings { baseThreshold = 5f, sizeThresholdExponent = 1f };

        Assert.Equal(5f, MutationSystem.GetThreshold(1f, mutation), 4);
        Assert.Equal(10f, MutationSystem.GetThreshold(2f, mutation), 4);
    }

    [Fact]
    public void HerbivoreEatingMeat_MutatesAtThreshold()
    {
        var world = CreateWorld();
        world.Settings.mutation.baseThreshold = 3f;
        world.Settings.mutation.sizeThresholdExponent = 0f;
        world.Settings.mutation.decayPerSecond = 0f;
        var herbivore = world.SpawnCreature(Vector2.Zero);
        var mutated = new List<Creature>();
        world.CreatureMutated += mutated.Add;

        for (var i = 0; i < 2; i++)
        {
            world.Food.Spawn(FoodKind.Meat, Vector2.Zero);
            world.Step(Dt);
        }

        Assert.Equal(CreatureRole.Herbivore, herbivore.Role);
        Assert.Equal(2f, herbivore.Mutation, 4);

        world.Food.Spawn(FoodKind.Meat, Vector2.Zero);
        world.Step(Dt);

        Assert.Equal(CreatureRole.Carnivore, herbivore.Role);
        Assert.Equal(0f, herbivore.Mutation);
        Assert.Same(herbivore, Assert.Single(mutated));
        Assert.Equal(3, herbivore.Intake);
    }

    [Fact]
    public void Mutation_DecaysToZeroAndStops()
    {
        var world = CreateWorld();
        world.Settings.mutation.decayPerSecond = 1f;
        var herbivore = world.SpawnCreature(Vector2.Zero);
        herbivore.SetMutation(0.5f);

        for (var i = 0; i < 60; i++)
        {
            herbivore.SetSatiety(100f);
            world.Step(Dt);
        }

        Assert.Equal(0f, herbivore.Mutation);
    }

    [Fact]
    public void ChangeRole_ReleasesFoodBeingAttracted()
    {
        var world = CreateWorld();
        var herbivore = world.SpawnCreature(Vector2.Zero);
        var plant = world.Food.Spawn(FoodKind.Plant, new Vector2(1.8f, 0f));
        world.Step(Dt);
        Assert.Equal(herbivore.Id, plant.AttractorId);

        world.ChangeRole(herbivore, CreatureRole.Carnivore);

        Assert.Equal(FoodItem.NoAttractor, plant.AttractorId);
    }

    [Fact]
    public void Carnivore_DoesNotAttractPlantsAndPushesThemOut()
    {
        var world = CreateWorld(maxPlantsPerTile: 1);
        world.Food.FillPlants();
        var plant = world.Food.Items[0];
        var origin = plant.OriginTile;
        var carnivore = SpawnCarnivore(world, plant.Position + new Vector2(0.1f, 0f), 20);
        var plantsBefore = world.Food.PlantCount;

        world.Step(Dt);

        Assert.Equal(plantsBefore, world.Food.PlantCount);
        Assert.Equal(20, carnivore.Intake);
        Assert.Equal(FoodItem.NoAttractor, plant.AttractorId);
        Assert.Equal(origin, plant.OriginTile);
        Assert.True(Vector2.Distance(plant.Position, carnivore.Movement.Position) >= carnivore.Radius);
    }

    [Fact]
    public void Carnivore_AttractsAndEatsMeat()
    {
        var world = CreateWorld();
        var carnivore = SpawnCarnivore(world, Vector2.Zero, 0);
        carnivore.SetSatiety(50f);
        world.Food.Spawn(FoodKind.Meat, new Vector2(1.5f, 0f));

        for (var i = 0; i < 30; i++)
        {
            world.Step(Dt);
        }

        Assert.Equal(1, carnivore.Intake);
        Assert.True(carnivore.Satiety > 50f);
    }

    [Fact]
    public void Herbivores_PushEachOtherApart()
    {
        var world = CreateWorld();
        var a = world.SpawnCreature(Vector2.Zero);
        var b = world.SpawnCreature(new Vector2(0.2f, 0f));

        for (var i = 0; i < 60; i++)
        {
            world.Step(Dt);
        }

        Assert.True(Vector2.Distance(a.Movement.Position, b.Movement.Position) >= a.Radius + b.Radius - 0.01f);
    }

    [Fact]
    public void NonKillingCarnivoreContact_PassesThrough()
    {
        var world = CreateWorld();
        var carnivore = SpawnCarnivore(world, Vector2.Zero, 1);
        var herbivore = world.SpawnCreature(new Vector2(0.2f, 0f));
        herbivore.AddIntake(5, world.Settings.growth);

        world.Step(Dt);

        Assert.Equal(Vector2.Zero, carnivore.Movement.Position);
        Assert.Equal(new Vector2(0.2f, 0f), herbivore.Movement.Position);
    }

    private static Creature SpawnCarnivore(WorldSimulation world, Vector2 position, int score)
    {
        var creature = world.SpawnCreature(position);
        world.ChangeRole(creature, CreatureRole.Carnivore);
        creature.AddIntake(score, world.Settings.growth);
        return creature;
    }

    private static WorldSimulation CreateWorld(int maxPlantsPerTile = 0)
    {
        var settings = new WorldSettings { width = 40f, height = 40f };
        settings.food.tileSize = 5f;
        settings.food.maxPlantsPerTile = maxPlantsPerTile;
        return new WorldSimulation(settings, seed: 4321);
    }
}
