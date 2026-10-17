using System;

namespace MutatIO.Profile
{
    /// <summary>
    /// 첫 화면에서 고르는 캐릭터 외형 목록입니다. 외형은 초식·육식 역할과 무관하며 능력치·충돌 반경 차이가 없습니다.
    /// 원형 몸체는 모두 같고 귀·눈·표정 장식만 다릅니다.
    /// 문자열 ID는 PlayerPrefs 저장에 쓰는 안정적인 식별자이므로 바꾸지 않습니다. 표시 방식과 분리합니다.
    /// 임시: 아트 확정 전 placeholder 4종입니다. 최종 장식 종류는 미결정입니다.
    /// </summary>
    public static class CharacterAppearances
    {
        /// <summary>귀 없음, 동그란 눈.</summary>
        public const string Basic = "basic";

        /// <summary>뾰족한 귀, 동그란 눈.</summary>
        public const string PointyEars = "pointy_ears";

        /// <summary>둥근 귀, 동그란 눈과 웃는 입.</summary>
        public const string RoundEars = "round_ears";

        /// <summary>귀 없음, 감은 눈.</summary>
        public const string Sleepy = "sleepy";

        public const string Default = Basic;

        private static readonly string[] ids = { Basic, PointyEars, RoundEars, Sleepy };

        public static int Count => ids.Length;

        public static string GetId(int index)
        {
            return ids[index];
        }

        public static bool IsValid(string id)
        {
            return id != null && Array.IndexOf(ids, id) >= 0;
        }
    }
}
