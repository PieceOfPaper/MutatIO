using System;

namespace MutatIO.Shared.Mutation
{
    /// <summary>
    /// 초식의 변이 수치 계산입니다. 수치는 고기를 먹을 때 오르고 시간에 따라 줄며, 임계치에 닿으면 육식으로 전환합니다.
    /// 임계치는 몸집이 클수록 높아집니다. 몸집이 바뀌어 임계치가 달라져도 누적 수치는 초기화하지 않습니다.
    /// </summary>
    public static class MutationSystem
    {
        /// <summary>(MUTATE-01, MUTATE-04) 변이 임계치 = 기본 임계치 × 몸집^지수. 몸집은 초기 1 기준입니다.</summary>
        public static float GetThreshold(float size, MutationSettings settings)
        {
            var multiplier = (float)Math.Pow(Math.Max(1f, size), Math.Max(0f, settings.sizeThresholdExponent));
            return Math.Max(0f, settings.baseThreshold) * multiplier;
        }

        public static float Decay(float mutation, MutationSettings settings, float deltaTime)
        {
            return Math.Max(0f, mutation - Math.Max(0f, settings.decayPerSecond) * deltaTime);
        }
    }
}
