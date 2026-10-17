using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MutatIO.Input
{
    /// <summary>
    /// 화면 좌표가 UI 위인지 판정합니다. UI 위의 포인터는 월드 이동 방향이나 월드 대시로 쓰지 않습니다.
    /// 입력 처리 순서에 영향을 받지 않도록 EventSystem의 마지막 상태 대신 매번 직접 레이캐스트합니다.
    /// </summary>
    public static class UiPointerFilter
    {
        private static readonly List<RaycastResult> results = new List<RaycastResult>();
        private static EventSystem cachedEventSystem;
        private static PointerEventData pointerData;

        public static bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (pointerData == null || cachedEventSystem != eventSystem)
            {
                cachedEventSystem = eventSystem;
                pointerData = new PointerEventData(eventSystem);
            }

            pointerData.position = screenPosition;
            results.Clear();
            eventSystem.RaycastAll(pointerData, results);
            return results.Count > 0;
        }
    }
}
