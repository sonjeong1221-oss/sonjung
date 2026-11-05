using System.Collections.Generic;
using UnityEngine;

namespace VirtualOffice.Core
{
    /// "대략적인" 메타버스 틀의 중심부: 허브(HubUI)에서 방을 고르면 그레이박스
    /// 공간을 코드로 즉석에서 지어서 텔레포트시키고, 나가면 허브로 돌아간다.
    ///
    /// 정식 버전(개발명세서 4·6절)은 방마다 Additive 서브씬 + 실제 가구 에셋을
    /// 쓰지만, 이 세션엔 Unity 에디터가 없어 씬/프리팹을 손으로 안전하게 만들 수
    /// 없다(CLAUDE.md 세션 제약). 그래서 이번 "대략적 구현"은 SceneLoader 대신
    /// 하나의 씬 안에서 큐브로 방을 즉석 생성·파괴하는 방식으로 같은 흐름
    /// (허브 → 선택 → 텔레포트 → 1인칭 둘러보기 → 복귀)을 보여준다. 나중에
    /// 실제 아트가 들어오면 이 클래스의 BuildGreyboxRoom만 교체하면 된다.
    public class FacilityController : MonoBehaviour
    {
        public static FacilityController Instance { get; private set; }

        private List<RoomData> _rooms;
        private GameObject _playerRoot;
        private FirstPersonController _playerController;
        private GameObject _currentRoomRoot;
        private RoomData _currentRoom;

        public static FacilityController Create(List<RoomData> rooms)
        {
            var go = new GameObject("FacilityController");
            var fc = go.AddComponent<FacilityController>();
            fc.Init(rooms);
            return fc;
        }

        private void Init(List<RoomData> rooms)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _rooms = rooms;

            BuildPlayer();

            HudUI.Instance.HomeClicked += ExitToHub;
            HubUI.Instance.RoomSelected += EnterRoom;

            ShowHub();
        }

        private void BuildPlayer()
        {
            _playerRoot = new GameObject("Player", typeof(CharacterController));
            DontDestroyOnLoad(_playerRoot);

            var controller = _playerRoot.GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0, 0.9f, 0);

            var camGo = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(_playerRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);

            var interaction = camGo.AddComponent<InteractionSystem>();
            interaction.SetPromptLabel(HudUI.Instance.PromptLabel);

            _playerController = _playerRoot.AddComponent<FirstPersonController>();
            _playerController.SetCameraPivot(camGo.transform);

            _playerRoot.SetActive(false);
        }

        private void ShowHub()
        {
            _currentRoom = null;
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);
            _playerRoot.SetActive(false);
            HubUI.Instance.SetVisible(true);
            QnaPanel.Instance.SetVisible(false);
        }

        private void EnterRoom(string roomId)
        {
            RoomData room = _rooms.Find(r => r.roomId == roomId);
            if (room == null) return;

            HubUI.Instance.SetVisible(false);
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);

            _currentRoom = room;
            _currentRoomRoot = BuildGreyboxRoom(room);

            _playerRoot.transform.position = room.spawnPosition + Vector3.up * 0.05f;
            _playerRoot.transform.rotation = Quaternion.Euler(0f, room.spawnYaw, 0f);
            _playerRoot.SetActive(true);

            QnaPanel.Instance.SetRoom(room);
            QnaPanel.Instance.SetVisible(true);

            HudUI.Instance.ShowToast("가상 업무 공간에 오신 것을 환영합니다! \"" + room.label + "\" — 필요한 자료를 찾아보세요.");
        }

        private void ExitToHub()
        {
            ShowHub();
        }

        // ---- grey-box room construction ----

        private GameObject BuildGreyboxRoom(RoomData room)
        {
            var root = new GameObject("Room_" + room.roomId);
            float w = room.size.x, h = room.size.y, d = room.size.z;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(w / 10f, 1f, d / 10f);
            ApplyColor(floor, Lighten(room.accentColor, 0.65f));

            BuildWall(root.transform, new Vector3(0, h / 2f, -d / 2f), new Vector3(w, h, 0.1f), room.accentColor);
            BuildWall(root.transform, new Vector3(0, h / 2f, d / 2f), new Vector3(w, h, 0.1f), room.accentColor);
            BuildWall(root.transform, new Vector3(-w / 2f, h / 2f, 0), new Vector3(0.1f, h, d), room.accentColor);
            BuildWall(root.transform, new Vector3(w / 2f, h / 2f, 0), new Vector3(0.1f, h, d), room.accentColor);

            foreach (var item in room.furniture)
            {
                BuildFurniturePiece(root.transform, item);
            }

            return root;
        }

        private static void BuildWall(Transform parent, Vector3 localPos, Vector3 scale, Color roomColor)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            ApplyColor(wall, Lighten(roomColor, 0.3f));
        }

        private static void BuildFurniturePiece(Transform parent, FurnitureEntry entry)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Furniture_" + entry.kind;
            go.transform.SetParent(parent, false);

            Vector3 size = FurnitureSize(entry.kind);
            go.transform.localPosition = entry.localPosition + new Vector3(0, size.y / 2f, 0);
            go.transform.localScale = size;
            ApplyColor(go, FurnitureColor(entry.kind));

            if (entry.interactable)
            {
                var interactable = go.AddComponent<SimpleInteractable>();
                interactable.Configure("E키로 상호작용 — " + entry.label);
            }
        }

        private static Vector3 FurnitureSize(FurnitureKind kind)
        {
            switch (kind)
            {
                case FurnitureKind.Table: return new Vector3(1.6f, 0.75f, 0.9f);
                case FurnitureKind.Desks: return new Vector3(1.2f, 0.75f, 0.6f);
                case FurnitureKind.Shelf: return new Vector3(0.9f, 1.8f, 0.4f);
                case FurnitureKind.Tv: return new Vector3(1.0f, 0.6f, 0.08f);
                case FurnitureKind.Whiteboard: return new Vector3(1.2f, 0.9f, 0.05f);
                case FurnitureKind.Printer: return new Vector3(0.5f, 0.5f, 0.5f);
                case FurnitureKind.Crate: return new Vector3(0.5f, 0.5f, 0.5f);
                case FurnitureKind.Sofa: return new Vector3(1.4f, 0.7f, 0.7f);
                case FurnitureKind.Cooler: return new Vector3(0.4f, 1.1f, 0.4f);
                default: return Vector3.one * 0.6f;
            }
        }

        private static Color FurnitureColor(FurnitureKind kind)
        {
            switch (kind)
            {
                case FurnitureKind.Tv: return new Color(0.1f, 0.1f, 0.12f);
                case FurnitureKind.Whiteboard: return Color.white;
                case FurnitureKind.Shelf: return new Color(0.55f, 0.4f, 0.25f);
                case FurnitureKind.Crate: return new Color(0.6f, 0.45f, 0.3f);
                default: return new Color(0.85f, 0.83f, 0.78f);
            }
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { color = color };
            renderer.material = mat;
        }

        private static Color Lighten(Color c, float amount)
        {
            return Color.Lerp(c, Color.white, amount);
        }
    }
}
