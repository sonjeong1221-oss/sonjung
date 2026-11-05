using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualOffice.Core
{
    /// 허브 화면: 평면도(각 방의 planRect를 그대로 축척해 그린 사각형)를 눌러서
    /// 방을 고르는 화면 + 심사 단계·일자 탭. 실제 미니맵(Top-down 카메라 +
    /// RenderTexture)은 다음 단계에서 교체 — 이번 "대략적" 구현은 버튼판으로
    /// 같은 동작(둘러보기 전 평면도에서 방 선택)을 보여준다(개발명세서 4·10절).
    public class HubUI : MonoBehaviour
    {
        public static HubUI Instance { get; private set; }

        public event Action<string> RoomSelected;

        private Font _font;
        private List<RoomData> _rooms;
        private readonly Dictionary<ReviewPhase, Button> _phaseButtons = new Dictionary<ReviewPhase, Button>();
        private readonly Dictionary<CampDay, Button> _dayButtons = new Dictionary<CampDay, Button>();

        private static readonly Color ActiveTab = new Color(0.86f, 0.63f, 0.27f, 0.95f);
        private static readonly Color InactiveTab = new Color(0.15f, 0.14f, 0.12f, 0.6f);

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
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HubCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = CreatePanel(canvasGo.transform, "Background", new Color(0.05f, 0.05f, 0.06f, 0.92f));
            FillParent(bg.GetComponent<RectTransform>());

            BuildTitle(bg.transform);
            BuildPhaseTabs(bg.transform);
            BuildDayTabs(bg.transform);
            BuildFloorplan(bg.transform);
        }

        private void BuildTitle(Transform parent)
        {
            var title = CreateText(parent, "Title", "가상 업무 인계인수 공간 — 둘러볼 공간을 선택하세요", 26, TextAnchor.MiddleCenter);
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
                    () => { GameManager.Instance.SetPhase(phase); RefreshTabHighlight(); if (HudUI.Instance != null) HudUI.Instance.RefreshPhaseDayLabel(); });
                Anchor(button.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -130), new Vector2(170, 46));
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
                    () => { GameManager.Instance.SetDay(day); RefreshTabHighlight(); if (HudUI.Instance != null) HudUI.Instance.RefreshPhaseDayLabel(); });
                Anchor(button.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -190), new Vector2(130, 40));
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
            var plan = CreatePanel(parent, "Floorplan", new Color(0.12f, 0.11f, 0.1f, 0.6f));
            var planRt = plan.GetComponent<RectTransform>();
            Anchor(planRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1500, 320));

            foreach (var room in _rooms)
            {
                var r = room.planRect; // x,y,w,h normalized 0..1 (y grows downward in the original plan)
                var accent = room.accentColor;

                var button = CreateButton(plan.transform, "Room_" + room.roomId, room.label,
                    new Color(accent.r, accent.g, accent.b, 0.85f),
                    () => RoomSelected?.Invoke(room.roomId));

                // 평면도 좌표계: planRect는 원본 2D 도면 기준(x,y,w,h), y는 아래로 증가 →
                // UI 앵커는 위가 1이므로 뒤집는다
                float left = r.x, top = r.y, width = r.z, height = r.w;
                var rt = button.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(left, 1f - top - height);
                rt.anchorMax = new Vector2(left + width, 1f - top);
                rt.offsetMin = new Vector2(4, 4);
                rt.offsetMax = new Vector2(-4, -4);
            }
        }

        // ---- small UI helpers (self-contained, same style as HudUI) ----

        private Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.text = text;
            return t;
        }

        private Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private Button CreateButton(Transform parent, string name, string label, Color color, Action onClick)
        {
            var panel = CreatePanel(parent, name, color);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = CreateText(panel.transform, "Label", label, 15, TextAnchor.MiddleCenter);
            var textRt = text.GetComponent<RectTransform>();
            FillParent(textRt);

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

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
