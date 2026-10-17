using System;

namespace MutatIO.Shared.Feeding
{
    /// <summary>
    /// 먹이 생성·흡수 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class FoodSettings
    {
        /// <summary>(FOOD-01) 새 초식 먹이가 기존 먹이와 떨어져야 하는 최소 거리. 월드 단위.</summary>
        public float plantSpacing = 1.5f;

        /// <summary>(FOOD-02) 초식 먹이를 하나 다시 생성하는 주기. 초.</summary>
        public float plantRegenInterval = 0.25f;

        /// <summary>(FOOD-03) 맵의 초식 먹이 최대 개수. 시작 시 이 개수까지 채웁니다(임시).</summary>
        public int maxPlantCount = 250;

        /// <summary>(FOOD-04) 초식 먹이 하나의 섭취량(점수) 가치.</summary>
        public float plantIntake = 1f;

        /// <summary>
        /// (MAGNET-01, MAGNET-04) 몸 가장자리에서 흡수가 시작되는 거리. 월드 단위.
        /// 임시: 흡수 반경 = 몸 반경 + 이 값이므로 몸집이 커지면 몸 반경만큼 함께 넓어집니다.
        /// </summary>
        public float magnetRange = 1.5f;

        /// <summary>(MAGNET-02) 흡수 중인 먹이가 끌려가는 속도. 월드 단위/초.</summary>
        public float magnetPullSpeed = 12f;

        /// <summary>(MAGNET-03) 몸 가장자리에서 이 거리 안에 들어오면 섭취를 완료합니다. 월드 단위.</summary>
        public float consumeDistance = 0f;

        /// <summary>
        /// (MAGNET-05) 흡수 대상이 흡수 반경보다 이만큼 더 멀어지면 끌림을 해제합니다. 월드 단위.
        /// 임시: 흡수 도중 대상이 멀어질 때의 처리는 미결정입니다.
        /// </summary>
        public float magnetReleaseMargin = 1f;
    }
}
