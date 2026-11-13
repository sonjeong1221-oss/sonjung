using UnityEngine;

namespace VirtualOffice.Core
{
    /// 코드로 만드는 UI(HudUI/HubUI/QnaPanel)가 전부 같은 느낌을 내도록, 둥근 모서리
    /// 스프라이트를 한 번만 만들어서 재사용한다. 외부 이미지 파일 없이 런타임에
    /// 텍스처를 직접 그려서 9-슬라이스 스프라이트로 쓰기 때문에 어떤 크기의
    /// 버튼/카드에도 모서리가 안 뭉개진다.
    public static class UiStyle
    {
        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;

        public static Sprite RoundedPanel()
        {
            if (_roundedSprite != null) return _roundedSprite;

            const int size = 64;
            const int radius = 18;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var colors = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = CornerAlpha(x, y, size, size, radius);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(colors);
            tex.Apply();

            var border = new Vector4(radius, radius, radius, radius);
            _roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return _roundedSprite;
        }

        /// 2D 평면도 가구 아이콘 안에 렌즈·버튼·병 모양 등 작은 원형 장식을 그릴 때 쓰는
        /// 단색 원 스프라이트(안티에일리어싱된 가장자리, 9-슬라이스 아님 — 항상 정원으로 보임).
        public static Sprite Circle()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var colors = new Color32[size * size];
            float r = size / 2f;
            var center = new Vector2(r, r);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(r - dist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(colors);
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circleSprite;
        }

        private static float CornerAlpha(int x, int y, int w, int h, int r)
        {
            float cx = Mathf.Clamp(x, r, w - r);
            float cy = Mathf.Clamp(y, r, h - r);
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= r - 1f) return 1f;
            if (dist >= r + 1f) return 0f;
            return 1f - (dist - (r - 1f)) / 2f;
        }
    }
}
