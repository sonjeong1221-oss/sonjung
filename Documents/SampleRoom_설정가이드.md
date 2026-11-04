# SampleRoom 설정 가이드 (로컬 Unity 에디터에서 진행)

이 저장소에는 `.unity` 씬 파일이 없습니다 — 에디터가 자동으로 부여하는 GUID 없이 손으로 직접
작성하면 참조가 깨지기 쉬워, 코드/문서만 준비하고 씬 구성은 이 가이드대로 에디터에서 직접 하도록
남겨뒀습니다. 전부 그대로 따라 하면 5분 내로 끝납니다.

## 0. 프로젝트 열기 전 확인
1. Unity Hub에서 이 폴더를 프로젝트로 추가 → 설치된 Unity 6000.x LTS로 열기
   (정확히 `6000.0.35f1`이 없으면 Hub가 가까운 6000.0.x LTS로 열기를 제안합니다. 그걸로 열어도 됩니다.)
2. 콘솔(Console) 창에 빨간 컴파일 에러가 없는지 먼저 확인하세요. 에러가 있다면 먼저 저에게 에러
   메시지를 알려주세요 — 추측으로 고치지 않습니다.

## 1. URP 적용
1. `Assets/_Project/Settings` 폴더에서 우클릭 → Create > Rendering > URP Asset (with Universal Renderer)
   → 이름을 `URP-LowSpec`으로 저장
2. Edit > Project Settings > Graphics → Scriptable Render Pipeline Settings에 방금 만든
   `URP-LowSpec` 할당
3. 같은 URP Asset 인스펙터에서:
   - Lighting > Main Light → Shadow Resolution을 Low/Medium 정도로 낮춤(저사양 기준)
   - Lighting > Additional Lights → Per Object Limit을 2~4 정도로 제한
   - Quality > HDR, Anti Aliasing은 끄거나 최소로(저사양 PC 기준)

## 2. 입력 시스템 활성화
1. Edit > Project Settings > Player > Other Settings
2. **Active Input Handling**을 `Input System Package (New)`로 변경 (또는 Both)
   - 변경 후 에디터 재시작을 요구하면 재시작

## 3. 씬 만들기
1. `Assets/_Project/Scenes` 폴더에 새 씬 생성 → 이름 `SampleRoom`
2. 기본 Main Camera, Directional Light는 유지(Directional Light는 베이크드용으로 Mode를
   `Baked` 또는 `Mixed`로 설정)

## 4. 플레이어 구성
1. 빈 GameObject 생성 → 이름 `Player`
2. `Player`에 Component 추가:
   - `Character Controller` (Height 1.8, Radius 0.3, Center Y 0.9 정도)
   - `VirtualOffice.Core.FirstPersonController` 스크립트
3. `Player`의 자식으로 Camera 생성(기존 Main Camera를 Player 밑으로 드래그해도 됩니다)
   - 로컬 위치 (0, 1.6, 0) — 눈높이
   - 이 Camera에 `VirtualOffice.Core.InteractionSystem` 스크립트 추가
4. `FirstPersonController`의 `Camera Pivot` 필드에 방금 만든 Camera의 Transform을 드래그해서 연결

## 5. 상호작용 프롬프트 UI
1. Hierarchy 우클릭 → UI > Canvas (자동으로 EventSystem도 생성됨)
2. Canvas 하위에 UI > Text - TextMeshPro 생성 → 이름 `InteractPrompt`
   - 화면 중앙 하단 쯤에 배치(앵커: 하단 중앙)
   - 기본적으로 비활성(체크박스 꺼짐) 상태로 둡니다 — 코드가 자동으로 켭니다
3. Camera의 `InteractionSystem` 컴포넌트 → `Prompt Label` 필드에 `InteractPrompt` 드래그 연결

## 6. 그레이박스 공간 만들기 (심의회실 샘플)
전부 3D Object 메뉴에서 Cube/Plane으로 만들면 됩니다. 정확한 치수보다 "동작 확인"이 목적입니다.
1. **바닥**: Plane, Scale (6.5, 1, 5) — 개발명세서의 심의회실 치수 기준
2. **벽 4면**: Cube를 납작하게 늘려서 배치(두께 0.2 정도)
3. **테이블**: Cube, Scale (2.4, 0.75, 1.2) 정도로 방 중앙에 배치
   - `VirtualOffice.Core.SimpleInteractable` 컴포넌트 추가
   - Prompt: `E키로 눌러 회의테이블 정보 보기`
   - On Interact() 이벤트에 테스트용으로 아무 GameObject의 `Debug.Log` 호출을 연결해도 되고,
     비워둬도 콘솔 에러 없이 동작합니다(다음 단계에서 실제 UI 패널 연결 예정)
4. **TV 자리**: 벽에 붙는 얇은 Cube, `SimpleInteractable` 추가, Prompt: `E키로 TV 보기`
5. **화이트보드 자리**: 마찬가지로 얇은 Cube + `SimpleInteractable`
6. 모든 정적 오브젝트(바닥/벽/테이블 등 움직이지 않는 것)는 Inspector 우측 상단
   **Static** 체크박스를 켜주세요 — 베이크드 라이팅 대상이 됩니다.

## 7. 라이팅 베이크
1. Window > Rendering > Lighting
2. Scene 탭에서 Lightmapping Settings 기본값 유지해도 됩니다(저사양 기준 Lightmap Resolution은
   20~30 정도로 낮춰도 충분)
3. 하단 **Generate Lighting** 클릭 → 베이크 완료까지 대기(그레이박스라 금방 끝남)

## 8. 확인
1. Play 버튼
2. 마우스가 화면 중앙에 고정되고(커서 숨김), 마우스를 움직이면 시점이 돌아가는지
3. W/A/S/D로 이동되는지
4. 테이블/TV/화이트보드 근처(2m 이내)로 가면 화면 하단에 "E키로 ..." 프롬프트가 뜨는지
5. E를 누르면 콘솔에 에러 없이 동작하는지(또는 연결해둔 Debug.Log가 찍히는지)

모두 확인되면 `Documents/개발진행현황.md`의 체크리스트에 표시해주세요. 문제가 있으면 정확한
에러 메시지나 증상을 알려주시면 원인을 분석해서 고치겠습니다.
