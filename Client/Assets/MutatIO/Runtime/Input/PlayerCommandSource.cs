using MutatIO.Core;
using MutatIO.Shared.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MutatIO.Input
{
    /// <summary>
    /// 선택한 조작 방식의 장치 입력을 장치 독립 <see cref="PlayerCommand"/>로 변환합니다.
    /// 네 조작 방식 모두 같은 명령을 만들며, 이동·대시 규칙은 명령만 소비합니다.
    /// 소비자는 프레임마다 한 번 <see cref="ReadCommand"/>를 호출합니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Input/Player Command Source")]
    public sealed class PlayerCommandSource : MonoBehaviour
    {
        // 터치 후 마우스 이동을 다시 마우스 조작으로 볼 때까지의 시간. 터치가 흉내 낸 마우스 이벤트를 걸러냅니다.
        private const float TouchToMouseSwitchDelay = 0.5f;
        private const int NoTouch = -1;

        [SerializeField] private ControlMode mode = ControlMode.PointerFollow;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform character;
        [SerializeField] private VirtualJoystick virtualJoystick;
        [SerializeField] private DashButton dashButton;

        [Tooltip("(INPUT-02) 포인터 중심 데드존. 화면 높이 대비 비율입니다. 임시값입니다.")]
        [SerializeField, Range(0f, 0.5f)] private float pointerDeadZone = 0.03f;

        private InputAction wasdMove;
        private InputAction arrowMove;
        private InputAction keyboardDash;
        private InputAction mouseDash;

        private Vector2 lastMousePointerMove;
        private bool usingTouch;
        private float lastTouchTime;
        private int trackedTouchId = NoTouch;

        public ControlMode Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                {
                    return;
                }

                mode = value;
                ResetPointerState();
            }
        }

        public void Bind(Camera camera, Transform characterTransform, VirtualJoystick joystick, DashButton button)
        {
            worldCamera = camera;
            character = characterTransform;
            virtualJoystick = joystick;
            dashButton = button;
        }

        /// <summary>포인터 방향의 기준이 되는 내 생명체 표시를 바꿉니다. 새로 입장할 때 사용합니다.</summary>
        public void SetCharacter(Transform characterTransform)
        {
            character = characterTransform;
            ResetPointerState();
        }

        public PlayerCommand ReadCommand()
        {
            var move = ReadMove();
            var dash = ReadDash();
            return new PlayerCommand(move.ToNumerics(), dash);
        }

        private void Awake()
        {
            wasdMove = new InputAction("MoveWasd", InputActionType.Value);
            wasdMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            arrowMove = new InputAction("MoveArrows", InputActionType.Value);
            arrowMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            keyboardDash = new InputAction("DashKeyboard", InputActionType.Button, "<Keyboard>/space");
            mouseDash = new InputAction("DashMouse", InputActionType.Button, "<Mouse>/leftButton");
        }

        private void OnEnable()
        {
            wasdMove.Enable();
            arrowMove.Enable();
            keyboardDash.Enable();
            mouseDash.Enable();
        }

        private void OnDisable()
        {
            wasdMove.Disable();
            arrowMove.Disable();
            keyboardDash.Disable();
            mouseDash.Disable();
            ResetPointerState();
        }

        private void OnDestroy()
        {
            wasdMove.Dispose();
            arrowMove.Dispose();
            keyboardDash.Dispose();
            mouseDash.Dispose();
        }

        private Vector2 ReadMove()
        {
            switch (mode)
            {
                case ControlMode.Wasd:
                    return wasdMove.ReadValue<Vector2>();
                case ControlMode.ArrowKeys:
                    return arrowMove.ReadValue<Vector2>();
                case ControlMode.VirtualPad:
                    return virtualJoystick != null ? virtualJoystick.Value : Vector2.zero;
                case ControlMode.PointerFollow:
                    return ReadPointerMove();
                default:
                    return Vector2.zero;
            }
        }

        private bool ReadDash()
        {
            // 버튼 기록은 모드와 관계없이 매 프레임 비워 이전 누름이 나중에 쓰이지 않게 합니다.
            var buttonPressed = dashButton != null && dashButton.ConsumePress();
            var keyPressed = keyboardDash.WasPressedThisFrame();

            // 마우스 추종 모드의 월드 좌클릭 대시. 누른 순간 한 번만 사용합니다(임시: 누르고 있을 때 반복 여부는 미결정).
            var clickPressed = mode == ControlMode.PointerFollow
                && !usingTouch
                && mouseDash.WasPressedThisFrame()
                && Mouse.current != null
                && !UiPointerFilter.IsOverUi(Mouse.current.position.ReadValue());

            return buttonPressed || keyPressed || clickPressed;
        }

        private Vector2 ReadPointerMove()
        {
            var touchMove = ReadTouchMove();
            if (usingTouch)
            {
                return touchMove;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return Vector2.zero;
            }

            var position = mouse.position.ReadValue();
            if (UiPointerFilter.IsOverUi(position))
            {
                // 임시: UI 위에서 기존 방향을 유지할지 감속할지는 미결정입니다. 현재는 새 방향을 만들지 않고 기존 방향을 유지합니다.
                return lastMousePointerMove;
            }

            lastMousePointerMove = GetPointerMoveInput(position);
            return lastMousePointerMove;
        }

        /// <summary>
        /// 모바일 터치 추종. 테스트 제안(미승인): 월드 영역을 누르는 동안 캐릭터에서 터치 방향으로 이동하고 손을 떼면 입력 0입니다.
        /// UI 위에서 시작한 터치는 이동에 쓰지 않습니다.
        /// </summary>
        private Vector2 ReadTouchMove()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return Vector2.zero;
            }

            var anyPressed = false;
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                anyPressed = true;
                if (trackedTouchId == NoTouch && touch.press.wasPressedThisFrame
                    && !UiPointerFilter.IsOverUi(touch.position.ReadValue()))
                {
                    trackedTouchId = touch.touchId.ReadValue();
                }
            }

            UpdateTouchMouseSwitch(anyPressed);

            if (trackedTouchId == NoTouch)
            {
                return Vector2.zero;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (touch.touchId.ReadValue() == trackedTouchId && touch.press.isPressed)
                {
                    return GetPointerMoveInput(touch.position.ReadValue());
                }
            }

            trackedTouchId = NoTouch;
            return Vector2.zero;
        }

        private void UpdateTouchMouseSwitch(bool anyTouchPressed)
        {
            if (anyTouchPressed)
            {
                usingTouch = true;
                lastTouchTime = Time.unscaledTime;
                return;
            }

            var mouse = Mouse.current;
            if (usingTouch && mouse != null
                && mouse.delta.ReadValue().sqrMagnitude > 0f
                && Time.unscaledTime - lastTouchTime > TouchToMouseSwitchDelay)
            {
                usingTouch = false;
            }
        }

        private Vector2 GetPointerMoveInput(Vector2 pointerScreen)
        {
            if (worldCamera == null || character == null)
            {
                return Vector2.zero;
            }

            Vector2 characterScreen = worldCamera.WorldToScreenPoint(character.position);
            return PointerFollow.GetMoveInput(pointerScreen, characterScreen, pointerDeadZone * Screen.height);
        }

        private void ResetPointerState()
        {
            lastMousePointerMove = Vector2.zero;
            trackedTouchId = NoTouch;
        }
    }
}
