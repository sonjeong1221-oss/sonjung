using System;
using UnityEngine;

namespace VirtualOffice.Core
{
    /// Single global owner of session state (current review phase + day) and the boot
    /// sequence. 싱글 사용자 전제이므로 네트워크 동기화 로직은 두지 않는다.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private ReviewPhase currentPhase = ReviewPhase.BasicSurvey;
        [SerializeField] private CampDay currentDay = CampDay.Setup1;

        public ReviewPhase CurrentPhase => currentPhase;
        public CampDay CurrentDay => currentDay;

        public event Action<ReviewPhase> PhaseChanged;
        public event Action<CampDay> DayChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SaveSystem.TryLoad(out SaveData state) && state != null)
            {
                currentPhase = state.currentPhase;
                currentDay = state.currentDay;
            }
        }

        public void SetPhase(ReviewPhase phase)
        {
            if (currentPhase == phase) return;

            currentPhase = phase;
            PhaseChanged?.Invoke(currentPhase);
            Persist();
        }

        public void SetDay(CampDay day)
        {
            if (currentDay == day) return;

            currentDay = day;
            DayChanged?.Invoke(currentDay);
            Persist();
        }

        private void Persist()
        {
            SaveSystem.Save(new SaveData { currentPhase = currentPhase, currentDay = currentDay });
        }
    }
}
