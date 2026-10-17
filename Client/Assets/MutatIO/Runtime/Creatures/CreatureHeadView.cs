using MutatIO.Shared.Hunger;
using UnityEngine;

namespace MutatIO.Creatures
{
    /// <summary>
    /// 생명체 머리 위의 닉네임과 남은 포만 게이지입니다. 본인뿐 아니라 모든 생명체에 표시합니다.
    /// 몸집·카메라 줌과 관계없이 화면에서 같은 크기로 보이도록 카메라 표시 범위에 비례해 크기를 맞춥니다.
    /// 생명체 오브젝트의 자식으로 두고, 몸 크기 반영(<see cref="CreatureView"/>) 뒤에 실행해 부모 배율을 상쇄합니다.
    /// 임시: 크기·간격(HUD-02)은 임시값이며 겹침·화면 밖 표시 정책은 미결정이라 처리하지 않습니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Creatures/Creature Head View")]
    [DefaultExecutionOrder(100)]
    public sealed class CreatureHeadView : MonoBehaviour
    {
        /// <summary>(HUD-02) 표시 한 단위의 크기. 카메라 세로 절반 범위에 대한 비율입니다.</summary>
        [SerializeField] private float screenScale = 0.12f;

        /// <summary>(HUD-02) 몸 위쪽 가장자리와 포만 게이지 사이 간격. 표시 단위.</summary>
        [SerializeField] private float gap = 0.35f;

        [SerializeField] private Color satietyColor = new Color32(0xED, 0xF4, 0xFF, 0xFF);

        /// <summary>임시: 포만이 이 비율 아래면 경고색으로 표시합니다.</summary>
        [SerializeField] private float lowSatietyRatio = 0.25f;

        [SerializeField] private Color lowSatietyColor = new Color32(0xFF, 0x6B, 0x6B, 0xFF);

        private CreatureView creatureView;
        private Camera viewCamera;
        private HungerSettings hunger;
        private TextMesh nameText;
        private Transform satietyFill;
        private SpriteRenderer satietyRenderer;
        private float topExtent = 1f;

        /// <summary>
        /// topRadiusMultiplier는 몸 중심에서 장식(귀 등) 윗끝까지의 거리를 몸 반경 배수로 나타냅니다. 장식 없는 몸은 1입니다.
        /// </summary>
        public void Bind(CreatureView view, Camera camera, HungerSettings hungerSettings, TextMesh label, SpriteRenderer fill, float topRadiusMultiplier)
        {
            topExtent = Mathf.Max(1f, topRadiusMultiplier);
            creatureView = view;
            viewCamera = camera;
            hunger = hungerSettings;
            nameText = label;
            satietyRenderer = fill;
            satietyFill = fill.transform;
        }

        private void LateUpdate()
        {
            var creature = creatureView != null ? creatureView.Creature : null;
            if (creature == null || viewCamera == null)
            {
                return;
            }

            var unit = viewCamera.orthographicSize * screenScale;
            var position = creature.Movement.Position;
            transform.position = new Vector3(position.X, position.Y + creature.Radius * topExtent + gap * unit, transform.parent.position.z);
            var parentScale = Mathf.Max(0.0001f, transform.parent.lossyScale.x);
            transform.localScale = Vector3.one * (unit / parentScale);

            // 임시: 빈 닉네임의 표시 문구는 미결정입니다.
            var displayName = string.IsNullOrEmpty(creature.Name) ? "Player" : creature.Name;
            if (nameText.text != displayName)
            {
                nameText.text = displayName;
            }

            var ratio = hunger.maxSatiety > 0f ? Mathf.Clamp01(creature.Satiety / hunger.maxSatiety) : 0f;
            satietyFill.localScale = new Vector3(ratio * 2f, satietyFill.localScale.y, 1f);
            satietyFill.localPosition = new Vector3((ratio - 1f), satietyFill.localPosition.y, 0f);
            satietyRenderer.color = ratio < lowSatietyRatio ? lowSatietyColor : satietyColor;
        }
    }
}
