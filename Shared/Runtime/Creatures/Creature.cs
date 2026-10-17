using System;
using MutatIO.Shared.Growth;
using MutatIO.Shared.Movement;

namespace MutatIO.Shared.Creatures
{
    /// <summary>
    /// 월드의 생명체 하나입니다. 상태 변경은 <see cref="World.WorldSimulation"/>이 합니다.
    /// </summary>
    public sealed class Creature
    {
        /// <summary>
        /// 임시: 몸집 → 충돌 반경 계수(CONTACT-01)는 미결정입니다. 현재는 몸집을 지름으로 보고 반경을 그 절반으로 둡니다.
        /// </summary>
        public const float RadiusPerSize = 0.5f;

        public MovementState Movement;

        public Creature(int id, MovementState movement)
        {
            Id = id;
            Movement = movement;
            Size = GrowthSystem.InitialSize;
        }

        public int Id { get; }

        /// <summary>이번 생의 누적 섭취량입니다. 현재는 점수로도 사용합니다.</summary>
        public float Intake { get; private set; }

        /// <summary>초기 몸집을 1로 둔 상대 지름입니다.</summary>
        public float Size { get; private set; }

        public float Radius => Size * RadiusPerSize;

        /// <summary>남은 포만량입니다. 0이 되면 사망합니다.</summary>
        public float Satiety { get; private set; }

        /// <summary>사망하여 월드에서 제거되면 false입니다.</summary>
        public bool IsAlive { get; private set; } = true;

        /// <summary>다음 스텝에 적용할 명령입니다. 대시 입력은 한 스텝에만 적용됩니다.</summary>
        public PlayerCommand Command { get; set; }

        public void AddIntake(float amount, GrowthSettings growth)
        {
            Intake += amount;
            Size = GrowthSystem.GetSize(Intake, growth);
        }

        public void SetSatiety(float satiety)
        {
            Satiety = Math.Max(0f, satiety);
        }

        internal void MarkDead()
        {
            IsAlive = false;
        }
    }
}
