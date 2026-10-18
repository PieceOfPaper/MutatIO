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
        /// <summary>(FOOD-01) 초식 리젠 타일 한 변의 길이. 월드 단위. 초기 가정 5.</summary>
        public float tileSize = 5f;

        /// <summary>(FOOD-02) 타일 하나가 먹이를 하나 다시 생성하는 주기. 초.</summary>
        public float plantRegenInterval = 15f;

        /// <summary>(FOOD-03) 타일 하나가 생성해 남아 있을 수 있는 초식 먹이 최대 개수 n. 0이면 리젠하지 않습니다.</summary>
        public int maxPlantsPerTile = 1;

        /// <summary>(MAGNET-01) 몸 표면에서 흡수가 시작되는 고정 거리. 월드 단위. 흡수 반경 = 몸 반경 + 이 값.</summary>
        public float magnetRange = 1.5f;

        /// <summary>(MAGNET-02) 흡수 중인 먹이가 끌려가는 속도. 월드 단위/초.</summary>
        public float magnetPullSpeed = 12f;

        /// <summary>(MAGNET-03) 몸 가장자리에서 이 거리 안에 들어오면 섭취를 완료합니다. 월드 단위.</summary>
        public float consumeDistance = 0f;

        /// <summary>
        /// (DROP-01) 사망 시 고기 환원율. 고기 개수 = 반올림(이번 생 점수 × 이 값). 초식·육식 모두 남깁니다.
        /// 임시: 개수 변환(반올림)과 분배 방식은 미결정이며 임시안입니다.
        /// </summary>
        public float meatDropRatio = 0.25f;

        /// <summary>(DROP-03) 고기가 흩어지는 범위. 죽은 몸 가장자리 바깥으로 이 거리까지. 월드 단위.</summary>
        public float meatDropSpread = 1.5f;

        /// <summary>
        /// (DROP-03) 고기 유지시간. 생성 후 이 시간이 지나면 썩어서 사라집니다. 초. 0 이하이면 썩지 않습니다.
        /// 생성 시에 적용하며, 흡수(끌려가는 중) 중인 고기는 사라지지 않고 섭취를 마칩니다.
        /// </summary>
        public float meatLifetime = 30f;

        /// <summary>
        /// (FOOD-08) 필드에 고기가 저절로 생기는 간격의 최소값. 맵 전체 기준. 초. 최대값이 0 이하이면 생기지 않습니다.
        /// 한 번에 1개를 생명체가 덮지 않은 맵 위 무작위 위치에 만들며, 사망 드롭 고기와 같이 <see cref="meatLifetime"/> 뒤 썩습니다.
        /// 월드 시작 시에는 정상 상태 개수(유지시간 ÷ 평균 간격)만큼 미리 깔아 둡니다.
        /// 임시: 사망 드롭 외 고기 공급은 프로토타입용 임시안입니다. 200×200 맵에서 약 175개가 유지되어
        /// 시작 화면(짧은 변 16)에 평균 2개 정도 보이는 값입니다(3~5개 기준값 0.05~0.12초의 절반 빈도).
        /// </summary>
        public float meatSpawnIntervalMin = 0.1f;

        /// <summary>(FOOD-08) 필드 고기 생성 간격의 최대값. 초. 매번 최소~최대 사이에서 무작위로 정합니다.</summary>
        public float meatSpawnIntervalMax = 0.24f;
    }
}
