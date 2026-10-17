using System;

namespace MutatIO.Shared.Growth
{
    /// <summary>
    /// 섭취량 → 몸집 성장 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값과 곡선 형태는 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class GrowthSettings
    {
        /// <summary>
        /// (GROW-01) 섭취량 1당 늘어나는 면적 비율. 몸집 = √(1 + 섭취량 × 이 값).
        /// 몸집은 초기 몸집을 1로 둔 상대 지름이므로 먹은 양이 화면상 면적에 비례합니다.
        /// </summary>
        public float areaPerIntake = 0.05f;

        /// <summary>(GROW-02) 최대 몸집. 0 이하이면 상한이 없습니다.</summary>
        public float maxSize = 0f;
    }
}
