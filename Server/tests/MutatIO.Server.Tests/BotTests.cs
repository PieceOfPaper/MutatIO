using System.Numerics;
using MutatIO.Shared.Bots;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.World;

namespace MutatIO.Server.Tests;

public sealed class BotTests
{
    private const float Dt = 1f / 30f;

    [Fact]
    public void Director_FillsMinimumPopulationAroundConnectedHumans()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        using var director = new BotDirector(world, CreateBotSettings(), seed: 1) { ConnectedHumans = 1 };

        director.FillImmediately();

        Assert.Equal(4, director.Bots.Count);
        Assert.Equal(4, world.Creatures.Count);
        Assert.Equal(4, director.Bots.Select(bot => bot.Name).Distinct().Count());
        Assert.All(director.Bots, bot => Assert.Equal(CreatureRole.Herbivore, bot.Creature.Role));
    }

    [Fact]
    public void Director_KeepsMeatSeekerCount()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var settings = CreateBotSettings();
        settings.meatSeekerCount = 2;
        settings.joinDelayMin = 0f;
        settings.joinDelayMax = 0f;
        using var director = new BotDirector(world, settings, seed: 5) { ConnectedHumans = 1 };
        director.FillImmediately();
        Assert.Equal(2, director.Bots.Count(bot => bot.SeeksMeat));

        director.Bots.First(bot => bot.SeeksMeat).Creature.SetSatiety(0.001f);
        Step(world, director, Dt * 3f);

        Assert.Equal(4, director.Bots.Count);
        Assert.Equal(2, director.Bots.Count(bot => bot.SeeksMeat));
    }

    [Fact]
    public void Director_ReplacesDeadBotAfterRandomDelay()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var settings = CreateBotSettings();
        settings.joinDelayMin = 2f;
        settings.joinDelayMax = 4f;
        using var director = new BotDirector(world, settings, seed: 2) { ConnectedHumans = 1 };
        director.FillImmediately();
        var left = 0;
        var joined = 0;
        director.BotLeft += _ => left++;
        director.BotJoined += _ => joined++;

        director.Bots[0].Creature.SetSatiety(0.001f);
        Step(world, director, Dt * 2f);
        Assert.Equal(1, left);
        Assert.Equal(3, director.Bots.Count);

        Step(world, director, 1.9f);
        Assert.Equal(0, joined);
        Assert.Equal(1, director.PendingJoinCount);

        Step(world, director, 2.2f);
        Assert.Equal(1, joined);
        Assert.Equal(4, director.Bots.Count);
    }

    [Fact]
    public void Director_CancelsPendingJoinWhenHumansFillTheWorld()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        using var director = new BotDirector(world, CreateBotSettings(), seed: 3) { ConnectedHumans = 1 };
        director.FillImmediately();
        director.Bots[0].Creature.SetSatiety(0.001f);
        Step(world, director, Dt * 2f);
        Assert.Equal(1, director.PendingJoinCount);

        director.ConnectedHumans = 5;
        Step(world, director, 20f);

        Assert.Equal(0, director.PendingJoinCount);
        // 이미 있는 봇은 내보내지 않습니다.
        Assert.Equal(3, director.Bots.Count);
    }

    [Fact]
    public void Director_SkillLevelAppliesToExistingBots()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        using var director = new BotDirector(world, CreateBotSettings(), seed: 4);
        director.FillImmediately();

        director.SkillLevel = 0f;

        Assert.All(director.Bots, bot => Assert.Equal(0f, bot.Brain.Skill.Level));
        director.SkillLevel = 2f;
        Assert.Equal(1f, director.SkillLevel);
    }

    [Fact]
    public void Brain_HerbivoreFleesFromLargerCarnivore()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        var hunter = world.SpawnCreature(new Vector2(4f, 0f));
        hunter.SetRole(CreatureRole.Carnivore);
        hunter.AddIntake(10, world.Settings.growth);
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Flee, brain.Intent);
        Assert.True(command.Move.X < -0.9f);
    }

    [Fact]
    public void Brain_NoticedThreatIsRememberedWhileInRange()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        var hunter = world.SpawnCreature(new Vector2(4f, 0f));
        hunter.SetRole(CreatureRole.Carnivore);
        hunter.AddIntake(10, world.Settings.growth);
        var settings = CreateBotSettings();
        settings.high.threatAwareness = 0.3f;
        var brain = new BotBrain(bot, BotSkill.FromLevel(1f, settings), new Random(11));

        var noticedAt = -1;
        for (var i = 0; i < 200 && noticedAt < 0; i++)
        {
            brain.Think(world, 1f);
            noticedAt = brain.Intent == BotIntent.Flee ? i : -1;
        }

        Assert.True(noticedAt >= 0);
        for (var i = 0; i < 20; i++)
        {
            brain.Think(world, 1f);
            Assert.Equal(BotIntent.Flee, brain.Intent);
        }
    }

    [Fact]
    public void Brain_LowestLevelDoesNotNoticeThreats()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        var hunter = world.SpawnCreature(new Vector2(2f, 0f));
        hunter.SetRole(CreatureRole.Carnivore);
        hunter.AddIntake(10, world.Settings.growth);
        var brain = new BotBrain(bot, BotSkill.FromLevel(0f, CreateBotSettings()), new Random(12));

        for (var i = 0; i < 20; i++)
        {
            brain.Think(world, 2f);
            Assert.NotEqual(BotIntent.Flee, brain.Intent);
        }
    }

    [Fact]
    public void Brain_HerbivoreIgnoresSmallerCarnivoreAndOtherHerbivores()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        bot.AddIntake(10, world.Settings.growth);
        var smallCarnivore = world.SpawnCreature(new Vector2(3f, 0f));
        smallCarnivore.SetRole(CreatureRole.Carnivore);
        var bigHerbivore = world.SpawnCreature(new Vector2(-3f, 0f));
        bigHerbivore.AddIntake(50, world.Settings.growth);
        var brain = CreateBrain(world, bot);

        brain.Think(world, Dt);

        Assert.NotEqual(BotIntent.Flee, brain.Intent);
    }

    [Fact]
    public void Brain_CarnivoreChasesSmallerPrey()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        bot.SetRole(CreatureRole.Carnivore);
        bot.AddIntake(10, world.Settings.growth);
        world.SpawnCreature(new Vector2(0f, 6f));
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Chase, brain.Intent);
        Assert.True(command.Move.Y > 0.9f);
    }

    [Fact]
    public void Brain_CarnivoreHuntsBeyondFoodPerception()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        bot.SetRole(CreatureRole.Carnivore);
        bot.AddIntake(10, world.Settings.growth);
        var skill = BotSkill.FromLevel(1f, CreateBotSettings());
        var prey = world.SpawnCreature(new Vector2(0f, bot.Radius + skill.PerceptionRange + 5f));
        // 먹이 인지 거리 밖, 사냥 거리 안입니다.
        Assert.True(skill.PerceptionRange + 5f < skill.HuntRange);
        var meat = world.Food.Spawn(FoodKind.Meat, new Vector2(4f, 0f));
        var brain = new BotBrain(bot, skill, new Random(7));

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Chase, brain.Intent);
        Assert.True(command.Move.Y > 0.9f, $"meat at {meat.Position}, move {command.Move}");
    }

    [Fact]
    public void Brain_CarnivoreKeepsChasingUntilPreyLeavesHuntRange()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        bot.SetRole(CreatureRole.Carnivore);
        bot.AddIntake(10, world.Settings.growth);
        var prey = world.SpawnCreature(new Vector2(0f, 8f));
        var brain = CreateBrain(world, bot);
        brain.Think(world, Dt);
        Assert.Equal(BotIntent.Chase, brain.Intent);

        // 집중 확률 0이어도 쫓던 사냥감은 놓지 않습니다.
        var settings = CreateBotSettings();
        settings.high.focus = 0f;
        brain.Skill = BotSkill.FromLevel(1f, settings);
        for (var i = 0; i < 20; i++)
        {
            brain.Think(world, 1f);
            Assert.Equal(BotIntent.Chase, brain.Intent);
        }

        prey.Movement.Position = new Vector2(0f, brain.Skill.HuntRange + bot.Radius + prey.Radius + 1f);
        brain.Think(world, 1f);
        Assert.NotEqual(BotIntent.Chase, brain.Intent);
    }

    [Fact]
    public void Brain_MeatSeekerPrefersFartherMeatOverNearPlant()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        var skill = BotSkill.FromLevel(1f, CreateBotSettings());
        world.Food.Spawn(FoodKind.Plant, new Vector2(0f, -4f));
        world.Food.Spawn(FoodKind.Meat, new Vector2(skill.PerceptionRange + 4f, 0f));
        var seeker = new BotBrain(bot, skill, new Random(7), seeksMeat: true);
        var normal = new BotBrain(bot, skill, new Random(7));

        var seekerMove = seeker.Think(world, Dt).Move;
        var normalMove = normal.Think(world, Dt).Move;

        Assert.Equal(BotIntent.Forage, seeker.Intent);
        Assert.True(seekerMove.X > 0.9f, $"seeker move {seekerMove}");
        Assert.True(normalMove.Y < -0.9f, $"normal move {normalMove}");
    }

    [Fact]
    public void Brain_CarnivoreStalksStarvingHerbivoreItCannotKill()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = SpawnBotCarnivore(world, Vector2.Zero, 5);
        var big = world.SpawnCreature(new Vector2(0f, 10f));
        big.AddIntake(50, world.Settings.growth);
        big.SetSatiety(world.Settings.hunger.maxSatiety * 0.2f);
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Stalk, brain.Intent);
        Assert.True(command.Move.Y > 0.9f, $"move {command.Move}");

        // 가까이 붙으면 멈춰 기다립니다.
        bot.Movement.Position = new Vector2(0f, 10f - big.Radius - bot.Radius - 1f);
        command = brain.Think(world, Dt);
        Assert.Equal(BotIntent.Stalk, brain.Intent);
        Assert.Equal(Vector2.Zero, command.Move);
    }

    [Fact]
    public void Brain_CarnivoreIgnoresWellFedHerbivoreItCannotKill()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = SpawnBotCarnivore(world, Vector2.Zero, 5);
        var big = world.SpawnCreature(new Vector2(0f, 10f));
        big.AddIntake(50, world.Settings.growth);
        var brain = CreateBrain(world, bot);

        brain.Think(world, Dt);

        Assert.Equal(BotIntent.Wander, brain.Intent);
    }

    [Fact]
    public void Brain_CarnivorePrefersNearbyMeatOverStalking()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = SpawnBotCarnivore(world, Vector2.Zero, 5);
        var big = world.SpawnCreature(new Vector2(0f, 10f));
        big.AddIntake(50, world.Settings.growth);
        big.SetSatiety(world.Settings.hunger.maxSatiety * 0.2f);
        world.Food.Spawn(FoodKind.Meat, new Vector2(6f, 0f));
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Forage, brain.Intent);
        Assert.True(command.Move.X > 0.9f, $"move {command.Move}");
    }

    [Fact]
    public void Brain_CarnivoreStalksStarvingCarnivoreItCannotKill()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = SpawnBotCarnivore(world, Vector2.Zero, 5);
        var equal = SpawnBotCarnivore(world, new Vector2(0f, 10f), 5);
        equal.SetSatiety(world.Settings.hunger.maxSatiety * 0.2f);
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Stalk, brain.Intent);
        Assert.True(command.Move.Y > 0.9f, $"move {command.Move}");
    }

    [Fact]
    public void Brain_FleesStarvingCarnivoreThatCanKillIt()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = SpawnBotCarnivore(world, Vector2.Zero, 5);
        var bigger = SpawnBotCarnivore(world, new Vector2(0f, 6f), 50);
        bigger.SetSatiety(world.Settings.hunger.maxSatiety * 0.2f);
        var brain = CreateBrain(world, bot);

        brain.Think(world, Dt);

        Assert.Equal(BotIntent.Flee, brain.Intent);
    }

    [Fact]
    public void Brain_HerbivoreDoesNotStalk()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        var other = world.SpawnCreature(new Vector2(0f, 6f));
        other.SetSatiety(world.Settings.hunger.maxSatiety * 0.1f);
        var brain = CreateBrain(world, bot);

        brain.Think(world, Dt);

        Assert.NotEqual(BotIntent.Stalk, brain.Intent);
    }

    [Fact]
    public void Brain_HerbivoreMovesToNearestPlant()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        world.Food.Spawn(FoodKind.Plant, new Vector2(-8f, 0f));
        world.Food.Spawn(FoodKind.Plant, new Vector2(0f, -5f));
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Forage, brain.Intent);
        Assert.True(command.Move.Y < -0.9f);
    }

    [Fact]
    public void Brain_CarnivoreDoesNotTargetPlants()
    {
        var world = CreateWorld(maxPlantsPerTile: 0);
        var bot = world.SpawnCreature(Vector2.Zero);
        bot.SetRole(CreatureRole.Carnivore);
        world.Food.Spawn(FoodKind.Plant, new Vector2(0f, -5f));
        var meat = world.Food.Spawn(FoodKind.Meat, new Vector2(7f, 0f));
        var brain = CreateBrain(world, bot);

        var command = brain.Think(world, Dt);

        Assert.Equal(BotIntent.Forage, brain.Intent);
        Assert.True(command.Move.X > 0.9f, $"meat at {meat.Position}, move {command.Move}");
    }

    [Theory]
    [InlineData(1f, false)]
    [InlineData(0.6f, false)]
    [InlineData(0.2f, true)]
    [InlineData(0f, true)]
    public void Brain_SkillLevelDecidesStarvation(float level, bool expectStarved)
    {
        var settings = new WorldSettings { width = 100f, height = 100f };
        settings.food.maxPlantsPerTile = 1;
        // 필드 고기(FOOD-08)는 끕니다. 초식 먹이만으로 수준별 생존을 비교합니다.
        settings.food.meatSpawnIntervalMax = 0f;
        var world = new WorldSimulation(settings, seed: 99);
        world.Food.FillPlants();
        var botSettings = CreateBotSettings();
        botSettings.minPopulation = 1;
        botSettings.skillLevel = level;
        botSettings.joinDelayMin = 1000f;
        botSettings.joinDelayMax = 1000f;
        using var director = new BotDirector(world, botSettings, seed: 5);
        director.FillImmediately();
        var starved = false;
        director.BotLeft += _ => starved = true;

        Step(world, director, 120f);

        Assert.Equal(expectStarved, starved);
    }

    private static void Step(WorldSimulation world, BotDirector director, float seconds)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += Dt)
        {
            director.Update(Dt);
            world.Step(Dt);
        }
    }

    private static Creature SpawnBotCarnivore(WorldSimulation world, Vector2 position, int score)
    {
        var creature = world.SpawnCreature(position);
        creature.SetRole(CreatureRole.Carnivore);
        creature.AddIntake(score, world.Settings.growth);
        return creature;
    }

    private static BotBrain CreateBrain(WorldSimulation world, Creature creature)
    {
        return new BotBrain(creature, BotSkill.FromLevel(1f, CreateBotSettings()), new Random(7));
    }

    private static BotSettings CreateBotSettings()
    {
        return new BotSettings { minPopulation = 5, skillVariance = 0f, spawnAreaHalfExtent = 0f, meatSeekerCount = 0 };
    }

    private static WorldSimulation CreateWorld(int maxPlantsPerTile)
    {
        var settings = new WorldSettings { width = 60f, height = 60f };
        settings.food.tileSize = 5f;
        settings.food.maxPlantsPerTile = maxPlantsPerTile;
        settings.food.meatSpawnIntervalMax = 0f;
        return new WorldSimulation(settings, seed: 1234);
    }
}
