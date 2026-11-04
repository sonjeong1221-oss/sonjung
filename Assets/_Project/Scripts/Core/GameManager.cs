using System;
using UnityEngine;

namespace VirtualOffice.Core
{
    /// Single global owner of session state (current day) and the boot sequence.
    /// 싱글 사용자 전제이므로 네트워크 동기화 로직은 두지 않는다.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private CampDay currentDay = CampDay.Setup1;

        public CampDay CurrentDay => currentDay;
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
                currentDay = state.currentDay;
            }
        }

        public void SetDay(CampDay day)
        {
            if (currentDay == day) return;

            currentDay = day;
            DayChanged?.Invoke(currentDay);
            SaveSystem.Save(new SaveData { currentDay = currentDay });
        }
    }
}
