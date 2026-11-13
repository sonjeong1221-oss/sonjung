using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VirtualOffice.Core
{
    /// 방 상세 화면의 두 번째 버전 — 2D 평면도(RoomFloorPlanUI) 대신, 이미 만들어뒀던 3D
    /// 그레이박스/Kenney 가구(FacilityController)를 그대로 재사용하되, 1인칭으로 걸어다니는
    /// 대신 그 방을 바깥 위쪽에서 비스듬히 내려다보는 고정 카메라로 보여준다("디오라마" 뷰).
    /// 사용자가 레퍼런스 이미지(아이소메트릭 느낌의 메타버스 사무실)를 보고 2D 색 블록
    /// 평면도보다 이쪽이 훨씬 "클래스가 나온다"고 판단해 전환함(2026-11-13). RoomFloorPlanUI는
    /// 삭제하지 않고 그대로 둠(원칙 #2) — Bootstrap에서 더 이상 부르지 않을 뿐.
    ///
    /// 카메라에 가까운 두 벽(+X, +Z)을 짓지 않아 시야를 가리지 않게 하는 것은
    /// FacilityController.BuildDioramaRoom 쪽 책임이고, 여기서는 카메라 배치/프레이밍과
    /// 마우스 클릭으로 가구를 짚어 QnaPanel에 질문하는 역할만 맡는다.
    public class RoomDiorama : MonoBehaviour
    {
        public static RoomDiorama Instance { get; private set; }

        private Camera _camera;
        private GameObject _currentRoomRoot;
        private RoomData _currentRoom;

        public static RoomDiorama Create()
        {
            var go = new GameObject("RoomDiorama");
            var diorama = go.AddComponent<RoomDiorama>();
            diorama.Init();
            return diorama;
        }

        private void Init()
        {
            Instance = this;

            var camGo = new GameObject("DioramaCamera", typeof(Camera));
            DontDestroyOnLoad(camGo);
            _camera = camGo.GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.86f, 0.84f, 0.80f);
            camGo.SetActive(false);
        }

        public void Show(RoomData room)
        {
            _currentRoom = room;
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);

            _currentRoomRoot = FacilityController.BuildDioramaRoom(room);
            FrameCamera(room);
            _camera.gameObject.SetActive(true);

            HubUI.Instance?.SetVisible(false);
            QnaPanel.Instance?.SetRoom(room);
            QnaPanel.Instance?.SetVisible(true);
            HudUI.Instance?.ShowToast("\"" + room.label + "\" — 가구를 클릭해서 업무를 확인하세요.");
        }

        public void GoHome()
        {
            if (_currentRoom == null) return; // 이미 허브 화면이면 무시(안전하게 중복 호출 허용)

            _camera.gameObject.SetActive(false);
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);
            _currentRoomRoot = null;
            _currentRoom = null;

            QnaPanel.Instance?.SetVisible(false);
            HubUI.Instance?.SetVisible(true);
        }

        /// 방의 가로/세로/높이에 맞춰 카메라를 (+X,+Z) 바깥 위쪽에 놓고 방 중앙을 바라보게
        /// 한 뒤, 바운딩 박스 8개 꼭짓점을 카메라 축에 투영해 orthographicSize를 정확히
        /// 계산한다 — 방마다 크기가 달라도 수동으로 맞출 필요 없이 항상 꽉 차게 나온다.
        private void FrameCamera(RoomData room)
        {
            float w = room.size.x, h = room.size.y, d = room.size.z;
            Vector3 center = new Vector3(0, h * 0.5f, 0);

            Vector3 outDir = new Vector3(1f, 0f, 1f).normalized;
            float distance = (w + d + h) * 1.5f + 5f;
            Vector3 position = center + outDir * distance + Vector3.up * (distance * 0.85f);

            _camera.transform.position = position;
            _camera.transform.rotation = Quaternion.LookRotation(center - position, Vector3.up);

            Vector3 right = _camera.transform.right;
            Vector3 up = _camera.transform.up;
            float maxRight = 0f, maxUp = 0f;

            for (int xi = -1; xi <= 1; xi += 2)
            {
                for (int yi = 0; yi <= 1; yi++)
                {
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 corner = new Vector3(xi * w / 2f, yi * h, zi * d / 2f);
                        Vector3 rel = corner - center;
                        maxRight = Mathf.Max(maxRight, Mathf.Abs(Vector3.Dot(rel, right)));
                        maxUp = Mathf.Max(maxUp, Mathf.Abs(Vector3.Dot(rel, up)));
                    }
                }
            }

            const float padding = 1.15f;
            float sizeForUp = maxUp * padding;
            float sizeForRight = (maxRight * padding) / Mathf.Max(0.01f, _camera.aspect);
            _camera.orthographicSize = Mathf.Max(sizeForUp, sizeForRight);
        }

        private void Update()
        {
            if (_currentRoom == null) return;
            HandleClick();
            HandleEscape();
        }

        private void HandleClick()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Collide))
            {
                var tag = hit.collider.GetComponentInParent<FurnitureTag>();
                if (tag != null) QnaPanel.Instance?.AskAbout(tag.label);
            }
        }

        private void HandleEscape()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) GoHome();
        }
    }
}
