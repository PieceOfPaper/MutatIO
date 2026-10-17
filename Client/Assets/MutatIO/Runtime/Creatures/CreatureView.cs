using MutatIO.Shared.Creatures;
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
        public Creature Creature { get; private set; }

        public void Bind(Creature creature)
        {
            Creature = creature;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (Creature == null)
            {
                return;
            }

            var position = Creature.Movement.Position;
            transform.position = new Vector3(position.X, position.Y, transform.position.z);
            // 임시: 외형 크기와 충돌 반경의 관계(CONTACT-01)는 미결정입니다. 현재 외형 지름은 충돌 지름과 같습니다.
            transform.localScale = Vector3.one * (Creature.Radius * 2f);
        }
    }
}
