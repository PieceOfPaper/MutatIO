using System;

namespace MutatIO.Shared.Growth
{
    /// <summary>
    /// 점수(섭취 완료한 먹이 개수)에서 몸집을 계산합니다. 점수와 독립된 질량 자원은 두지 않습니다.
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
