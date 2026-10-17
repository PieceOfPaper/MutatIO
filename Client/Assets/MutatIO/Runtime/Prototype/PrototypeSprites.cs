using System;
using System.Collections.Generic;
using MutatIO.Profile;
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
        private static Sprite square;
        private static readonly Dictionary<string, Sprite> appearances = new Dictionary<string, Sprite>();

        /// <summary>한 변이 1 월드 단위인 흰색 사각형입니다.</summary>
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                    {
                        name = "PrototypeSquare",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point,
                    };
                    var pixels = new Color32[16];
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = new Color32(255, 255, 255, 255);
                    }

                    texture.SetPixels32(pixels);
                    texture.Apply();
                    square = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }

                return square;
            }
        }

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

        /// <summary>
        /// 캐릭터 외형별 지름 1 월드 단위 원입니다. 색은 역할 표현에 쓰므로 외형은 원 안의 어두운 무늬로만 구분합니다.
        /// 임시: 아트가 정해지기 전의 placeholder입니다.
        /// </summary>
        public static Sprite Appearance(string appearanceId)
        {
            if (!CharacterAppearances.IsValid(appearanceId))
            {
                appearanceId = CharacterAppearances.Default;
            }

            if (!appearances.TryGetValue(appearanceId, out var sprite) || sprite == null)
            {
                sprite = appearanceId == CharacterAppearances.Plain
                    ? Circle
                    : CreateCircle("PrototypeAppearance_" + appearanceId, GetAppearanceShade(appearanceId));
                appearances[appearanceId] = sprite;
            }

            return sprite;
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
            return CreateCircle("PrototypeCircle", null);
        }

        /// <summary>원 안쪽 밝기를 정규화 좌표(-1~1)로 받는 함수로 무늬를 그립니다. 색 틴트와 곱해지므로 1이 무늬 없음입니다.</summary>
        private static Sprite CreateCircle(string name, Func<float, float, float> shade)
        {
            var texture = new Texture2D(CircleResolution, CircleResolution, TextureFormat.RGBA32, false)
            {
                name = name,
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
                    var value = (byte)((shade != null ? Mathf.Clamp01(shade(dx / radius, dy / radius)) : 1f) * 255);
                    pixels[y * CircleResolution + x] = new Color32(value, value, value, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, CircleResolution, CircleResolution), new Vector2(0.5f, 0.5f), CircleResolution);
        }

        private static Func<float, float, float> GetAppearanceShade(string appearanceId)
        {
            switch (appearanceId)
            {
                case CharacterAppearances.Ring:
                    return (x, y) =>
                    {
                        var r = Mathf.Sqrt(x * x + y * y);
                        return r > 0.5f && r < 0.68f ? 0.45f : 1f;
                    };
                case CharacterAppearances.Eyes:
                    return (x, y) => IsInDot(x, y, -0.32f, 0.2f, 0.17f) || IsInDot(x, y, 0.32f, 0.2f, 0.17f) ? 0.12f : 1f;
                case CharacterAppearances.Spots:
                    return (x, y) => IsInDot(x, y, -0.35f, 0.3f, 0.15f)
                        || IsInDot(x, y, 0.3f, 0.4f, 0.11f)
                        || IsInDot(x, y, 0.1f, -0.1f, 0.13f)
                        || IsInDot(x, y, -0.25f, -0.45f, 0.1f)
                        || IsInDot(x, y, 0.45f, -0.35f, 0.12f)
                        ? 0.55f : 1f;
                default:
                    return null;
            }
        }

        private static bool IsInDot(float x, float y, float cx, float cy, float radius)
        {
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy < radius * radius;
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
