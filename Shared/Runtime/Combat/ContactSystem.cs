using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;

namespace MutatIO.Shared.Combat
{
    /// <summary>
    /// 몸체 접촉 판정입니다. 접촉은 보이는 원과 같은 반경으로 판단하며, 두 원이 겹치면(중심 거리 &lt; 반경 합) 접촉입니다.
    /// 임시: 모든 쌍을 비교합니다(개체 수가 적은 로컬 프로토타입 기준). 서버 인원이 늘면 공간 분할로 바꿉니다.
    /// </summary>
    public static class ContactSystem
    {
        /// <summary>
        /// 서로 처치할 수 없는 두 생명체(초식끼리, 동점, 작은 육식과 큰 초식 등)가 겹치면 서로 밀어 겹침을 줄입니다.
        /// 몸이 큰(무거운) 쪽이 덜 밀립니다. 무게 = 반경^<see cref="ContactSettings.separationMassExponent"/>.
        /// 한쪽이 다른 쪽을 처치할 수 있는 쌍은 밀지 않고 접촉 처치로 넘깁니다.
        /// 임시: 육식이 포함된 비처치 접촉도 밀침으로 처리하는 것은 프로토타입 임시안입니다.
        /// </summary>
        public static void SeparateNonLethal(IReadOnlyList<Creature> creatures, WorldBounds bounds, ContactSettings settings, float deltaTime)
        {
            var response = Math.Min(1f, Math.Max(0f, settings.separationRate) * deltaTime);
            if (response <= 0f)
            {
                return;
            }

            var exponent = Math.Max(0f, settings.separationMassExponent);
            for (var i = 0; i < creatures.Count; i++)
            {
                var a = creatures[i];
                for (var j = i + 1; j < creatures.Count; j++)
                {
                    var b = creatures[j];
                    if (EcologyRules.CanKill(a, b) || EcologyRules.CanKill(b, a))
                    {
                        continue;
                    }

                    var offset = b.Movement.Position - a.Movement.Position;
                    var distance = offset.Length();
                    var overlap = a.Radius + b.Radius - distance;
                    if (overlap <= 0f)
                    {
                        continue;
                    }

                    // 중심이 완전히 겹치면 ID 순서로 정한 고정 방향으로 떼어 놓습니다.
                    var direction = distance > 0.0001f ? offset / distance : Vector2.UnitX;
                    var massA = (float)Math.Pow(Math.Max(0.0001f, a.Radius), exponent);
                    var massB = (float)Math.Pow(Math.Max(0.0001f, b.Radius), exponent);
                    var correction = overlap * response;
                    a.Movement.Position -= direction * (correction * massB / (massA + massB));
                    b.Movement.Position += direction * (correction * massA / (massA + massB));
                    bounds.ClampCircle(ref a.Movement, a.Radius);
                    bounds.ClampCircle(ref b.Movement, b.Radius);
                }
            }
        }

        /// <summary>
        /// 접촉 처치를 찾아 kills에 추가합니다. 생명체 상태는 바꾸지 않습니다.
        /// 처치 조건은 <see cref="EcologyRules.CanKill"/>(육식이고 점수가 엄격히 높음)이며 대시 중에도 같습니다.
        /// 임시: 동시 사망은 이 스텝의 접촉을 모두 같은 상태로 판정한 뒤 일괄 반영합니다. 같은 스텝에 처치당하는 개체도 다른 개체를 처치할 수 있습니다.
        /// 한 개체가 여러 처치자와 닿으면 목록 순서상 먼저 판정된 처치자 하나로 기록합니다.
        /// </summary>
        public static void FindKills(IReadOnlyList<Creature> creatures, List<CreatureDeath> kills)
        {
            for (var i = 0; i < creatures.Count; i++)
            {
                var a = creatures[i];
                for (var j = i + 1; j < creatures.Count; j++)
                {
                    var b = creatures[j];
                    var reach = a.Radius + b.Radius;
                    if (Vector2.DistanceSquared(a.Movement.Position, b.Movement.Position) >= reach * reach)
                    {
                        continue;
                    }

                    if (EcologyRules.CanKill(a, b))
                    {
                        AddKill(kills, b, a);
                    }
                    else if (EcologyRules.CanKill(b, a))
                    {
                        AddKill(kills, a, b);
                    }
                }
            }
        }

        private static void AddKill(List<CreatureDeath> kills, Creature victim, Creature killer)
        {
            foreach (var kill in kills)
            {
                if (kill.Creature == victim)
                {
                    return;
                }
            }

            kills.Add(new CreatureDeath(victim, DeathCause.Killed, killer));
        }
    }
}
