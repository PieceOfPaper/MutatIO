using System.Numerics;

namespace MutatIO.Shared.Feeding
{
    public enum FoodKind
    {
        /// <summary>초식 먹이. 초록색, 타일별로 리젠합니다.</summary>
        Plant = 1,

        /// <summary>육식 먹이. 붉은색, 초식 생명체 사망 시와 가끔 필드에 저절로 생성합니다. 타일 리젠·상한과 무관하며 시간이 지나면 썩습니다.</summary>
        Meat = 2,
    }

    /// <summary>
    /// 필드의 먹이 하나입니다. 상태 변경은 <see cref="FoodField"/>이 합니다.
    /// 먹이 1개는 섭취 시 1점이며 먹이별 점수 가중치는 없습니다.
    /// </summary>
    public sealed class FoodItem
    {
        public const int NoAttractor = 0;
        public const int NoTile = -1;

        internal FoodItem(int id, FoodKind kind, Vector2 position, int originTile, float lifetime)
        {
            Id = id;
            Kind = kind;
            Position = position;
            OriginTile = originTile;
            Lifetime = lifetime;
            TimeLeft = lifetime;
        }

        public int Id { get; }

        public FoodKind Kind { get; }

        public Vector2 Position { get; internal set; }

        /// <summary>
        /// 이 먹이를 리젠한 타일 인덱스입니다. 이동해도 바뀌지 않으며 타일 상한은 이 귀속으로 셉니다.
        /// 타일 리젠이 아닌 먹이(직접 생성·육식 먹이)는 <see cref="NoTile"/>입니다.
        /// </summary>
        public int OriginTile { get; }

        /// <summary>생성 시 정한 유지시간. 초. 썩지 않는 먹이(초식 먹이 등)는 <see cref="float.PositiveInfinity"/>입니다.</summary>
        public float Lifetime { get; }

        /// <summary>썩어서 사라지기까지 남은 시간. 초. 썩지 않는 먹이는 <see cref="float.PositiveInfinity"/>입니다.</summary>
        public float TimeLeft { get; internal set; }

        public bool Decays => !float.IsPositiveInfinity(Lifetime);

        /// <summary>이 먹이를 끌어당기고 있는 생명체 ID. 없으면 <see cref="NoAttractor"/>입니다.</summary>
        public int AttractorId { get; internal set; }

        /// <summary>현재 위치가 속한 공간 분할 칸. 탐색용입니다.</summary>
        internal int Cell { get; set; }

        /// <summary><see cref="FoodField.Items"/> 안의 위치. 빠른 제거용입니다.</summary>
        internal int ListIndex { get; set; }
    }
}
