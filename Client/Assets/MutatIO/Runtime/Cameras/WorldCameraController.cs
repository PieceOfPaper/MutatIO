using MutatIO.Shared.Creatures;
using UnityEngine;

namespace MutatIO.Cameras
{
    /// <summary>
    /// 내 생명체를 따라가고 몸집이 커질수록 보이는 월드 범위를 넓히는 카메라입니다.
    /// UI는 Screen Space 캔버스이므로 줌의 영향을 받지 않습니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Cameras/World Camera Controller")]
    [RequireComponent(typeof(Camera))]
    public sealed class WorldCameraController : MonoBehaviour
    {
        [SerializeField] private CameraZoomSettings zoom = new CameraZoomSettings();

        private Camera targetCamera;
        private Creature target;

        public CameraZoomSettings Zoom => zoom;

        /// <summary>몸집에 따른 목표 화면 세로 절반 크기입니다.</summary>
        public static float GetTargetHalfHeight(float size, CameraZoomSettings settings)
        {
            var halfHeight = settings.baseHalfHeight * Mathf.Pow(Mathf.Max(1f, size), settings.sizeExponent);
            return Mathf.Clamp(halfHeight, settings.minHalfHeight, Mathf.Max(settings.minHalfHeight, settings.maxHalfHeight));
        }

        /// <summary>시간 상수 방식으로 목표 크기에 다가간 값입니다. 프레임 속도와 무관하게 같은 속도로 수렴합니다.</summary>
        public static float SmoothHalfHeight(float current, float target, float smoothTime, float deltaTime)
        {
            if (smoothTime <= 0f)
            {
                return target;
            }

            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-deltaTime / smoothTime));
        }

        public void Follow(Creature creature, bool snap = true)
        {
            target = creature;
            if (snap && target != null)
            {
                ApplyPosition();
                GetCamera().orthographicSize = GetTargetHalfHeight(target.Size, zoom);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ApplyPosition();
            var camera = GetCamera();
            var desired = GetTargetHalfHeight(target.Size, zoom);
            camera.orthographicSize = SmoothHalfHeight(camera.orthographicSize, desired, zoom.zoomSmoothTime, Time.deltaTime);
        }

        private void ApplyPosition()
        {
            // 임시: 맵 경계 근처에서 카메라를 맵 안으로 제한할지는 미결정입니다. 현재는 항상 내 생명체를 중앙에 둡니다.
            var position = target.Movement.Position;
            transform.position = new Vector3(position.X, position.Y, transform.position.z);
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
