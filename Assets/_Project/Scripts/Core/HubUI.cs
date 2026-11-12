using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualOffice.Core
{
    /// 허브 화면: 평면도(각 방의 planRect를 그대로 축척해 그린 사각형)를 눌러서
    /// 방을 고르는 화면 + 심사 단계·일자 탭. 실제 미니맵(Top-down 카메라 +
    /// RenderTexture)은 다음 단계에서 교체 — 이번 "대략적" 구현은 버튼판으로
    /// 같은 동작(둘러보기 전 평면도에서 방 선택)을 보여준다(개발명세서 4·10절).
    /// 카드·버튼은 전부 둥근 모서리 + 그림자(UiStyle)로 통일한다. 텍스트는
    /// TextMeshPro + Pretendard SDF 폰트(한글 지원, HudUI.cs 참고).
    public class HubUI : MonoBehaviour
    {
        public static HubUI Instance { get; private set; }

        public event Action<string> RoomSelected;

        private TMP_FontAsset _font;
        private List<RoomData> _rooms;
        private readonly Dictionary<ReviewPhase, Button> _phaseButtons = new Dictionary<ReviewPhase, Button>();
        private readonly Dictionary<CampDay, Button> _dayButtons = new Dictionary<CampDay, Button>();

        private static readonly Color ActiveTab = new Color(0.86f, 0.63f, 0.27f, 0.95f);
        private static readonly Color InactiveTab = new Color(0.18f, 0.17f, 0.16f, 0.85f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.30f);
        private static readonly Vector2 ShadowOffset = new Vector2(3f, -3f);

        public static HubUI Create(List<RoomData> rooms)
        {
            var go = new GameObject("HubUI");
            var hub = go.AddComponent<HubUI>();
            hub.Init(rooms);
            return hub;
        }

        private void Init(List<RoomData> rooms)
        {
            Instance = this;
            _rooms = rooms;
            _font = Resources.Load<TMP_FontAsset>("Pretendard-Regular SDF");
            if (_font == null) _font = TMP_Settings.defaultFontAsset;

            var canvasGo = new GameObject("HubCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 전체 배경은 화면 꽉 채우는 용도라 모서리를 안 둥글린다(CreateSquarePanel)
            var bg = CreateSquarePanel(canvasGo.transform, "Background", new Color(0.05f, 0.05f, 0.06f, 0.92f));
            FillParent(bg.GetComponent<RectTransform>());

            BuildTitle(bg.transform);
            BuildPhaseTabs(bg.transform);
            BuildDayTabs(bg.transform);
            BuildFloorplan(bg.transform);
        }

        private void BuildTitle(Transform parent)
        {
            var title = CreateText(parent, "Title", "가상 업무 인계인수 공간 — 둘러볼 공간을 선택하세요", 26, TextAlignmentOptions.Center);
            Anchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1000, 50));
        }

        private void BuildPhaseTabs(Transform parent)
        {
            ReviewPhase[] phases = { ReviewPhase.BasicSurvey, ReviewPhase.MainReview, ReviewPhase.RevisionReview, ReviewPhase.SampleInspection };
            float spacing = 190f;
            float startX = -(spacing * (phases.Length - 1)) / 2f;

            for (int i = 0; i < phases.Length; i++)
            {
                ReviewPhase phase = phases[i];
                var button = CreateButton(parent, "Phase_" + phase, HudUI.PhaseLabel(phase), InactiveTab,
                    () => { GameManager.Instance.SetPhase(phase); RefreshTabHighlight(); if (HudUI.Instance != null) HudUI.Instance.RefreshPhaseDayLabel(); },
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -130), new Vector2(170, 46));
                _phaseButtons[phase] = button;
            }
        }

        private void BuildDayTabs(Transform parent)
        {
            CampDay[] days = { CampDay.Setup1, CampDay.Setup2, CampDay.Mid, CampDay.CheckOut1, CampDay.CheckOut2 };
            float spacing = 150f;
            float startX = -(spacing * (days.Length - 1)) / 2f;

            for (int i = 0; i < days.Length; i++)
            {
                CampDay day = days[i];
                var button = CreateButton(parent, "Day_" + day, HudUI.DayLabel(day), InactiveTab,
                    () => { GameManager.Instance.SetDay(day); RefreshTabHighlight(); if (HudUI.Instance != null) HudUI.Instance.RefreshPhaseDayLabel(); },
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -190), new Vector2(130, 40));
                _dayButtons[day] = button;
            }

            RefreshTabHighlight();
        }

        private void RefreshTabHighlight()
        {
            if (GameManager.Instance == null) return;

            foreach (var kv in _phaseButtons)
            {
                kv.Value.targetGraphic.color = kv.Key == GameManager.Instance.CurrentPhase ? ActiveTab : InactiveTab;
            }
            foreach (var kv in _dayButtons)
            {
                kv.Value.targetGraphic.color = kv.Key == GameManager.Instance.CurrentDay ? ActiveTab : InactiveTab;
            }
        }

        private void BuildFloorplan(Transform parent)
        {
            var plan = CreateCard(parent, "Floorplan", new Color(0.12f, 0.11f, 0.1f, 0.55f));
            var planRt = plan.GetComponent<RectTransform>();
            Anchor(planRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1520, 330));

            foreach (var room in _rooms)
            {
                var r = room.planRect; // x,y,w,h normalized 0..1 (y grows downward in the original plan)
                var accent = room.accentColor;

                // 평면도 좌표계: planRect는 원본 2D 도면 기준(x,y,w,h), y는 아래로 증가 →
                // UI 앵커는 위가 1이므로 뒤집는다
                float left = r.x, top = r.y, width = r.z, height = r.w;
                var min = new Vector2(left, 1f - top - height);
                var max = new Vector2(left + width, 1f - top);

                var button = CreateButton(plan.transform, "Room_" + room.roomId, room.label,
                    new Color(accent.r, accent.g, accent.b, 0.92f),
                    () => RoomSelected?.Invoke(room.roomId),
                    min, max, Vector2.zero, Vector2.zero, inset: 6f);
            }
        }

        // ---- small UI helpers (self-contained, same style as HudUI) ----

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

        /// 화면 전체 배경처럼 모서리를 둥글리면 안 되는 패널(FillParent로 꽉 채울 때)
        private Image CreateSquarePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        /// 둥근 모서리 카드(버튼이 아닌 배경판에 사용 — 예: 평면도 전체 틀)
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

        /// 둥근 모서리 + 그림자가 있는 버튼. anchorMin/anchorMax/pos/size로 직접 배치하거나,
        /// (평면도처럼) anchorMin/anchorMax만으로 영역을 잡고 inset으로 안쪽 여백만 줄 수도 있다.
        private Button CreateButton(Transform parent, string name, string label, Color color, Action onClick,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, float inset = 0f)
        {
            var shadow = CreateCard(parent, name + "_Shadow", ShadowColor);
            var shadowRt = shadow.GetComponent<RectTransform>();
            ApplyAnchorRect(shadowRt, anchorMin, anchorMax, pos + ShadowOffset, size, inset);

            var card = CreateCard(parent, name, color);
            var cardRt = card.GetComponent<RectTransform>();
            ApplyAnchorRect(cardRt, anchorMin, anchorMax, pos, size, inset);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = CreateText(card.transform, "Label", label, 15, TextAlignmentOptions.Center);
            FillParent(text.GetComponent<RectTransform>());

            return button;
        }

        private static void ApplyAnchorRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, float inset)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            if (size == Vector2.zero && anchorMin != anchorMax)
            {
                // 영역(anchorMin~anchorMax)만으로 배치 — 늘어난(stretched) 사각형이라
                // anchoredPosition이 아니라 offsetMin/Max 양쪽에 pos를 더해야 그림자가
                // 카드 뒤에서 실제로 비껴 보인다(inset은 안쪽 여백)
                rt.offsetMin = new Vector2(inset, inset) + pos;
                rt.offsetMax = new Vector2(-inset, -inset) + pos;
            }
            else
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
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

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
