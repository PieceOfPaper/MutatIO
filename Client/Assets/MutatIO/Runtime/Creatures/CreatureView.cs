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
            // 몸 원의 표시 지름 = 충돌 지름입니다. 귀·얼굴 장식은 자식으로 함께 확대되지만 충돌 반경에는 포함하지 않습니다.
            transform.localScale = Vector3.one * (Creature.Radius * 2f);
        }
    }
}
