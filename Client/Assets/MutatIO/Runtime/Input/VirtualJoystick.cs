using UnityEngine;
using UnityEngine.EventSystems;

namespace MutatIO.Input
{
    /// <summary>
    /// 화면 좌측 하단에 고정된 가상 조이스틱입니다. 이 조이스틱을 누른 포인터만 추적하므로
    /// 다른 손가락으로 대시 버튼을 눌러도 이동 입력이 바뀌지 않습니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Input/Virtual Joystick")]
    public sealed class VirtualJoystick : MonoBehaviour,
        IInitializePotentialDragHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private RectTransform handle;

        [Tooltip("(INPUT-01) 손잡이가 움직일 수 있는 반경. 캔버스 단위. 임시값입니다.")]
        [SerializeField, Min(1f)] private float radius = 90f;

        [Tooltip("(INPUT-01) 반경 대비 데드존 비율. 임시값입니다.")]
        [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.15f;

        private int activePointerId = NoPointer;

        /// <summary>현재 이동 입력입니다. 길이는 0~1입니다.</summary>
        public Vector2 Value { get; private set; }

        public void Configure(RectTransform handleTransform, float handleRadius)
        {
            handle = handleTransform;
            radius = handleRadius;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // 드래그 임계값 때문에 첫 몇 픽셀의 움직임이 무시되지 않도록 합니다.
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                UpdateValue(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                Release();
            }
        }

        private void OnDisable()
        {
            Release();
        }

        private void UpdateValue(PointerEventData eventData)
        {
            var area = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var local))
            {
                return;
            }

            var offset = Vector2.ClampMagnitude(local, radius);
            if (handle != null)
            {
                handle.anchoredPosition = offset;
            }

            var normalized = offset / radius;
            var magnitude = normalized.magnitude;
            Value = magnitude <= deadZone
                ? Vector2.zero
                : normalized / magnitude * ((magnitude - deadZone) / (1f - deadZone));
        }

        private void Release()
        {
            activePointerId = NoPointer;
            Value = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
