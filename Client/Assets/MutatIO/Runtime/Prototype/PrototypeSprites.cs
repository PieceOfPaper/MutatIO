using UnityEngine;

namespace MutatIO.Prototype
{
    /// <summary>
    /// 아트가 정해지기 전까지 쓰는 런타임 생성 스프라이트입니다.
    /// </summary>
    public static class PrototypeSprites
    {
        private const int CircleResolution = 128;
        private const int GridCellPixels = 64;

        // 타일 하나가 덮는 월드 단위. Tiled 모드는 타일마다 정점을 만들므로 너무 작으면 넓은 격자를 그릴 수 없습니다.
        public const float GridCellWorldSize = 4f;

        private static Sprite circle;
        private static Sprite grid;

        /// <summary>지름 1 월드 단위의 흰색 원입니다.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    circle = CreateCircle();
                }

                return circle;
            }
        }

        /// <summary>한 칸이 <see cref="GridCellWorldSize"/> 월드 단위인 격자입니다. SpriteRenderer의 Tiled 모드로 씁니다.</summary>
        public static Sprite Grid
        {
            get
            {
                if (grid == null)
                {
                    grid = CreateGrid();
                }

                return grid;
            }
        }

        private static Sprite CreateCircle()
        {
            var texture = new Texture2D(CircleResolution, CircleResolution, TextureFormat.RGBA32, false)
            {
                name = "PrototypeCircle",
                wrapMode = TextureWrapMode.Clamp,
            };

            var radius = CircleResolution * 0.5f;
            var pixels = new Color32[CircleResolution * CircleResolution];
            for (var y = 0; y < CircleResolution; y++)
            {
                for (var x = 0; x < CircleResolution; x++)
                {
                    var dx = x + 0.5f - radius;
                    var dy = y + 0.5f - radius;
                    var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * CircleResolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, CircleResolution, CircleResolution), new Vector2(0.5f, 0.5f), CircleResolution);
        }

        private static Sprite CreateGrid()
        {
            var texture = new Texture2D(GridCellPixels, GridCellPixels, TextureFormat.RGBA32, false)
            {
                name = "PrototypeGrid",
                wrapMode = TextureWrapMode.Repeat,
            };

            var line = new Color32(255, 255, 255, 40);
            var empty = new Color32(255, 255, 255, 0);
            var pixels = new Color32[GridCellPixels * GridCellPixels];
            for (var y = 0; y < GridCellPixels; y++)
            {
                for (var x = 0; x < GridCellPixels; x++)
                {
                    pixels[y * GridCellPixels + x] = x < 2 || y < 2 ? line : empty;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, GridCellPixels, GridCellPixels), new Vector2(0.5f, 0.5f), GridCellPixels / GridCellWorldSize, 0, SpriteMeshType.FullRect);
        }
    }
}
