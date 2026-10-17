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

        /// <summary>(HUNGER-02) 초당 포만 감소량. 게이지 단위/초. 몸집·역할과 무관하게 모든 생명체가 같은 값을 씁니다.</summary>
        public float decayPerSecond = 2f;

        /// <summary>(FOOD-05) 초식 먹이 하나의 기본 포만 회복량(초기 몸집 기준). 게이지 단위.</summary>
        public float plantSatiety = 4f;

        /// <summary>
        /// (HUNGER-05) 몸집별 먹이당 포만 회복 배율의 지수. 배율 = 몸집^(-지수). 0이면 몸집과 무관합니다.
        /// 임시: 곡선 형태도 임시안입니다.
        /// </summary>
        public float sizeSatietyExponent = 0.5f;

        /// <summary>(HUNGER-06) 먹이당 포만 회복 배율의 하한. 회복량이 0에 가까워지지 않게 합니다.</summary>
        public float minSatietyMultiplier = 0.25f;
    }
}
