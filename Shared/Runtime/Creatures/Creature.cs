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
        /// <summary>몸집은 지름입니다. 외형 반경 = 충돌 반경 = 몸집 × 이 값이며 별도 접촉 반경 배율은 없습니다.</summary>
        public const float RadiusPerSize = 0.5f;

        public MovementState Movement;

        public Creature(int id, MovementState movement)
        {
            Id = id;
            Movement = movement;
            Size = GrowthSystem.InitialSize;
        }

        public int Id { get; }

        /// <summary>이번 생에 섭취 완료한 먹이 개수이자 점수입니다. 먹이 1개 = 1점이며 몸집은 이 값에서 계산합니다.</summary>
        public int Intake { get; private set; }

        /// <summary>초기 몸집을 1로 둔 상대 지름입니다.</summary>
        public float Size { get; private set; }

        public float Radius => Size * RadiusPerSize;

        /// <summary>현재 역할입니다. 모두 초식으로 시작합니다.</summary>
        public CreatureRole Role { get; private set; } = CreatureRole.Herbivore;

        /// <summary>남은 포만량입니다. 0이 되면 사망합니다.</summary>
        public float Satiety { get; private set; }

        /// <summary>사망하여 월드에서 제거되면 false입니다.</summary>
        public bool IsAlive { get; private set; } = true;

        /// <summary>다음 스텝에 적용할 명령입니다. 대시 입력은 한 스텝에만 적용됩니다.</summary>
        public PlayerCommand Command { get; set; }

        public void AddIntake(int count, GrowthSettings growth)
        {
            Intake += Math.Max(0, count);
            Size = GrowthSystem.GetSize(Intake, growth);
        }

        /// <summary>역할을 바꿉니다. 변이 판정(M6)과 개발용 확인에서 사용합니다.</summary>
        public void SetRole(CreatureRole role)
        {
            Role = role;
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
