using System;
using MutatIO.Shared.Creatures;
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
        /// 먹이 하나의 포만 회복량 = 먹이·역할별 기본 회복량 × 몸집별 회복 배율입니다. 몸집이 클수록 같은 먹이로 덜 회복합니다.
        /// 초식이 고기를 먹으면 고기 기본 회복량에 초식 효율(FOOD-06)을 곱합니다. 육식은 초식 먹이를 먹지 못하므로 0입니다.
        /// </summary>
        public static float GetSatietyGain(FoodKind kind, CreatureRole role, float size, HungerSettings settings)
        {
            if (!EcologyRules.CanEat(role, kind))
            {
                return 0f;
            }

            var baseGain = kind == FoodKind.Plant
                ? settings.plantSatiety
                : settings.meatSatiety * (role == CreatureRole.Herbivore ? Math.Max(0f, settings.herbivoreMeatEfficiency) : 1f);
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
