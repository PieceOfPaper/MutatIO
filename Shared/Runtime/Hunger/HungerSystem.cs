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
        /// 먹이 종류별 포만 회복량입니다.
        /// 임시: 육식 먹이의 회복량과 초식 상태 효율(FOOD-06)은 M6에서 정하며 현재는 회복하지 않습니다.
        /// </summary>
        public static float GetSatietyGain(FoodKind kind, HungerSettings settings)
        {
            return kind == FoodKind.Plant ? settings.plantSatiety : 0f;
        }
    }
}
