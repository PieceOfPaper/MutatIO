using MutatIO.Input;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;
using UnityEngine;

namespace MutatIO.World
{
    /// <summary>
    /// 로컬 프로토타입에서 Shared 월드 시뮬레이션을 실행합니다. 매 프레임 플레이어 명령을 넣고 Time.deltaTime만큼 진행합니다.
    /// 표시 컴포넌트는 LateUpdate에서 시뮬레이션 상태를 읽어 Transform에 반영합니다.
    /// </summary>
    [AddComponentMenu("MutatIO/World/World Simulation Runner")]
    public sealed class WorldSimulationRunner : MonoBehaviour
    {
        [SerializeField] private WorldSettings settings = new WorldSettings();
        [SerializeField] private int seed = 12345;
        [SerializeField] private PlayerCommandSource commandSource;

        public WorldSimulation Simulation { get; private set; }

        public Creature LocalPlayer { get; private set; }

        public void SetCommandSource(PlayerCommandSource source)
        {
            commandSource = source;
        }

        private void Awake()
        {
            Simulation = new WorldSimulation(settings, seed);
            LocalPlayer = Simulation.SpawnCreature(System.Numerics.Vector2.Zero);
            Simulation.Food.FillPlants();
        }

        private void Update()
        {
            if (LocalPlayer != null && commandSource != null)
            {
                LocalPlayer.Command = commandSource.ReadCommand();
            }

            Simulation.Step(Time.deltaTime);
        }
    }
}
