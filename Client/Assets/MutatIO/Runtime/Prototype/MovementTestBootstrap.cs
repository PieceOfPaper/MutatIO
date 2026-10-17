using MutatIO.Creatures;
using MutatIO.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MutatIO.Prototype
{
    /// <summary>
    /// 이동·입력 확인용 테스트 씬(M2)을 런타임에 구성합니다. 원 하나를 네 가지 조작으로 움직이고 대시해 볼 수 있습니다.
    /// 화면 버튼으로 조작 방식과 몸집을 바꿉니다. 첫 화면(M5)이 생기기 전까지의 개발용 구성입니다.
    /// WebGL 기본 폰트에 한글 글리프가 없으므로 화면 문구는 영어로 둡니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Prototype/Movement Test Bootstrap")]
    public sealed class MovementTestBootstrap : MonoBehaviour
    {
        private const float SizeStep = 0.5f;
        private const float MaxTestSize = 20f;

        [SerializeField] private ControlMode initialMode = ControlMode.PointerFollow;
        [SerializeField] private float cameraOrthographicSize = 8f;
        [SerializeField] private Vector2 gridSize = new Vector2(120f, 120f);

        private Camera worldCamera;
        private Transform grid;
        private PlayerCommandSource commandSource;
        private LocalCreatureMover creature;
        private GameObject joystickRoot;
        private Text statusText;
        private Font font;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            worldCamera = Camera.main;
            if (worldCamera != null)
            {
                worldCamera.orthographic = true;
                worldCamera.orthographicSize = cameraOrthographicSize;
                worldCamera.backgroundColor = new Color(0.08f, 0.1f, 0.13f);
            }

            CreateGrid();
            var creatureObject = CreateCreature();
            EnsureEventSystem();
            var canvas = CreateCanvas();
            var joystick = CreateJoystick(canvas);
            var dashButton = CreateDashButton(canvas);
            CreateControls(canvas);

            commandSource = gameObject.AddComponent<PlayerCommandSource>();
            commandSource.Bind(worldCamera, creatureObject.transform, joystick, dashButton);
            creature.Bind(commandSource);
            SetMode(initialMode);
        }

        private void Update()
        {
            var state = creature.State;
            var dash = state.IsDashing ? "dashing"
                : state.DashCooldownRemaining > 0f ? $"{state.DashCooldownRemaining:0.0}s"
                : "ready";
            statusText.text =
                $"Mode: {commandSource.Mode}\n" +
                $"Speed: {state.Velocity.Length():0.00}  Size: {creature.Size:0.0}\n" +
                $"Dash: {dash}";
        }

        private void LateUpdate()
        {
            if (worldCamera != null)
            {
                var target = creature.transform.position;
                worldCamera.transform.position = new Vector3(target.x, target.y, worldCamera.transform.position.z);
            }

            // 맵 경계(M3) 전까지는 격자가 칸 단위로 카메라를 따라가 어디서든 이동이 보이게 합니다.
            var cell = PrototypeSprites.GridCellWorldSize;
            var center = creature.transform.position;
            grid.position = new Vector3(Mathf.Round(center.x / cell) * cell, Mathf.Round(center.y / cell) * cell, 0f);
        }

        private void SetMode(ControlMode mode)
        {
            commandSource.Mode = mode;
            joystickRoot.SetActive(mode == ControlMode.VirtualPad);
        }

        private void ChangeSize(float delta)
        {
            creature.Size = Mathf.Min(MaxTestSize, creature.Size + delta);
        }

        private void CreateGrid()
        {
            var gridObject = new GameObject("Grid");
            var renderer = gridObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSprites.Grid;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = gridSize;
            renderer.sortingOrder = -10;
            grid = gridObject.transform;
        }

        private GameObject CreateCreature()
        {
            var creatureObject = new GameObject("PlayerCreature");
            var renderer = creatureObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSprites.Circle;
            renderer.color = new Color(0.55f, 0.85f, 1f);
            creature = creatureObject.AddComponent<LocalCreatureMover>();
            return creatureObject;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("TestCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return (RectTransform)canvasObject.transform;
        }

        private VirtualJoystick CreateJoystick(RectTransform canvas)
        {
            const float baseDiameter = 240f;
            const float handleDiameter = 110f;

            var area = CreateRect("VirtualJoystick", canvas, new Vector2(0f, 0f), new Vector2(200f, 200f), new Vector2(baseDiameter, baseDiameter));
            AddCircleImage(area, new Color(1f, 1f, 1f, 0.15f), true);
            var handle = CreateRect("Handle", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(handleDiameter, handleDiameter));
            AddCircleImage(handle, new Color(1f, 1f, 1f, 0.5f), false);

            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(handle, (baseDiameter - handleDiameter) * 0.5f);
            joystickRoot = area.gameObject;
            return joystick;
        }

        private DashButton CreateDashButton(RectTransform canvas)
        {
            var area = CreateRect("DashButton", canvas, new Vector2(1f, 0f), new Vector2(-200f, 200f), new Vector2(180f, 180f));
            AddCircleImage(area, new Color(1f, 0.6f, 0.3f, 0.35f), true);
            CreateLabel(area, "DASH", 32);
            return area.gameObject.AddComponent<DashButton>();
        }

        private void CreateControls(RectTransform canvas)
        {
            var status = CreateRect("Status", canvas, new Vector2(0f, 1f), new Vector2(240f, -80f), new Vector2(440f, 130f));
            statusText = CreateLabel(status, string.Empty, 28);
            statusText.alignment = TextAnchor.UpperLeft;

            var modes = new[] { ControlMode.Wasd, ControlMode.ArrowKeys, ControlMode.VirtualPad, ControlMode.PointerFollow };
            var labels = new[] { "WASD", "Arrows", "Pad", "Pointer" };
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                CreateButton(canvas, labels[i], new Vector2(-90f - (modes.Length - 1 - i) * 170f, -50f), () => SetMode(mode));
            }

            CreateButton(canvas, "Size -", new Vector2(-260f, -130f), () => ChangeSize(-SizeStep));
            CreateButton(canvas, "Size +", new Vector2(-90f, -130f), () => ChangeSize(SizeStep));
        }

        private void CreateButton(RectTransform canvas, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var rect = CreateRect(label, canvas, new Vector2(1f, 1f), position, new Vector2(160f, 64f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.2f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            CreateLabel(rect, label, 26);
        }

        private Text CreateLabel(RectTransform parent, string text, int fontSize)
        {
            var rect = CreateRect("Label", parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.text = text;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)rectObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AddCircleImage(RectTransform rect, Color color, bool raycastTarget)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = PrototypeSprites.Circle;
            image.color = color;
            image.raycastTarget = raycastTarget;
        }
    }
}
