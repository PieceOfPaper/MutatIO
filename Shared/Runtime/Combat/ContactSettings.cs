using System;

namespace MutatIO.Shared.Combat
{
    /// <summary>
    /// 몸체 접촉 반응 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class ContactSettings
    {
        /// <summary>
        /// (PUSH-02) 서로 처치할 수 없는 두 생명체가 겹친 거리를 초당 얼마나 빠르게 풀지. 1/초. 한 스텝에 겹침 × min(1, 값 × dt)만큼 떼어 놓습니다.
        /// 0이면 밀지 않습니다.
        /// </summary>
        public float separationRate = 20f;

        /// <summary>
        /// (PUSH-03) 밀침의 무게 = 몸 반경^값. 겹침을 무게의 반비례로 나누어 큰(무거운) 쪽이 덜 밀리고 작은 쪽을 더 밀어냅니다.
        /// 2이면 면적, 3이면 부피 비율입니다. 0이면 크기와 무관하게 반씩 밀립니다.
        /// </summary>
        public float separationMassExponent = 3f;

        /// <summary>(PUSH-01) 육식이 초식 먹이를 밀어낼 때 몸 가장자리 바깥으로 더 띄우는 거리. 월드 단위.</summary>
        public float plantPushMargin = 0.05f;
    }
}
