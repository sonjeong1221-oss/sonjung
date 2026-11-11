using System.Collections.Generic;
using UnityEngine;

namespace VirtualOffice.Core
{
    /// "대략적인" 메타버스 틀의 중심부: 허브(HubUI)에서 방을 고르면 그레이박스
    /// 공간을 코드로 즉석에서 지어서 텔레포트시키고, 나가면 허브로 돌아간다.
    ///
    /// 정식 버전(개발명세서 4·6절)은 방마다 Additive 서브씬 + 실제(구매/다운로드한)
    /// 가구 에셋을 쓰지만, 이 세션은 네트워크가 제한돼 있어 3D 에셋을 직접 받아올
    /// 수 없다(CLAUDE.md 세션 제약 — 가구 에셋은 사용자가 로컬 에디터에서 받아
    /// `Assets/_Project/Art/Furniture/Resources/`에 넣어야 함). 그래서 각 가구
    /// 종류마다 "Resources.Load로 실제 모델(Kenney Furniture Kit)을 먼저 찾아보고,
    /// 없으면 프리미티브 조합으로 대체"하는 구조로 짰다. 단일 모델(Shelf/Tv/Sofa)은
    /// 항상 실제 모델을 쓰고, 여러 부품을 하드코딩된 상대 위치로 조합하는 가구
    /// (Desks/ExecDesk/Table/Crate)는 모델 피벗·실제 치수가 프리미티브 가정과 달라
    /// 스폰 지점과 겹치는 문제가 확인되어 `UseRealComposedModels`로 임시 꺼둠(검증되면
    /// true로). Printer/Whiteboard/Cctv/CallBell/Counter/DocSet/SnackCounter/Cooler는
    /// 해당 팩에 없어 프리미티브 그대로.
    public class FacilityController : MonoBehaviour
    {
        public static FacilityController Instance { get; private set; }

        /// 여러 부품(책상+의자+모니터처럼)을 하드코딩된 상대 위치로 조합하는 가구만
        /// 모델 피벗/실제 치수가 달라 스폰 지점과 겹치거나 비율이 안 맞는 문제가 확인돼
        /// 임시로 꺼둔다(false = 해당 가구만 프리미티브 사용). 단일 모델(책장·TV·소파)은
        /// 이 문제와 무관해 항상 실제 모델을 쓴다. 조합형도 치수 검증되면 true로.
        private const bool UseRealComposedModels = false;

        private static readonly Color WoodColor = new Color(0.62f, 0.47f, 0.30f);
        private static readonly Color WoodMid = new Color(0.52f, 0.39f, 0.25f);
        private static readonly Color WoodDark = new Color(0.40f, 0.30f, 0.20f);
        private static readonly Color LegColor = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color ChairColor = new Color(0.55f, 0.55f, 0.58f);
        private static readonly Color DarkBezel = new Color(0.08f, 0.08f, 0.10f);
        private static readonly Color ScreenGlow = new Color(0.55f, 0.78f, 0.95f);
        private static readonly Color LightGrey = new Color(0.85f, 0.85f, 0.86f);
        private static readonly Color MidGrey = new Color(0.65f, 0.65f, 0.68f);
        private static readonly Color Cardboard = new Color(0.72f, 0.58f, 0.40f);
        private static readonly Color SofaFabric = new Color(0.72f, 0.60f, 0.45f);
        private static readonly Color WaterBlue = new Color(0.55f, 0.78f, 0.92f);

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

        // ---- room shell: floor, walls, ceiling, lighting ----

        private GameObject BuildGreyboxRoom(RoomData room)
        {
            var root = new GameObject("Room_" + room.roomId);
            float w = room.size.x, h = room.size.y, d = room.size.z;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(w / 10f, 1f, d / 10f);
            ApplyColor(floor, Lighten(room.accentColor, 0.65f), 0.15f);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform, false);
            ceiling.transform.localPosition = new Vector3(0, h, 0);
            ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            ceiling.transform.localScale = new Vector3(w / 10f, 1f, d / 10f);
            ApplyColor(ceiling, new Color(0.96f, 0.95f, 0.90f), 0.08f);

            BuildWall(root.transform, new Vector3(0, h / 2f, -d / 2f), new Vector3(w, h, 0.1f), room.accentColor);
            BuildWall(root.transform, new Vector3(0, h / 2f, d / 2f), new Vector3(w, h, 0.1f), room.accentColor);
            BuildWall(root.transform, new Vector3(-w / 2f, h / 2f, 0), new Vector3(0.1f, h, d), room.accentColor);
            BuildWall(root.transform, new Vector3(w / 2f, h / 2f, 0), new Vector3(0.1f, h, d), room.accentColor);

            BuildRoomLight(root.transform, h);

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
            ApplyColor(wall, Lighten(roomColor, 0.3f), 0.1f);
        }

        private static void BuildRoomLight(Transform parent, float ceilingHeight)
        {
            var lightGo = new GameObject("RoomLight", typeof(Light));
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(0, ceilingHeight - 0.1f, 0);
            lightGo.transform.localRotation = Quaternion.Euler(55f, 30f, 0f);

            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f); // 베이크드 라이팅 전까지의 따뜻한 임시 조명
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.43f, 0.40f);
        }

        // ---- furniture: composed from a few primitives instead of one flat cube ----

        private static void BuildFurniturePiece(Transform parent, FurnitureEntry entry)
        {
            var group = new GameObject("Furniture_" + entry.kind);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = entry.localPosition;

            Bounds bounds = BuildFurnitureCluster(group.transform, entry.kind);

            var box = group.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;

            if (entry.interactable)
            {
                var interactable = group.AddComponent<SimpleInteractable>();
                interactable.Configure("E키로 상호작용 — " + entry.label);
            }
        }

        private static Bounds BuildFurnitureCluster(Transform parent, FurnitureKind kind)
        {
            switch (kind)
            {
                case FurnitureKind.Desks: return BuildDesks(parent);
                case FurnitureKind.Table: return BuildTable(parent);
                case FurnitureKind.Shelf: return BuildShelf(parent);
                case FurnitureKind.Tv: return BuildTv(parent);
                case FurnitureKind.Whiteboard: return BuildWhiteboard(parent);
                case FurnitureKind.Printer: return BuildPrinter(parent);
                case FurnitureKind.Crate: return BuildCrate(parent);
                case FurnitureKind.Sofa: return BuildSofa(parent);
                case FurnitureKind.Cooler: return BuildCooler(parent);
                case FurnitureKind.ExecDesk: return BuildExecDesk(parent);
                case FurnitureKind.Cctv: return BuildCctv(parent);
                case FurnitureKind.CallBell: return BuildCallBell(parent);
                case FurnitureKind.Counter: return BuildCounter(parent);
                case FurnitureKind.DocSet: return BuildDocSet(parent);
                case FurnitureKind.SnackCounter: return BuildSnackCounter(parent);
                default: return BuildDefault(parent);
            }
        }

        private static Bounds BuildDesks(Transform parent)
        {
            var desk = UseRealComposedModels ? LoadModel("desk") : null;
            var chair = UseRealComposedModels ? LoadModel("chairDesk") : null;
            var screen = UseRealComposedModels ? LoadModel("computerScreen") : null;

            if (desk != null && chair != null && screen != null)
            {
                SpawnModel(parent, desk, new Vector3(0, 0, -0.1f), 180f);
                SpawnModel(parent, screen, new Vector3(0, 0.72f, -0.25f), 180f);

                var keyboard = LoadModel("computerKeyboard");
                if (keyboard != null) SpawnModel(parent, keyboard, new Vector3(0, 0.72f, 0.0f), 180f);
                var mouse = LoadModel("computerMouse");
                if (mouse != null) SpawnModel(parent, mouse, new Vector3(0.22f, 0.72f, 0.0f), 180f);

                SpawnModel(parent, chair, new Vector3(0, 0, 0.55f), 0f);

                return LocalBoundsOf(parent);
            }

            // 실제 모델이 없으면(에셋 미도입) 기존 프리미티브 조합으로 대체
            SpawnPart(parent, new Vector3(0, 0.72f, 0), new Vector3(1.15f, 0.05f, 0.58f), WoodColor);

            float hx = 0.52f, hz = 0.24f;
            foreach (var sx in new[] { -1f, 1f })
            {
                foreach (var sz in new[] { -1f, 1f })
                {
                    SpawnPart(parent, new Vector3(sx * hx, 0.36f, sz * hz), new Vector3(0.04f, 0.72f, 0.04f), LegColor);
                }
            }

            SpawnPart(parent, new Vector3(0, 1.0f, -0.20f), new Vector3(0.50f, 0.32f, 0.03f), DarkBezel);
            SpawnPart(parent, new Vector3(0, 1.0f, -0.185f), new Vector3(0.44f, 0.26f, 0.01f), ScreenGlow, 0.6f);
            SpawnPart(parent, new Vector3(0, 0.80f, -0.20f), new Vector3(0.06f, 0.12f, 0.06f), LegColor);

            SpawnChair(parent, new Vector3(0, 0, 0.55f), 180f);

            return new Bounds(new Vector3(0, 0.6f, 0), new Vector3(1.4f, 1.4f, 1.4f));
        }

        private static Bounds BuildTable(Transform parent)
        {
            var table = UseRealComposedModels ? LoadModel("table") : null;
            var chair = UseRealComposedModels ? LoadModel("chair") : null;

            const int chairCount = 6;
            float rx = 1.0f, rz = 0.65f;

            if (table != null && chair != null)
            {
                SpawnModel(parent, table, Vector3.zero);

                for (int i = 0; i < chairCount; i++)
                {
                    float ang = i * Mathf.PI * 2f / chairCount;
                    var c = new Vector3(Mathf.Sin(ang) * rx, 0, Mathf.Cos(ang) * rz);
                    float yaw = Mathf.Atan2(c.x, c.z) * Mathf.Rad2Deg + 180f;
                    SpawnModel(parent, chair, c, yaw);
                }

                return LocalBoundsOf(parent);
            }

            SpawnPart(parent, new Vector3(0, 0.74f, 0), new Vector3(1.6f, 0.06f, 0.95f), WoodColor);

            float hx = 0.72f, hz = 0.40f;
            foreach (var sx in new[] { -1f, 1f })
            {
                foreach (var sz in new[] { -1f, 1f })
                {
                    SpawnPart(parent, new Vector3(sx * hx, 0.37f, sz * hz), new Vector3(0.05f, 0.74f, 0.05f), LegColor);
                }
            }

            for (int i = 0; i < chairCount; i++)
            {
                float ang = i * Mathf.PI * 2f / chairCount;
                var c = new Vector3(Mathf.Sin(ang) * rx, 0, Mathf.Cos(ang) * rz);
                float yaw = Mathf.Atan2(c.x, c.z) * Mathf.Rad2Deg + 180f;
                SpawnChair(parent, c, yaw);
            }

            return new Bounds(new Vector3(0, 0.6f, 0), new Vector3(2.2f, 1.4f, 1.8f));
        }

        private static Bounds BuildShelf(Transform parent)
        {
            var bookcase = LoadModel("bookcaseOpen");
            if (bookcase != null)
            {
                SpawnModel(parent, bookcase, Vector3.zero);
                return LocalBoundsOf(parent);
            }

            SpawnPart(parent, new Vector3(0, 0.9f, 0), new Vector3(0.9f, 1.8f, 0.35f), WoodDark);
            for (int i = 1; i < 4; i++)
            {
                float y = i * 0.42f;
                SpawnPart(parent, new Vector3(0, y, 0.01f), new Vector3(0.82f, 0.03f, 0.33f), WoodMid, 0.1f);
            }

            return new Bounds(new Vector3(0, 0.9f, 0), new Vector3(1.0f, 1.9f, 0.5f));
        }

        private static Bounds BuildTv(Transform parent)
        {
            var tv = LoadModel("televisionModern");
            if (tv != null)
            {
                SpawnModel(parent, tv, Vector3.zero);
                return LocalBoundsOf(parent);
            }

            SpawnPart(parent, Vector3.zero, new Vector3(1.0f, 0.6f, 0.06f), DarkBezel);
            SpawnPart(parent, new Vector3(0, 0, 0.035f), new Vector3(0.92f, 0.52f, 0.01f), ScreenGlow, 0.65f);

            return new Bounds(Vector3.zero, new Vector3(1.1f, 0.7f, 0.2f));
        }

        private static Bounds BuildWhiteboard(Transform parent)
        {
            SpawnPart(parent, Vector3.zero, new Vector3(1.3f, 1.0f, 0.05f), WoodMid);
            SpawnPart(parent, new Vector3(0, 0, 0.03f), new Vector3(1.18f, 0.86f, 0.01f), Color.white, 0.5f);

            return new Bounds(Vector3.zero, new Vector3(1.4f, 1.1f, 0.15f));
        }

        private static Bounds BuildPrinter(Transform parent)
        {
            SpawnPart(parent, new Vector3(0, 0.22f, 0), new Vector3(0.55f, 0.45f, 0.45f), LightGrey);
            SpawnPart(parent, new Vector3(0, 0.47f, 0), new Vector3(0.50f, 0.06f, 0.40f), MidGrey);

            return new Bounds(new Vector3(0, 0.25f, 0), new Vector3(0.65f, 0.6f, 0.55f));
        }

        private static Bounds BuildCrate(Transform parent)
        {
            var closedBox = UseRealComposedModels ? LoadModel("cardboardBoxClosed") : null;
            var openBox = UseRealComposedModels ? LoadModel("cardboardBoxOpen") : null;

            if (closedBox != null && openBox != null)
            {
                SpawnModel(parent, closedBox, new Vector3(-0.3f, 0f, 0f));
                SpawnModel(parent, openBox, new Vector3(0.3f, 0f, 0.05f));
                return LocalBoundsOf(parent);
            }

            SpawnPart(parent, new Vector3(-0.3f, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f), Cardboard);
            SpawnPart(parent, new Vector3(0.3f, 0.22f, 0.05f), new Vector3(0.45f, 0.44f, 0.45f), Cardboard);

            return new Bounds(new Vector3(0, 0.25f, 0), new Vector3(1.1f, 0.6f, 0.6f));
        }

        private static Bounds BuildSofa(Transform parent)
        {
            var sofa = LoadModel("loungeSofa");
            if (sofa != null)
            {
                SpawnModel(parent, sofa, Vector3.zero);
                return LocalBoundsOf(parent);
            }

            SpawnPart(parent, new Vector3(0, 0.22f, 0), new Vector3(1.5f, 0.3f, 0.75f), SofaFabric);
            SpawnPart(parent, new Vector3(0, 0.5f, -0.30f), new Vector3(1.5f, 0.45f, 0.12f), SofaFabric);
            SpawnPart(parent, new Vector3(-0.68f, 0.4f, 0), new Vector3(0.15f, 0.35f, 0.75f), SofaFabric);
            SpawnPart(parent, new Vector3(0.68f, 0.4f, 0), new Vector3(0.15f, 0.35f, 0.75f), SofaFabric);

            return new Bounds(new Vector3(0, 0.35f, 0), new Vector3(1.7f, 0.8f, 0.9f));
        }

        private static Bounds BuildCooler(Transform parent)
        {
            SpawnPart(parent, new Vector3(0, 0.55f, 0), new Vector3(0.38f, 1.1f, 0.38f), LightGrey, 0.3f, 0f, PrimitiveType.Cylinder);
            SpawnPart(parent, new Vector3(0, 1.15f, 0), new Vector3(0.30f, 0.25f, 0.30f), WaterBlue, 0.6f, 0f, PrimitiveType.Sphere);

            return new Bounds(new Vector3(0, 0.6f, 0), new Vector3(0.5f, 1.4f, 0.5f));
        }

        private static Bounds BuildExecDesk(Transform parent)
        {
            var desk = UseRealComposedModels ? LoadModel("desk") : null;
            var chair = UseRealComposedModels ? LoadModel("chairDesk") : null;
            var screen = UseRealComposedModels ? LoadModel("computerScreen") : null;
            var cabinet = UseRealComposedModels ? LoadModel("cabinetTelevision") : null; // 낮은 목재 캐비닛 모양이라 서류장 대용으로 재사용

            if (desk != null && chair != null && screen != null)
            {
                SpawnModel(parent, desk, new Vector3(0, 0, -0.1f), 180f);
                SpawnModel(parent, screen, new Vector3(0, 0.72f, -0.25f), 180f);
                SpawnModel(parent, chair, new Vector3(0, 0, 0.65f), 0f);
                if (cabinet != null) SpawnModel(parent, cabinet, new Vector3(0.95f, 0f, 0.3f), 90f);

                return LocalBoundsOf(parent);
            }

            SpawnPart(parent, new Vector3(0, 0.75f, 0), new Vector3(1.5f, 0.06f, 0.75f), WoodDark);

            float hx = 0.68f, hz = 0.32f;
            foreach (var sx in new[] { -1f, 1f })
            {
                foreach (var sz in new[] { -1f, 1f })
                {
                    SpawnPart(parent, new Vector3(sx * hx, 0.37f, sz * hz), new Vector3(0.05f, 0.74f, 0.05f), LegColor);
                }
            }

            SpawnPart(parent, new Vector3(0, 1.03f, -0.28f), new Vector3(0.5f, 0.32f, 0.03f), DarkBezel);
            SpawnPart(parent, new Vector3(0, 1.03f, -0.265f), new Vector3(0.44f, 0.26f, 0.01f), ScreenGlow, 0.6f);

            SpawnChair(parent, new Vector3(0, 0, 0.65f), 180f);

            // 서류 캐비닛(책상 옆)
            SpawnPart(parent, new Vector3(0.95f, 0.45f, 0.3f), new Vector3(0.45f, 0.9f, 0.4f), WoodMid);

            return new Bounds(new Vector3(0.2f, 0.6f, 0), new Vector3(2.0f, 1.4f, 1.5f));
        }

        private static Bounds BuildCctv(Transform parent)
        {
            SpawnPart(parent, Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), LegColor, 0.4f, 0.3f, PrimitiveType.Sphere);
            SpawnPart(parent, new Vector3(0, 0, 0.12f), new Vector3(0.08f, 0.08f, 0.08f), DarkBezel, 0.6f);

            return new Bounds(Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f));
        }

        private static Bounds BuildCallBell(Transform parent)
        {
            SpawnPart(parent, Vector3.zero, new Vector3(0.22f, 0.3f, 0.06f), LightGrey);
            SpawnPart(parent, new Vector3(0, 0.08f, 0.035f), new Vector3(0.1f, 0.1f, 0.02f), new Color(0.85f, 0.2f, 0.2f), 0.5f);
            SpawnPart(parent, new Vector3(0, -0.08f, 0.035f), new Vector3(0.14f, 0.05f, 0.01f), DarkBezel, 0.4f);

            return new Bounds(Vector3.zero, new Vector3(0.3f, 0.4f, 0.15f));
        }

        private static Bounds BuildCounter(Transform parent)
        {
            SpawnPart(parent, new Vector3(0, 0.55f, 0), new Vector3(1.4f, 1.1f, 0.12f), WoodMid);
            SpawnPart(parent, new Vector3(0, 0.95f, 0), new Vector3(1.0f, 0.35f, 0.02f), new Color(0.80f, 0.90f, 0.95f, 0.6f), 0.8f);
            SpawnPart(parent, new Vector3(0, 0.58f, 0.07f), new Vector3(1.3f, 0.05f, 0.05f), WoodDark);

            return new Bounds(new Vector3(0, 0.55f, 0), new Vector3(1.5f, 1.3f, 0.3f));
        }

        private static Bounds BuildDocSet(Transform parent)
        {
            SpawnPart(parent, new Vector3(0, 0.74f, 0), new Vector3(0.28f, 0.02f, 0.22f), new Color(0.93f, 0.88f, 0.75f));
            SpawnPart(parent, new Vector3(0.22f, 0.75f, 0), new Vector3(0.24f, 0.03f, 0.20f), new Color(0.95f, 0.80f, 0.15f));
            SpawnPart(parent, new Vector3(0.44f, 0.76f, 0), new Vector3(0.24f, 0.04f, 0.20f), new Color(0.20f, 0.45f, 0.85f));

            return new Bounds(new Vector3(0.2f, 0.75f, 0), new Vector3(0.7f, 0.15f, 0.3f));
        }

        private static Bounds BuildSnackCounter(Transform parent)
        {
            SpawnPart(parent, new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.05f, 0.5f), WoodColor);

            foreach (var sx in new[] { -1f, 1f })
            {
                foreach (var sz in new[] { -1f, 1f })
                {
                    SpawnPart(parent, new Vector3(sx * 0.38f, 0.22f, sz * 0.18f), new Vector3(0.04f, 0.44f, 0.04f), LegColor);
                }
            }

            SpawnPart(parent, new Vector3(-0.2f, 0.52f, 0), new Vector3(0.22f, 0.14f, 0.18f), Cardboard);
            SpawnPart(parent, new Vector3(0.15f, 0.50f, 0.05f), new Vector3(0.18f, 0.10f, 0.15f), new Color(0.85f, 0.25f, 0.25f));

            return new Bounds(new Vector3(0, 0.4f, 0), new Vector3(1.1f, 0.9f, 0.7f));
        }

        private static Bounds BuildDefault(Transform parent)
        {
            SpawnPart(parent, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f), WoodMid);

            return new Bounds(new Vector3(0, 0.25f, 0), new Vector3(0.6f, 0.6f, 0.6f));
        }

        private static void SpawnChair(Transform parent, Vector3 center, float yaw)
        {
            var chair = new GameObject("ChairPart");
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = center;
            chair.transform.localRotation = Quaternion.Euler(0, yaw, 0);

            SpawnPart(chair.transform, new Vector3(0, 0.22f, 0), new Vector3(0.38f, 0.05f, 0.38f), ChairColor);
            SpawnPart(chair.transform, new Vector3(0, 0.45f, -0.17f), new Vector3(0.38f, 0.46f, 0.05f), ChairColor);

            float lx = 0.16f, lz = 0.16f;
            SpawnPart(chair.transform, new Vector3(lx, 0.1f, lz), new Vector3(0.03f, 0.2f, 0.03f), LegColor);
            SpawnPart(chair.transform, new Vector3(-lx, 0.1f, lz), new Vector3(0.03f, 0.2f, 0.03f), LegColor);
            SpawnPart(chair.transform, new Vector3(lx, 0.1f, -lz), new Vector3(0.03f, 0.2f, 0.03f), LegColor);
            SpawnPart(chair.transform, new Vector3(-lx, 0.1f, -lz), new Vector3(0.03f, 0.2f, 0.03f), LegColor);
        }

        // ---- 실제 모델(Resources/Furniture/*) 로딩 — 없으면 null, 호출부에서 프리미티브로 대체 ----

        private static GameObject LoadModel(string name)
        {
            // Assets/_Project/Art/Furniture/Resources/<name>.fbx — "Resources" 바로 밑에
            // 모델이 있으므로 접두사 없이 이름만으로 찾는다(Unity Resources.Load 규칙).
            return Resources.Load<GameObject>(name);
        }

        private static GameObject SpawnModel(Transform parent, GameObject prefab, Vector3 localPos, float yaw = 0f)
        {
            var go = Object.Instantiate(prefab, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            // 그룹 전체에 콜라이더 하나만 쓰는 기존 규칙과 맞추려 프리팹에 딸려온 콜라이더는 제거
            foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);

            FixUrpShader(go);

            return go;
        }

        /// 임포트된 FBX(Kenney 에셋 등)는 재질이 내장된 채로 들어와 구버전(Built-in) Standard
        /// 셰이더를 쓰는 경우가 많다 — URP 프로젝트에서 그대로 두면 핑크(에러 셰이더)로 보인다.
        /// 에디터의 "Render Pipeline Converter"로 미리 변환해두는 방법도 있지만(에셋이 외부 .mat
        /// 파일로 추출돼 있어야 동작), 내장 재질까지 포함해 항상 안전하게 적용되도록 런타임에
        /// 인스턴스마다 자동으로 셰이더를 바꿔준다(텍스처·색상은 그대로 옮김).
        private static void FixUrpShader(GameObject root)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.materials; // 접근하는 순간 렌더러 전용 인스턴스로 복제됨(원본 에셋은 안 건드림)
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null || mat.shader == urpLit) continue;

                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                    mat.shader = urpLit;
                    if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                }
                renderer.materials = mats;
            }
        }

        /// parent(가구 그룹 Transform) 아래 실제로 배치된 모델들의 렌더러를 모아
        /// parent 기준 로컬 Bounds로 변환 — 실제 모델 크기를 몰라도 콜라이더 크기를 맞출 수 있다.
        private static Bounds LocalBoundsOf(Transform parent)
        {
            var renderers = parent.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 0.5f);

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = parent.InverseTransformPoint(world.center);
            return new Bounds(localCenter, world.size);
        }

        // ---- low-level helpers ----

        private static GameObject SpawnPart(Transform parent, Vector3 localPos, Vector3 size, Color color,
            float smoothness = 0.2f, float metallic = 0f, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // 조각 각각의 충돌은 필요 없음 — 그룹 전체에 콜라이더 하나만 둔다

            ApplyColor(go, color, smoothness, metallic);
            return go;
        }

        private static void ApplyColor(GameObject go, Color color, float smoothness = 0.2f, float metallic = 0f)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            renderer.material = mat;
        }

        private static Color Lighten(Color c, float amount)
        {
            return Color.Lerp(c, Color.white, amount);
        }
    }
}
