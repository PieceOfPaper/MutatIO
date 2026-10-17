using System;
using System.Collections.Generic;
using System.Numerics;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.Movement;
using MutatIO.Shared.World;

namespace MutatIO.Shared.Bots
{
    public enum BotIntent
    {
        /// <summary>노릴 대상이 없어 떠돌아다닙니다.</summary>
        Wander,

        /// <summary>가까운 먹이로 이동합니다.</summary>
        Forage,

        /// <summary>나를 처치할 수 있는 육식에게서 도망칩니다.</summary>
        Flee,

        /// <summary>육식일 때 처치할 수 있는 개체를 쫓습니다.</summary>
        Chase,
    }

    /// <summary>
    /// 봇 한 마리의 판단입니다. 사람과 같은 <see cref="PlayerCommand"/>만 만들고 시뮬레이션 상태는 읽기만 합니다.
    /// 판단 순서: 위험 회피 → (육식) 사냥감 추격 → 가까운 먹이 → 배회. 수준(<see cref="BotSkill"/>)이 낮을수록
    /// 판단이 늦고, 보는 범위가 좁고, 위험을 놓치고, 먹이를 덜 노리고, 방향이 부정확하고, 느리게 움직입니다.
    /// 임시: 판단 방식과 아래 상수는 프로토타입 확인용입니다.
    /// </summary>
    public sealed class BotBrain
    {
        /// <summary>맵 가장자리에서 안쪽으로 방향을 틀기 시작하는 거리. 월드 단위.</summary>
        private const float WallMargin = 4f;

        /// <summary>위험과의 가장자리 거리가 이 값 안이면 대시로 도망칩니다. 월드 단위.</summary>
        private const float FleeDashDistance = 2.5f;

        /// <summary>사냥감과의 가장자리 거리가 이 값 안이면 대시로 덮칩니다. 월드 단위.</summary>
        private const float ChaseDashDistance = 3f;

        /// <summary>사냥감 위치를 예측하는 최대 시간. 초.</summary>
        private const float MaxChaseLead = 0.5f;

        private const float WanderTurnMin = 2f;
        private const float WanderTurnMax = 5f;

        private readonly Random random;
        private readonly List<FoodItem> nearbyFood = new List<FoodItem>();
        private readonly HashSet<int> noticedThreats = new HashSet<int>();
        private readonly HashSet<int> threatsInRange = new HashSet<int>();
        private float decisionTimer;
        private float wanderTimer;
        private float aimOffset;
        private bool dashRequested;
        private Vector2 moveDirection;
        private Vector2 wanderDirection;
        private FoodItem targetFood;
        private Creature targetCreature;

        public BotBrain(Creature creature, BotSkill skill, Random random)
        {
            Creature = creature;
            Skill = skill;
            this.random = random;
        }

        public Creature Creature { get; }

        public BotSkill Skill { get; set; }

        public BotIntent Intent { get; private set; }

        /// <summary>dt만큼 판단을 진행하고 이번 스텝의 명령을 반환합니다. 판단은 수준별 주기마다 하고 그 사이에는 목표를 따라갑니다.</summary>
        public PlayerCommand Think(WorldSimulation world, float deltaTime)
        {
            wanderTimer -= deltaTime;
            decisionTimer -= deltaTime;
            if (decisionTimer <= 0f)
            {
                Decide(world);
                decisionTimer = Skill.DecisionInterval * (0.75f + 0.5f * NextFloat());
            }

            var direction = Steer(world);
            var dash = dashRequested;
            dashRequested = false;
            return new PlayerCommand(direction * Skill.Pace, dash);
        }

        private void Decide(WorldSimulation world)
        {
            var self = Creature;
            var position = self.Movement.Position;
            var range = Skill.PerceptionRange;
            targetFood = null;
            targetCreature = null;
            aimOffset = (NextFloat() * 2f - 1f) * Skill.AimError;

            var flee = Vector2.Zero;
            var nearestThreat = float.MaxValue;
            Creature prey = null;
            var nearestPrey = float.MaxValue;
            threatsInRange.Clear();
            foreach (var other in world.Creatures)
            {
                if (other == self)
                {
                    continue;
                }

                var offset = position - other.Movement.Position;
                var distance = offset.Length();
                var edge = distance - self.Radius - other.Radius;
                if (edge > range)
                {
                    continue;
                }

                if (EcologyRules.CanKill(other, self))
                {
                    // 수준이 낮으면 범위 안의 위험을 늦게 알아차립니다. 한 번 알아차린 위험은 범위를 벗어날 때까지 기억합니다.
                    threatsInRange.Add(other.Id);
                    if (noticedThreats.Contains(other.Id) || NextFloat() < Skill.ThreatAwareness)
                    {
                        noticedThreats.Add(other.Id);
                        var away = distance > 0.0001f ? offset / distance : RandomDirection();
                        flee += away / Math.Max(edge, 0.25f);
                        nearestThreat = Math.Min(nearestThreat, edge);
                    }
                }
                else if (EcologyRules.CanKill(self, other) && edge < nearestPrey)
                {
                    prey = other;
                    nearestPrey = edge;
                }
            }

            noticedThreats.IntersectWith(threatsInRange);
            if (flee != Vector2.Zero)
            {
                Intent = BotIntent.Flee;
                moveDirection = Normalize(Vector2.Normalize(flee) + GetWallAvoidance(world));
                dashRequested = nearestThreat < FleeDashDistance && NextFloat() < Skill.DashChance;
                return;
            }

            if (NextFloat() >= Skill.Focus)
            {
                Wander(world);
                return;
            }

            if (prey != null)
            {
                Intent = BotIntent.Chase;
                targetCreature = prey;
                dashRequested = nearestPrey < ChaseDashDistance && NextFloat() < Skill.DashChance;
                return;
            }

            targetFood = FindNearestFood(world, range);
            if (targetFood != null)
            {
                Intent = BotIntent.Forage;
                return;
            }

            Wander(world);
        }

        private Vector2 Steer(WorldSimulation world)
        {
            var position = Creature.Movement.Position;
            if (Intent == BotIntent.Forage && targetFood != null)
            {
                // 이미 먹혔거나 다른 개체가 흡수 중이면 다음 판단까지 지금 방향을 유지합니다. 내가 흡수 중이면 끌려오므로 쫓지 않습니다.
                if (!world.Food.Contains(targetFood) || targetFood.AttractorId != FoodItem.NoAttractor)
                {
                    targetFood = null;
                }
                else
                {
                    moveDirection = Aim(targetFood.Position - position);
                }
            }
            else if (Intent == BotIntent.Chase && targetCreature != null)
            {
                if (!targetCreature.IsAlive)
                {
                    targetCreature = null;
                }
                else
                {
                    var target = targetCreature.Movement.Position;
                    var speed = Math.Max(0.01f, MovementSystem.GetMaxSpeed(Creature.Size, world.Settings.movement));
                    var lead = Math.Min(MaxChaseLead, Vector2.Distance(target, position) / speed);
                    moveDirection = Aim(target + targetCreature.Movement.Velocity * lead - position);
                }
            }

            return moveDirection;
        }

        private void Wander(WorldSimulation world)
        {
            Intent = BotIntent.Wander;
            if (wanderTimer <= 0f || wanderDirection == Vector2.Zero)
            {
                wanderDirection = RandomDirection();
                wanderTimer = WanderTurnMin + NextFloat() * (WanderTurnMax - WanderTurnMin);
            }

            // 맵 가장자리에 붙어 멈추지 않도록 배회 방향 자체를 안쪽으로 돌립니다.
            wanderDirection = Normalize(wanderDirection + GetWallAvoidance(world) * 2f);
            moveDirection = Rotate(wanderDirection, aimOffset);
        }

        private FoodItem FindNearestFood(WorldSimulation world, float range)
        {
            var self = Creature;
            var position = self.Movement.Position;
            var reach = range + self.Radius;
            nearbyFood.Clear();
            world.Food.QueryRect(position - new Vector2(reach, reach), position + new Vector2(reach, reach), nearbyFood);

            FoodItem nearest = null;
            var nearestDistance = reach * reach;
            foreach (var food in nearbyFood)
            {
                // 다른 개체가 선점한 먹이는 가져올 수 없고, 내가 선점한 먹이는 이미 끌려오고 있습니다.
                if (food.AttractorId != FoodItem.NoAttractor || !EcologyRules.CanEat(self.Role, food.Kind))
                {
                    continue;
                }

                var distance = Vector2.DistanceSquared(food.Position, position);
                if (distance <= nearestDistance)
                {
                    nearest = food;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>맵 가장자리에 가까울수록 커지는 안쪽 방향 벡터입니다. 각 축 0~1.</summary>
        private Vector2 GetWallAvoidance(WorldSimulation world)
        {
            var position = Creature.Movement.Position;
            var limitX = Math.Max(0f, world.Bounds.HalfWidth - Creature.Radius);
            var limitY = Math.Max(0f, world.Bounds.HalfHeight - Creature.Radius);
            return new Vector2(
                -Math.Sign(position.X) * WallPush(limitX - Math.Abs(position.X)),
                -Math.Sign(position.Y) * WallPush(limitY - Math.Abs(position.Y)));
        }

        private static float WallPush(float distanceToWall)
        {
            return distanceToWall >= WallMargin ? 0f : 1f - Math.Max(0f, distanceToWall) / WallMargin;
        }

        private Vector2 Aim(Vector2 toTarget)
        {
            return Rotate(Normalize(toTarget), aimOffset);
        }

        private Vector2 RandomDirection()
        {
            var angle = NextFloat() * (float)(Math.PI * 2.0);
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        private float NextFloat()
        {
            return (float)random.NextDouble();
        }

        private static Vector2 Normalize(Vector2 value)
        {
            var length = value.Length();
            return length > 0.0001f ? value / length : Vector2.Zero;
        }

        private static Vector2 Rotate(Vector2 value, float radians)
        {
            var cos = (float)Math.Cos(radians);
            var sin = (float)Math.Sin(radians);
            return new Vector2(value.X * cos - value.Y * sin, value.X * sin + value.Y * cos);
        }
    }
}
