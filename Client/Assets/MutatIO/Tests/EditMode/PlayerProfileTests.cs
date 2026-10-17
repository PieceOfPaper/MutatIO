using MutatIO.Input;
using MutatIO.Profile;
using NUnit.Framework;
using UnityEngine;

namespace MutatIO.Tests.EditMode
{
    public sealed class PlayerProfileTests
    {
        private const string TestKeyPrefix = "MutatIO.Tests.Profile.";

        private PlayerProfileStore store;

        [SetUp]
        public void SetUp()
        {
            store = new PlayerProfileStore(TestKeyPrefix);
            store.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            store.Clear();
        }

        [Test]
        public void Load_WithoutSavedValues_UsesDeviceDefaults()
        {
            var pc = store.Load(isMobile: false);
            Assert.That(pc.AppearanceId, Is.EqualTo(CharacterAppearances.Default));
            Assert.That(pc.Nickname, Is.Empty);
            Assert.That(pc.ControlMode, Is.EqualTo(ControlMode.PointerFollow));

            Assert.That(store.Load(isMobile: true).ControlMode, Is.EqualTo(ControlMode.VirtualPad));
        }

        [Test]
        public void SaveThenLoad_RestoresSelection()
        {
            store.Save(new PlayerProfile(CharacterAppearances.RoundEars, "Amoeba", ControlMode.ArrowKeys));

            var loaded = store.Load(isMobile: true);
            Assert.That(loaded.AppearanceId, Is.EqualTo(CharacterAppearances.RoundEars));
            Assert.That(loaded.Nickname, Is.EqualTo("Amoeba"));
            Assert.That(loaded.ControlMode, Is.EqualTo(ControlMode.ArrowKeys));
        }

        [Test]
        public void Load_InvalidValues_FallBackPerField()
        {
            PlayerPrefs.SetString(TestKeyPrefix + "Appearance", "deleted-character");
            PlayerPrefs.SetString(TestKeyPrefix + "Nickname", "Kept");
            PlayerPrefs.SetInt(TestKeyPrefix + "ControlMode", 99);

            var loaded = store.Load(isMobile: false);
            Assert.That(loaded.AppearanceId, Is.EqualTo(CharacterAppearances.Default));
            Assert.That(loaded.Nickname, Is.EqualTo("Kept"));
            Assert.That(loaded.ControlMode, Is.EqualTo(ControlMode.PointerFollow));
        }

        [Test]
        public void NormalizeNickname_TrimsAndLimitsLength()
        {
            Assert.That(PlayerProfile.NormalizeNickname(null), Is.Empty);
            Assert.That(PlayerProfile.NormalizeNickname("  Cell  "), Is.EqualTo("Cell"));
            Assert.That(PlayerProfile.NormalizeNickname("가나다라마바사아자차카타파하"), Is.EqualTo("가나다라마바사아자차카타"));

            // 서로게이트 쌍(이모지)을 중간에서 자르지 않습니다.
            var emoji = string.Concat(System.Linq.Enumerable.Repeat("\U0001F9A0", PlayerProfile.MaxNicknameLength + 2));
            var normalized = PlayerProfile.NormalizeNickname(emoji);
            Assert.That(new System.Globalization.StringInfo(normalized).LengthInTextElements, Is.EqualTo(PlayerProfile.MaxNicknameLength));
            Assert.That(char.IsHighSurrogate(normalized[normalized.Length - 1]), Is.False);
        }
    }
}
