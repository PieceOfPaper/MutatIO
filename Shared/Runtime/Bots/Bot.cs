using MutatIO.Shared.Creatures;

namespace MutatIO.Shared.Bots
{
    /// <summary>
    /// 월드에 입장한 봇 하나입니다. 생명체는 사람과 같은 <see cref="Creature"/>이며 명령만 <see cref="Brain"/>이 만듭니다.
    /// </summary>
    public sealed class Bot
    {
        internal Bot(string name, Creature creature, BotBrain brain, float skillOffset)
        {
            Name = name;
            Creature = creature;
            Brain = brain;
            SkillOffset = skillOffset;
        }

        public string Name { get; }

        public Creature Creature { get; }

        public BotBrain Brain { get; }

        /// <summary>전체 봇 수준에 더하는 이 봇만의 편차입니다.</summary>
        public float SkillOffset { get; }

        public BotIntent Intent => Brain.Intent;
    }
}
