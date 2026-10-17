using MutatIO.Shared.Creatures;
using MutatIO.Shared.Mutation;
using UnityEngine;

namespace MutatIO.Creatures
{
    /// <summary>
    /// 시뮬레이션의 생명체 상태를 Transform에 반영하는 표시 컴포넌트입니다. 규칙 계산은 하지 않습니다.
    /// 스프라이트는 지름 1 월드 단위 원을 기준으로 합니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Creatures/Creature View")]
    public sealed class CreatureView : MonoBehaviour
    {
        /// <summary>임시: 고기 섭취 피드백(VFX-01) 지속시간. 초. 연출 방식은 미결정입니다.</summary>
        [SerializeField] private float feedbackDuration = 0.35f;

        /// <summary>임시: 고기 섭취 피드백 중 몸이 부풀었다 줄어드는 최대 비율.</summary>
        [SerializeField] private float feedbackScale = 0.18f;

        [SerializeField] private Color feedbackColor = new Color32(0xFF, 0x6B, 0x6B, 0xFF);

        private SpriteRenderer[] roleRenderers;
        private Color herbivoreColor;
        private Color carnivoreColor;
        private Transform gaugeRoot;
        private Transform gaugeFill;
        private MutationSettings mutationSettings;
        private float feedbackRemaining;

        public Creature Creature { get; private set; }

        public void Bind(Creature creature)
        {
            Creature = creature;
            Apply();
        }

        /// <summary>역할(초식·육식)에 따라 색을 바꿀 몸·귀 렌더러를 지정합니다. 외형 장식 형태는 역할과 무관합니다.</summary>
        public void SetRoleColors(Color herbivore, Color carnivore, params SpriteRenderer[] renderers)
        {
            herbivoreColor = herbivore;
            carnivoreColor = carnivore;
            roleRenderers = renderers;
            Apply();
        }

        /// <summary>
        /// 변이 게이지를 지정합니다. fill은 root 안에서 왼쪽부터 채워지는 가로 막대(가운데 피벗, 가로 길이 1)입니다.
        /// 초식의 변이 수치가 0보다 클 때만 보이며 0이거나 육식이면 숨깁니다.
        /// </summary>
        public void SetMutationGauge(Transform root, Transform fill, MutationSettings settings)
        {
            gaugeRoot = root;
            gaugeFill = fill;
            mutationSettings = settings;
            Apply();
        }

        /// <summary>초식이 고기를 먹을 때마다 짧게 부풀고 붉게 깜빡이는 이상 신호를 보여 줍니다.</summary>
        public void PlayMutationFeedback()
        {
            feedbackRemaining = feedbackDuration;
        }

        private void LateUpdate()
        {
            feedbackRemaining = Mathf.Max(0f, feedbackRemaining - Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            if (Creature == null)
            {
                return;
            }

            // 0 → 1 → 0으로 한 번 오르내리는 피드백 강도입니다.
            var pulse = feedbackDuration > 0f ? Mathf.Sin(Mathf.PI * feedbackRemaining / feedbackDuration) : 0f;

            var position = Creature.Movement.Position;
            transform.position = new Vector3(position.X, position.Y, transform.position.z);
            // 몸 원의 표시 지름 = 충돌 지름입니다. 귀·얼굴 장식은 자식으로 함께 확대되지만 충돌 반경에는 포함하지 않습니다.
            // 피드백으로 부푸는 크기는 표시 전용이며 충돌에는 영향이 없습니다.
            transform.localScale = Vector3.one * (Creature.Radius * 2f * (1f + feedbackScale * pulse));

            if (roleRenderers != null)
            {
                var color = Creature.Role == CreatureRole.Carnivore ? carnivoreColor : herbivoreColor;
                color = Color.Lerp(color, feedbackColor, pulse);
                foreach (var renderer in roleRenderers)
                {
                    renderer.color = color;
                }
            }

            ApplyGauge();
        }

        private void ApplyGauge()
        {
            if (gaugeRoot == null)
            {
                return;
            }

            var visible = Creature.Role == CreatureRole.Herbivore && Creature.Mutation > 0f;
            if (gaugeRoot.gameObject.activeSelf != visible)
            {
                gaugeRoot.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            var threshold = MutationSystem.GetThreshold(Creature.Size, mutationSettings);
            var ratio = threshold > 0f ? Mathf.Clamp01(Creature.Mutation / threshold) : 1f;
            gaugeFill.localScale = new Vector3(ratio, 1f, 1f);
            gaugeFill.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
        }
    }
}
