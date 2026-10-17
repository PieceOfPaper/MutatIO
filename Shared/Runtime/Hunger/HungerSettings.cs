using System;

namespace MutatIO.Shared.Hunger
{
    /// <summary>
    /// 포만 게이지 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class HungerSettings
    {
        /// <summary>(HUNGER-01) 최대 포만량. 게이지 단위.</summary>
        public float maxSatiety = 100f;

        /// <summary>(HUNGER-01) 입장 시 포만량. 게이지 단위.</summary>
        public float startSatiety = 100f;

        /// <summary>
        /// (HUNGER-02) 초당 포만 감소량. 게이지 단위/초.
        /// 임시: 몸집·역할별 배율(HUNGER-03)은 적용 여부가 미결정이므로 모든 생명체에 같은 값을 씁니다.
        /// </summary>
        public float decayPerSecond = 2f;

        /// <summary>(FOOD-05) 초식 먹이 하나의 포만 회복량. 게이지 단위.</summary>
        public float plantSatiety = 4f;
    }
}
