using UnityEngine;
using UnityEngine.UI;

namespace QRNG
{
    public enum ArcadeIconKind
    {
        Coin,
        Monster,
        Chest,
        Character,
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeIconGraphic : MaskableGraphic
    {
        [SerializeField] ArcadeIconKind kind;
        [SerializeField] int variant;
        [SerializeField] int segments = 72;

        public ArcadeIconKind Kind
        {
            get => kind;
            set
            {
                if (kind == value)
                {
                    return;
                }

                kind = value;
                SetVerticesDirty();
            }
        }

        public int Variant
        {
            get => variant;
            set
            {
                if (variant == value)
                {
                    return;
                }

                variant = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var size = Mathf.Min(rect.width, rect.height);

            switch (kind)
            {
                case ArcadeIconKind.Monster:
                    DrawMonster(vh, center, size);
                    break;
                case ArcadeIconKind.Chest:
                    DrawChest(vh, center, size);
                    break;
                case ArcadeIconKind.Character:
                    DrawCharacter(vh, center, size);
                    break;
                default:
                    DrawCoin(vh, center, size);
                    break;
            }
        }

        void DrawCoin(VertexHelper vh, Vector2 center, float size)
        {
            var palette = variant == 1
                ? new[] { Hex(0xff8a5b), Hex(0x4a1523), Hex(0xffd7a8) }
                : new[] { Hex(0x4ce0c6), Hex(0x102b3a), Hex(0xb7fff2) };
            AddCircle(vh, center, size * 0.45f, Hex(0xffcf54));
            AddCircle(vh, center, size * 0.36f, palette[0]);
            AddCircle(vh, center + new Vector2(-size * 0.08f, size * 0.1f), size * 0.065f, palette[2]);
            AddRect(vh, center + new Vector2(size * 0.08f, -size * 0.02f), size * 0.18f, size * 0.34f, palette[1]);
            AddRect(vh, center + new Vector2(-size * 0.03f, -size * 0.02f), size * 0.18f, size * 0.07f, palette[1]);
        }

        void DrawMonster(VertexHelper vh, Vector2 center, float size)
        {
            var caught = variant == 1;
            var body = caught ? Hex(0x63e38c) : Hex(0x9a69ff);
            var shadow = caught ? Hex(0x24583a) : Hex(0x352361);
            AddEllipse(vh, center + new Vector2(0f, -size * 0.03f), size * 0.62f, size * 0.48f, shadow);
            AddEllipse(vh, center + new Vector2(0f, size * 0.02f), size * 0.54f, size * 0.48f, body);
            AddTriangle(vh, center + new Vector2(-size * 0.19f, size * 0.19f), size * 0.15f, size * 0.2f, body);
            AddTriangle(vh, center + new Vector2(size * 0.19f, size * 0.19f), size * 0.15f, size * 0.2f, body);
            AddCircle(vh, center + new Vector2(-size * 0.13f, size * 0.07f), size * 0.065f, Color.white);
            AddCircle(vh, center + new Vector2(size * 0.13f, size * 0.07f), size * 0.065f, Color.white);
            AddCircle(vh, center + new Vector2(-size * 0.11f, size * 0.055f), size * 0.028f, Hex(0x07151f));
            AddCircle(vh, center + new Vector2(size * 0.11f, size * 0.055f), size * 0.028f, Hex(0x07151f));
            AddRect(vh, center + new Vector2(0f, -size * 0.11f), size * 0.2f, size * 0.055f, Hex(0x07151f));
            AddCircle(vh, center + new Vector2(size * 0.3f, -size * 0.24f), size * 0.12f, Hex(0xfff2d5));
            AddCircle(vh, center + new Vector2(size * 0.3f, -size * 0.24f), size * 0.07f, caught ? Hex(0x63e38c) : Hex(0xf05f78));
        }

        void DrawChest(VertexHelper vh, Vector2 center, float size)
        {
            var colors = new[]
            {
                Hex(0x9fc7ff),
                Hex(0x6dff9d),
                Hex(0x8d6dff),
                Hex(0xffb84c),
                Hex(0xff5cff),
            };
            var glow = colors[Mathf.Abs(variant) % colors.Length];
            AddCircle(vh, center + new Vector2(0f, size * 0.03f), size * 0.48f, WithAlpha(glow, 0.23f));
            AddRect(vh, center + new Vector2(0f, -size * 0.09f), size * 0.68f, size * 0.32f, Hex(0x704028));
            AddRect(vh, center + new Vector2(0f, size * 0.09f), size * 0.7f, size * 0.24f, Hex(0xb86d32));
            AddRect(vh, center + new Vector2(0f, size * 0.04f), size * 0.74f, size * 0.08f, Hex(0xffcf54));
            AddRect(vh, center, size * 0.12f, size * 0.42f, Hex(0xffcf54));
            AddRect(vh, center + new Vector2(0f, -size * 0.05f), size * 0.17f, size * 0.12f, glow);
        }

        void DrawCharacter(VertexHelper vh, Vector2 center, float size)
        {
            var colors = new[]
            {
                Hex(0x55d6ff),
                Hex(0xff6d99),
                Hex(0xffd166),
                Hex(0x8cff66),
            };
            var accent = colors[Mathf.Abs(variant) % colors.Length];
            AddCircle(vh, center, size * 0.47f, WithAlpha(accent, 0.18f));
            AddCircle(vh, center + new Vector2(0f, size * 0.15f), size * 0.17f, Hex(0xf6e1bd));
            AddRect(vh, center + new Vector2(0f, -size * 0.06f), size * 0.36f, size * 0.27f, accent);
            AddEllipse(vh, center + new Vector2(0f, -size * 0.21f), size * 0.62f, size * 0.22f, Hex(0x132b3f));
            AddRect(vh, center + new Vector2(-size * 0.055f, size * 0.18f), size * 0.045f, size * 0.045f, Hex(0x07151f));
            AddRect(vh, center + new Vector2(size * 0.055f, size * 0.18f), size * 0.045f, size * 0.045f, Hex(0x07151f));
            AddRect(vh, center + new Vector2(0f, size * 0.09f), size * 0.14f, size * 0.035f, Hex(0x07151f));
            AddTriangle(vh, center + new Vector2(0f, size * 0.33f), size * 0.28f, size * 0.16f, accent);
        }

        void AddCircle(VertexHelper vh, Vector2 center, float radius, Color colorValue)
        {
            AddEllipse(vh, center, radius * 2f, radius * 2f, colorValue);
        }

        void AddEllipse(VertexHelper vh, Vector2 center, float width, float height, Color colorValue)
        {
            var steps = Mathf.Clamp(segments, 24, 120);
            var centerIndex = vh.currentVertCount;
            vh.AddVert(center, colorValue, Vector2.zero);
            var ringStart = vh.currentVertCount;
            for (var i = 0; i < steps; i++)
            {
                var angle = Mathf.PI * 2f * i / steps;
                var point = center + new Vector2(Mathf.Cos(angle) * width * 0.5f, Mathf.Sin(angle) * height * 0.5f);
                vh.AddVert(point, colorValue, Vector2.zero);
            }

            for (var i = 0; i < steps; i++)
            {
                vh.AddTriangle(centerIndex, ringStart + i, ringStart + ((i + 1) % steps));
            }
        }

        static void AddRect(VertexHelper vh, Vector2 center, float width, float height, Color colorValue)
        {
            var start = vh.currentVertCount;
            var half = new Vector2(width * 0.5f, height * 0.5f);
            vh.AddVert(center + new Vector2(-half.x, -half.y), colorValue, Vector2.zero);
            vh.AddVert(center + new Vector2(-half.x, half.y), colorValue, Vector2.zero);
            vh.AddVert(center + new Vector2(half.x, half.y), colorValue, Vector2.zero);
            vh.AddVert(center + new Vector2(half.x, -half.y), colorValue, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        static void AddTriangle(VertexHelper vh, Vector2 center, float width, float height, Color colorValue)
        {
            var start = vh.currentVertCount;
            vh.AddVert(center + new Vector2(0f, height * 0.5f), colorValue, Vector2.zero);
            vh.AddVert(center + new Vector2(-width * 0.5f, -height * 0.5f), colorValue, Vector2.zero);
            vh.AddVert(center + new Vector2(width * 0.5f, -height * 0.5f), colorValue, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xff) / 255f,
                ((rgb >> 8) & 0xff) / 255f,
                (rgb & 0xff) / 255f,
                1f);
        }

        static Color WithAlpha(Color value, float alpha)
        {
            value.a = alpha;
            return value;
        }
    }
}
