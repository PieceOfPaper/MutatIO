using MutatIO.Input;
using UnityEngine;

namespace MutatIO.Profile
{
    /// <summary>
    /// 입장 정보를 PlayerPrefs에 저장하고 불러옵니다. 입장 확정 시에만 저장하며 매 프레임 저장하지 않습니다.
    /// 값이 없거나 유효하지 않으면 항목별로 기본값으로 복구합니다.
    /// WebGL에서는 브라우저 IndexedDB에 저장되므로 브라우저 데이터 삭제 시 기본값으로 돌아갑니다.
    /// </summary>
    public sealed class PlayerProfileStore
    {
        public const string DefaultKeyPrefix = "MutatIO.Profile.";

        private readonly string appearanceKey;
        private readonly string nicknameKey;
        private readonly string controlModeKey;

        public PlayerProfileStore(string keyPrefix = DefaultKeyPrefix)
        {
            appearanceKey = keyPrefix + "Appearance";
            nicknameKey = keyPrefix + "Nickname";
            controlModeKey = keyPrefix + "ControlMode";
        }

        public PlayerProfile Load(bool isMobile)
        {
            var fallback = PlayerProfile.CreateDefault(isMobile);

            var appearance = PlayerPrefs.GetString(appearanceKey, fallback.AppearanceId);
            if (!CharacterAppearances.IsValid(appearance))
            {
                appearance = fallback.AppearanceId;
            }

            var nickname = PlayerProfile.NormalizeNickname(PlayerPrefs.GetString(nicknameKey, fallback.Nickname));

            var modeValue = PlayerPrefs.GetInt(controlModeKey, (int)fallback.ControlMode);
            var mode = PlayerProfile.IsValidControlMode(modeValue) ? (ControlMode)modeValue : fallback.ControlMode;

            return new PlayerProfile(appearance, nickname, mode);
        }

        public void Save(PlayerProfile profile)
        {
            PlayerPrefs.SetString(appearanceKey, profile.AppearanceId);
            PlayerPrefs.SetString(nicknameKey, PlayerProfile.NormalizeNickname(profile.Nickname));
            PlayerPrefs.SetInt(controlModeKey, (int)profile.ControlMode);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(appearanceKey);
            PlayerPrefs.DeleteKey(nicknameKey);
            PlayerPrefs.DeleteKey(controlModeKey);
        }
    }
}
