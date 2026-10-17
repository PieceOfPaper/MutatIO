using System;

namespace MutatIO.Shared.Growth
{
    /// <summary>
    /// 섭취량에서 몸집을 계산합니다.
    /// 임시: 현재는 점수와 질량을 나누지 않고 이번 생의 누적 섭취량 하나로 점수와 몸집을 모두 정합니다.
    /// </summary>
    public static class GrowthSystem
    {
        public const float InitialSize = 1f;

        public static float GetSize(float intake, GrowthSettings settings)
        {
            var area = 1f + Math.Max(0f, intake) * settings.areaPerIntake;
            var size = (float)Math.Sqrt(area);
            return settings.maxSize > 0f ? Math.Min(size, Math.Max(InitialSize, settings.maxSize)) : size;
        }
    }
}
