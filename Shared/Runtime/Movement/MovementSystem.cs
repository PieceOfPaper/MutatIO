using System;
using System.Numerics;

namespace MutatIO.Shared.Movement
{
    /// <summary>
    /// 이동 규칙입니다. 별도의 회전속도 제한 없이, 속도 벡터가 목표 속도로 일정 비율만큼 다가가는 방식으로 약한 관성을 표현합니다.
    /// 고정 틱을 가정하지 않고 호출자가 넘긴 dt만큼 진행합니다.
    /// </summary>
    public static class MovementSystem
    {
        /// <summary>
        /// 몸집이 반영된 최고 이동속도입니다. 몸집은 초기 몸집을 1로 둔 상대값입니다.
        /// </summary>
        public static float GetMaxSpeed(float size, MovementSettings settings)
        {
            return settings.baseSpeed * GetSizeSpeedMultiplier(size, settings);
        }

        public static float GetSizeSpeedMultiplier(float size, MovementSettings settings)
        {
            if (size <= 1f)
            {
                return 1f;
            }

            var multiplier = (float)Math.Pow(size, -settings.sizeSpeedExponent);
            return Math.Max(settings.minSpeedMultiplier, multiplier);
        }

        /// <summary>
        /// 이동 상태를 dt만큼 진행합니다. 이번 스텝에 대시가 시작되면 true를 반환합니다.
        /// </summary>
        public static bool Step(ref MovementState state, in PlayerCommand command, float size, MovementSettings settings, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return false;
            }

            var maxSpeed = GetMaxSpeed(size, settings);
            var input = command.Move;
            var inputLength = input.Length();
            var hasInput = inputLength > settings.minMoveInput;
            if (hasInput)
            {
                state.LastValidDirection = input / inputLength;
            }

            var dashStarted = command.Dash && TryStartDash(ref state, settings);

            if (state.IsDashing)
            {
                // 임시: 대시 중 방향 전환은 미결정이므로 시작 방향을 유지합니다.
                state.Velocity = state.DashDirection * (maxSpeed * settings.dashSpeedMultiplier);
                state.DashTimeRemaining -= deltaTime;
                if (state.DashTimeRemaining <= 0f)
                {
                    state.DashTimeRemaining = 0f;
                    state.DashCooldownRemaining = settings.dashCooldown;
                }
            }
            else
            {
                state.DashCooldownRemaining = Math.Max(0f, state.DashCooldownRemaining - deltaTime);

                var targetVelocity = hasInput ? input * maxSpeed : Vector2.Zero;
                var responseTime = hasInput ? settings.accelerationTime : settings.decelerationTime;
                state.Velocity = MoveTowards(state.Velocity, targetVelocity, maxSpeed, responseTime, deltaTime);
            }

            state.Position += state.Velocity * deltaTime;
            return dashStarted;
        }

        private static bool TryStartDash(ref MovementState state, MovementSettings settings)
        {
            // 움직이는 중에는 현재 입력 방향, 정지 중에는 마지막 유효 방향을 사용합니다(Step에서 이미 갱신).
            // 임시: 첫 이동 전처럼 유효 방향이 없으면 대시를 무시하고 대기시간도 소모하지 않습니다.
            if (!state.CanDash || state.LastValidDirection == Vector2.Zero)
            {
                return false;
            }

            state.DashDirection = state.LastValidDirection;
            state.DashTimeRemaining = settings.dashDuration;
            return true;
        }

        private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxSpeed, float responseTime, float deltaTime)
        {
            if (responseTime <= 0f)
            {
                return target;
            }

            var maxDelta = maxSpeed / responseTime * deltaTime;
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
