using System;

namespace MutatIO.Shared.Bots
{
    /// <summary>
    /// 봇 인원·재입장·수준 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class BotSettings
    {
        /// <summary>(BOT-01) 월드에 유지할 최소 인원. 접속한 사람 수 + 봇 수가 이 값보다 적으면 봇을 입장시킵니다. 프로토타입 5명.</summary>
        public int minPopulation = 5;

        /// <summary>(BOT-02) 빈자리가 생긴 뒤 봇이 입장하기까지의 최소 지연. 초.</summary>
        public float joinDelayMin = 3f;

        /// <summary>(BOT-02) 빈자리가 생긴 뒤 봇이 입장하기까지의 최대 지연. 초.</summary>
        public float joinDelayMax = 10f;

        /// <summary>
        /// (BOT-05) 봇이 입장하는 맵 중앙 정사각 영역의 절반 크기. 월드 단위. 0 이하이면 맵 전체입니다.
        /// 임시: 스폰 위치 규칙(SPAWN-01)은 미결정입니다. 1000×1000 맵에서 5명이 서로 만나 보도록 중앙 근처에 입장시킵니다.
        /// </summary>
        public float spawnAreaHalfExtent = 40f;

        /// <summary>
        /// (BOT-03) 봇 수준. 0(먹이를 거의 찾지 않고 위험도 못 보는 수준) ~ 1(가장 영리한 수준).
        /// 봇이 필요 없어지면 낮춰서 봇이 자연스럽게 죽어 사라지게 합니다.
        /// </summary>
        public float skillLevel = 0.6f;

        /// <summary>(BOT-03) 봇마다 수준에 더하는 무작위 편차의 최대 크기. 모두 같은 움직임이 되지 않게 합니다.</summary>
        public float skillVariance = 0.15f;

        /// <summary>(BOT-04) 수준 0의 행동 값. 실제 값은 수준에 따라 low와 high 사이를 보간합니다.</summary>
        public BotSkillProfile low = BotSkillProfile.CreateLowDefaults();

        /// <summary>(BOT-04) 수준 1의 행동 값.</summary>
        public BotSkillProfile high = BotSkillProfile.CreateHighDefaults();
    }

    /// <summary>
    /// 한 수준에서의 봇 행동 값입니다(BOT-04). 임시: 모두 임시값입니다.
    /// </summary>
    [Serializable]
    public sealed class BotSkillProfile
    {
        /// <summary>판단 주기. 초. 클수록 반응이 늦습니다.</summary>
        public float decisionInterval;

        /// <summary>몸 가장자리에서 먹이·다른 생명체를 알아차리는 거리. 월드 단위.</summary>
        public float perceptionRange;

        /// <summary>판단할 때 범위 안의 위험(나를 처치할 수 있는 육식)을 알아차릴 확률. 0~1.</summary>
        public float threatAwareness;

        /// <summary>판단할 때 먹이·사냥감을 노릴 확률. 노리지 않으면 배회합니다. 0~1.</summary>
        public float focus;

        /// <summary>목표 방향의 최대 오차. 도.</summary>
        public float aimErrorDegrees;

        /// <summary>이동 입력의 세기. 0~1. 낮으면 느리게 움직여 지나가며 흡수하는 먹이도 줄어듭니다.</summary>
        public float pace;

        /// <summary>도주·추격 중 대시 조건을 만족했을 때 실제로 대시할 확률. 0~1.</summary>
        public float dashChance;

        public static BotSkillProfile CreateLowDefaults()
        {
            return new BotSkillProfile
            {
                decisionInterval = 1.2f,
                perceptionRange = 3f,
                threatAwareness = 0f,
                focus = 0.05f,
                aimErrorDegrees = 60f,
                pace = 0.15f,
                dashChance = 0f,
            };
        }

        public static BotSkillProfile CreateHighDefaults()
        {
            return new BotSkillProfile
            {
                decisionInterval = 0.15f,
                perceptionRange = 14f,
                threatAwareness = 1f,
                focus = 1f,
                aimErrorDegrees = 3f,
                pace = 1f,
                dashChance = 0.9f,
            };
        }
    }
}
