using MutatIO.Input;
using MutatIO.Shared.Movement;
using NUnit.Framework;
using UnityEngine;

namespace MutatIO.Tests.EditMode
{
    /// <summary>
    /// Shared 이동 규칙이 Unity 런타임에서도 같게 동작하는지와 포인터 추종 입력 변환을 확인합니다.
    /// 이동 규칙의 자세한 경우는 Server/tests의 MovementSystemTests에서 다룹니다.
    /// </summary>
    public sealed class MovementTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void Movement_AcceleratesWithInertiaAndDashesInInputDirection()
        {
            var settings = MovementSettings.CreateTemporaryDefaults();
            var state = MovementState.At(System.Numerics.Vector2.Zero);
            var right = new PlayerCommand(System.Numerics.Vector2.UnitX, false);

            MovementSystem.Step(ref state, right, 1f, settings, Dt);
            Assert.That(state.Velocity.X, Is.GreaterThan(0f).And.LessThan(settings.baseSpeed));

            for (var i = 0; i < 60; i++)
            {
                MovementSystem.Step(ref state, right, 1f, settings, Dt);
            }

            Assert.That(state.Velocity.X, Is.EqualTo(settings.baseSpeed).Within(1e-3f));

            var started = MovementSystem.Step(ref state, new PlayerCommand(System.Numerics.Vector2.UnitY, true), 1f, settings, Dt);
            Assert.That(started, Is.True);
            Assert.That(state.DashDirection, Is.EqualTo(System.Numerics.Vector2.UnitY));
        }

        [Test]
        public void Dash_BeforeFirstMove_IsIgnored()
        {
            var settings = MovementSettings.CreateTemporaryDefaults();
            var state = MovementState.At(System.Numerics.Vector2.Zero);

            var started = MovementSystem.Step(ref state, new PlayerCommand(System.Numerics.Vector2.Zero, true), 1f, settings, Dt);

            Assert.That(started, Is.False);
            Assert.That(state.DashCooldownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void PointerFollow_ReturnsUnitDirectionOutsideDeadZone()
        {
            var input = PointerFollow.GetMoveInput(new Vector2(400f, 300f), new Vector2(100f, 300f), 20f);

            Assert.That(input.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(input.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void PointerFollow_ReturnsZeroInsideDeadZone()
        {
            var input = PointerFollow.GetMoveInput(new Vector2(105f, 300f), new Vector2(100f, 300f), 20f);

            Assert.That(input, Is.EqualTo(Vector2.zero));
        }
    }
}
