using System;
using System.Collections.Generic;

namespace MutatIO.Shared.Bots
{
    /// <summary>
    /// 봇이 입장할 때 무작위로 고르는 영문 이름 목록입니다. 닉네임 길이 제한(UI-01, 12자) 안의 이름만 둡니다.
    /// 임시: 목록 내용은 프로토타입용이며 자유롭게 늘리거나 바꿀 수 있습니다.
    /// </summary>
    public static class BotNames
    {
        private static readonly string[] names =
        {
            "Oliver", "Emma", "Liam", "Ava", "Noah", "Mia", "Lucas", "Chloe", "Ethan", "Grace",
            "Mason", "Lily", "Logan", "Zoe", "Jack", "Ruby", "Leo", "Ivy", "Owen", "Nora",
            "Felix", "Hazel", "Milo", "Luna", "Jasper", "Stella", "Theo", "Violet", "Hugo", "Iris",
            "Oscar", "Alice", "Finn", "Clara", "Arthur", "Maya", "Henry", "Sophie", "Max", "Ella",
            "Ezra", "Aria", "Silas", "Willow", "Rowan", "Daisy", "Caleb", "Freya", "Miles", "Penny",
            "Atlas", "Juniper", "Otto", "Poppy", "Rex", "Olive", "Toby", "Pearl", "Gus", "Mabel",
        };

        public static int Count => names.Length;

        public static string Get(int index)
        {
            return names[index];
        }

        /// <summary>사용 중인 이름을 피해 무작위로 고릅니다. 모두 사용 중이면 중복을 허용합니다.</summary>
        public static string Pick(Random random, ICollection<string> inUse)
        {
            var start = random.Next(names.Length);
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[(start + i) % names.Length];
                if (!inUse.Contains(name))
                {
                    return name;
                }
            }

            return names[start];
        }
    }
}
