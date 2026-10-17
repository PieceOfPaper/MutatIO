using MutatIO.Input;
using MutatIO.Shared.Movement;
using UnityEngine;

namespace MutatIO.Creatures
{
    /// <summary>
    /// 로컬 플레이어 개체를 Shared 이동 규칙으로 움직이고 결과를 Transform에 반영합니다.
    /// 규칙 계산은 <see cref="MovementSystem"/>이 하고 이 컴포넌트는 입력 전달과 표시만 담당합니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Creatures/Local Creature Mover")]
    public sealed class LocalCreatureMover : MonoBehaviour
    {
        [SerializeField] private PlayerCommandSource commandSource;
        [SerializeField] private MovementSettings movementSettings = new MovementSettings();

        [Tooltip("초기 몸집을 1로 둔 상대 몸집입니다.")]
        [SerializeField, Min(1f)] private float size = 1f;

        private MovementState state;

        public MovementState State => state;

        public MovementSettings Settings => movementSettings;

        public float Size
        {
            get => size;
            set => size = Mathf.Max(1f, value);
        }

        public void Bind(PlayerCommandSource source)
        {
            commandSource = source;
        }

        private void Start()
        {
            state = MovementState.At(new System.Numerics.Vector2(transform.position.x, transform.position.y));
        }

        private void Update()
        {
            var command = commandSource != null ? commandSource.ReadCommand() : PlayerCommand.None;
            MovementSystem.Step(ref state, command, size, movementSettings, Time.deltaTime);

            transform.position = new Vector3(state.Position.X, state.Position.Y, transform.position.z);
            // 임시: 몸집 → 외형 크기 곡선(GROW-01)은 M3에서 정합니다. 지금은 몸집을 지름으로 그대로 씁니다.
            transform.localScale = Vector3.one * size;
        }
    }
}
