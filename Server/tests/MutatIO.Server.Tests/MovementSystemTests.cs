using System.Numerics;
using MutatIO.Shared.Movement;

namespace MutatIO.Server.Tests;

public sealed class MovementSystemTests
{
    private const float Dt = 1f / 60f;
    private const float Tolerance = 1e-3f;

    private static readonly Vector2 Right = Vector2.UnitX;
    private static readonly Vector2 Left = -Vector2.UnitX;
    private static readonly Vector2 Up = Vector2.UnitY;

    private readonly MovementSettings settings = MovementSettings.CreateTemporaryDefaults();

    [Fact]
    public void Command_NormalizesInputLongerThanOne()
    {
        var command = new PlayerCommand(new Vector2(3f, 4f), false);

        Assert.Equal(1f, command.Move.Length(), 4);
        Assert.Equal(0.6f, command.Move.X, 4);
    }

    [Fact]
    public void Acceleration_ReachesMaxSpeedGraduallyNotInstantly()
    {
        var state = MovementState.At(Vector2.Zero);

        Step(ref state, new PlayerCommand(Right, false));
        Assert.True(state.Velocity.X > 0f);
        Assert.True(state.Velocity.X < settings.baseSpeed);

        Simulate(ref state, new PlayerCommand(Right, false), settings.accelerationTime);
        Assert.Equal(settings.baseSpeed, state.Velocity.X, 3);
    }

    [Fact]
    public void ReleasingInput_SlidesThenStops()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Right, false), 1f);

        Step(ref state, PlayerCommand.None);
        Assert.True(state.Velocity.X > 0f);

        Simulate(ref state, PlayerCommand.None, settings.decelerationTime);
        Assert.Equal(Vector2.Zero, state.Velocity);
    }

    [Fact]
    public void Reversing_ChangesVelocityAlongLineWithoutTurnRadius()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Right, false), 1f);

        for (var i = 0; i < 10; i++)
        {
            Step(ref state, new PlayerCommand(Left, false));
            Assert.Equal(0f, state.Velocity.Y, 5);
        }

        Simulate(ref state, new PlayerCommand(Left, false), settings.accelerationTime * 2f);
        Assert.Equal(-settings.baseSpeed, state.Velocity.X, 3);
    }

    [Fact]
    public void LargerSize_MovesSlowerButNotBelowMinimum()
    {
        var small = MovementSystem.GetMaxSpeed(1f, settings);
        var medium = MovementSystem.GetMaxSpeed(4f, settings);
        var huge = MovementSystem.GetMaxSpeed(100000f, settings);

        Assert.Equal(settings.baseSpeed, small, 4);
        Assert.True(medium < small);
        Assert.Equal(settings.baseSpeed * settings.minSpeedMultiplier, huge, 4);
    }

    [Fact]
    public void Dash_BeforeAnyMovement_IsIgnoredWithoutCooldown()
    {
        var state = MovementState.At(Vector2.Zero);

        var started = MovementSystem.Step(ref state, new PlayerCommand(Vector2.Zero, true), 1f, settings, Dt);

        Assert.False(started);
        Assert.False(state.IsDashing);
        Assert.Equal(0f, state.DashCooldownRemaining);
    }

    [Fact]
    public void Dash_WhileMoving_UsesCurrentInputDirection()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Right, false), 0.5f);

        var started = MovementSystem.Step(ref state, new PlayerCommand(Up, true), 1f, settings, Dt);

        Assert.True(started);
        Assert.Equal(Up, state.DashDirection);
        Assert.Equal(settings.baseSpeed * settings.dashSpeedMultiplier, state.Velocity.Y, 3);
    }

    [Fact]
    public void Dash_WhileStopped_UsesLastValidDirection()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Up, false), 0.5f);
        Simulate(ref state, PlayerCommand.None, 1f);
        Assert.Equal(Vector2.Zero, state.Velocity);

        Assert.True(MovementSystem.Step(ref state, new PlayerCommand(Vector2.Zero, true), 1f, settings, Dt));
        Assert.Equal(Up, state.DashDirection);
    }

    [Fact]
    public void Dash_KeepsStartDirectionDuringDash()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Right, false), 0.5f);
        MovementSystem.Step(ref state, new PlayerCommand(Right, true), 1f, settings, Dt);

        Step(ref state, new PlayerCommand(Up, false));

        Assert.True(state.IsDashing);
        Assert.Equal(0f, state.Velocity.Y, 5);
    }

    [Fact]
    public void Dash_CannotRepeatUntilCooldownEnds()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Right, false), 0.5f);
        Assert.True(MovementSystem.Step(ref state, new PlayerCommand(Right, true), 1f, settings, Dt));

        Simulate(ref state, new PlayerCommand(Right, false), settings.dashDuration + Dt);
        Assert.False(state.IsDashing);
        Assert.True(state.DashCooldownRemaining > 0f);
        Assert.False(MovementSystem.Step(ref state, new PlayerCommand(Right, true), 1f, settings, Dt));

        Simulate(ref state, new PlayerCommand(Right, false), settings.dashCooldown + Tolerance);
        Assert.True(MovementSystem.Step(ref state, new PlayerCommand(Right, true), 1f, settings, Dt));
    }

    [Fact]
    public void Dash_SlowsBackToNormalSpeedAfterEnding()
    {
        var state = MovementState.At(Vector2.Zero);
        Simulate(ref state, new PlayerCommand(Right, false), 0.5f);
        MovementSystem.Step(ref state, new PlayerCommand(Right, true), 1f, settings, Dt);

        Simulate(ref state, new PlayerCommand(Right, false), settings.dashDuration + 1f);

        Assert.Equal(settings.baseSpeed, state.Velocity.X, 3);
    }

    private void Step(ref MovementState state, PlayerCommand command)
    {
        MovementSystem.Step(ref state, command, 1f, settings, Dt);
    }

    private void Simulate(ref MovementState state, PlayerCommand command, float seconds)
    {
        var steps = (int)System.Math.Ceiling(seconds / Dt);
        for (var i = 0; i < steps; i++)
        {
            Step(ref state, command);
        }
    }
}
