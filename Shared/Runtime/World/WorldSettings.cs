using System;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Growth;
using MutatIO.Shared.Hunger;
using MutatIO.Shared.Movement;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 월드 시뮬레이션 전체 설정입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldSettings
    {
        /// <summary>(WORLD-01) 맵 가로 크기. 월드 단위. 임시: 고정 크기 사각 맵입니다.</summary>
        public float width = 80f;

        /// <summary>(WORLD-01) 맵 세로 크기. 월드 단위.</summary>
        public float height = 80f;

        public MovementSettings movement = new MovementSettings();
        public GrowthSettings growth = new GrowthSettings();
        public FoodSettings food = new FoodSettings();
        public HungerSettings hunger = new HungerSettings();
    }
}
