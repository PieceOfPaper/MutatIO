using MutatIO.Shared.Feeding;

namespace MutatIO.Shared.Creatures
{
    /// <summary>
    /// 역할·점수에 따른 확정 생태 규칙입니다(docs/DECISIONS.md). 시뮬레이션의 흡수·접촉 판정과 봇 판단이 함께 사용합니다.
    /// </summary>
    public static class EcologyRules
    {
        /// <summary>
        /// attacker가 접촉으로 victim을 처치할 수 있는지입니다. 육식만 처치하며 점수가 조금이라도 높아야 합니다.
        /// 동점은 무처치, 초식은 아무도 처치하지 않으므로 작은 육식과 큰 초식은 둘 다 생존합니다.
        /// </summary>
        public static bool CanKill(Creature attacker, Creature victim)
        {
            return attacker != victim
                && attacker.Role == CreatureRole.Carnivore
                && attacker.Intake > victim.Intake;
        }

        /// <summary>육식은 초식 먹이를 먹지 못합니다. 초식은 두 종류를 모두 먹을 수 있습니다.</summary>
        public static bool CanEat(CreatureRole role, FoodKind kind)
        {
            return role == CreatureRole.Herbivore || kind == FoodKind.Meat;
        }
    }
}
