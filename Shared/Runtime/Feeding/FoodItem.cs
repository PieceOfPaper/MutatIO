using System.Numerics;

namespace MutatIO.Shared.Feeding
{
    public enum FoodKind
    {
        /// <summary>초식 먹이. 초록색, 주기적으로 리젠합니다.</summary>
        Plant = 1,

        /// <summary>육식 먹이. 붉은색, 초식 생명체 사망 시에만 생성합니다(M6).</summary>
        Meat = 2,
    }

    /// <summary>
    /// 필드의 먹이 하나입니다. 상태 변경은 <see cref="FoodField"/>이 합니다.
    /// </summary>
    public sealed class FoodItem
    {
        public const int NoAttractor = 0;

        internal FoodItem(int id, FoodKind kind, Vector2 position, float intake)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Intake = intake;
        }

        public int Id { get; }

        public FoodKind Kind { get; }

        public Vector2 Position { get; internal set; }

        /// <summary>섭취 시 얻는 섭취량(점수).</summary>
        public float Intake { get; }

        /// <summary>이 먹이를 끌어당기고 있는 생명체 ID. 없으면 <see cref="NoAttractor"/>입니다.</summary>
        public int AttractorId { get; internal set; }
    }
}
