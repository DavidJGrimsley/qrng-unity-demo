using UnityEngine;
using UnityEngine.UI;

namespace QRNG
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CoinDiscGraphic : MaskableGraphic
    {
        [SerializeField] Color fillColor = new Color(0.95f, 0.72f, 0.25f, 1f);
        [SerializeField] Color rimColor = new Color(1f, 0.86f, 0.42f, 1f);
        [SerializeField] int segments = 96;

        static readonly Color NeutralFill = new Color(0.95f, 0.72f, 0.25f, 1f);
        static readonly Color NeutralRim = new Color(1f, 0.86f, 0.42f, 1f);
        static readonly Color HeadsFill = new Color(0.18f, 0.74f, 0.67f, 1f);
        static readonly Color HeadsRim = new Color(0.65f, 1f, 0.91f, 1f);
        static readonly Color TailsFill = new Color(0.93f, 0.46f, 0.28f, 1f);
        static readonly Color TailsRim = new Color(1f, 0.75f, 0.52f, 1f);

        public void ShowNeutral()
        {
            fillColor = NeutralFill;
            rimColor = NeutralRim;
            SetVerticesDirty();
        }

        public void ShowResult(bool isHeads)
        {
            fillColor = isHeads ? HeadsFill : TailsFill;
            rimColor = isHeads ? HeadsRim : TailsRim;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var innerRadius = radius * 0.78f;
            var steps = Mathf.Clamp(segments, 24, 160);
            var center = rect.center;

            var centerIndex = vh.currentVertCount;
            vh.AddVert(center, fillColor, Vector2.zero);

            var innerStart = vh.currentVertCount;
            for (var i = 0; i < steps; i++)
            {
                var angle = Mathf.PI * 2f * i / steps;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * innerRadius;
                vh.AddVert(point, fillColor, Vector2.zero);
            }

            for (var i = 0; i < steps; i++)
            {
                vh.AddTriangle(centerIndex, innerStart + i, innerStart + ((i + 1) % steps));
            }

            var ringInnerStart = vh.currentVertCount;
            for (var i = 0; i < steps; i++)
            {
                var angle = Mathf.PI * 2f * i / steps;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * innerRadius;
                vh.AddVert(point, rimColor, Vector2.zero);
            }

            var ringOuterStart = vh.currentVertCount;
            for (var i = 0; i < steps; i++)
            {
                var angle = Mathf.PI * 2f * i / steps;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(point, rimColor, Vector2.zero);
            }

            for (var i = 0; i < steps; i++)
            {
                var next = (i + 1) % steps;
                vh.AddTriangle(ringInnerStart + i, ringOuterStart + i, ringOuterStart + next);
                vh.AddTriangle(ringInnerStart + i, ringOuterStart + next, ringInnerStart + next);
            }
        }
    }
}
