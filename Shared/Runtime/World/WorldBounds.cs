using System;
using System.Numerics;
using MutatIO.Shared.Movement;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 원점을 중심으로 하는 사각형 맵 경계입니다.
    /// </summary>
    public readonly struct WorldBounds
    {
        public WorldBounds(float width, float height)
        {
            HalfWidth = Math.Max(0f, width) * 0.5f;
            HalfHeight = Math.Max(0f, height) * 0.5f;
        }

        public float HalfWidth { get; }

        public float HalfHeight { get; }

        public bool Contains(Vector2 point, float margin = 0f)
        {
            return Math.Abs(point.X) <= HalfWidth - margin && Math.Abs(point.Y) <= HalfHeight - margin;
        }

        /// <summary>
        /// 반경 radius인 원이 경계 안에 머물도록 위치를 제한하고, 경계 밖을 향하는 속도 성분을 없앱니다.
        /// </summary>
        public void ClampCircle(ref MovementState state, float radius)
        {
            var limitX = Math.Max(0f, HalfWidth - radius);
            var limitY = Math.Max(0f, HalfHeight - radius);

            if (state.Position.X > limitX || state.Position.X < -limitX)
            {
                state.Position.X = Math.Sign(state.Position.X) * limitX;
                if (Math.Sign(state.Velocity.X) == Math.Sign(state.Position.X))
                {
                    state.Velocity.X = 0f;
                }
            }

            if (state.Position.Y > limitY || state.Position.Y < -limitY)
            {
                state.Position.Y = Math.Sign(state.Position.Y) * limitY;
                if (Math.Sign(state.Velocity.Y) == Math.Sign(state.Position.Y))
                {
                    state.Velocity.Y = 0f;
                }
            }
        }
    }
}
