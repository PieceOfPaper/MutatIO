using System;

namespace MutatIO.Shared.Bots
{
    /// <summary>
    /// 수준 하나에 해당하는 실제 봇 행동 값입니다. <see cref="BotSettings.low"/>와 <see cref="BotSettings.high"/>를 수준으로 보간합니다.
    /// </summary>
    public readonly struct BotSkill
    {
        private BotSkill(float level, BotSkillProfile low, BotSkillProfile high)
        {
            Level = level;
            DecisionInterval = Math.Max(0.02f, Lerp(low.decisionInterval, high.decisionInterval, level));
            PerceptionRange = Math.Max(0f, Lerp(low.perceptionRange, high.perceptionRange, level));
            ThreatAwareness = Clamp01(Lerp(low.threatAwareness, high.threatAwareness, level));
            Focus = Clamp01(Lerp(low.focus, high.focus, level));
            AimError = Math.Max(0f, Lerp(low.aimErrorDegrees, high.aimErrorDegrees, level)) * (float)(Math.PI / 180.0);
            Pace = Clamp01(Lerp(low.pace, high.pace, level));
            DashChance = Clamp01(Lerp(low.dashChance, high.dashChance, level));
        }

        public float Level { get; }

        public float DecisionInterval { get; }

        public float PerceptionRange { get; }

        public float ThreatAwareness { get; }

        public float Focus { get; }

        /// <summary>목표 방향의 최대 오차. 라디안.</summary>
        public float AimError { get; }

        /// <summary>이동 입력의 세기. 0~1.</summary>
        public float Pace { get; }

        public float DashChance { get; }

        public static BotSkill FromLevel(float level, BotSettings settings)
        {
            return new BotSkill(Clamp01(level), settings.low, settings.high);
        }

        internal static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
