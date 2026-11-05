using UnityEngine;

namespace VirtualOffice.Core
{
    /// 이 프로젝트를 실행해볼 때 씬에 두는 단 하나의 컴포넌트. 빈 GameObject 하나에
    /// 이것만 붙이고 Play를 누르면 GameManager·HUD·허브·플레이어·Q&A까지 전부
    /// 코드로 즉석에서 만들어진다 — 손으로 쓴 씬/프리팹이 필요 없다(세션 제약,
    /// Documents/SampleRoom_설정가이드.md 참고).
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }

            var rooms = RoomCatalog.BuildAll();

            HudUI.Create();
            HubUI.Create(rooms);
            QnaPanel.Create();
            FacilityController.Create(rooms);

            Debug.Log("[Bootstrap] 가상 업무 인계인수 공간 — 그레이박스 프로토타입 준비 완료. 방 " + rooms.Count + "개 로드됨.");
        }
    }
}
