using System.Collections.Generic;
using MutatIO.Shared.Creatures;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.World;
using UnityEngine;

namespace MutatIO.Feeding
{
    /// <summary>
    /// 필드의 먹이를 스프라이트로 표시합니다. 생성·섭취 이벤트로 표시 객체를 만들고 지우며, 위치는 매 프레임 갱신합니다.
    /// 표시 객체는 재사용합니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Feeding/Food Field View")]
    public sealed class FoodFieldView : MonoBehaviour
    {
        [Tooltip("먹이 표시 지름. 월드 단위. 표시 전용 임시값이며 섭취 판정에는 쓰지 않습니다.")]
        [SerializeField, Min(0.01f)] private float displayDiameter = 0.4f;

        [SerializeField] private Color plantColor = new Color(0.35f, 0.85f, 0.35f);
        [SerializeField] private Color meatColor = new Color(0.9f, 0.25f, 0.25f);
        [SerializeField] private Sprite sprite;

        private readonly Dictionary<int, SpriteRenderer> active = new Dictionary<int, SpriteRenderer>();
        private readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
        private WorldSimulation simulation;

        public void Bind(WorldSimulation world, Sprite foodSprite)
        {
            Unbind();
            simulation = world;
            sprite = foodSprite;
            simulation.Food.Spawned += OnSpawned;
            simulation.FoodConsumed += OnConsumed;
            foreach (var food in simulation.Food.Items)
            {
                OnSpawned(food);
            }
        }

        private void LateUpdate()
        {
            if (simulation == null)
            {
                return;
            }

            foreach (var food in simulation.Food.Items)
            {
                if (active.TryGetValue(food.Id, out var renderer))
                {
                    renderer.transform.localPosition = new Vector3(food.Position.X, food.Position.Y, 0f);
                }
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (simulation == null)
            {
                return;
            }

            simulation.Food.Spawned -= OnSpawned;
            simulation.FoodConsumed -= OnConsumed;
            simulation = null;
        }

        private void OnSpawned(FoodItem food)
        {
            var renderer = pool.Count > 0 ? pool.Pop() : CreateRenderer();
            renderer.gameObject.SetActive(true);
            renderer.color = food.Kind == FoodKind.Meat ? meatColor : plantColor;
            renderer.transform.localPosition = new Vector3(food.Position.X, food.Position.Y, 0f);
            active[food.Id] = renderer;
        }

        private void OnConsumed(FoodItem food, Creature eater)
        {
            if (active.TryGetValue(food.Id, out var renderer))
            {
                active.Remove(food.Id);
                renderer.gameObject.SetActive(false);
                pool.Push(renderer);
            }
        }

        private SpriteRenderer CreateRenderer()
        {
            var foodObject = new GameObject("Food");
            foodObject.transform.SetParent(transform, false);
            foodObject.transform.localScale = Vector3.one * displayDiameter;
            var renderer = foodObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -1;
            return renderer;
        }
    }
}
