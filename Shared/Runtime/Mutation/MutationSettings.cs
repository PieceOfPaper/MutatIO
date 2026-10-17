using System;

namespace MutatIO.Shared.Mutation
{
    /// <summary>
    /// 초식의 변이(육식 전환) 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class MutationSettings
    {
        /// <summary>(MUTATE-01) 초기 몸집(1)에서의 육식 전환 임계치. 변이 단위.</summary>
        public float baseThreshold = 5f;

        /// <summary>
        /// (MUTATE-04) 몸집별 임계치 배율의 지수. 배율 = 몸집^지수. 0이면 몸집과 무관합니다.
        /// 임시: 곡선 형태도 임시안입니다.
        /// </summary>
        public float sizeThresholdExponent = 1f;

        /// <summary>(MUTATE-02) 초식의 변이 수치 초당 감소량. 변이 단위/초. 0 아래로 내려가지 않습니다.</summary>
        public float decayPerSecond = 0.1f;

        /// <summary>(MUTATE-03) 초식이 고기 1개를 먹을 때 오르는 변이 수치. 변이 단위.</summary>
        public float gainPerMeat = 1f;
    }
}
