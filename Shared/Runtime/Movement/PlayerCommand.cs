using System.Numerics;

namespace MutatIO.Shared.Movement
{
    /// <summary>
    /// 입력 장치와 무관한 한 스텝의 플레이어 명령입니다.
    /// 키보드·가상패드·마우스 추종·터치가 모두 이 명령을 만들고 이동 규칙은 이 명령만 소비합니다.
    /// </summary>
    public readonly struct PlayerCommand
    {
        public PlayerCommand(Vector2 move, bool dash)
        {
            var lengthSquared = move.LengthSquared();
            Move = lengthSquared > 1f ? move / (float)System.Math.Sqrt(lengthSquared) : move;
            Dash = dash;
        }

        /// <summary>이동 입력입니다. 길이는 0~1이며 1을 넘으면 정규화합니다.</summary>
        public Vector2 Move { get; }

        /// <summary>이번 스텝에 대시 입력이 새로 눌렸는지 여부입니다. 누르고 있는 동안 반복하지 않습니다.</summary>
        public bool Dash { get; }

        public static PlayerCommand None => default;
    }
}
