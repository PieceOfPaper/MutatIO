using System;
using System.Collections.Generic;
using System.Text;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;
using UnityEngine;
using UnityEngine.UI;

namespace MutatIO.UI
{
    /// <summary>
    /// 화면 좌측 상단의 점수 랭킹입니다. 일정 주기마다 <see cref="Ranking"/>으로 순위를 계산해 표시합니다.
    /// 임시: 표시 인원·갱신 주기(HUD-01)는 임시값입니다. 내 순위가 표시 인원 밖이면 마지막 줄에 내 순위를 덧붙입니다(내 순위 표시 여부 미결정).
    /// </summary>
    [AddComponentMenu("MutatIO/UI/Ranking View")]
    public sealed class RankingView : MonoBehaviour
    {
        /// <summary>(HUD-01) 표시 인원.</summary>
        [SerializeField] private int maxEntries = 10;

        /// <summary>(HUD-01) 갱신 주기. 초.</summary>
        [SerializeField] private float refreshInterval = 0.5f;

        [SerializeField] private Color localPlayerColor = new Color32(0xFF, 0xD6, 0x6B, 0xFF);

        private readonly List<Creature> ranked = new List<Creature>();
        private readonly StringBuilder builder = new StringBuilder();
        private WorldSimulation simulation;
        private Func<Creature> localPlayer;
        private Text text;
        private float timer;

        public void Bind(WorldSimulation world, Func<Creature> getLocalPlayer, Text label)
        {
            simulation = world;
            localPlayer = getLocalPlayer;
            text = label;
            text.supportRichText = true;
            Refresh();
        }

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            timer = refreshInterval;
            if (simulation == null || text == null)
            {
                return;
            }

            Ranking.Build(simulation.Creatures, ranked);
            var me = localPlayer?.Invoke();
            builder.Clear();
            builder.Append("RANKING\n");
            var count = Math.Min(maxEntries, ranked.Count);
            for (var i = 0; i < count; i++)
            {
                AppendLine(i + 1, ranked[i], ranked[i] == me);
            }

            var myRank = me != null ? Ranking.GetRank(ranked, me) : 0;
            if (myRank > count)
            {
                builder.Append("...\n");
                AppendLine(myRank, me, true);
            }

            text.text = builder.ToString();
        }

        private void AppendLine(int rank, Creature creature, bool isLocal)
        {
            var name = string.IsNullOrEmpty(creature.Name) ? "Player" : creature.Name;
            // 닉네임에 들어 있는 리치 텍스트 태그가 해석되지 않게 꺾쇠를 바꿉니다.
            name = name.Replace("<", "‹").Replace(">", "›");
            var line = $"{rank}. {name}  {creature.Intake}";
            if (isLocal)
            {
                builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(localPlayerColor)).Append('>').Append(line).Append("</color>\n");
            }
            else
            {
                builder.Append(line).Append('\n');
            }
        }
    }
}
