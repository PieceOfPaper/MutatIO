using System.Globalization;
using MutatIO.Input;

namespace MutatIO.Profile
{
    /// <summary>
    /// 첫 화면에서 고르는 입장 정보입니다. 이전 선택 복원용이며 점수·몸집·육식 상태 같은 플레이 진행은 담지 않습니다.
    /// </summary>
    public readonly struct PlayerProfile
    {
        /// <summary>
        /// (UI-01) 닉네임 최대 문자 수. 임시값입니다.
        /// 임시: 길이만 제한하며 허용 문자·중복·게스트 식별은 미결정입니다. 빈 닉네임도 허용합니다.
        /// </summary>
        public const int MaxNicknameLength = 12;

        public PlayerProfile(string appearanceId, string nickname, ControlMode controlMode)
        {
            AppearanceId = appearanceId;
            Nickname = nickname;
            ControlMode = controlMode;
        }

        public string AppearanceId { get; }

        public string Nickname { get; }

        public ControlMode ControlMode { get; }

        /// <summary>
        /// 저장값이 없을 때의 기본 선택입니다.
        /// 임시: 첫 실행 기본 조작은 미결정이며, PC는 마우스 추종·모바일은 가상패드로 둡니다.
        /// </summary>
        public static PlayerProfile CreateDefault(bool isMobile)
        {
            return new PlayerProfile(CharacterAppearances.Default, string.Empty, DefaultControlMode(isMobile));
        }

        public static ControlMode DefaultControlMode(bool isMobile)
        {
            return isMobile ? ControlMode.VirtualPad : ControlMode.PointerFollow;
        }

        public static bool IsValidControlMode(int value)
        {
            switch ((ControlMode)value)
            {
                case ControlMode.Wasd:
                case ControlMode.ArrowKeys:
                case ControlMode.VirtualPad:
                case ControlMode.PointerFollow:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>앞뒤 공백을 지우고 최대 문자 수로 자릅니다. 결합 문자나 서로게이트 쌍을 중간에서 자르지 않습니다.</summary>
        public static string NormalizeNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return string.Empty;
            }

            var trimmed = nickname.Trim();
            var info = new StringInfo(trimmed);
            return info.LengthInTextElements > MaxNicknameLength
                ? info.SubstringByTextElements(0, MaxNicknameLength).TrimEnd()
                : trimmed;
        }
    }
}
