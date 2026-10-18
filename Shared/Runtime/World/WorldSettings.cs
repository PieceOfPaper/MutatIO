using System;
using MutatIO.Shared.Combat;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Growth;
using MutatIO.Shared.Hunger;
using MutatIO.Shared.Movement;
using MutatIO.Shared.Mutation;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 월드 시뮬레이션 전체 설정입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldSettings
    {
        /// <summary>
        /// (WORLD-01) 맵 가로 크기. 월드 단위. 고정 크기 사각 맵이며 시작 시에만 적용합니다.
        /// 임시: 초기 가정 1000에서 프로토타입은 만남 빈도를 위해 200으로 줄였습니다.
        /// </summary>
        public float width = 200f;

        /// <summary>(WORLD-01) 맵 세로 크기. 월드 단위. 프로토타입 200(초기 가정 1000).</summary>
        public float height = 200f;

        public MovementSettings movement = new MovementSettings();
        public GrowthSettings growth = new GrowthSettings();
        public FoodSettings food = new FoodSettings();
        public HungerSettings hunger = new HungerSettings();
        public MutationSettings mutation = new MutationSettings();
        public ContactSettings contact = new ContactSettings();
    }
}
