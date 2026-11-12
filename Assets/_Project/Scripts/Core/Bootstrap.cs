using UnityEngine;

namespace VirtualOffice.Core
{
    /// 이 프로젝트를 실행해볼 때 씬에 두는 단 하나의 컴포넌트. 빈 GameObject 하나에
    /// 이것만 붙이고 Play를 누르면 GameManager·HUD·허브·Q&A까지 전부 코드로
    /// 즉석에서 만들어진다 — 손으로 쓴 씬/프리팹이 필요 없다(세션 제약,
    /// Documents/SampleRoom_설정가이드.md 참고).
    ///
    /// 3D 1인칭 체험은 사용자 요청으로 2D 평면도 방식(RoomFloorPlanUI)으로 전면
    /// 교체했다 — FirstPersonController·InteractionSystem·FacilityController의
    /// 3D 가구 스폰 로직은 더 이상 안 쓴다(삭제는 안 했지만 Bootstrap에서 더는
    /// 생성하지 않음, 개발명세서 참고).
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
            var floorPlan = RoomFloorPlanUI.Create();

            HubUI.Instance.RoomSelected += roomId =>
            {
                var room = rooms.Find(r => r.roomId == roomId);
                if (room != null) floorPlan.Show(room);
            };
            HudUI.Instance.HomeClicked += () => HubUI.Instance.SetVisible(true);

            Debug.Log("[Bootstrap] 가상 업무 인계인수 공간 — 2D 평면도 프로토타입 준비 완료. 방 " + rooms.Count + "개 로드됨.");
        }
    }
}
