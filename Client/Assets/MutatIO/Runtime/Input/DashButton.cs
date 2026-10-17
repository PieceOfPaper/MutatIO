using UnityEngine;
using UnityEngine.EventSystems;

namespace MutatIO.Input
{
    /// <summary>
    /// 화면 내 대시 버튼입니다. 누른 순간을 한 번의 대시 입력으로 기록하고, 이 버튼을 누른 포인터는 이동 입력에 쓰이지 않습니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Input/Dash Button")]
    public sealed class DashButton : MonoBehaviour, IPointerDownHandler
    {
        private bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        /// <summary>마지막 확인 이후 버튼이 눌렸으면 true를 반환하고 기록을 지웁니다.</summary>
        public bool ConsumePress()
        {
            var result = pressed;
            pressed = false;
            return result;
        }

        private void OnDisable()
        {
            pressed = false;
        }
    }
}
