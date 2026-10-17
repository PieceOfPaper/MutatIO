namespace MutatIO.Core
{
    /// <summary>
    /// Shared 규칙이 쓰는 System.Numerics 벡터와 Unity 벡터 사이의 변환입니다.
    /// </summary>
    public static class VectorConversions
    {
        public static System.Numerics.Vector2 ToNumerics(this UnityEngine.Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }

        public static UnityEngine.Vector2 ToUnity(this System.Numerics.Vector2 value)
        {
            return new UnityEngine.Vector2(value.X, value.Y);
        }
    }
}
