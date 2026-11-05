# 실행 가이드 (Bootstrap 기반, 로컬 Unity 에디터에서 진행)

이전 버전의 이 문서는 그레이박스 샘플룸 하나를 손으로 직접 배치하는 8단계 가이드였습니다.
이제는 `Bootstrap.cs`가 7개 공간·HUD·허브·플레이어·Q&A를 전부 코드로 즉석에서 만들기 때문에
그 과정이 필요 없어졌습니다 — 아래 훨씬 짧은 절차로 대체합니다. (`.unity`/`.prefab`을 손으로
쓰지 않는 이유는 CLAUDE.md "세션 제약" 참고.)

## 0. 프로젝트 열기 전 확인
1. Unity Hub에서 이 폴더를 프로젝트로 추가 → 설치된 Unity 6000.x LTS로 열기
2. 콘솔(Console) 창에 빨간 컴파일 에러가 없는지 먼저 확인 — 에러가 있으면 추측으로 고치지 말고
   정확한 메시지를 알려주세요.

## 1. URP 적용 (한 번만 하면 됨)
1. `Assets/_Project/Settings` 폴더에서 우클릭 → Create > Rendering > URP Asset (with Universal Renderer)
   → 이름 `URP-LowSpec`으로 저장
2. Edit > Project Settings > Graphics → Scriptable Render Pipeline Settings에 `URP-LowSpec` 할당
3. (선택, 저사양 최적화) 같은 URP Asset에서 Shadow Resolution 낮춤 / Additional Lights Per Object
   Limit 2~4로 제한 / HDR·Anti Aliasing 끔

## 2. 입력 시스템 활성화 (한 번만 하면 됨)
1. Edit > Project Settings > Player > Other Settings
2. **Active Input Handling**을 `Input System Package (New)`로 변경 (또는 Both) → 재시작 요구 시 재시작

## 3. 실행용 씬 만들기
1. `Assets/_Project/Scenes`에 새 씬 생성 → 이름 `Main`
2. 기본으로 들어있는 `Main Camera`, `Directional Light`는 **삭제**합니다
   (Bootstrap이 플레이어 카메라를 직접 만들고, `AudioListener` 중복 경고를 피하기 위함 — 조명은
   당장은 없어도 그레이박스 큐브들이 밝은 단색이라 눈으로 확인하는 데는 문제 없습니다. 실제
   베이크드 라이팅은 다음 단계에서 진짜 가구 에셋을 넣을 때 같이 세팅합니다)
3. 빈 GameObject 하나 생성 → 이름 아무거나(예: `_Bootstrap`)
4. 그 오브젝트에 `VirtualOffice.Core.Bootstrap` 컴포넌트를 추가

## 4. 확인
1. Play 버튼
2. 화면에 "가상 업무 인계인수 공간 — 둘러볼 공간을 선택하세요"라는 허브 화면이 뜨는지
   (심사 단계 탭 4개, 일자 탭 5개, 평면도 버튼 7개가 보여야 함)
3. 평면도에서 아무 방이나 클릭 → 그 방 내부로 들어가는지, 화면 좌상단에 환영 토스트가 뜨는지
4. 마우스가 화면 중앙에 고정되고 시점이 돌아가는지, WASD로 이동되는지
5. 가구(큐브)에 가까이 가면 화면 하단에 "E키로 상호작용 — ..." 프롬프트가 뜨는지, E를 누르면
   에러 없이 동작하는지
6. 화면 하단의 질문 입력창에 아무거나 입력하고 "질문하기" → 그 방·현재 단계·현재 일자에 맞는
   업무 목록이 나오는지 (예: 기초조사 탭 + 설치1일차 상태에서 입력하면 그 조합의 업무가 나와야 함)
7. 좌상단 홈 버튼을 누르면 허브로 돌아가는지
8. 콘솔에 `[Bootstrap] ... 방 7개 로드됨.` 로그가 뜨고 에러가 없는지

모두 확인되면 `Documents/개발진행현황.md`의 체크리스트에 표시해주세요. 문제가 있으면 정확한
에러 메시지나 증상을 알려주시면 원인을 분석해서 고치겠습니다.

## 지금 이 버전의 한계 (의도적으로 "대략적")
- 가구는 전부 색깔만 다른 큐브입니다 — 실제 가구 에셋은 다음 단계에서 입힙니다(개발명세서 7절)
- 조명은 Unity 기본 실시간 조명이고, 베이크드 라이팅은 아직 안 했습니다(디자인 방향 "베이크드
  라이팅 중심"은 다음 단계)
- 수정본 검토·견본 검수 탭은 구조만 있고 업무 내용이 비어 있습니다 — 자료 도착 후 채웁니다
- 방 이동은 Additive 서브씬이 아니라 한 씬 안에서 즉석 생성·파괴 방식입니다(개발명세서 12절
  "대략적 구현 범위" 참고) — 나중에 실제 아트가 들어오면 서브씬 방식으로 바꿀 수 있습니다
