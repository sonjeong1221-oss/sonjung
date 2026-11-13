using UnityEngine;

namespace VirtualOffice.Core
{
    /// 이 프로젝트를 실행해볼 때 씬에 두는 단 하나의 컴포넌트. 빈 GameObject 하나에
    /// 이것만 붙이고 Play를 누르면 GameManager·HUD·허브·Q&A까지 전부 코드로
    /// 즉석에서 만들어진다 — 손으로 쓴 씬/프리팹이 필요 없다(세션 제약,
    /// Documents/SampleRoom_설정가이드.md 참고).
    ///
    /// 3D 1인칭 WASD 걸어다니기는 사용자 요청으로 2D 평면도 방식(RoomFloorPlanUI)으로
    /// 한 번 전면 교체했었으나(2026-11-12), 평면도 아이콘만으로는 레퍼런스 이미지
    /// 수준의 "클래스"가 안 난다는 피드백에 따라 다시 **고정 각도(아이소메트릭) 3D
    /// 디오라마 방식(RoomDiorama)**으로 전환했다(2026-11-13). FirstPersonController·
    /// InteractionSystem의 1인칭 전용 로직과 RoomFloorPlanUI는 삭제하지 않고 그대로
    /// 뒀다(원칙 #2) — Bootstrap에서 더는 생성하지 않을 뿐. FacilityController의 가구
    /// 스폰 로직(BuildFurniturePiece 등)은 RoomDiorama가 그대로 재사용한다.
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
            var diorama = RoomDiorama.Create();

            HubUI.Instance.RoomSelected += roomId =>
            {
                var room = rooms.Find(r => r.roomId == roomId);
                if (room != null) diorama.Show(room);
            };
            HudUI.Instance.HomeClicked += () => diorama.GoHome();

            Debug.Log("[Bootstrap] 가상 업무 인계인수 공간 — 아이소메트릭 디오라마 프로토타입 준비 완료. 방 " + rooms.Count + "개 로드됨.");
        }
    }
}
