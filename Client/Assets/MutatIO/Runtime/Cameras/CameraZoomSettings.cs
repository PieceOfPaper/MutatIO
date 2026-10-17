using System;
using UnityEngine;

namespace MutatIO.Cameras
{
    /// <summary>
    /// 몸집 연동 카메라 줌 값입니다. 괄호 안은 docs/BALANCE_PARAMETERS.md의 항목 ID입니다.
    /// 임시: 모든 기본값과 곡선 형태는 플레이 테스트 전 임시값입니다.
    /// 값은 화면 짧은 변이 보여주는 월드 범위의 절반(월드 단위)입니다. 긴 변은 화면 비율만큼 더 보여줍니다.
    /// </summary>
    [Serializable]
    public sealed class CameraZoomSettings
    {
        [Tooltip("(CAMERA-01) 초기 몸집(1)에서 화면 짧은 변이 보여주는 범위의 절반. 월드 단위.")]
        public float baseHalfExtent = 8f;

        [Tooltip("(CAMERA-01) 몸집별 확대 지수. 절반 범위 = 기본값 × 몸집^지수. 1보다 작으면 캐릭터가 커질수록 화면에서 차지하는 비율도 완만히 커집니다.")]
        public float sizeExponent = 0.6f;

        [Tooltip("(CAMERA-02) 짧은 변 절반 범위의 최솟값. 월드 단위.")]
        public float minHalfExtent = 8f;

        [Tooltip("(CAMERA-02) 짧은 변 절반 범위의 최댓값. 월드 단위.")]
        public float maxHalfExtent = 30f;

        [Tooltip("(CAMERA-03) 목표 줌까지 따라가는 시간 상수. 초. 0이면 즉시 바뀝니다.")]
        public float zoomSmoothTime = 0.4f;
    }
}
