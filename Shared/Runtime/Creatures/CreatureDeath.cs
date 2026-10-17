namespace MutatIO.Shared.Creatures
{
    public enum DeathCause
    {
        /// <summary>포만 게이지 소진.</summary>
        Starvation = 1,
    }

    /// <summary>
    /// 생명체 사망 정보입니다. 사망한 생명체는 이미 월드에서 제거되었으며 점수(<see cref="Creature.Intake"/>)는 사망 시점 값입니다.
    /// </summary>
    public readonly struct CreatureDeath
    {
        public CreatureDeath(Creature creature, DeathCause cause)
        {
            Creature = creature;
            Cause = cause;
        }

        public Creature Creature { get; }

        public DeathCause Cause { get; }
    }
}
