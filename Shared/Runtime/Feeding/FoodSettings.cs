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
    }
}
