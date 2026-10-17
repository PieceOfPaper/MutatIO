using System.Numerics;

namespace MutatIO.Shared.Movement
{
    /// <summary>
    /// 한 개체의 이동 상태입니다. <see cref="MovementSystem.Step"/>이 갱신합니다.
    /// </summary>
    public struct MovementState
    {
        public Vector2 Position;
        public Vector2 Velocity;

        /// <summary>마지막으로 유효했던 이동 입력 방향(단위 벡터). 아직 이동한 적이 없으면 0입니다.</summary>
        public Vector2 LastValidDirection;

        /// <summary>진행 중인 대시의 방향(단위 벡터).</summary>
        public Vector2 DashDirection;

        /// <summary>남은 대시 시간. 0보다 크면 대시 중입니다. 초.</summary>
        public float DashTimeRemaining;

        /// <summary>남은 대시 대기시간. 대시가 끝난 시점부터 줄어듭니다. 초.</summary>
        public float DashCooldownRemaining;

        public bool IsDashing => DashTimeRemaining > 0f;

        public bool CanDash => DashTimeRemaining <= 0f && DashCooldownRemaining <= 0f;

        public static MovementState At(Vector2 position)
        {
            return new MovementState { Position = position };
        }
    }
}
