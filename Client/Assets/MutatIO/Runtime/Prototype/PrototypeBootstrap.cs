using System.Collections.Generic;
using MutatIO.Cameras;
using MutatIO.Creatures;
using MutatIO.Feeding;
using MutatIO.Flow;
using MutatIO.Input;
using MutatIO.Profile;
using MutatIO.Shared.Bots;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Mutation;
using MutatIO.UI;
using MutatIO.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MutatIO.Prototype
{
    /// <summary>
    /// 로컬 프로토타입 씬을 런타임에 구성합니다. Shared 월드 시뮬레이션을 실행하고 맵·먹이·내 생명체·조작 UI를 만듭니다.
    /// M2 이동·조작 확인, M3 먹이·성장·카메라 줌 확인, M4 허기·사망·결과·첫 화면 복귀 확인, M5 첫 화면 선택·저장 확인,
    /// M6 전 봇 플레이어 확인, M6 고기 드롭·변이·접촉 처치 확인, M7 HUD 확인에 사용합니다.
    /// WebGL 기본 폰트에 한글 글리프가 없으므로 화면 문구는 영어로 둡니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Prototype/Prototype Bootstrap")]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float MapBorderThickness = 0.3f;
        private const float BotSkillStep = 0.2f;

        // 임시 팔레트 기본안(docs/DECISIONS.md): 초식 #4CC9F0, 월드 바탕 #17212B. 월드 밖은 바탕과 구분되는 더 어두운 색입니다.
        private static readonly Color HerbivoreColor = new Color32(0x4C, 0xC9, 0xF0, 0xFF);
        private static readonly Color CarnivoreColor = new Color32(0xB5, 0x6E, 0xFF, 0xFF);
        private static readonly Color WorldGroundColor = new Color32(0x17, 0x21, 0x2B, 0xFF);
        private static readonly Color OutsideWorldColor = new Color32(0x08, 0x0C, 0x10, 0xFF);
        private static readonly Color OptionColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color SelectedOptionColor = new Color(0.4f, 0.8f, 0.5f, 0.6f);

        private readonly GameFlow flow = new GameFlow();
        private readonly PlayerProfileStore profileStore = new PlayerProfileStore();
        private readonly Dictionary<string, Image> appearanceOptions = new Dictionary<string, Image>();
        private readonly Dictionary<ControlMode, Image> modeOptions = new Dictionary<ControlMode, Image>();
        private readonly Dictionary<Bot, GameObject> botObjects = new Dictionary<Bot, GameObject>();
        private readonly Dictionary<Creature, CreatureView> creatureViews = new Dictionary<Creature, CreatureView>();
        private readonly System.Text.StringBuilder statusBuilder = new System.Text.StringBuilder();

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
        private GameObject statusRoot;
        private GameObject settingsPanel;
        private GameObject dashButtonRoot;
        private Image dashCooldownImage;
        private InputField nicknameField;
        private Font font;
        private bool isMobile;
        private string selectedAppearance = CharacterAppearances.Default;
        private ControlMode selectedMode;
        private PlayerProfile currentProfile;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            isMobile = Application.isMobilePlatform;
            worldCamera = Camera.main;
            if (worldCamera != null)
            {
                worldCamera.orthographic = true;
                worldCamera.backgroundColor = OutsideWorldColor;
            }

            runner = gameObject.AddComponent<WorldSimulationRunner>();
            CreateMap();
            var foodView = new GameObject("FoodField").AddComponent<FoodFieldView>();
            foodView.Bind(runner.Simulation, PrototypeSprites.Circle, worldCamera);
            runner.LocalPlayerDied += OnLocalPlayerDied;
            runner.Bots.BotJoined += OnBotJoined;
            runner.Bots.BotLeft += OnBotLeft;
            runner.Simulation.FoodConsumed += OnFoodConsumed;

            EnsureEventSystem();
            var canvas = CreateCanvas();
            gameplayRoot = CreateStretchRect("Gameplay", canvas).gameObject;
            var gameplay = (RectTransform)gameplayRoot.transform;
            var joystick = CreateJoystick(gameplay);
            var dashButton = CreateDashButton(gameplay);
            CreateControls(gameplay);
            CreateTitlePanel(canvas);
            CreateResultPanel(canvas);

            commandSource = gameObject.AddComponent<PlayerCommandSource>();
            commandSource.Bind(worldCamera, null, joystick, dashButton);
            runner.SetCommandSource(commandSource);

            if (worldCamera != null)
            {
                cameraController = worldCamera.GetComponent<WorldCameraController>();
                if (cameraController == null)
                {
                    cameraController = worldCamera.gameObject.AddComponent<WorldCameraController>();
                }

                cameraController.SetWorldBounds(runner.Simulation.Bounds);
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
                runner.Bots.BotJoined -= OnBotJoined;
                runner.Bots.BotLeft -= OnBotLeft;
                runner.Simulation.FoodConsumed -= OnFoodConsumed;
            }
        }

        private void Update()
        {
            var player = runner.LocalPlayer;
            if (player == null)
            {
                return;
            }

            UpdateDashCooldown(player);
            if (!statusRoot.activeSelf)
            {
                return;
            }

            var hunger = runner.Simulation.Settings.hunger;
            var state = player.Movement;
            var dash = state.IsDashing ? "dashing"
                : state.DashCooldownRemaining > 0f ? $"{state.DashCooldownRemaining:0.0}s"
                : "ready";
            var bots = runner.Bots;
            var threshold = MutationSystem.GetThreshold(player.Size, runner.Simulation.Settings.mutation);
            statusBuilder.Clear();
            statusBuilder
                .Append($"Name: {currentProfile.Nickname}  Look: {currentProfile.AppearanceId}\n")
                .Append($"Mode: {commandSource.Mode}  Role: {player.Role}\n")
                .Append($"Score: {player.Intake}  Size: {player.Size:0.00}\n")
                .Append($"Satiety: {player.Satiety:0.0} / {hunger.maxSatiety:0}\n")
                .Append(player.Role == CreatureRole.Herbivore ? $"Mutation: {player.Mutation:0.0} / {threshold:0.0}\n" : "Mutation: carnivore\n")
                .Append($"Speed: {state.Velocity.Length():0.00}  Dash: {dash}\n")
                .Append($"Plants: {runner.Simulation.Food.PlantCount}  Pos: {state.Position.X:0}, {state.Position.Y:0}\n")
                .Append($"Bots: {bots.Bots.Count} (+{bots.PendingJoinCount} joining)  Level: {bots.SkillLevel:0.0}\n");
            foreach (var bot in bots.Bots)
            {
                var creature = bot.Creature;
                var role = creature.Role == CreatureRole.Carnivore ? "C" : "H";
                statusBuilder.Append($"  {bot.Name}{(bot.SeeksMeat ? "*" : "")} [{role}]: {bot.Intent}  Score {creature.Intake}  Satiety {creature.Satiety:0}\n");
            }

            statusText.text = statusBuilder.ToString();
        }

        private void SetMode(ControlMode mode)
        {
            commandSource.Mode = mode;
            joystickRoot.SetActive(mode == ControlMode.VirtualPad);
            // 임시: 대시 버튼은 모바일, 그리고 PC에서는 가상패드 모드일 때만 보입니다. PC 다른 모드는 스페이스·좌클릭을 씁니다.
            dashButtonRoot.SetActive(isMobile || mode == ControlMode.VirtualPad);
        }

        /// <summary>임시: 쿨타임 표시 방식은 미결정입니다. 남은 대기시간 비율만큼 버튼을 어둡게 덮습니다.</summary>
        private void UpdateDashCooldown(Creature player)
        {
            var movement = runner.Simulation.Settings.movement;
            var state = player.Movement;
            dashCooldownImage.fillAmount = state.IsDashing ? 1f
                : movement.dashCooldown > 0f ? Mathf.Clamp01(state.DashCooldownRemaining / movement.dashCooldown)
                : 0f;
        }

        private void ToggleSettings()
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void ToggleDebugInfo()
        {
            statusRoot.SetActive(!statusRoot.activeSelf);
        }

        private void ApplyState(GameFlowState state)
        {
            // 숨겨지는 버튼이 선택된 채로 남아 이후 키 입력을 받지 않게 합니다.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (state == GameFlowState.Title)
            {
                // 첫 화면에 올 때마다 저장된 이전 선택을 복원합니다.
                ShowProfile(profileStore.Load(isMobile));
            }

            gameplayRoot.SetActive(state == GameFlowState.Playing);
            settingsPanel.SetActive(false);
            titlePanel.SetActive(state == GameFlowState.Title);
            resultPanel.SetActive(state == GameFlowState.Result);
            if (state == GameFlowState.Result)
            {
                resultText.text = $"GAME OVER\n{flow.LastResult.DeathReason}\nScore: {flow.LastResult.Score}";
            }
        }

        private void ShowProfile(PlayerProfile profile)
        {
            SelectAppearance(profile.AppearanceId);
            SelectMode(profile.ControlMode);
            nicknameField.SetTextWithoutNotify(profile.Nickname);
        }

        private void SelectAppearance(string appearanceId)
        {
            selectedAppearance = appearanceId;
            foreach (var option in appearanceOptions)
            {
                option.Value.color = option.Key == appearanceId ? SelectedOptionColor : OptionColor;
            }
        }

        private void SelectMode(ControlMode mode)
        {
            selectedMode = mode;
            foreach (var option in modeOptions)
            {
                option.Value.color = option.Key == mode ? SelectedOptionColor : OptionColor;
            }
        }

        private void EnterWorld()
        {
            if (flow.State != GameFlowState.Title)
            {
                return;
            }

            // 입장 확정 시에만 저장합니다.
            currentProfile = new PlayerProfile(selectedAppearance, PlayerProfile.NormalizeNickname(nicknameField.text), selectedMode);
            profileStore.Save(currentProfile);
            SetMode(currentProfile.ControlMode);

            if (!flow.Enter())
            {
                return;
            }

            var player = runner.SpawnLocalPlayer(currentProfile.Nickname);
            creatureObject = CreateCreature(player, currentProfile.AppearanceId, "PlayerCreature");
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
            creatureViews.Remove(death.Creature);
            if (creatureObject != null)
            {
                Destroy(creatureObject);
                creatureObject = null;
            }

            flow.EndPlay(new PlayResult(death.Creature.Intake, DescribeDeath(death)));
        }

        /// <summary>임시: 결과 항목은 미결정이며 플레이 확인을 위해 사망 원인만 표시합니다.</summary>
        private string DescribeDeath(CreatureDeath death)
        {
            if (death.Cause != DeathCause.Killed)
            {
                return "Starved";
            }

            foreach (var bot in runner.Bots.Bots)
            {
                if (bot.Creature == death.Killer)
                {
                    return $"Eaten by {bot.Name}";
                }
            }

            return "Eaten by a carnivore";
        }

        private void OnFoodConsumed(FoodItem food, Creature eater)
        {
            // 초식이 고기를 먹을 때마다 이상 신호를 보여 줍니다. 변이로 육식이 되면 역할색이 바뀝니다.
            if (food.Kind == FoodKind.Meat && eater.Role == CreatureRole.Herbivore && creatureViews.TryGetValue(eater, out var view))
            {
                view.PlayMutationFeedback();
            }
        }

        private void Feed(int intake)
        {
            // 개발용: 먹이를 먹은 것처럼 섭취량을 더해 성장·속도·줌 변화를 빠르게 확인합니다.
            runner.LocalPlayer?.AddIntake(intake, runner.Simulation.Settings.growth);
        }

        private void Starve()
        {
            // 개발용: 포만을 거의 비워 굶주림 사망과 결과 화면을 빠르게 확인합니다.
            runner.LocalPlayer?.SetSatiety(1f);
        }

        private void ToggleRole()
        {
            // 개발용: 변이를 기다리지 않고 내 역할을 바꿔 처치·도주·추격을 확인합니다. 정식 규칙에는 육식 → 초식 전환이 없습니다.
            var player = runner.LocalPlayer;
            if (player != null)
            {
                runner.Simulation.ChangeRole(player, player.Role == CreatureRole.Herbivore ? CreatureRole.Carnivore : CreatureRole.Herbivore);
            }
        }

        private void DropMeatNearPlayer()
        {
            // 개발용: 내 주변에 고기 5개를 떨어뜨려 고기 섭취 피드백·변이 게이지·육식 전환을 확인합니다.
            var player = runner.LocalPlayer;
            if (player != null)
            {
                runner.Simulation.Food.DropMeat(player.Movement.Position, player.Radius, 5);
            }
        }

        private void ChangeBotSkill(float delta)
        {
            // 개발용: 봇 수준(BOT-03)을 바꿉니다. 0에 가까우면 봇이 먹이를 거의 찾지 않아 굶어 죽고, 빈자리는 같은 수준의 새 봇이 채웁니다.
            runner.Bots.SkillLevel = Mathf.Round((runner.Bots.SkillLevel + delta) * 10f) / 10f;
        }

        private void OnBotJoined(Bot bot)
        {
            // 임시: 외형은 표시 정보이므로 봇마다 무작위로 고릅니다. 온라인 단계에서는 서버가 정한 외형을 받아야 합니다.
            var appearanceId = CharacterAppearances.GetId(Random.Range(0, CharacterAppearances.Count));
            var botObject = CreateCreature(bot.Creature, appearanceId, "Bot " + bot.Name);
            botObjects[bot] = botObject;
        }

        private void OnBotLeft(Bot bot)
        {
            creatureViews.Remove(bot.Creature);
            if (botObjects.TryGetValue(bot, out var botObject))
            {
                botObjects.Remove(bot);
                Destroy(botObject);
            }
        }

        private void CreateMap()
        {
            var bounds = runner.Simulation.Bounds;
            var size = new Vector2(bounds.HalfWidth * 2f, bounds.HalfHeight * 2f);

            var ground = new GameObject("MapGround").AddComponent<SpriteRenderer>();
            ground.sprite = PrototypeSprites.Square;
            ground.color = WorldGroundColor;
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

        /// <summary>
        /// 원형 몸체에 귀(몸 뒤)와 얼굴(몸 위)을 별도 스프라이트로 얹습니다. 장식은 표시 전용이며 충돌 반경은 몸 원 그대로입니다.
        /// 몸과 귀의 색은 역할(초식·육식) 표현이고, 외형은 장식 형태로만 구분합니다.
        /// </summary>
        private GameObject CreateCreature(Creature creature, string appearanceId, string objectName)
        {
            var creatureObject = new GameObject(objectName);
            var body = creatureObject.AddComponent<SpriteRenderer>();
            body.sprite = PrototypeSprites.Circle;
            body.sortingOrder = 1;

            var view = creatureObject.AddComponent<CreatureView>();
            var ears = PrototypeSprites.GetEars(appearanceId);
            if (ears != null)
            {
                var earRenderer = AddCreaturePart(creatureObject.transform, "Ears", ears, HerbivoreColor, 0);
                view.SetRoleColors(HerbivoreColor, CarnivoreColor, body, earRenderer);
            }
            else
            {
                view.SetRoleColors(HerbivoreColor, CarnivoreColor, body);
            }

            AddCreaturePart(creatureObject.transform, "Face", PrototypeSprites.GetFace(appearanceId), Color.white, 2);
            AddMutationGauge(view);
            AddHeadDisplay(view, ears != null ? PrototypeSprites.EarSpriteScale : 1f);
            view.Bind(creature);
            creatureViews[creature] = view;
            return creatureObject;
        }

        /// <summary>
        /// 임시: 변이 게이지 위치·공개 범위는 미결정입니다. 몸 아래에 몸 지름 길이의 막대로 두고 모든 생명체에 표시합니다.
        /// </summary>
        private void AddMutationGauge(CreatureView view)
        {
            var root = new GameObject("MutationGauge").transform;
            root.SetParent(view.transform, false);
            root.localPosition = new Vector3(0f, -0.72f, 0f);
            root.localScale = new Vector3(1f, 0.12f, 1f);

            var background = new GameObject("Background").AddComponent<SpriteRenderer>();
            background.transform.SetParent(root, false);
            background.sprite = PrototypeSprites.Square;
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.sortingOrder = 3;

            var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(root, false);
            fill.sprite = PrototypeSprites.Square;
            fill.color = CarnivoreColor;
            fill.sortingOrder = 4;

            view.SetMutationGauge(root, fill.transform, runner.Simulation.Settings.mutation);
        }

        /// <summary>머리 위 닉네임과 포만 게이지입니다. 크기는 <see cref="CreatureHeadView"/>가 화면 기준으로 맞춥니다.</summary>
        private void AddHeadDisplay(CreatureView view, float topRadiusMultiplier)
        {
            var head = new GameObject("Head").AddComponent<CreatureHeadView>();
            head.transform.SetParent(view.transform, false);

            var background = new GameObject("SatietyBackground").AddComponent<SpriteRenderer>();
            background.transform.SetParent(head.transform, false);
            background.transform.localScale = new Vector3(2f, 0.22f, 1f);
            background.sprite = PrototypeSprites.Square;
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.sortingOrder = 5;

            var fill = new GameObject("SatietyFill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(head.transform, false);
            fill.transform.localScale = new Vector3(2f, 0.22f, 1f);
            fill.sprite = PrototypeSprites.Square;
            fill.sortingOrder = 6;

            var label = new GameObject("Name").AddComponent<TextMesh>();
            label.transform.SetParent(head.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            label.font = font;
            label.fontSize = 48;
            label.characterSize = 0.1f;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 1f, 1f, 0.9f);
            var renderer = label.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 6;

            head.Bind(view, worldCamera, runner.Simulation.Settings.hunger, label, fill, topRadiusMultiplier);
        }

        private static SpriteRenderer AddCreaturePart(Transform parent, string name, Sprite sprite, Color color, int sortingOrder)
        {
            var part = new GameObject(name).AddComponent<SpriteRenderer>();
            part.transform.SetParent(parent, false);
            part.sprite = sprite;
            part.color = color;
            part.sortingOrder = sortingOrder;
            return part;
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

        /// <summary>임시: 대시 버튼 위치는 미결정이며 우하단에 둡니다. 쿨타임 동안 어두운 원형 채움으로 덮습니다.</summary>
        private DashButton CreateDashButton(RectTransform canvas)
        {
            var area = CreateRect("DashButton", canvas, new Vector2(1f, 0f), new Vector2(-200f, 200f), new Vector2(180f, 180f));
            AddCircleImage(area, new Color(1f, 0.6f, 0.3f, 0.35f), true);
            var cooldown = CreateRect("Cooldown", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 180f));
            dashCooldownImage = cooldown.gameObject.AddComponent<Image>();
            dashCooldownImage.sprite = PrototypeSprites.Circle;
            dashCooldownImage.color = new Color(0f, 0f, 0f, 0.5f);
            dashCooldownImage.type = Image.Type.Filled;
            dashCooldownImage.fillMethod = Image.FillMethod.Radial360;
            dashCooldownImage.fillOrigin = (int)Image.Origin360.Top;
            dashCooldownImage.fillAmount = 0f;
            dashCooldownImage.raycastTarget = false;
            CreateLabel(area, "DASH", 32);
            dashButtonRoot = area.gameObject;
            return area.gameObject.AddComponent<DashButton>();
        }

        /// <summary>
        /// HUD 배치: 좌측 상단 랭킹, 우측 상단 설정 버튼. 디버그 정보는 랭킹 아래에 설정에서 켤 때만 보입니다.
        /// 임시: 설정 항목은 미결정이며 지금은 개발용 버튼과 디버그 정보 토글만 둡니다. 설정을 열어도 월드와 이동은 멈추지 않습니다.
        /// </summary>
        private void CreateControls(RectTransform gameplay)
        {
            var ranking = CreateRect("Ranking", gameplay, new Vector2(0f, 1f), new Vector2(210f, -200f), new Vector2(380f, 360f));
            var rankingText = CreateLabel(ranking, string.Empty, 26);
            rankingText.alignment = TextAnchor.UpperLeft;
            ranking.gameObject.AddComponent<RankingView>().Bind(runner.Simulation, () => runner.LocalPlayer, rankingText);

            var status = CreateRect("DebugInfo", gameplay, new Vector2(0f, 1f), new Vector2(310f, -640f), new Vector2(580f, 480f));
            statusText = CreateLabel(status, string.Empty, 22);
            statusText.alignment = TextAnchor.UpperLeft;
            statusRoot = status.gameObject;
            statusRoot.SetActive(false);

            CreateButton(gameplay, "Settings", new Vector2(1f, 1f), new Vector2(-110f, -50f), ToggleSettings, new Vector2(180f, 64f));

            var panel = CreateRect("SettingsPanel", gameplay, new Vector2(1f, 1f), new Vector2(-280f, -330f), new Vector2(520f, 440f));
            panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            var center = new Vector2(0.5f, 0.5f);
            var caption = CreateRect("Caption", panel, center, new Vector2(0f, 180f), new Vector2(480f, 50f));
            CreateLabel(caption, "Settings (dev)", 28);
            CreateButton(panel, "Feed +20", center, new Vector2(-170f, 100f), () => Feed(20));
            CreateButton(panel, "Feed +200", center, new Vector2(0f, 100f), () => Feed(200));
            CreateButton(panel, "Starve", center, new Vector2(170f, 100f), Starve);
            CreateButton(panel, "Role", center, new Vector2(-170f, 20f), ToggleRole);
            CreateButton(panel, "Meat x5", center, new Vector2(0f, 20f), DropMeatNearPlayer);
            CreateButton(panel, "Debug", center, new Vector2(170f, 20f), ToggleDebugInfo);
            CreateButton(panel, "Bot Lv -", center, new Vector2(-170f, -60f), () => ChangeBotSkill(-BotSkillStep));
            CreateButton(panel, "Bot Lv +", center, new Vector2(0f, -60f), () => ChangeBotSkill(BotSkillStep));
            CreateButton(panel, "Close", center, new Vector2(0f, -160f), ToggleSettings);
            settingsPanel = panel.gameObject;
            settingsPanel.SetActive(false);
        }

        /// <summary>
        /// 외형·닉네임·조작 선택과 입장 버튼을 한 화면에 둡니다.
        /// 임시: 기기에서 쓸 수 없는 조작의 표시 방식은 미결정이므로 네 선택지를 모두 같은 방식으로 보여 줍니다.
        /// 네 번째 선택지는 PC에서 'Mouse', 모바일에서 'Touch'로 표시합니다(최종 문구 미결정).
        /// </summary>
        private void CreateTitlePanel(RectTransform canvas)
        {
            var center = new Vector2(0.5f, 0.5f);
            var panel = CreatePanel("TitlePanel", canvas, new Vector2(900f, 720f));
            var title = CreateRect("Title", panel, center, new Vector2(0f, 285f), new Vector2(560f, 100f));
            CreateLabel(title, "MutatIO", 72);

            CreateCaption(panel, "Character", 195f);
            for (var i = 0; i < CharacterAppearances.Count; i++)
            {
                var id = CharacterAppearances.GetId(i);
                var background = CreateButton(panel, string.Empty, center, new Vector2(-225f + i * 150f, 115f), () => SelectAppearance(id), new Vector2(120f, 120f));
                CreateAppearancePreview((RectTransform)background.transform, id, 66f);
                appearanceOptions[id] = background;
            }

            CreateCaption(panel, "Nickname", 20f);
            nicknameField = CreateInputField(panel, new Vector2(0f, -40f), new Vector2(480f, 64f), "Nickname");
            nicknameField.characterLimit = PlayerProfile.MaxNicknameLength;

            CreateCaption(panel, "Control", -120f);
            var modes = new[] { ControlMode.Wasd, ControlMode.ArrowKeys, ControlMode.VirtualPad, ControlMode.PointerFollow };
            var labels = new[] { "WASD", "Arrows", "Pad", isMobile ? "Touch" : "Mouse" };
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                modeOptions[mode] = CreateButton(panel, labels[i], center, new Vector2(-285f + i * 190f, -180f), () => SelectMode(mode), new Vector2(170f, 64f));
            }

            CreateButton(panel, "PLAY", center, new Vector2(0f, -290f), EnterWorld, new Vector2(220f, 76f));
            titlePanel = panel.gameObject;
        }

        /// <summary>귀·몸·얼굴 순서로 겹친 외형 미리보기입니다. bodyDiameter는 몸 원의 UI 크기입니다.</summary>
        private static void CreateAppearancePreview(RectTransform parent, string appearanceId, float bodyDiameter)
        {
            var ears = PrototypeSprites.GetEars(appearanceId);
            if (ears != null)
            {
                AddPreviewImage(parent, "Ears", ears, HerbivoreColor, bodyDiameter * PrototypeSprites.EarSpriteScale);
            }

            AddPreviewImage(parent, "Body", PrototypeSprites.Circle, HerbivoreColor, bodyDiameter);
            AddPreviewImage(parent, "Face", PrototypeSprites.GetFace(appearanceId), Color.white, bodyDiameter);
        }

        private static void AddPreviewImage(RectTransform parent, string name, Sprite sprite, Color color, float size)
        {
            var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
        }

        private void CreateCaption(RectTransform parent, string text, float y)
        {
            var rect = CreateRect(text + "Caption", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(600f, 40f));
            var label = CreateLabel(rect, text, 26);
            label.color = new Color(1f, 1f, 1f, 0.6f);
        }

        private InputField CreateInputField(RectTransform parent, Vector2 position, Vector2 size, string placeholderText)
        {
            var rect = CreateRect("NicknameField", parent, new Vector2(0.5f, 0.5f), position, size);
            var background = rect.gameObject.AddComponent<Image>();
            background.color = OptionColor;

            var text = CreateLabel(rect, string.Empty, 30);
            text.supportRichText = false;
            var placeholder = CreateLabel(rect, placeholderText, 30);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.fontStyle = FontStyle.Italic;

            var field = rect.gameObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        private void CreateResultPanel(RectTransform canvas)
        {
            var panel = CreatePanel("ResultPanel", canvas, new Vector2(640f, 420f));
            var text = CreateRect("ResultText", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(560f, 200f));
            resultText = CreateLabel(text, string.Empty, 44);
            CreateButton(panel, "OK", new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), () => flow.ConfirmResult());
            resultPanel = panel.gameObject;
        }

        /// <summary>화면 중앙의 불투명 패널입니다. 패널 영역 클릭이 월드 대시로 쓰이지 않도록 레이캐스트를 받습니다.</summary>
        private static RectTransform CreatePanel(string name, RectTransform canvas, Vector2 size)
        {
            var panel = CreateRect(name, canvas, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);
            return panel;
        }

        private Image CreateButton(RectTransform parent, string label, Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction onClick, Vector2? size = null)
        {
            var rect = CreateRect(string.IsNullOrEmpty(label) ? "Button" : label, parent, anchor, position, size ?? new Vector2(160f, 64f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.2f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            CreateLabel(rect, label, 26);
            return image;
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
