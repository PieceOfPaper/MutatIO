using System;

namespace MutatIO.Shared.Movement
{
    /// <summary>
    /// 이동·관성·대시 밸런스 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값은 플레이 테스트 전 임시값입니다. 확정 수치가 아닙니다.
    /// Unity 인스펙터에서 조정할 수 있도록 public 필드와 [Serializable]을 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class MovementSettings
    {
        /// <summary>(MOVE-01) 최고 이동속도. 월드 단위/초. 몸집과 무관하게 같습니다(몸집별 감속 MOVE-02·03 제거).</summary>
        public float baseSpeed = 5f;

        /// <summary>(MOVE-04, MOVE-06) 정지 상태에서 최고 속도에 도달하는 시간. 방향 전환도 같은 응답을 사용합니다. 초.</summary>
        public float accelerationTime = 0.12f;

        /// <summary>(MOVE-05) 입력을 놓았을 때 최고 속도에서 정지까지 걸리는 시간. 초.</summary>
        public float decelerationTime = 0.2f;

        /// <summary>
        /// 이 길이 이하의 이동 입력은 정지로 보고 마지막 유효 방향을 갱신하지 않습니다.
        /// 임시: 정지 판정 임계값은 구현 전 결정 사항이며 현재 값은 임시값입니다.
        /// </summary>
        public float minMoveInput = 0.05f;

        /// <summary>(DASH-01) 대시 중 속도 배율. 최고 속도에 곱합니다.</summary>
        public float dashSpeedMultiplier = 3f;

        /// <summary>(DASH-02) 대시 지속시간. 초.</summary>
        public float dashDuration = 0.18f;

        /// <summary>(DASH-03) 대시가 끝난 뒤 다시 쓸 수 있을 때까지의 대기시간. 초.</summary>
        public float dashCooldown = 1.5f;

        // DASH-04 대시 비용: 임시안으로 비용 없음. 비용 자원이 결정되면 추가합니다.

        public static MovementSettings CreateTemporaryDefaults()
        {
            return new MovementSettings();
        }
    }
}
