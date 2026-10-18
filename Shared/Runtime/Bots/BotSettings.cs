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
        /// <summary>(BOT-01) 월드에 유지할 최소 인원. 접속한 사람 수 + 봇 수가 이 값보다 적으면 봇을 입장시킵니다. 프로토타입 29명(나 + 봇 28).</summary>
        public int minPopulation = 29;

        /// <summary>(BOT-02) 빈자리가 생긴 뒤 봇이 입장하기까지의 최소 지연. 초.</summary>
        public float joinDelayMin = 3f;

        /// <summary>(BOT-02) 빈자리가 생긴 뒤 봇이 입장하기까지의 최대 지연. 초.</summary>
        public float joinDelayMax = 10f;

        /// <summary>
        /// (BOT-05) 봇이 입장하는 맵 중앙 정사각 영역의 절반 크기. 월드 단위. 0 이하이면 맵 전체입니다.
        /// 임시: 스폰 위치 규칙(SPAWN-01)은 미결정입니다. 프로토타입 맵(200×200)이 작아 맵 전체에 입장시킵니다.
        /// </summary>
        public float spawnAreaHalfExtent = 0f;

        /// <summary>
        /// (BOT-06) 육식 지향 봇 수. 이 봇들은 초식일 때 사냥 거리(<see cref="BotSkillProfile.huntRange"/>) 안의 고기를 풀보다 먼저 노려 육식 변이를 노립니다.
        /// 입장할 때 살아 있는 육식 지향 봇이 이 수보다 적으면 새 봇에게 성향을 줍니다. 0이면 없습니다.
        /// 임시: 월드가 평화로워지지 않도록 육식을 공급하는 프로토타입용 장치입니다.
        /// </summary>
        public int meatSeekerCount = 3;

        /// <summary>
        /// (BOT-07) 육식 봇이 따라다닐 "많이 굶주린 생명체(초식·육식)"의 기준. 포만 ÷ 최대 포만이 이 값 이하이면 처치할 수 없는 상대라도
        /// 사냥 거리 안에서 따라다니다가, 굶어 죽어 떨어지는 고기를 먹습니다. 0이면 따라가지 않습니다.
        /// 임시: 프로토타입용 봇 행동입니다.
        /// </summary>
        public float stalkSatietyRatio = 0.25f;

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

        /// <summary>
        /// 육식일 때 몸 가장자리에서 사냥감(처치할 수 있는 개체)을 찾는 거리. 월드 단위.
        /// 먹이·위험 인지 거리보다 넓게 두어 화면에 보이는 정도의 초식을 노리게 합니다. 쫓던 사냥감은 이 거리를 벗어날 때까지 계속 쫓습니다.
        /// </summary>
        public float huntRange;

        /// <summary>판단할 때 범위 안의 위험(나를 처치할 수 있는 육식)을 알아차릴 확률. 0~1.</summary>
        public float threatAwareness;

        /// <summary>판단할 때 먹이·사냥감을 노릴 확률. 노리지 않으면 배회합니다. 0~1.</summary>
        public float focus;

        /// <summary>목표 방향의 최대 오차. 도.</summary>
        public float aimErrorDegrees;

        /// <summary>
        /// 판단할 때 다음 판단까지 멈춰 서 있을 확률. 0~1. 위험 회피·추격 중에는 멈추지 않습니다.
        /// 이동 속도는 모두 같으므로, 수준이 낮은 봇이 먹이를 덜 먹어 자연스럽게 굶게 하는 역할입니다.
        /// </summary>
        public float idleChance;

        /// <summary>도주·추격 중 대시 조건을 만족했을 때 실제로 대시할 확률. 0~1.</summary>
        public float dashChance;

        public static BotSkillProfile CreateLowDefaults()
        {
            return new BotSkillProfile
            {
                decisionInterval = 1.2f,
                perceptionRange = 3f,
                huntRange = 4f,
                threatAwareness = 0f,
                focus = 0.05f,
                aimErrorDegrees = 60f,
                idleChance = 0.6f,
                dashChance = 0f,
            };
        }

        public static BotSkillProfile CreateHighDefaults()
        {
            return new BotSkillProfile
            {
                decisionInterval = 0.15f,
                perceptionRange = 14f,
                huntRange = 24f,
                threatAwareness = 1f,
                focus = 1f,
                aimErrorDegrees = 3f,
                idleChance = 0f,
                dashChance = 0.9f,
            };
        }
    }
}
