using System;
using MutatIO.Input;
using MutatIO.Shared.Bots;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;
using UnityEngine;

namespace MutatIO.World
{
    /// <summary>
    /// 로컬 프로토타입에서 Shared 월드 시뮬레이션을 실행합니다. 매 프레임 플레이어 명령을 넣고 Time.deltaTime만큼 진행합니다.
    /// 표시 컴포넌트는 LateUpdate에서 시뮬레이션 상태를 읽어 Transform에 반영합니다.
    /// 월드는 내 생명체가 없어도(첫 화면·결과 화면) 계속 진행합니다.
    /// 봇은 <see cref="BotDirector"/>가 최소 인원까지 채웁니다. 로컬 프로토타입에서는 나를 접속한 사람 한 명으로 셉니다.
    /// </summary>
    [AddComponentMenu("MutatIO/World/World Simulation Runner")]
    public sealed class WorldSimulationRunner : MonoBehaviour
    {
        [SerializeField] private WorldSettings settings = new WorldSettings();
        [SerializeField] private int seed = 12345;
        [SerializeField] private PlayerCommandSource commandSource;
        [SerializeField] private BotSettings botSettings = new BotSettings();
        [SerializeField] private int localHumans = 1;

        /// <summary>내 생명체가 사망했을 때 발생합니다. 이때 <see cref="LocalPlayer"/>는 이미 null입니다.</summary>
        public event Action<CreatureDeath> LocalPlayerDied;

        public WorldSimulation Simulation { get; private set; }

        public BotDirector Bots { get; private set; }

        /// <summary>현재 살아 있는 내 생명체입니다. 입장 전이나 사망 후에는 null입니다.</summary>
        public Creature LocalPlayer { get; private set; }

        public void SetCommandSource(PlayerCommandSource source)
        {
            commandSource = source;
        }

        /// <summary>작은 초식 생명체로 새로 입장합니다. 이미 살아 있는 내 생명체가 있으면 그대로 반환합니다.</summary>
        public Creature SpawnLocalPlayer(string nickname = null)
        {
            if (LocalPlayer == null)
            {
                // 임시: 스폰 위치·안전 거리(SPAWN-01)는 미결정입니다. 현재는 맵 중앙에 입장합니다.
                LocalPlayer = Simulation.SpawnCreature(System.Numerics.Vector2.Zero, nickname);
            }

            return LocalPlayer;
        }

        private void Awake()
        {
            Simulation = new WorldSimulation(settings, seed);
            Simulation.CreatureDied += OnCreatureDied;
            Simulation.Food.FillPlants();

            Bots = new BotDirector(Simulation, botSettings, seed + 1);
            Bots.ConnectedHumans = localHumans;
        }

        private void Start()
        {
            // 표시 컴포넌트가 Awake에서 BotJoined를 구독한 뒤 첫 봇들을 입장시킵니다.
            Bots.FillImmediately();
        }

        private void OnDestroy()
        {
            if (Simulation != null)
            {
                Simulation.CreatureDied -= OnCreatureDied;
            }

            Bots?.Dispose();
        }

        private void Update()
        {
            if (LocalPlayer != null && commandSource != null)
            {
                LocalPlayer.Command = commandSource.ReadCommand();
            }

            Bots.Update(Time.deltaTime);
            Simulation.Step(Time.deltaTime);
        }

        private void OnCreatureDied(CreatureDeath death)
        {
            if (death.Creature != LocalPlayer)
            {
                return;
            }

            LocalPlayer = null;
            LocalPlayerDied?.Invoke(death);
        }
    }
}
