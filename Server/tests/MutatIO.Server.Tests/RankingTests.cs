using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;

namespace MutatIO.Server.Tests;

public sealed class RankingTests
{
    [Fact]
    public void Ranking_SortsByScoreThenEarlierJoin()
    {
        var world = new WorldSimulation(new WorldSettings { width = 100f, height = 100f }, seed: 1);
        var first = world.SpawnCreature(new Vector2(-30f, 0f), "First");
        var second = world.SpawnCreature(new Vector2(0f, 0f), "Second");
        var third = world.SpawnCreature(new Vector2(30f, 0f), "Third");
        first.AddIntake(5, world.Settings.growth);
        second.AddIntake(9, world.Settings.growth);
        third.AddIntake(5, world.Settings.growth);
        var ranked = new List<Creature>();

        Ranking.Build(world.Creatures, ranked);

        Assert.Equal(new[] { second, first, third }, ranked);
        Assert.Equal(3, Ranking.GetRank(ranked, third));
        Assert.Equal("Third", ranked[2].Name);
    }

    [Fact]
    public void Ranking_ExcludesDeadCreatures()
    {
        var world = new WorldSimulation(new WorldSettings { width = 100f, height = 100f }, seed: 1);
        var dead = world.SpawnCreature(new Vector2(-30f, 0f));
        var alive = world.SpawnCreature(new Vector2(30f, 0f));
        dead.SetSatiety(0.001f);
        alive.SetSatiety(100f);
        world.Step(1f / 60f);
        var ranked = new List<Creature>();

        Ranking.Build(world.Creatures, ranked);

        Assert.Equal(new[] { alive }, ranked);
        Assert.Equal(0, Ranking.GetRank(ranked, dead));
    }

    [Fact]
    public void Bots_UseTheirNameAsCreatureName()
    {
        var world = new WorldSimulation(new WorldSettings { width = 100f, height = 100f }, seed: 1);
        using var director = new MutatIO.Shared.Bots.BotDirector(world, new MutatIO.Shared.Bots.BotSettings(), seed: 2);
        director.FillImmediately();

        Assert.All(director.Bots, bot => Assert.Equal(bot.Name, bot.Creature.Name));
    }
}
