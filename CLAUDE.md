# 프로젝트: 가상 업무 인계인수 공간 (Unity)

Windows/WebGL용 싱글 사용자 가상 오피스. 신규 담당자가 역할별 공간을 고정 각도(아이소메트릭)
3D 디오라마로 보며 인계인수 자료·체크리스트·업무 안내를 확인하는 것이 목적이다. 모든 세션은
이 파일과 `Documents/개발명세서.md`, `Documents/개발진행현황.md`를 먼저 읽고 작업을 시작한다.

**2026-11-12 방향 전환**: 원래 1인칭 3D 워크스루로 시작했으나, 실제 Kenney 3D 모델을
붙이는 과정에서 품질·작업 난이도 문제가 반복되자 사용자 요청으로 2D 평면도 기반으로
전면 교체했었다.

**2026-11-13 재전환**: 2D 평면도(색 블록 아이콘)만으로는 레퍼런스 이미지 수준의 "클래스"가
안 난다는 피드백에 따라, **고정 각도(아이소메트릭) 3D 디오라마로 다시 전환**했다(아래 "핵심
흐름" 참고). 1인칭 WASD 걸어다니기는 여전히 안 쓰지만, 이전에 다듬어둔 FacilityController의
3D 가구(Kenney 모델·텍스처·T자 배치·피벗 보정 등)는 그대로 되살려 쓴다 — 걷는 대신 방을
바깥 위쪽에서 고정 카메라로 내려다본다(`RoomDiorama`). 3D 전용 스크립트 중 1인칭 이동에만
쓰이던 것들(`FirstPersonController`, `InteractionSystem`, `FacilityController.BuildGreyboxRoom`)과
2D 평면도(`RoomFloorPlanUI`)는 삭제하지 않고 남겨뒀지만 더 이상 `Bootstrap`에서 생성하지
않는다 — 혼동하지 말 것.

## 개발 환경
- Unity 6000.x LTS
- URP (Universal Render Pipeline)
- Windows 64-bit 빌드 + WebGL 빌드(브라우저 공유용, 둘 다 지원)
- 고정 각도(아이소메트릭) 3D 디오라마 — 1인칭 워크스루도 2D 평면도도 아님(위 "2026-11-13
  재전환" 참고)
- 아바타 없음 — 자기 자신도, 다른 NPC도 등장하지 않는다
- 싱글 사용자 — 네트워킹/멀티플레이 코드 금지
- 저사양 PC에서도 원활하게 실행되는 최적화된 환경이 최우선 기준

## 디자인 방향
- 따뜻한 우드톤, 베이지·웜그레이 팔레트
- 세미 리얼리스틱 3D (포토리얼은 아니지만 저품질 느낌도 아님)
- 단순하지만 완성도 높은 사무용 가구
- 베이크드 라이팅 중심 (실시간 라이트는 최소한만)

## 개발 원칙
1. 기존 프로젝트 구조를 임의로 변경하지 않는다.
2. 기존에 구현된 기능을 임의로 삭제하지 않는다.
3. 기능을 추가하기 전에 기존 코드와 의존성을 확인한다.
4. 오류 발생 시 원인을 분석하고 수정한다 — 증상만 가리는 수정 금지.
5. 작업 후 `Documents/개발진행현황.md`를 업데이트한다.
6. Unity에서 실제 실행 및 검증 가능한 형태로 작성한다.
7. 사용자 승인 없이 대규모 구조 변경을 진행하지 않는다 — 애매하면 먼저 계획을 제시하고 승인받는다.

## 세션 제약 (중요)
이 코드베이스를 다루는 AI 세션은 대부분 클라우드 컨테이너에서 실행되며 **Unity 에디터가 설치되어
있지 않다.** 따라서:
- 씬(`.unity`)·프리팹(`.prefab`)의 GUID/메타파일 연결이 깨지기 쉬운 작업은 에디터에서 직접 하거나,
  "무엇을 어떻게 설정해야 하는지"를 단계별 가이드 문서로 남겨 사용자가 로컬에서 수행하게 한다.
- 실제 플레이모드 실행·시각적 확인은 불가능하므로, "검증"은 (a) C# 문법·API가 Unity 6/URP/신규
  Input System과 맞는지 꼼꼼히 재확인하는 것 + (b) 사용자가 로컬에서 따라 할 체크리스트 제공, 두
  가지로 대체한다. 이걸 "실행해서 확인했다"처럼 과장해서 보고하지 않는다.
- 포토리얼한 가구·텍스처 등 3D 아트 에셋은 생성할 수 없다. 구조 검증은 프리미티브(큐브 등)로 하고,
  실제 아트는 Asset Store 구매 에셋 또는 사용자가 준비한 리소스를 입히는 방식으로 진행한다.

## 핵심 흐름
허브(평면도)에서 방을 클릭 → 그 방을 바깥 위쪽에서 비스듬히 내려다보는 **고정 각도 3D
디오라마**(`RoomDiorama`)로 전환 → 실제 3D 가구(Kenney 모델 기반)가 배치되어 있고, 마우스로
가구를 클릭하면 그 가구 이름을 그대로 Q&A에 질문해서 그 방·현재 심사단계·일자 기준 업무
내용을 보여줌. 1인칭으로 걸어다니지 않는다(카메라 고정, 이동 없음) — 자세한 흐름은
`Documents/개발명세서.md` 4·5절(갱신 예정) 참고.

편집(가구 추가/삭제/위치 조정)은 운영자(개발자) 전용 기능으로 다음 단계에서 추가 예정 —
최종 사용자(신규 담당자)는 보기 전용.

## 아키텍처 핵심
`Assets/_Project/Scripts/Core/`
- `GameManager` — 싱글턴, 현재 상태(Phase·Day) 보유, 부팅 시퀀스
- `RoomData` (ScriptableObject) — 방 ID·치수·비품 리스트(`furniture`, 3D 로컬 좌표)·심사
  단계별 일자 업무(`schedules`)·허브 평면도 위치(`planRect`) 정의. 심사 단계(기초조사/본심사/
  수정본검토/견본검수)는 `ReviewPhase`.
- `RoomCatalog` — 7개 방(+세부 파티션 포함 10개)의 실제 데이터를 런타임에 `RoomData` 인스턴스로
  생성. 지금은 `.asset` 파일 대신 이 방식을 쓴다(세션 제약 — 스크립트 GUID 미확정)
- `HudUI` / `HubUI` / `QnaPanel` — HUD·허브 평면도(방 선택용)·Q&A 패널을 코드로 직접 생성
  (프리팹/씬 없이). 전부 TextMeshPro + Pretendard SDF 폰트 사용(`Resources/Pretendard-Regular
  SDF` — Unity 기본 내장 폰트는 한글이 없어 WebGL에서 빈 칸으로 보이는 문제가 있었음)
- `FacilityController` — 가구 조립 로직 전체(`BuildDesks`/`BuildTable`/`BuildTriDeskCluster`
  등, Kenney 모델 로드·피벗 보정·텍스처 적용)를 정적 메서드로 보유. `BuildDioramaRoom`(벽
  2개만 지어 카메라 시야를 안 가리는 디오라마용)과 `BuildGreyboxRoom`(벽 4개, 1인칭 전용·
  더 이상 안 씀) 두 버전이 공존.
- `RoomDiorama` — 방 상세 화면. `FacilityController.BuildDioramaRoom`으로 방을 짓고,
  Orthographic 카메라를 방 바깥 위쪽 고정 각도에 배치(방 크기에 맞춰 자동 프레이밍), 마우스
  클릭 Raycast로 가구의 `FurnitureTag.label`을 읽어 `QnaPanel.AskAbout`을 부름. ESC로 홈 복귀.
- `FurnitureTag` — 코드로 생성된 가구 그룹에 라벨을 달아두는 식별표(마우스 클릭 Raycast용).
- `SaveSystem` — `Application.persistentDataPath`에 JSON으로 진행 상태(Phase·Day) 저장/복원
- `Bootstrap` — 위 전부를 순서대로 생성하는 진입점. 빈 씬에 이 컴포넌트 하나만 있으면 된다
  (`Documents/SampleRoom_설정가이드.md`)
- **(더 이상 안 씀, 삭제는 안 함)** `RoomFloorPlanUI`(2D 평면도), `FirstPersonController`,
  `InteractionSystem`, `IInteractable`, `SimpleInteractable`, `FacilityController.
  BuildGreyboxRoom`(1인칭 4벽 버전) — Bootstrap에서 더 이상 생성/호출하지 않음

Q&A는 오프라인 키워드 매칭으로 **확정**(네트워킹 금지 원칙상 LLM API 미사용). WebGL 빌드 시
`Assets/link.xml`(MeshCollider 등 Physics 모듈 스트리핑 방지)은 3D 가구 모델 Instantiate 때문에
필요했고, 디오라마 전환으로 다시 3D 모델을 쓰므로 계속 필요하다(제거하지 않음).
전체 공간 구성, 비주얼 레퍼런스 스펙, 로드맵은 `Documents/개발명세서.md` 참고(디오라마 전환
반영 갱신 필요).
