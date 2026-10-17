using UnityEngine;

namespace MutatIO.Input
{
    /// <summary>
    /// 화면상 캐릭터 위치에서 포인터 방향으로의 이동 입력을 계산합니다.
    /// 매 프레임 현재 화면 좌표로 다시 계산하므로 고정된 월드 목적지를 저장하지 않습니다.
    /// </summary>
    public static class PointerFollow
    {
        /// <summary>
        /// 데드존 밖이면 길이 1의 방향을, 안이면 0을 반환합니다.
        /// 임시: 캐릭터 중심 근처 처리는 미결정이며 현재는 데드존 안에서 입력 0입니다.
        /// </summary>
        public static Vector2 GetMoveInput(Vector2 pointerScreen, Vector2 characterScreen, float deadZonePixels)
        {
            var offset = pointerScreen - characterScreen;
            var distance = offset.magnitude;
            if (distance <= deadZonePixels || distance <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            return offset / distance;
        }
    }
}
