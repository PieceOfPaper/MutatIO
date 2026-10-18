using System;

namespace MutatIO.Shared.Bots
{
    /// <summary>
    /// 수준 하나에 해당하는 실제 봇 행동 값입니다. <see cref="BotSettings.low"/>와 <see cref="BotSettings.high"/>를 수준으로 보간합니다.
    /// </summary>
    public readonly struct BotSkill
    {
        private BotSkill(float level, BotSkillProfile low, BotSkillProfile high, float stalkSatietyRatio)
        {
            Level = level;
            StalkSatietyRatio = Clamp01(stalkSatietyRatio);
            DecisionInterval = Math.Max(0.02f, Lerp(low.decisionInterval, high.decisionInterval, level));
            PerceptionRange = Math.Max(0f, Lerp(low.perceptionRange, high.perceptionRange, level));
            HuntRange = Math.Max(0f, Lerp(low.huntRange, high.huntRange, level));
            ThreatAwareness = Clamp01(Lerp(low.threatAwareness, high.threatAwareness, level));
            Focus = Clamp01(Lerp(low.focus, high.focus, level));
            AimError = Math.Max(0f, Lerp(low.aimErrorDegrees, high.aimErrorDegrees, level)) * (float)(Math.PI / 180.0);
            IdleChance = Clamp01(Lerp(low.idleChance, high.idleChance, level));
            DashChance = Clamp01(Lerp(low.dashChance, high.dashChance, level));
        }

        public float Level { get; }

        /// <summary>육식이 따라다닐 굶주린 초식의 포만 비율 기준(BOT-07). 수준과 무관합니다.</summary>
        public float StalkSatietyRatio { get; }

        public float DecisionInterval { get; }

        public float PerceptionRange { get; }

        /// <summary>육식일 때 사냥감을 찾고 계속 쫓는 거리. 몸 가장자리 기준. 월드 단위.</summary>
        public float HuntRange { get; }

        public float ThreatAwareness { get; }

        public float Focus { get; }

        /// <summary>목표 방향의 최대 오차. 라디안.</summary>
        public float AimError { get; }

        /// <summary>판단할 때 멈춰 서 있을 확률. 0~1.</summary>
        public float IdleChance { get; }

        public float DashChance { get; }

        public static BotSkill FromLevel(float level, BotSettings settings)
        {
            return new BotSkill(Clamp01(level), settings.low, settings.high, settings.stalkSatietyRatio);
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
