using MutatIO.Cameras;
using MutatIO.Creatures;
using MutatIO.Feeding;
using MutatIO.Input;
using MutatIO.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MutatIO.Prototype
{
    /// <summary>
    /// 로컬 프로토타입 씬을 런타임에 구성합니다. Shared 월드 시뮬레이션을 실행하고 맵·먹이·내 생명체·조작 UI를 만듭니다.
    /// M2 이동·조작 확인, M3 먹이·성장·카메라 줌 확인에 사용합니다. 첫 화면(M5)이 생기기 전까지의 개발용 구성입니다.
    /// WebGL 기본 폰트에 한글 글리프가 없으므로 화면 문구는 영어로 둡니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Prototype/Prototype Bootstrap")]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float MapBorderThickness = 0.3f;

        [SerializeField] private ControlMode initialMode = ControlMode.PointerFollow;

        private Camera worldCamera;
        private WorldSimulationRunner runner;
        private PlayerCommandSource commandSource;
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
                worldCamera.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            }

            runner = gameObject.AddComponent<WorldSimulationRunner>();
            CreateMap();
            var foodView = new GameObject("FoodField").AddComponent<FoodFieldView>();
            foodView.Bind(runner.Simulation, PrototypeSprites.Circle);
            var creatureObject = CreateCreature();

            EnsureEventSystem();
            var canvas = CreateCanvas();
            var joystick = CreateJoystick(canvas);
            var dashButton = CreateDashButton(canvas);
            CreateControls(canvas);

            commandSource = gameObject.AddComponent<PlayerCommandSource>();
            commandSource.Bind(worldCamera, creatureObject.transform, joystick, dashButton);
            runner.SetCommandSource(commandSource);
            SetMode(initialMode);

            if (worldCamera != null)
            {
                var cameraController = worldCamera.GetComponent<WorldCameraController>();
                if (cameraController == null)
                {
                    cameraController = worldCamera.gameObject.AddComponent<WorldCameraController>();
                }

                cameraController.Follow(runner.LocalPlayer);
            }
        }

        private void Update()
        {
            var player = runner.LocalPlayer;
            var state = player.Movement;
            var dash = state.IsDashing ? "dashing"
                : state.DashCooldownRemaining > 0f ? $"{state.DashCooldownRemaining:0.0}s"
                : "ready";
            statusText.text =
                $"Mode: {commandSource.Mode}\n" +
                $"Score: {player.Intake:0}  Size: {player.Size:0.00}\n" +
                $"Speed: {state.Velocity.Length():0.00}  Dash: {dash}\n" +
                $"Plants: {runner.Simulation.Food.PlantCount}";
        }

        private void SetMode(ControlMode mode)
        {
            commandSource.Mode = mode;
            joystickRoot.SetActive(mode == ControlMode.VirtualPad);
        }

        private void Feed(float intake)
        {
            // 개발용: 먹이를 먹은 것처럼 섭취량을 더해 성장·속도·줌 변화를 빠르게 확인합니다.
            runner.LocalPlayer.AddIntake(intake, runner.Simulation.Settings.growth);
        }

        private void CreateMap()
        {
            var bounds = runner.Simulation.Bounds;
            var size = new Vector2(bounds.HalfWidth * 2f, bounds.HalfHeight * 2f);

            var ground = new GameObject("MapGround").AddComponent<SpriteRenderer>();
            ground.sprite = PrototypeSprites.Square;
            ground.color = new Color(0.08f, 0.1f, 0.13f);
            ground.transform.localScale = new Vector3(size.x, size.y, 1f);
            ground.sortingOrder = -20;

            var grid = new GameObject("MapGrid").AddComponent<SpriteRenderer>();
            grid.sprite = PrototypeSprites.Grid;
            grid.drawMode = SpriteDrawMode.Tiled;
            grid.size = size;
            grid.sortingOrder = -10;

            var borderColor = new Color(0.9f, 0.4f, 0.4f, 0.8f);
            CreateBorder(new Vector2(0f, bounds.HalfHeight), new Vector2(size.x + MapBorderThickness * 2f, MapBorderThickness), borderColor);
            CreateBorder(new Vector2(0f, -bounds.HalfHeight), new Vector2(size.x + MapBorderThickness * 2f, MapBorderThickness), borderColor);
            CreateBorder(new Vector2(bounds.HalfWidth, 0f), new Vector2(MapBorderThickness, size.y), borderColor);
            CreateBorder(new Vector2(-bounds.HalfWidth, 0f), new Vector2(MapBorderThickness, size.y), borderColor);
        }

        private static void CreateBorder(Vector2 center, Vector2 size, Color color)
        {
            var border = new GameObject("MapBorder").AddComponent<SpriteRenderer>();
            border.sprite = PrototypeSprites.Square;
            border.color = color;
            border.transform.position = center;
            border.transform.localScale = new Vector3(size.x, size.y, 1f);
            border.sortingOrder = -5;
        }

        private GameObject CreateCreature()
        {
            var creatureObject = new GameObject("PlayerCreature");
            var renderer = creatureObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSprites.Circle;
            renderer.color = new Color(0.55f, 0.85f, 1f);
            creatureObject.AddComponent<CreatureView>().Bind(runner.LocalPlayer);
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
            var status = CreateRect("Status", canvas, new Vector2(0f, 1f), new Vector2(240f, -95f), new Vector2(440f, 170f));
            statusText = CreateLabel(status, string.Empty, 28);
            statusText.alignment = TextAnchor.UpperLeft;

            var modes = new[] { ControlMode.Wasd, ControlMode.ArrowKeys, ControlMode.VirtualPad, ControlMode.PointerFollow };
            var labels = new[] { "WASD", "Arrows", "Pad", "Pointer" };
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                CreateButton(canvas, labels[i], new Vector2(-90f - (modes.Length - 1 - i) * 170f, -50f), () => SetMode(mode));
            }

            CreateButton(canvas, "Feed +20", new Vector2(-260f, -130f), () => Feed(20f));
            CreateButton(canvas, "Feed +200", new Vector2(-90f, -130f), () => Feed(200f));
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
