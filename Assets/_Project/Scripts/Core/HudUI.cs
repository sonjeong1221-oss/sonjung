using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace VirtualOffice.Core
{
    /// 전체 화면 HUD를 코드로 직접 만든다(.prefab/.scene 없이) — 이 세션에는 Unity
    /// 에디터가 없어 손으로 쓴 씬/프리팹의 GUID가 깨지기 쉽다는 제약을 피하기 위한
    /// 선택이다. 레퍼런스 이미지의 레이아웃(좌상단 홈+토스트, 좌하단 조작키 카드,
    /// 하단 중앙 아이콘 4개)을 그대로 따른다(개발명세서 10절). 미니맵은 이번 "대략적"
    /// 구현에서는 실제 Top-down 카메라 대신 평면도 버튼판(HubUI)으로 대체한다.
    public class HudUI : MonoBehaviour
    {
        public static HudUI Instance { get; private set; }

        public event Action HomeClicked;

        private Text _toastText;
        private CanvasGroup _toastGroup;
        private Coroutine _toastRoutine;

        private Text _promptText;
        private Text _phaseDayLabel;
        private Font _font;

        public Text PromptLabel => _promptText;

        public static HudUI Create()
        {
            var go = new GameObject("HudUI");
            return go.AddComponent<HudUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            EnsureEventSystem();
            var canvas = BuildCanvasRoot();

            BuildHomeAndToast(canvas);
            BuildKeyHintCard(canvas);
            BuildBottomDock(canvas);
            BuildPromptLabel(canvas);
            BuildPhaseDayIndicator(canvas);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(go);
        }

        private RectTransform BuildCanvasRoot()
        {
            var go = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return go.GetComponent<RectTransform>();
        }

        // ---- building blocks ----

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
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
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

            var text = CreateText(panel.transform, "Label", label, 16, TextAnchor.MiddleCenter);
            var textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return button;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ---- panels ----

        private void BuildHomeAndToast(Transform canvas)
        {
            var homeColor = new Color(0.08f, 0.07f, 0.06f, 0.55f);
            var home = CreateButton(canvas, "HomeButton", "홈", homeColor, () => HomeClicked?.Invoke());
            Anchor(home.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -50), new Vector2(64, 64));

            var toastPanel = CreatePanel(canvas, "Toast", new Color(0.08f, 0.07f, 0.06f, 0.55f));
            var toastRt = toastPanel.GetComponent<RectTransform>();
            Anchor(toastRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -30), new Vector2(360, 64));

            _toastGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;

            _toastText = CreateText(toastPanel.transform, "ToastText", "", 15, TextAnchor.MiddleLeft);
            var textRt = _toastText.GetComponent<RectTransform>();
            Anchor(textRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            textRt.offsetMin = new Vector2(14, 6);
            textRt.offsetMax = new Vector2(-14, -6);
        }

        private void BuildKeyHintCard(Transform canvas)
        {
            var panel = CreatePanel(canvas, "KeyHintCard", new Color(0.08f, 0.07f, 0.06f, 0.45f));
            var rt = panel.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(150, 120), new Vector2(260, 150));

            var text = CreateText(panel.transform, "Text",
                "W A S D   이동\n마우스   시점 회전\nE   상호작용\nESC   메뉴 열기",
                14, TextAnchor.UpperLeft);
            var textRt = text.GetComponent<RectTransform>();
            Anchor(textRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            textRt.offsetMin = new Vector2(14, 10);
            textRt.offsetMax = new Vector2(-14, -10);
        }

        private void BuildBottomDock(Transform canvas)
        {
            string[] labels = { "자료함", "교육자료", "체크리스트", "설정" };
            float spacing = 110f;
            float startX = -(spacing * (labels.Length - 1)) / 2f;

            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                var button = CreateButton(canvas, "Dock_" + label, label,
                    new Color(0.08f, 0.07f, 0.06f, 0.5f),
                    () => ShowToast(label + " — 다음 단계에서 실제 기능 연결 예정"));
                var rt = button.GetComponent<RectTransform>();
                Anchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(startX + i * spacing, 70), new Vector2(84, 84));
            }
        }

        private void BuildPromptLabel(Transform canvas)
        {
            _promptText = CreateText(canvas, "InteractPrompt", "", 18, TextAnchor.MiddleCenter);
            _promptText.gameObject.SetActive(false);
            var rt = _promptText.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(560, 36));
        }

        private void BuildPhaseDayIndicator(Transform canvas)
        {
            _phaseDayLabel = CreateText(canvas, "PhaseDayLabel", "", 16, TextAnchor.MiddleCenter);
            var rt = _phaseDayLabel.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(500, 30));
            RefreshPhaseDayLabel();
        }

        // ---- public API ----

        public void ShowToast(string message, float seconds = 4f)
        {
            _toastText.text = message;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(seconds));
        }

        private IEnumerator ToastRoutine(float seconds)
        {
            float t = 0f;
            while (t < 0.2f) { t += Time.deltaTime; _toastGroup.alpha = t / 0.2f; yield return null; }
            _toastGroup.alpha = 1f;
            yield return new WaitForSeconds(seconds);
            t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; _toastGroup.alpha = 1f - t / 0.4f; yield return null; }
            _toastGroup.alpha = 0f;
        }

        public void RefreshPhaseDayLabel()
        {
            if (GameManager.Instance == null) return;
            _phaseDayLabel.text = PhaseLabel(GameManager.Instance.CurrentPhase) + " · " + DayLabel(GameManager.Instance.CurrentDay);
        }

        public static string PhaseLabel(ReviewPhase phase)
        {
            switch (phase)
            {
                case ReviewPhase.BasicSurvey: return "기초조사";
                case ReviewPhase.MainReview: return "본심사";
                case ReviewPhase.RevisionReview: return "수정본 검토";
                case ReviewPhase.SampleInspection: return "견본 검수";
                default: return phase.ToString();
            }
        }

        public static string DayLabel(CampDay day)
        {
            switch (day)
            {
                case CampDay.Setup1: return "설치1일차";
                case CampDay.Setup2: return "설치2일차";
                case CampDay.Mid: return "중간";
                case CampDay.CheckOut1: return "퇴소1일차";
                case CampDay.CheckOut2: return "퇴소2일차";
                default: return day.ToString();
            }
        }
    }
}
