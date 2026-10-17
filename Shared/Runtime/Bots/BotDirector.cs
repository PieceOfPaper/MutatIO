using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;

namespace MutatIO.Shared.Bots
{
    /// <summary>
    /// 월드의 최소 인원을 봇으로 채우고 봇의 명령을 만듭니다.
    /// 인원 = 접속한 사람 수(<see cref="ConnectedHumans"/>, 살아 있는지와 무관) + 봇 수입니다. 사람은 첫 화면·결과 화면에 있어도 한 자리를 차지합니다.
    /// 빈자리가 생기면 무작위 지연 뒤에 봇이 사람과 같은 방식(작은 초식)으로 입장합니다. 사람이 늘어 자리가 차면 대기 중인 입장만 취소하고,
    /// 이미 있는 봇은 강제로 내보내지 않습니다. 봇을 줄이려면 수준(<see cref="SkillLevel"/>)을 낮춰 자연스럽게 죽게 합니다.
    /// 순수 C#이므로 로컬 프로토타입에서는 클라이언트가, 온라인 단계에서는 서버가 같은 코드로 실행합니다.
    /// </summary>
    public sealed class BotDirector : IDisposable
    {
        /// <summary>임시: 입장 위치 후보 수. 스폰 안전 규칙(SPAWN-01)은 미결정이며 다른 생명체에서 가장 먼 후보를 고릅니다.</summary>
        private const int SpawnCandidates = 8;

        /// <summary>입장 위치가 맵 가장자리에서 떨어질 거리. 월드 단위.</summary>
        private const float SpawnEdgeMargin = 10f;

        private readonly WorldSimulation world;
        private readonly BotSettings settings;
        private readonly Random random;
        private readonly List<Bot> bots = new List<Bot>();
        private readonly List<float> pendingJoins = new List<float>();
        private readonly HashSet<string> namesInUse = new HashSet<string>();
        private float skillLevel;
        private int connectedHumans;

        public BotDirector(WorldSimulation world, BotSettings settings, int seed)
        {
            this.world = world;
            this.settings = settings;
            random = new Random(seed);
            skillLevel = BotSkill.Clamp01(settings.skillLevel);
            world.CreatureDied += OnCreatureDied;
        }

        /// <summary>봇이 월드에 입장했을 때 발생합니다.</summary>
        public event Action<Bot> BotJoined;

        /// <summary>봇이 사망해 월드에서 빠졌을 때 발생합니다.</summary>
        public event Action<Bot> BotLeft;

        public IReadOnlyList<Bot> Bots => bots;

        /// <summary>입장 지연 중인 봇 수입니다.</summary>
        public int PendingJoinCount => pendingJoins.Count;

        /// <summary>월드에 접속한 사람 수입니다. 살아 있는 생명체가 없어도(첫 화면·결과 화면) 접속 중이면 셉니다.</summary>
        public int ConnectedHumans
        {
            get => connectedHumans;
            set => connectedHumans = Math.Max(0, value);
        }

        /// <summary>지금 유지하려는 봇 수입니다.</summary>
        public int DesiredBotCount => Math.Max(0, settings.minPopulation - connectedHumans);

        /// <summary>전체 봇 수준(0~1)입니다. 바꾸면 이미 입장한 봇에도 즉시 적용합니다.</summary>
        public float SkillLevel
        {
            get => skillLevel;
            set
            {
                skillLevel = BotSkill.Clamp01(value);
                foreach (var bot in bots)
                {
                    bot.Brain.Skill = CreateSkill(bot.SkillOffset);
                }
            }
        }

        /// <summary>지연 없이 빈자리를 모두 봇으로 채웁니다. 월드 시작 시 이미 사람들이 돌아다니는 상태로 보이게 할 때 씁니다.</summary>
        public void FillImmediately()
        {
            pendingJoins.Clear();
            while (bots.Count < DesiredBotCount)
            {
                Join();
            }
        }

        /// <summary>입장 대기를 진행하고 모든 봇의 이번 스텝 명령을 정합니다. 월드 <see cref="WorldSimulation.Step"/> 전에 호출합니다.</summary>
        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateJoins(deltaTime);
            foreach (var bot in bots)
            {
                bot.Creature.Command = bot.Brain.Think(world, deltaTime);
            }
        }

        public void Dispose()
        {
            world.CreatureDied -= OnCreatureDied;
        }

        private void UpdateJoins(float deltaTime)
        {
            var desired = DesiredBotCount;
            while (bots.Count + pendingJoins.Count > desired && pendingJoins.Count > 0)
            {
                // 사람이 들어와 자리가 찼으면 가장 늦게 들어올 예정인 봇부터 취소합니다.
                pendingJoins.RemoveAt(IndexOfLongestDelay());
            }

            while (bots.Count + pendingJoins.Count < desired)
            {
                pendingJoins.Add(NextJoinDelay());
            }

            for (var i = pendingJoins.Count - 1; i >= 0; i--)
            {
                pendingJoins[i] -= deltaTime;
                if (pendingJoins[i] <= 0f)
                {
                    pendingJoins.RemoveAt(i);
                    Join();
                }
            }
        }

        private Bot Join()
        {
            var name = BotNames.Pick(random, namesInUse);
            var creature = world.SpawnCreature(FindSpawnPosition(), name);
            var offset = (NextFloat() * 2f - 1f) * Math.Max(0f, settings.skillVariance);
            var bot = new Bot(name, creature, new BotBrain(creature, CreateSkill(offset), new Random(random.Next())), offset);
            bots.Add(bot);
            namesInUse.Add(name);
            BotJoined?.Invoke(bot);
            return bot;
        }

        private void OnCreatureDied(CreatureDeath death)
        {
            var index = bots.FindIndex(bot => bot.Creature == death.Creature);
            if (index < 0)
            {
                return;
            }

            var bot = bots[index];
            bots.RemoveAt(index);
            namesInUse.Remove(bot.Name);
            BotLeft?.Invoke(bot);
        }

        private Vector2 FindSpawnPosition()
        {
            var halfWidth = Math.Max(0f, world.Bounds.HalfWidth - SpawnEdgeMargin);
            var halfHeight = Math.Max(0f, world.Bounds.HalfHeight - SpawnEdgeMargin);
            if (settings.spawnAreaHalfExtent > 0f)
            {
                halfWidth = Math.Min(halfWidth, settings.spawnAreaHalfExtent);
                halfHeight = Math.Min(halfHeight, settings.spawnAreaHalfExtent);
            }

            var best = Vector2.Zero;
            var bestDistance = float.MinValue;
            for (var i = 0; i < SpawnCandidates; i++)
            {
                var candidate = new Vector2((NextFloat() * 2f - 1f) * halfWidth, (NextFloat() * 2f - 1f) * halfHeight);
                var nearest = float.MaxValue;
                foreach (var creature in world.Creatures)
                {
                    nearest = Math.Min(nearest, Vector2.Distance(candidate, creature.Movement.Position) - creature.Radius);
                }

                if (nearest > bestDistance)
                {
                    best = candidate;
                    bestDistance = nearest;
                }
            }

            return best;
        }

        private BotSkill CreateSkill(float offset)
        {
            return BotSkill.FromLevel(skillLevel + offset, settings);
        }

        private float NextJoinDelay()
        {
            var min = Math.Max(0f, settings.joinDelayMin);
            var max = Math.Max(min, settings.joinDelayMax);
            return min + NextFloat() * (max - min);
        }

        private int IndexOfLongestDelay()
        {
            var index = 0;
            for (var i = 1; i < pendingJoins.Count; i++)
            {
                if (pendingJoins[i] > pendingJoins[index])
                {
                    index = i;
                }
            }

            return index;
        }

        private float NextFloat()
        {
            return (float)random.NextDouble();
        }
    }
}
