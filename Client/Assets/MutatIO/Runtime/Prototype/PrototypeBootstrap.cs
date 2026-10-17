using MutatIO.Cameras;
using MutatIO.Creatures;
using MutatIO.Feeding;
using MutatIO.Flow;
using MutatIO.Input;
using MutatIO.Shared.Creatures;
using MutatIO.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MutatIO.Prototype
{
    /// <summary>
    /// 로컬 프로토타입 씬을 런타임에 구성합니다. Shared 월드 시뮬레이션을 실행하고 맵·먹이·내 생명체·조작 UI를 만듭니다.
    /// M2 이동·조작 확인, M3 먹이·성장·카메라 줌 확인, M4 허기·사망·결과·첫 화면 복귀 확인에 사용합니다.
    /// 임시: 첫 화면은 입장 버튼만 있는 자리표시이며 외형·닉네임·조작 선택은 M5에서 만듭니다.
    /// WebGL 기본 폰트에 한글 글리프가 없으므로 화면 문구는 영어로 둡니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Prototype/Prototype Bootstrap")]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float MapBorderThickness = 0.3f;

        [SerializeField] private ControlMode initialMode = ControlMode.PointerFollow;

        private readonly GameFlow flow = new GameFlow();

        private Camera worldCamera;
        private WorldCameraController cameraController;
        private WorldSimulationRunner runner;
        private PlayerCommandSource commandSource;
        private GameObject creatureObject;
        private GameObject gameplayRoot;
        private GameObject joystickRoot;
        private GameObject titlePanel;
        private GameObject resultPanel;
        private Text resultText;
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
            runner.LocalPlayerDied += OnLocalPlayerDied;

            EnsureEventSystem();
            var canvas = CreateCanvas();
            gameplayRoot = CreateStretchRect("Gameplay", canvas).gameObject;
            var gameplay = (RectTransform)gameplayRoot.transform;
            var joystick = CreateJoystick(gameplay);
            var dashButton = CreateDashButton(gameplay);
            CreateControls(canvas, gameplay);
            CreateTitlePanel(canvas);
            CreateResultPanel(canvas);

            commandSource = gameObject.AddComponent<PlayerCommandSource>();
            commandSource.Bind(worldCamera, null, joystick, dashButton);
            runner.SetCommandSource(commandSource);
            SetMode(initialMode);

            if (worldCamera != null)
            {
                cameraController = worldCamera.GetComponent<WorldCameraController>();
                if (cameraController == null)
                {
                    cameraController = worldCamera.gameObject.AddComponent<WorldCameraController>();
                }
            }

            flow.StateChanged += ApplyState;
            ApplyState(flow.State);
        }

        private void OnDestroy()
        {
            flow.StateChanged -= ApplyState;
            if (runner != null)
            {
                runner.LocalPlayerDied -= OnLocalPlayerDied;
            }
        }

        private void Update()
        {
            var player = runner.LocalPlayer;
            if (player == null)
            {
                return;
            }

            var hunger = runner.Simulation.Settings.hunger;
            var state = player.Movement;
            var dash = state.IsDashing ? "dashing"
                : state.DashCooldownRemaining > 0f ? $"{state.DashCooldownRemaining:0.0}s"
                : "ready";
            statusText.text =
                $"Mode: {commandSource.Mode}\n" +
                $"Score: {player.Intake:0}  Size: {player.Size:0.00}\n" +
                $"Satiety: {player.Satiety:0.0} / {hunger.maxSatiety:0}\n" +
                $"Speed: {state.Velocity.Length():0.00}  Dash: {dash}\n" +
                $"Plants: {runner.Simulation.Food.PlantCount}";
        }

        private void SetMode(ControlMode mode)
        {
            commandSource.Mode = mode;
            joystickRoot.SetActive(mode == ControlMode.VirtualPad);
        }

        private void ApplyState(GameFlowState state)
        {
            gameplayRoot.SetActive(state == GameFlowState.Playing);
            titlePanel.SetActive(state == GameFlowState.Title);
            resultPanel.SetActive(state == GameFlowState.Result);
            if (state == GameFlowState.Result)
            {
                resultText.text = $"GAME OVER\n\nScore: {flow.LastResult.Score:0}";
            }
        }

        private void EnterWorld()
        {
            if (!flow.Enter())
            {
                return;
            }

            var player = runner.SpawnLocalPlayer();
            creatureObject = CreateCreature(player);
            commandSource.SetCharacter(creatureObject.transform);
            if (cameraController != null)
            {
                cameraController.Follow(player);
            }
        }

        private void OnLocalPlayerDied(CreatureDeath death)
        {
            // 카메라는 사망 위치에 멈추고, 월드는 결과 화면 뒤에서 계속 진행합니다.
            if (cameraController != null)
            {
                cameraController.Follow(null);
            }

            commandSource.SetCharacter(null);
            if (creatureObject != null)
            {
                Destroy(creatureObject);
                creatureObject = null;
            }

            flow.EndPlay(new PlayResult(death.Creature.Intake));
        }

        private void Feed(float intake)
        {
            // 개발용: 먹이를 먹은 것처럼 섭취량을 더해 성장·속도·줌 변화를 빠르게 확인합니다.
            runner.LocalPlayer?.AddIntake(intake, runner.Simulation.Settings.growth);
        }

        private void Starve()
        {
            // 개발용: 포만을 거의 비워 굶주림 사망과 결과 화면을 빠르게 확인합니다.
            runner.LocalPlayer?.SetSatiety(1f);
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

        private static GameObject CreateCreature(Creature creature)
        {
            var creatureObject = new GameObject("PlayerCreature");
            var renderer = creatureObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSprites.Circle;
            renderer.color = new Color(0.55f, 0.85f, 1f);
            creatureObject.AddComponent<CreatureView>().Bind(creature);
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

        private void CreateControls(RectTransform canvas, RectTransform gameplay)
        {
            var status = CreateRect("Status", gameplay, new Vector2(0f, 1f), new Vector2(240f, -115f), new Vector2(440f, 210f));
            statusText = CreateLabel(status, string.Empty, 28);
            statusText.alignment = TextAnchor.UpperLeft;

            var modes = new[] { ControlMode.Wasd, ControlMode.ArrowKeys, ControlMode.VirtualPad, ControlMode.PointerFollow };
            var labels = new[] { "WASD", "Arrows", "Pad", "Pointer" };
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                CreateButton(canvas, labels[i], new Vector2(1f, 1f), new Vector2(-90f - (modes.Length - 1 - i) * 170f, -50f), () => SetMode(mode));
            }

            CreateButton(gameplay, "Feed +20", new Vector2(1f, 1f), new Vector2(-430f, -130f), () => Feed(20f));
            CreateButton(gameplay, "Feed +200", new Vector2(1f, 1f), new Vector2(-260f, -130f), () => Feed(200f));
            CreateButton(gameplay, "Starve", new Vector2(1f, 1f), new Vector2(-90f, -130f), Starve);
        }

        private void CreateTitlePanel(RectTransform canvas)
        {
            var panel = CreatePanel("TitlePanel", canvas);
            var title = CreateRect("Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(560f, 120f));
            CreateLabel(title, "MutatIO", 72);
            CreateButton(panel, "PLAY", new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), EnterWorld);
            titlePanel = panel.gameObject;
        }

        private void CreateResultPanel(RectTransform canvas)
        {
            var panel = CreatePanel("ResultPanel", canvas);
            var text = CreateRect("ResultText", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(560f, 200f));
            resultText = CreateLabel(text, string.Empty, 44);
            CreateButton(panel, "OK", new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), () => flow.ConfirmResult());
            resultPanel = panel.gameObject;
        }

        /// <summary>화면 중앙의 불투명 패널입니다. 패널 영역 클릭이 월드 대시로 쓰이지 않도록 레이캐스트를 받습니다.</summary>
        private static RectTransform CreatePanel(string name, RectTransform canvas)
        {
            var panel = CreateRect(name, canvas, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 420f));
            var image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);
            return panel;
        }

        private void CreateButton(RectTransform parent, string label, Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var rect = CreateRect(label, parent, anchor, position, new Vector2(160f, 64f));
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

        private static RectTransform CreateStretchRect(string name, RectTransform parent)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one;
            return rect;
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
