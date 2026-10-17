using System.Collections.Generic;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.World;
using UnityEngine;

namespace MutatIO.Feeding
{
    /// <summary>
    /// 필드의 먹이를 스프라이트로 표시합니다. 매 프레임 카메라에 보이는 영역의 먹이만 조회해 재사용 스프라이트에 그립니다.
    /// 월드 전체 먹이 수(타일 수 × 타일 상한)와 무관하게 화면 안 먹이만큼만 표시 객체를 씁니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Feeding/Food Field View")]
    public sealed class FoodFieldView : MonoBehaviour
    {
        [Tooltip("먹이 표시 지름. 월드 단위. 표시 전용 임시값이며 섭취 판정에는 쓰지 않습니다.")]
        [SerializeField, Min(0.01f)] private float displayDiameter = 0.4f;

        [Tooltip("화면 밖으로 더 조회할 여유. 월드 단위.")]
        [SerializeField, Min(0f)] private float viewMargin = 1f;

        // 임시 팔레트 기본안(docs/DECISIONS.md): 초식 먹이 #67D66B, 육식 먹이 #FF6B6B.
        [SerializeField] private Color plantColor = new Color32(0x67, 0xD6, 0x6B, 0xFF);
        [SerializeField] private Color meatColor = new Color32(0xFF, 0x6B, 0x6B, 0xFF);
        [SerializeField] private Sprite sprite;

        private readonly List<FoodItem> visible = new List<FoodItem>();
        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        private WorldSimulation simulation;
        private Camera viewCamera;
        private int activeCount;

        public void Bind(WorldSimulation world, Sprite foodSprite, Camera camera)
        {
            simulation = world;
            sprite = foodSprite;
            viewCamera = camera;
        }

        private void LateUpdate()
        {
            visible.Clear();
            if (simulation != null && viewCamera != null)
            {
                var halfHeight = viewCamera.orthographicSize + viewMargin;
                var halfWidth = viewCamera.orthographicSize * viewCamera.aspect + viewMargin;
                var center = viewCamera.transform.position;
                simulation.Food.QueryRect(
                    new System.Numerics.Vector2(center.x - halfWidth, center.y - halfHeight),
                    new System.Numerics.Vector2(center.x + halfWidth, center.y + halfHeight),
                    visible);
            }

            for (var i = 0; i < visible.Count; i++)
            {
                if (i == renderers.Count)
                {
                    renderers.Add(CreateRenderer());
                }

                var food = visible[i];
                var renderer = renderers[i];
                if (i >= activeCount)
                {
                    renderer.gameObject.SetActive(true);
                }

                renderer.color = food.Kind == FoodKind.Meat ? meatColor : plantColor;
                renderer.transform.localPosition = new Vector3(food.Position.X, food.Position.Y, 0f);
            }

            for (var i = visible.Count; i < activeCount; i++)
            {
                renderers[i].gameObject.SetActive(false);
            }

            activeCount = visible.Count;
        }

        private SpriteRenderer CreateRenderer()
        {
            var foodObject = new GameObject("Food");
            foodObject.transform.SetParent(transform, false);
            foodObject.transform.localScale = Vector3.one * displayDiameter;
            foodObject.SetActive(false);
            var renderer = foodObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -1;
            return renderer;
        }
    }
}
