using System;
using MutatIO.Shared.Feeding;

namespace MutatIO.Shared.Hunger
{
    /// <summary>
    /// 포만 감소와 회복을 계산합니다. 굶주림은 점수·몸집을 깎지 않으며, 포만이 0이 되면 사망합니다.
    /// </summary>
    public static class HungerSystem
    {
        public static float Drain(float satiety, HungerSettings settings, float deltaTime)
        {
            return Math.Max(0f, satiety - Math.Max(0f, settings.decayPerSecond) * deltaTime);
        }

        public static float Restore(float satiety, float amount, HungerSettings settings)
        {
            return Math.Min(settings.maxSatiety, satiety + Math.Max(0f, amount));
        }

        /// <summary>
        /// 먹이 하나의 포만 회복량 = 먹이 종류별 기본 회복량 × 몸집별 회복 배율입니다. 몸집이 클수록 같은 먹이로 덜 회복합니다.
        /// 임시: 육식 먹이의 회복량과 초식 상태 효율(FOOD-06)은 M6에서 정하며 현재는 회복하지 않습니다.
        /// </summary>
        public static float GetSatietyGain(FoodKind kind, float size, HungerSettings settings)
        {
            var baseGain = kind == FoodKind.Plant ? settings.plantSatiety : 0f;
            return baseGain * GetSizeMultiplier(size, settings);
        }

        /// <summary>(HUNGER-05, HUNGER-06) 몸집별 먹이당 포만 회복 배율. 초기 몸집(1)에서 1이며 하한 아래로 내려가지 않습니다.</summary>
        public static float GetSizeMultiplier(float size, HungerSettings settings)
        {
            var multiplier = (float)Math.Pow(Math.Max(1f, size), -Math.Max(0f, settings.sizeSatietyExponent));
            return Math.Max(multiplier, Math.Min(1f, Math.Max(0f, settings.minSatietyMultiplier)));
        }
    }
}
