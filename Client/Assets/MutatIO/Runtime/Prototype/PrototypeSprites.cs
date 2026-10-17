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
        /// <summary>귀 스프라이트 한 변이 몸 지름의 몇 배인지. 귀는 몸 원 밖으로 나오므로 몸보다 큰 영역에 그립니다.</summary>
        public const float EarSpriteScale = 1.6f;

        // 한 칸이 덮는 월드 단위. 초식 리젠 타일 크기(FOOD-01 초기 가정 5)와 맞춰 타일 경계를 보여줍니다.
        public const float GridCellWorldSize = 5f;

        private const int CircleResolution = 128;
        private const int EarResolution = 160;
        private const int FaceResolution = 128;
        private const int GridCellPixels = 32;

        // Tiled 모드는 스프라이트 한 장마다 정점을 만들므로, 한 장에 여러 칸을 그려 넓은 맵에서도 정점 수를 줄입니다.
        private const int GridCellsPerSprite = 10;

        private static readonly Color32 FaceColor = new Color32(0x14, 0x1B, 0x24, 0xFF);

        private static readonly Dictionary<string, Sprite> ears = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> faces = new Dictionary<string, Sprite>();
        private static Sprite circle;
        private static Sprite grid;
        private static Sprite square;

        private enum EarStyle
        {
            None,
            Pointy,
            Round,
        }

        private enum FaceStyle
        {
            DotEyes,
            DotEyesSmile,
            ClosedEyes,
        }

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

        /// <summary>지름 1 월드 단위의 흰색 원입니다. 캐릭터 몸체와 먹이에 씁니다.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    circle = CreateShape("PrototypeCircle", CircleResolution, 1f, (x, y) => x * x + y * y <= 0.25f, new Color32(255, 255, 255, 255));
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

        /// <summary>
        /// 외형의 귀 스프라이트입니다. 귀가 없으면 null입니다. 흰색이며 몸과 같은 역할색으로 틴트해 몸 뒤에 그립니다.
        /// 크기는 몸 지름의 <see cref="EarSpriteScale"/>배이고 중심은 몸 중심과 같습니다. 충돌 반경에는 영향을 주지 않습니다.
        /// </summary>
        public static Sprite GetEars(string appearanceId)
        {
            var style = GetEarStyle(Normalize(appearanceId));
            if (style == EarStyle.None)
            {
                return null;
            }

            return GetCached(ears, appearanceId, () => CreateShape(
                "PrototypeEars_" + style, EarResolution, EarSpriteScale, GetEarShape(style), new Color32(255, 255, 255, 255)));
        }

        /// <summary>외형의 얼굴(눈·표정) 스프라이트입니다. 몸 지름 1 기준이며 틴트하지 않고 몸 위에 그립니다.</summary>
        public static Sprite GetFace(string appearanceId)
        {
            var style = GetFaceStyle(Normalize(appearanceId));
            return GetCached(faces, appearanceId, () => CreateShape(
                "PrototypeFace_" + style, FaceResolution, 1f, GetFaceShape(style), FaceColor));
        }

        private static string Normalize(string appearanceId)
        {
            return CharacterAppearances.IsValid(appearanceId) ? appearanceId : CharacterAppearances.Default;
        }

        private static EarStyle GetEarStyle(string appearanceId)
        {
            switch (appearanceId)
            {
                case CharacterAppearances.PointyEars:
                    return EarStyle.Pointy;
                case CharacterAppearances.RoundEars:
                    return EarStyle.Round;
                default:
                    return EarStyle.None;
            }
        }

        private static FaceStyle GetFaceStyle(string appearanceId)
        {
            switch (appearanceId)
            {
                case CharacterAppearances.RoundEars:
                    return FaceStyle.DotEyesSmile;
                case CharacterAppearances.Sleepy:
                    return FaceStyle.ClosedEyes;
                default:
                    return FaceStyle.DotEyes;
            }
        }

        private static Sprite GetCached(Dictionary<string, Sprite> cache, string key, Func<Sprite> create)
        {
            key = Normalize(key);
            if (!cache.TryGetValue(key, out var sprite) || sprite == null)
            {
                sprite = create();
                cache[key] = sprite;
            }

            return sprite;
        }

        /// <summary>도형 판정 함수는 몸 중심 기준 좌표(몸 지름 = 1)를 받습니다.</summary>
        private static Func<float, float, bool> GetEarShape(EarStyle style)
        {
            if (style == EarStyle.Pointy)
            {
                return (x, y) => IsInPointyEar(Mathf.Abs(x), y);
            }

            return (x, y) => IsInDot(Mathf.Abs(x), y, 0.31f, 0.36f, 0.15f);
        }

        private static bool IsInPointyEar(float x, float y)
        {
            // 몸 가장자리 안쪽에서 시작해 바깥 위로 뾰족하게 솟은 삼각형입니다.
            return IsInTriangle(x, y, new Vector2(0.08f, 0.38f), new Vector2(0.4f, 0.2f), new Vector2(0.42f, 0.66f));
        }

        private static Func<float, float, bool> GetFaceShape(FaceStyle style)
        {
            switch (style)
            {
                case FaceStyle.DotEyesSmile:
                    return (x, y) => IsInDot(Mathf.Abs(x), y, 0.15f, 0.08f, 0.06f) || IsInSmile(x, y);
                case FaceStyle.ClosedEyes:
                    return (x, y) => Mathf.Abs(Mathf.Abs(x) - 0.15f) < 0.075f && Mathf.Abs(y - 0.06f) < 0.018f;
                default:
                    return (x, y) => IsInDot(Mathf.Abs(x), y, 0.15f, 0.08f, 0.065f);
            }
        }

        private static bool IsInSmile(float x, float y)
        {
            var distance = Mathf.Sqrt(x * x + (y + 0.02f) * (y + 0.02f));
            return y < -0.09f && distance > 0.14f && distance < 0.175f;
        }

        private static bool IsInDot(float x, float y, float cx, float cy, float radius)
        {
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool IsInTriangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            var p = new Vector2(x, y);
            var d1 = Cross(p, a, b);
            var d2 = Cross(p, b, c);
            var d3 = Cross(p, c, a);
            var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }

        /// <summary>
        /// 한 변이 worldSize 월드 단위인 스프라이트에 도형을 그립니다. 픽셀당 4개 표본으로 가장자리를 부드럽게 합니다.
        /// </summary>
        private static Sprite CreateShape(string name, int resolution, float worldSize, Func<float, float, bool> inside, Color32 color)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[resolution * resolution];
            var pixelSize = worldSize / resolution;
            for (var py = 0; py < resolution; py++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    var hits = 0;
                    for (var sample = 0; sample < 4; sample++)
                    {
                        var x = (px + 0.25f + (sample & 1) * 0.5f) * pixelSize - worldSize * 0.5f;
                        var y = (py + 0.25f + (sample >> 1) * 0.5f) * pixelSize - worldSize * 0.5f;
                        if (inside(x, y))
                        {
                            hits++;
                        }
                    }

                    pixels[py * resolution + px] = new Color32(color.r, color.g, color.b, (byte)(color.a * hits / 4));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution / worldSize);
        }

        private static Sprite CreateGrid()
        {
            var size = GridCellPixels * GridCellsPerSprite;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PrototypeGrid",
                wrapMode = TextureWrapMode.Repeat,
            };

            var line = new Color32(255, 255, 255, 28);
            var empty = new Color32(255, 255, 255, 0);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = x % GridCellPixels < 1 || y % GridCellPixels < 1 ? line : empty;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            var worldSize = GridCellWorldSize * GridCellsPerSprite;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / worldSize, 0, SpriteMeshType.FullRect);
        }
    }
}
