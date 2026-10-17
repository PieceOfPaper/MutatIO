using System;

namespace MutatIO.Profile
{
    /// <summary>
    /// 첫 화면에서 고르는 캐릭터 외형 목록입니다. 외형은 초식·육식 역할과 무관하며 능력치 차이가 없습니다.
    /// 문자열 ID는 PlayerPrefs 저장에 쓰는 안정적인 식별자이므로 바꾸지 않습니다. 표시 방식과 분리합니다.
    /// 임시: 원형 placeholder 4종입니다. 원형 안의 실루엣·표면·눈·장식 차이는 미결정입니다.
    /// </summary>
    public static class CharacterAppearances
    {
        public const string Plain = "plain";
        public const string Ring = "ring";
        public const string Eyes = "eyes";
        public const string Spots = "spots";

        public const string Default = Plain;

        private static readonly string[] ids = { Plain, Ring, Eyes, Spots };

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
