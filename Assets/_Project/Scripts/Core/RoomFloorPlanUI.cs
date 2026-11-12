using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualOffice.Core
{
    /// 방 상세 화면 — 3D로 걸어 들어가는 대신, 그 방을 위에서 본 2D 평면도로 보여준다
    /// (사용자 요청으로 3D 1인칭 체험을 전면 대체함, 개발명세서 참고). 허브의 평면도
    /// 버튼판과 같은 "정규화 좌표 → 사각 영역" 방식을 그대로 가구 배치에 적용한다.
    /// 가구 아이콘을 누르면 QnaPanel에 그 가구 이름을 그대로 질문해서 답을 보여준다.
    /// 1차 구현은 보기 전용 — 드래그 편집/저장은 다음 단계.
    public class RoomFloorPlanUI : MonoBehaviour
    {
        public static RoomFloorPlanUI Instance { get; private set; }

        private TMP_FontAsset _font;
        private RectTransform _stage;
        private TMP_Text _titleText;
        private RoomData _currentRoom;

        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.30f);
        private static readonly Vector2 ShadowOffset = new Vector2(2f, -2f);

        public static RoomFloorPlanUI Create()
        {
            var go = new GameObject("RoomFloorPlanUI");
            var ui = go.AddComponent<RoomFloorPlanUI>();
            ui.Init();
            return ui;
        }

        private void Init()
        {
            Instance = this;
            _font = Resources.Load<TMP_FontAsset>("Pretendard-Regular SDF");
            if (_font == null) _font = TMP_Settings.defaultFontAsset;

            var canvasGo = new GameObject("RoomFloorPlanCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = CreateSquarePanel(canvasGo.transform, "Background", new Color(0.05f, 0.05f, 0.06f, 0.96f));
            FillParent(bg.GetComponent<RectTransform>());

            CreateButton(bg.transform, "HomeButton", "홈", new Color(0.10f, 0.09f, 0.08f, 0.85f), GoHome,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -50), new Vector2(64, 64));

            _titleText = CreateText(bg.transform, "Title", "", 24, TextAlignmentOptions.Center);
            Anchor(_titleText.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(900, 40));

            var stagePanel = CreateCard(bg.transform, "Stage", new Color(0.14f, 0.13f, 0.12f, 0.9f));
            Anchor(stagePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1100, 620));
            _stage = stagePanel.GetComponent<RectTransform>();

            gameObject.SetActive(false);
        }

        public void Show(RoomData room)
        {
            _currentRoom = room;
            _titleText.text = room.label + " — 평면도";

            for (int i = _stage.childCount - 1; i >= 0; i--)
            {
                Destroy(_stage.GetChild(i).gameObject);
            }

            // 바닥(방 색상 은은하게)
            var floor = CreateSquarePanel(_stage, "Floor", Lighten(room.accentColor, 0.75f));
            FillParent(floor.GetComponent<RectTransform>());

            foreach (var item in room.furniture)
            {
                BuildIcon(item);
            }

            HubUI.Instance?.SetVisible(false);
            QnaPanel.Instance?.SetRoom(room);
            QnaPanel.Instance?.SetVisible(true);
            HudUI.Instance?.ShowToast("\"" + room.label + "\" 평면도 — 가구를 눌러서 업무를 확인하세요.");

            gameObject.SetActive(true);
        }

        private void GoHome()
        {
            gameObject.SetActive(false);
            QnaPanel.Instance?.SetVisible(false);
            HubUI.Instance?.SetVisible(true);
        }

        // ---- furniture icons ----

        private void BuildIcon(FurnitureEntry entry)
        {
            // room.size 기준으로 로컬 좌표(x,z, 미터)를 0~1 정규화 — 허브 평면도와 같은 방식
            float nx = Mathf.Clamp01(entry.localPosition.x / _currentRoom.size.x + 0.5f);
            float nz = Mathf.Clamp01(entry.localPosition.z / _currentRoom.size.z + 0.5f);
            float ny = 1f - nz; // 화면 위쪽이 방 안쪽(+Z)이 되도록 뒤집음

            (Vector2 size, Color color) = IconStyle(entry.kind);

            var icon = CreateButton(_stage, "Icon_" + entry.kind, "", color,
                () => QnaPanel.Instance?.AskAbout(entry.label),
                new Vector2(nx, ny), new Vector2(nx, ny), Vector2.zero, size);

            var label = CreateText(icon.transform, "Label", entry.label, 11, TextAlignmentOptions.Center);
            var labelRt = label.GetComponent<RectTransform>();
            Anchor(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -16), new Vector2(size.x + 40, 28));
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
        }

        private static (Vector2 size, Color color) IconStyle(FurnitureKind kind)
        {
            switch (kind)
            {
                case FurnitureKind.Table: return (new Vector2(220, 90), new Color(0.62f, 0.47f, 0.30f));
                case FurnitureKind.TriDeskCluster: return (new Vector2(150, 110), new Color(0.55f, 0.42f, 0.30f));
                case FurnitureKind.Desks: return (new Vector2(90, 60), new Color(0.62f, 0.47f, 0.30f));
                case FurnitureKind.ExecDesk: return (new Vector2(100, 70), new Color(0.40f, 0.30f, 0.20f));
                case FurnitureKind.Shelf: return (new Vector2(60, 90), new Color(0.40f, 0.30f, 0.20f));
                case FurnitureKind.Whiteboard: return (new Vector2(80, 20), new Color(0.90f, 0.90f, 0.90f));
                case FurnitureKind.Tv: return (new Vector2(70, 18), new Color(0.08f, 0.08f, 0.10f));
                case FurnitureKind.Printer: return (new Vector2(45, 45), new Color(0.75f, 0.75f, 0.76f));
                case FurnitureKind.Crate: return (new Vector2(55, 55), new Color(0.72f, 0.58f, 0.40f));
                case FurnitureKind.Sofa: return (new Vector2(110, 55), new Color(0.72f, 0.60f, 0.45f));
                case FurnitureKind.Cooler: return (new Vector2(30, 30), new Color(0.55f, 0.78f, 0.92f));
                case FurnitureKind.SideTable: return (new Vector2(40, 40), new Color(0.62f, 0.47f, 0.30f));
                case FurnitureKind.SnackCounter: return (new Vector2(70, 45), new Color(0.72f, 0.58f, 0.40f));
                case FurnitureKind.Counter: return (new Vector2(90, 30), new Color(0.52f, 0.39f, 0.25f));
                case FurnitureKind.Cctv: return (new Vector2(20, 20), new Color(0.2f, 0.2f, 0.22f));
                case FurnitureKind.CallBell: return (new Vector2(20, 24), new Color(0.80f, 0.25f, 0.25f));
                case FurnitureKind.DocSet: return (new Vector2(30, 20), new Color(0.93f, 0.80f, 0.40f));
                default: return (new Vector2(50, 50), new Color(0.5f, 0.5f, 0.5f));
            }
        }

        private static Color Lighten(Color c, float amount) => Color.Lerp(c, Color.white, amount);

        // ---- small UI helpers (same style as HubUI/HudUI) ----

        private TMP_Text CreateText(Transform parent, string name, string text, int size, TextAlignmentOptions anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.text = text;
            return t;
        }

        private Image CreateSquarePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private Image CreateCard(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiStyle.RoundedPanel();
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private Button CreateButton(Transform parent, string name, string label, Color color, System.Action onClick,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var shadow = CreateCard(parent, name + "_Shadow", ShadowColor);
            Anchor(shadow.GetComponent<RectTransform>(), anchorMin, anchorMax, pos + ShadowOffset, size);

            var card = CreateCard(parent, name, color);
            Anchor(card.GetComponent<RectTransform>(), anchorMin, anchorMax, pos, size);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(label))
            {
                var text = CreateText(card.transform, "Label", label, 16, TextAlignmentOptions.Center);
                FillParent(text.GetComponent<RectTransform>());
            }

            return button;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
