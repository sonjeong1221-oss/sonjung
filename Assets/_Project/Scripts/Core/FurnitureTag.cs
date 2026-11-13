using UnityEngine;

namespace VirtualOffice.Core
{
    /// 코드로 생성된 가구 그룹에 원래 라벨을 달아두는 아주 작은 식별표. RoomDiorama가
    /// 마우스 클릭 Raycast로 어떤 가구를 짚었는지 알아낼 때 이걸로 QnaPanel.AskAbout에
    /// 넘길 이름을 가져온다.
    public class FurnitureTag : MonoBehaviour
    {
        public string label;
    }
}
