using System;
using System.Collections.Generic;
using MutatIO.Shared.Creatures;

namespace MutatIO.Shared.World
{
    /// <summary>
    /// 점수 랭킹 계산입니다. 표시와 갱신 주기는 호출자가 정합니다.
    /// 임시: 정렬 기준·동점 순서는 미결정입니다. 점수 내림차순, 동점이면 먼저 입장한 개체(ID가 작은 쪽)가 위입니다. 봇도 포함합니다.
    /// </summary>
    public static class Ranking
    {
        private static readonly Comparison<Creature> Order = (a, b) =>
            a.Intake != b.Intake ? b.Intake.CompareTo(a.Intake) : a.Id.CompareTo(b.Id);

        /// <summary>살아 있는 생명체를 순위 순서로 results에 채웁니다. 1위가 0번입니다.</summary>
        public static void Build(IReadOnlyList<Creature> creatures, List<Creature> results)
        {
            results.Clear();
            foreach (var creature in creatures)
            {
                results.Add(creature);
            }

            results.Sort(Order);
        }

        /// <summary>정렬된 목록에서 creature의 순위(1부터)를 찾습니다. 없으면 0입니다.</summary>
        public static int GetRank(List<Creature> ranked, Creature creature)
        {
            var index = ranked.IndexOf(creature);
            return index < 0 ? 0 : index + 1;
        }
    }
}
