using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualOffice.Core
{
    /// 방 안에서 자유 질문을 입력하면 현재 방(RoomData) + 현재 심사 단계/일자
    /// (GameManager)를 기준으로 schedules에서 관련 업무를 찾아 보여준다. CLAUDE.md
    /// 개발 환경의 "싱글 사용자 — 네트워킹/멀티플레이 코드 금지" 원칙에 따라 LLM API
    /// 호출 없이 전부 로컬 키워드 매칭으로 동작한다 — 이게 이전에 "결정 필요 사항"에
    /// 있던 Q&A 구현 방식(오프라인 vs API)을 사실상 확정한다(개발명세서 5·9절).
    /// 텍스트·입력창은 TextMeshPro + Pretendard SDF 폰트(한글 지원, HudUI.cs 참고).
    public class QnaPanel : MonoBehaviour
    {
        public static QnaPanel Instance { get; private set; }

        private RoomData _currentRoom;
        private TMP_InputField _inputField;
        private TMP_Text _answerText;
        private TMP_FontAsset _font;

        public static QnaPanel Create()
        {
            var go = new GameObject("QnaPanel");
            return go.AddComponent<QnaPanel>();
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
            _font = Resources.Load<TMP_FontAsset>("Pretendard-Regular SDF");
            if (_font == null) _font = TMP_Settings.defaultFontAsset;
            BuildUI();
            SetVisible(false);
        }

        public void SetRoom(RoomData room)
        {
            _currentRoom = room;
            _answerText.text = room != null
                ? "\"" + room.label + "\"에 대해 질문해보세요. 예: \"오늘 뭐 준비해야 돼?\""
                : "";
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void Submit()
        {
            string question = _inputField.text;
            _answerText.text = Answer(question);
            _inputField.text = "";
        }

        private string Answer(string question)
        {
            if (_currentRoom == null) return "먼저 공간에 들어가 주세요.";
            if (GameManager.Instance == null) return "";

            var phase = GameManager.Instance.CurrentPhase;
            var day = GameManager.Instance.CurrentDay;

            var schedule = _currentRoom.schedules.Find(s => s.phase == phase);
            if (schedule == null || schedule.tasksByDay.Count == 0)
            {
                return HudUI.PhaseLabel(phase) + " 단계의 업무 정보가 아직 없어요.";
            }

            var daily = schedule.tasksByDay.Find(d => d.day == day);
            if (daily == null || daily.tasks.Count == 0)
            {
                return HudUI.PhaseLabel(phase) + " · " + HudUI.DayLabel(day) + " 업무 내용은 아직 준비 중이에요.";
            }

            string q = (question ?? string.Empty).Trim();
            List<string> matched = string.IsNullOrEmpty(q) ? null : daily.tasks.Where(t => ContainsAnyKeyword(t, q)).ToList();
            if (matched == null || matched.Count == 0) matched = daily.tasks; // 질문이 비었거나 매칭이 없으면 그날 전체 업무

            string header = "[" + _currentRoom.label + " · " + HudUI.PhaseLabel(phase) + " · " + HudUI.DayLabel(day) + "]\n";
            return header + "- " + string.Join("\n- ", matched);
        }

        private static bool ContainsAnyKeyword(string task, string question)
        {
            // 아주 단순한 로컬 부분일치 — 질문 속 2글자 이상 토큰이 업무 문장에 포함되면 매칭
            var tokens = question.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Any(tok => tok.Length >= 2 && task.Contains(tok));
        }

        // ---- UI construction ----

        private void BuildUI()
        {
            var canvasGo = new GameObject("QnaCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = CreatePanel(canvasGo.transform, "QnaBackPanel", new Color(0.08f, 0.07f, 0.06f, 0.78f));
            var panelRt = panel.GetComponent<RectTransform>();
            Anchor(panelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 360), new Vector2(760, 260));

            _answerText = CreateText(panel.transform, "Answer", "", 15, TextAlignmentOptions.TopLeft);
            var answerRt = _answerText.GetComponent<RectTransform>();
            Anchor(answerRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -16), new Vector2(0, 170));
            answerRt.pivot = new Vector2(0.5f, 1f);
            answerRt.offsetMin = new Vector2(20, answerRt.offsetMin.y);
            answerRt.offsetMax = new Vector2(-20, answerRt.offsetMax.y);
            _answerText.enableWordWrapping = true;
            _answerText.overflowMode = TextOverflowModes.Overflow;

            var fieldPanel = CreatePanel(panel.transform, "InputFieldBg", new Color(1f, 1f, 1f, 0.9f));
            var fieldRt = fieldPanel.GetComponent<RectTransform>();
            Anchor(fieldRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 20), new Vector2(-170, 44));
            fieldRt.pivot = new Vector2(0.5f, 0f);
            fieldRt.offsetMin = new Vector2(20, fieldRt.offsetMin.y);
            fieldRt.offsetMax = new Vector2(-160, fieldRt.offsetMax.y);

            var fieldText = CreateText(fieldPanel.transform, "Text", "", 15, TextAlignmentOptions.Left);
            fieldText.color = Color.black;
            FillParent(fieldText.GetComponent<RectTransform>());

            var placeholder = CreateText(fieldPanel.transform, "Placeholder", "질문을 입력하세요 (예: 오늘 뭐 해야 돼?)", 15, TextAlignmentOptions.Left);
            placeholder.color = new Color(0, 0, 0, 0.4f);
            FillParent(placeholder.GetComponent<RectTransform>());

            _inputField = fieldPanel.gameObject.AddComponent<TMP_InputField>();
            _inputField.textComponent = fieldText;
            _inputField.placeholder = placeholder;
            _inputField.onSubmit.AddListener(_ => Submit());

            var submitButton = CreateButton(panel.transform, "SubmitButton", "질문하기", new Color(0.86f, 0.63f, 0.27f, 0.95f), Submit);
            var submitRt = submitButton.GetComponent<RectTransform>();
            Anchor(submitRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20, 20), new Vector2(130, 44));
            submitRt.pivot = new Vector2(1f, 0f);
        }

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

        private Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiStyle.RoundedPanel();
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private Button CreateButton(Transform parent, string name, string label, Color color, Action onClick)
        {
            var panel = CreatePanel(parent, name, color);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = CreateText(panel.transform, "Label", label, 15, TextAlignmentOptions.Center);
            text.color = Color.black;
            FillParent(text.GetComponent<RectTransform>());

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
