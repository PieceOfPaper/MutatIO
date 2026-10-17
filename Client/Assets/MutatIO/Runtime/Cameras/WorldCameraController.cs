using MutatIO.Shared.Creatures;
using MutatIO.Shared.World;
using UnityEngine;

namespace MutatIO.Cameras
{
    /// <summary>
    /// 내 생명체를 따라가고 몸집이 커질수록 보이는 월드 범위를 넓히는 탑다운 직교 카메라입니다.
    /// - 몸집별 시야는 화면의 짧은 변에 맞추고, 긴 변은 화면 비율만큼 더 보여줍니다.
    /// - 보이는 영역을 월드 안으로 제한해 맵 끝에서는 카메라가 멈춥니다. 보이는 영역이 월드보다 큰 축은 월드 중심에 맞춥니다.
    /// - 줌·화면 비율이 바뀌면 매 프레임 다시 계산합니다. UI는 Screen Space 캔버스이므로 영향을 받지 않습니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Cameras/World Camera Controller")]
    [RequireComponent(typeof(Camera))]
    public sealed class WorldCameraController : MonoBehaviour
    {
        [SerializeField] private CameraZoomSettings zoom = new CameraZoomSettings();

        private Camera targetCamera;
        private Creature target;
        private WorldBounds worldBounds;
        private bool hasWorldBounds;
        private float halfExtent = -1f;

        public CameraZoomSettings Zoom => zoom;

        /// <summary>몸집에 따른 목표 짧은 변 절반 범위입니다.</summary>
        public static float GetTargetHalfExtent(float size, CameraZoomSettings settings)
        {
            var halfExtent = settings.baseHalfExtent * Mathf.Pow(Mathf.Max(1f, size), settings.sizeExponent);
            return Mathf.Clamp(halfExtent, settings.minHalfExtent, Mathf.Max(settings.minHalfExtent, settings.maxHalfExtent));
        }

        /// <summary>시간 상수 방식으로 목표 범위에 다가간 값입니다. 프레임 속도와 무관하게 같은 속도로 수렴합니다.</summary>
        public static float SmoothHalfExtent(float current, float target, float smoothTime, float deltaTime)
        {
            if (smoothTime <= 0f)
            {
                return target;
            }

            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-deltaTime / smoothTime));
        }

        /// <summary>
        /// 짧은 변 절반 범위를 직교 카메라 크기(세로 절반)로 바꿉니다. aspect = 가로 / 세로.
        /// 가로 화면은 세로가 짧은 변이고, 세로 화면은 가로가 짧은 변이므로 세로 절반 = 범위 / aspect입니다.
        /// </summary>
        public static float GetOrthographicSize(float shortHalfExtent, float aspect)
        {
            return aspect >= 1f || aspect <= 0f ? shortHalfExtent : shortHalfExtent / aspect;
        }

        /// <summary>한 축의 카메라 중심을 월드 안으로 제한합니다. 보이는 범위가 월드보다 크면 월드 중심(0)에 맞춥니다.</summary>
        public static float ClampAxis(float desired, float visibleHalf, float worldHalf)
        {
            if (visibleHalf >= worldHalf)
            {
                return 0f;
            }

            return Mathf.Clamp(desired, visibleHalf - worldHalf, worldHalf - visibleHalf);
        }

        public void SetWorldBounds(WorldBounds bounds)
        {
            worldBounds = bounds;
            hasWorldBounds = true;
        }

        /// <summary>따라갈 생명체를 바꿉니다. null이면 현재 위치에 멈춥니다(사망 위치 등).</summary>
        public void Follow(Creature creature, bool snap = true)
        {
            target = creature;
            if (snap && target != null)
            {
                halfExtent = GetTargetHalfExtent(target.Size, zoom);
                Apply();
            }
        }

        private void LateUpdate()
        {
            if (halfExtent < 0f)
            {
                halfExtent = zoom.baseHalfExtent;
            }

            if (target != null)
            {
                var desired = GetTargetHalfExtent(target.Size, zoom);
                halfExtent = SmoothHalfExtent(halfExtent, desired, zoom.zoomSmoothTime, Time.deltaTime);
            }

            Apply();
        }

        private void Apply()
        {
            var camera = GetCamera();
            var orthographicSize = GetOrthographicSize(halfExtent, camera.aspect);
            camera.orthographicSize = orthographicSize;

            var position = target != null
                ? new Vector2(target.Movement.Position.X, target.Movement.Position.Y)
                : (Vector2)transform.position;
            if (hasWorldBounds)
            {
                position.x = ClampAxis(position.x, orthographicSize * camera.aspect, worldBounds.HalfWidth);
                position.y = ClampAxis(position.y, orthographicSize, worldBounds.HalfHeight);
            }

            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private Camera GetCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                targetCamera.orthographic = true;
            }

            return targetCamera;
        }
    }
}
