using System.Collections.Generic;
using UnityEngine;

namespace VirtualOffice.Core
{
    /// Builds the 7 real RoomData instances at runtime instead of as hand-authored
    /// .asset files — this script's own GUID doesn't exist yet (no Unity Editor has
    /// touched this project), so a .asset referencing it by GUID would risk a broken
    /// "Missing (Mono Script)" reference on first import (CLAUDE.md 세션 제약). Once
    /// the user opens the project in the Editor, these can be "frozen" into real
    /// .asset files via RoomData's CreateAssetMenu and hand-tuned there if wanted.
    ///
    /// Content is ported 1:1 from the earlier validated 2D prototype (camp-space.html),
    /// itself built from the uploaded 사업계획서/인계인수서 — not invented here.
    /// 기초조사·본심사 단계는 같은 업무 내용을 임시로 공유한다(실제 단계별 세부 차이는
    /// 추가 확인 필요, 개발명세서 11절).
    ///
    /// 수정본 검토·견본 검수: 운영팀실(실제 수정본검토 24.6.17 / 견본검수 24.8.6 합숙 체크리스트
    /// 원문), 진행팀실·외부지원팀실(업로드된 실제 합숙 체크리스트 원문 — 다만 해당 두 파일은
    /// 문서 자체 제목이 "본심사"·"기초조사 및 본심사"로 적혀 있어 수정본검토/견본검수 시점
    /// 전용 문서는 아님. 진행팀·외부지원팀의 합숙 운영 절차(보안·숙소·차량·협의실 세팅 등)는
    /// 심사 단계가 바뀌어도 거의 동일하다는 전제로 임시 적용했다 — 실제 수정본검토/견본검수용
    /// 진행팀·외부지원팀 문서가 도착하면 교체 필요)는 채워졌다. 기획팀실·자료보관실·
    /// 내외부소통공간·심의회실은 아직 해당 자료가 없어 구조만 비어 있다.
    public static class RoomCatalog
    {
        public static List<RoomData> BuildAll()
        {
            return new List<RoomData>
            {
                BuildPlanning(),
                BuildProgress(),
                BuildProgressStorage(),
                BuildOperation(),
                BuildOpsSearchRoom(),
                BuildOpsStorage(),
                BuildData(),
                BuildHub(),
                BuildReview(),
                BuildExternal()
            };
        }

        // ---- shared helpers ----

        private static RoomData NewRoom(string id, string label, string role, Vector3 size,
            Color32 accent, Vector4 planRect, string nestedIn = null)
        {
            var room = ScriptableObject.CreateInstance<RoomData>();
            room.roomId = id;
            room.label = label;
            room.roleDescription = role;
            room.size = size;
            room.accentColor = accent;
            room.planRect = planRect;
            room.nestedInRoomId = nestedIn;
            room.sceneName = "Room_" + id;
            room.spawnPosition = Vector3.zero;
            room.spawnYaw = 0f;
            return room;
        }

        private static void AddFurniture(RoomData room, FurnitureKind kind, Vector3 pos, string label)
        {
            room.furniture.Add(new FurnitureEntry
            {
                kind = kind,
                localPosition = pos,
                label = label,
                interactable = true
            });
        }

        private static PhaseSchedule AddPhase(RoomData room, ReviewPhase phase)
        {
            var schedule = new PhaseSchedule { phase = phase };
            room.schedules.Add(schedule);
            return schedule;
        }

        private static void AddDay(PhaseSchedule schedule, CampDay day, params string[] tasks)
        {
            schedule.tasksByDay.Add(new DailyTask { day = day, tasks = new List<string>(tasks) });
        }

        // FillCommonSchedules가 미리 비워 만들어둔 RevisionReview/SampleInspection
        // PhaseSchedule을 찾아 실제 합숙 체크리스트 문서 내용으로 채운다.
        private static void FillPhase(RoomData room, ReviewPhase phase,
            string[] d1, string[] d2, string[] mid, string[] e1, string[] e2)
        {
            var schedule = room.schedules.Find(s => s.phase == phase);
            AddDay(schedule, CampDay.Setup1, d1);
            AddDay(schedule, CampDay.Setup2, d2);
            AddDay(schedule, CampDay.Mid, mid);
            AddDay(schedule, CampDay.CheckOut1, e1);
            AddDay(schedule, CampDay.CheckOut2, e2);
        }

        private static void FillCommonSchedules(RoomData room,
            string[] d1, string[] d2, string[] mid, string[] e1, string[] e2)
        {
            foreach (var phase in new[] { ReviewPhase.BasicSurvey, ReviewPhase.MainReview })
            {
                var schedule = AddPhase(room, phase);
                AddDay(schedule, CampDay.Setup1, d1);
                AddDay(schedule, CampDay.Setup2, d2);
                AddDay(schedule, CampDay.Mid, mid);
                AddDay(schedule, CampDay.CheckOut1, e1);
                AddDay(schedule, CampDay.CheckOut2, e2);
            }

            // 수정본 검토·견본 검수: 구조만 미리 만들어두고 내용은 비움(11절) — 자료 도착 후 채움
            AddPhase(room, ReviewPhase.RevisionReview);
            AddPhase(room, ReviewPhase.SampleInspection);
        }

        // ---- 기획팀실 ----

        private static RoomData BuildPlanning()
        {
            var room = NewRoom(
                "planning", "기획팀실", "기획위원 · 검정 기획팀",
                new Vector3(4.5f, 2.6f, 3.6f),
                new Color32(0x7a, 0x6a, 0x3e, 0xff),
                new Vector4(0.0078f, 0.0455f, 0.1239f, 0.9091f));

            AddFurniture(room, FurnitureKind.Desks, new Vector3(-2.06f, 0f, -1.38f), "데스크탑 PC·모니터 (6대)");
            AddFurniture(room, FurnitureKind.Shelf, new Vector3(1.63f, 0f, -1.08f), "도서서가 (3조)");
            AddFurniture(room, FurnitureKind.Sofa, new Vector3(1.35f, 0f, 1.26f), "소파");
            AddFurniture(room, FurnitureKind.Tv, new Vector3(-1.3f, 1.3f, -1.79f), "TV (PC 연결)");

            FillCommonSchedules(room,
                d1: new[] {
                    "검정기준·편찬상의 유의점 최종 점검 및 심의회 배부 준비",
                    "심의위원 위촉장·업무이행약정서 서식 최종 확인",
                    "합숙 운영 관련 교육부 협의 사항 정리",
                    "기획팀 협의실(607호) 집기·회의테이블 배치 확인"
                },
                d2: new[] {
                    "입소식 참석, 위원장단에 검정기준·절차 안내",
                    "심의회별 산출 보고서 양식 점검",
                    "간사·검정운영팀 간 업무 분장 공유"
                },
                mid: new[] {
                    "심의회 산출 보고서(기초조사·본심사) 검토 및 의견 제시",
                    "검정기준 적용 관련 질의 대응",
                    "불합격(예정) 도서 사유서 초안 검토"
                },
                e1: new[] {
                    "결과 발표 자료집(안) 최종 검토",
                    "심사 결과 관련 민원·소송 대응 자료 준비",
                    "기획 관련 산출물·서류 평가원행 정리"
                },
                e2: new[] {
                    "검정심사관리위원회 결과 보고 취합",
                    "교육부 보고용 최종 자료 정리 및 송부",
                    "검정 규정·절차 개선 사항 기록"
                });

            return room;
        }

        // ---- 진행팀실 ----

        private static RoomData BuildProgress()
        {
            var room = NewRoom(
                "progress", "진행팀실", "진행팀장 · 심사진행팀장",
                new Vector3(5.5f, 2.6f, 4.0f),
                new Color32(0x3a, 0x6c, 0xb5, 0xff),
                new Vector4(0.1395f, 0.0455f, 0.1434f, 0.9091f));

            AddFurniture(room, FurnitureKind.Desks, new Vector3(0.12f, 0f, -1.37f), "QR·인터넷 검색실 책상");
            AddFurniture(room, FurnitureKind.Crate, new Vector3(1.72f, 0f, -0.35f), "합숙 물품 박스");
            AddFurniture(room, FurnitureKind.Tv, new Vector3(-0.38f, 1.3f, -1.98f), "TV (PC 연결)");

            FillCommonSchedules(room,
                d1: new[] {
                    "합숙 장소 보안구역 범위 확정(장소선정위원회 결과 반영) 및 외부 출입구 보안요원 배치 계획 수립",
                    "위원 입소 집결지 운영 계획 수립, 인솔요원(원 직원)·보안요원 편성",
                    "합숙 장소 출입자(직원 포함) 명단 사전 접수 → 명단 외 인원 출입 통제 준비",
                    "합숙 물품·사무기기·장비 임차 반입 확인, 협의실 배치도 최종 점검",
                    "검정 합숙 입소식·각종 행사 준비, 검정 수수료·예산 집행계획 최종 점검"
                },
                d2: new[] {
                    "집결지에서 인솔요원·보안요원이 위원 인솔(이동 중 합숙 장소 사전 안내 금지, 통화 자제 요청)",
                    "입소식 진행, 생활안내·명찰 배부",
                    "위원 준수사항 고지: 외출·외박 금지, 외부통화 차단, 집필진 접촉 금지, 명찰 패용",
                    "보안요원 24시간 근무 체계 가동 및 1차 보안구역 순찰 실시"
                },
                mid: new[] {
                    "보안구역 정기 순찰 및 일일 출입자 현황·순찰일지 본부 보고",
                    "위원 외출·외박, 공정성 훼손 우려 행위 모니터링",
                    "합숙 운영 전반(숙소·식당·이동) 점검, 돌발 상황 대응 및 물품 보충",
                    "위원장단 요청사항 처리"
                },
                e1: new[] {
                    "심의회별 퇴소 장소 마련(타 심의회 위원 출입 통제) 및 퇴소 순서 조율",
                    "퇴소 일정 공지 및 퇴소식 준비",
                    "합숙 정산 항목 취합 시작(임차·물품·식대)"
                },
                e2: new[] {
                    "해당 심의회 위원에 한해 퇴소 조치, 출입 명단 대비 인원 최종 확인",
                    "합숙 정산 마무리 및 임차 시설·장비 반납 확인",
                    "검정 수수료·예산 집행 내역 정리, 다음 단계 일정 안내"
                });

            // 수정본 검토(원문 제목은 "본심사 심의회 개최 2024.2.14.~3.2."): 진행팀의 합숙
            // 전/후/당일/퇴소 절차 자체는 단계가 달라져도 거의 동일하다는 전제로 임시 적용.
            FillPhase(room, ReviewPhase.RevisionReview,
                d1: new[] {
                    "참여인원·일정 확정(내부/외부 입소 인원·날짜별 구분) 및 위원위촉 진행",
                    "발주: 합숙시설(객실·협의실·식사·보안공사·인터넷), 보안인력(내부·외부·인솔·후발대), 사무기기(PC·노트북·빔·세탁기 등)",
                    "일용직 채용 및 진행팀·운영팀 인원 구성",
                    "합숙시설 객실 배치도·협의실 사무기기 배치도 확인, 식단 수령·검토",
                    "공용·개인간식 리스트 업체 전달, 입소분 차량(대형버스·승합차·화물차) 발주"
                },
                d2: new[] {
                    "간식대·보안석 설치 확인, 객실별 생활용품·침대 점검, 빈방 키 회수 및 마스터키 요청",
                    "흡연실 위치·명찰-명패-배치도-알람시계 객실번호 일치 확인, 집결지별 명찰 제작",
                    "협의실 배치도와 실제 배치 일치 확인, 문구통·파지함·알람시계 배치, 위원장단 협의실 펜접시·명패 세팅",
                    "객실·협의실·세탁실·쓰레기통·비상계단 등 명패 부착, 전화번호 부착",
                    "입소식·워크숍 준비(장소·리허설·생활안내 PPT·국민의례 파일), 행정서식·보건·보안일지 준비"
                },
                mid: new[] {
                    "입소 당일 집결지별 명찰·생활안내문(약 350부) 배부, 진행팀 인력 배치해 객실·입소식 안내",
                    "USB·외장하드 분출 절차 진행(간사 보안물품 전달·체크), 외부 위원 보안물품 반출 요청은 불가 안내",
                    "위원장단 협의실 마이크 점검, 명패 배치(벽쪽 간사/복도쪽 위원장, 기획위원 명패 앞쪽)",
                    "객실 용품 미비·변경 요청 대응, 개인물품 구매 원칙 안내(완제품만 허용, 조리식품·특정 브랜드 불가)",
                    "일용직 근무표 순환 근무 운영(진행팀·창고·휴식 3교대)"
                },
                e1: new[] {
                    "퇴소 전전일 위원장단 회의 때 퇴소 안내문 전달(해당 심의회 인원수에 맞춰)",
                    "지류봉투 제작(위원별 2장: 위원용/간사용/자문위원용)",
                    "간담회 물품 준비(만찬 비용, 퇴소일별 일정 확인 후 전날 19시 30분까지 전달 요청)"
                },
                e2: new[] {
                    "간담회 물품 배부(기획팀 보고서 전달 → 운영팀 확인·접수 → 심사자료·사무용품 수거 → 지류봉투 전달 순서)",
                    "퇴소자 명단 정리(가나다순), 명찰 수거함·명단 챙겨 퇴소 장소로 이동",
                    "정해진 시간에 맞춰 퇴소 진행, 내·외부 보안에 퇴소 인원·시간 사전 전달"
                });

            // 견본 검수(원문 제목은 "AI DT 기초조사 및 본심사 8.21.~9.12."): 위와 같은 전제로 임시 적용.
            FillPhase(room, ReviewPhase.SampleInspection,
                d1: new[] {
                    "심사 시행계획 수립(참여인원·기간·예산 확인)",
                    "단가계약 발주: 합숙장소, 보안(내부 10명+보안스티커 / 외부 10명+인솔 / 후발대 5명), 사무기기",
                    "일용직 섭외 및 기안(입소 시 가족관계증명서 지참 안내)",
                    "객실 배치도·협의실별 사무기기·책상 배치도 작성 및 검토"
                },
                d2: new[] {
                    "객실 배치도(개인정보 삭제)·식수인원 업체 발송, 식단 수령·검토(개인간식 메뉴 중복 확인)",
                    "업체 요구사항 반영: 간식대 린넨·정수기 배치, 보안석 위치, 객실 생활용품·생수, 가스·난방 점검, 신문 비치, 식당 안전 점검",
                    "공용·개인간식 구입(유통기한 확인), 사무용품 정리·구입(가습기·스탠드·A4 등)",
                    "승합차·버스·화물차 운행 내역 작성"
                },
                mid: new[] {
                    "협의실 세팅(배치도 일치 확인, 문구통·파지함·알람시계·스탠드 배치, 위원장단 명패·펜접시)",
                    "각종 안내문구·명패 부착(객실·협의실·세탁실 등), 전화번호 부착",
                    "객실 비품·청결 확인, 조리기구 비치·창문 잠금 확인, 빈방 키 회수, 마스터키 요청",
                    "승강기·비상계단 동선 확인, 입소식장 리허설(마이크·현수막·좌석 등)"
                },
                e1: new[] {
                    "행정서식 준비(개인물품 구매신청서·복사대장 등, 보건·보안 일지)",
                    "근무표 작성 및 진행실 업무 안내(행정서식 작성·위원 응대 요령)",
                    "간식 정리(인솔자·공용·개인 간식 집행내역 사전 산정)"
                },
                e2: new[] {
                    "보안서약서 배부(보안·운전기사·보안구역 출입자)",
                    "보안구역 점검·순찰(잠금시설·CCTV 설치 확인), 체육시설 점검",
                    "생활안내문·PPT 작성, 전염병 방역 대응 원칙 안내(격리 미실시 등)"
                });

            return room;
        }

        // ---- 진행팀 물품창고 (진행팀실 내 파티션) ----

        private static RoomData BuildProgressStorage()
        {
            var room = NewRoom(
                "progress_storage", "물품창고(진행팀)", "진행팀 · 합숙 물품 보관",
                new Vector3(2.2f, 2.6f, 1.8f),
                new Color32(0x2e, 0x55, 0x92, 0xff),
                new Vector4(0.2250f, 0.0900f, 0.0579f, 0.8200f),
                nestedIn: "progress");

            AddFurniture(room, FurnitureKind.Crate, new Vector3(-0.6f, 0f, -0.3f), "간식 창고(과자 등 공용·개인간식)");
            AddFurniture(room, FurnitureKind.Crate, new Vector3(0.6f, 0f, -0.3f), "세제 창고(세탁비누·청소용품)");
            AddFurniture(room, FurnitureKind.Crate, new Vector3(0f, 0f, 0.9f), "학용품 창고(A4·독서대·알람시계 등 종류별 보관)");

            return room;
        }

        // ---- 운영팀실 ----

        private static RoomData BuildOperation()
        {
            var room = NewRoom(
                "operation", "운영팀실", "운영팀장 · 심사운영팀장",
                new Vector3(5.5f, 2.6f, 4.0f),
                new Color32(0x2f, 0x5f, 0xa8, 0xff),
                new Vector4(0.2907f, 0.0455f, 0.2021f, 0.9091f));

            AddFurniture(room, FurnitureKind.Desks, new Vector3(-2.47f, 0f, -1.33f), "데스크탑 PC·듀얼모니터 (7대)");
            AddFurniture(room, FurnitureKind.Whiteboard, new Vector3(-2.13f, 1.3f, 1.47f), "화이트보드");
            AddFurniture(room, FurnitureKind.Printer, new Vector3(-1.24f, 0f, 1.28f), "컬러복합기");

            FillCommonSchedules(room,
                d1: new[] {
                    "검정 심사 매뉴얼 최종 제작(생활안내·전산매뉴얼 보완, 연구물 등록번호 생성)",
                    "합숙 임차 PC·노트북 사이트 등록(검정시스템·표준국어대사전 즐겨찾기), 무선랜카드 제거 요청",
                    "협의실별 IP 요청·배분(네트워크업체), 운영팀 협의실 방화벽 포트 허용 요청(정보보안부)",
                    "심사 시스템 최종 시뮬레이션(검정메뉴·보고서 서식 일치 확인, 전산업체 동시 점검)",
                    "심사본 분류 작업(기획위원/표기표현/연구위원/어휘검색/원어민별) 및 협의실 세팅",
                    "운영팀 물품 최종 확인(보고서 상자·증빙자료·천공기·도장박스·라벨지·빈 상자 등)"
                },
                d2: new[] {
                    "임차 업체 설치 완료 후 전체 협의실 PC·프린터·빔·모니터 점검",
                    "협의실별 심사본 세팅(위원장용/개별 위원용 구분, 개별본은 재택용 박스 밀봉 보관)",
                    "공통·교과 매뉴얼 배부 준비 및 제출 서류 점검",
                    "전산연수 시간 확정·실시(인증서 등록, 개별 입력 방법 안내, QnA 지원 안내)",
                    "심사본 우체국 택배 배송 신청(재택 전환 대비, 합숙지 근처 우체국 사전 연락)"
                },
                mid: new[] {
                    "위원 접속기록 모니터링(3일 이상 미접속자 확인 및 개별 안내)",
                    "시스템 내 QnA 게시판 상시 모니터링 및 원격 지원",
                    "라운드별(1·2·3라운드) 평정 진행 상황 점검, 미평정 위원 확인",
                    "심사본 이중관리(접수번호-심사번호) 원칙 준수, 분실·훼손 신고서 접수 시 재배부 처리"
                },
                e1: new[] {
                    "최종 심의 완료 여부 확인, 결과보고서 박스·증빙자료·심사본·매뉴얼 수거 시작",
                    "본심사 결과 발표 자료집(안) 제작, 설명회 장소 예약",
                    "심의위원장 및 전체 위원 날인 결과물 수합(간사 경유)"
                },
                e2: new[] {
                    "퇴소일 오전 설문조사 실시",
                    "보안 해제 후 내부결재(KIMS) 진행, 심사 결과 발표·공개 업무 수행",
                    "심사 결과물 원내 직접 운반(운영위원 동행) 및 외부인 통제 구역 보관",
                    "접수번호로 변환한 심사 결과물 출원자별 송부 처리"
                });

            // 수정본 검토(실제 운영팀 합숙 체크리스트 원문, 24.6.17.)
            FillPhase(room, ReviewPhase.RevisionReview,
                d1: new[] {
                    "수정본 검토 시스템·서식 최종 확인(6/14), 전산 매뉴얼 PPT 작성·탑재(6/17), 설문 문항(안) 탑재 준비",
                    "수정본 검토 매뉴얼 최종 컨펌(6/12) 및 납품(6/13, 검정운영팀)",
                    "수정본 온라인 등록(6/7~13)·접수(6/11~13) 진행, 수정보완대조표 업로드 오류 확인",
                    "우석대학교 접수 장비 설치(6/9~10) 및 1층 접수 간판·안내 표시 부착"
                },
                d2: new[] {
                    "수정본 접수 상세 업무: 출원사 번호표 제공(21개 출판사), 라벨 부착·분류, 전자저작물 외장하드 제작",
                    "수정본 심의회 세팅표 기준 교과서별 보관본·전시본·심사본·기획팀·운영팀 수량 배분(총 607권)",
                    "피티박스 분류·창고 적재 및 시건 완료(6/10), 수정본 합숙지 이동(6/13)",
                    "윤문 협의실 설치(외래어·로마자 용례집, 표준국어대사전 시스템·프로그램 설치)"
                },
                mid: new[] {
                    "네트워크 점검(6/17~18): 수정본 검토 시스템·표준국어대사전 즐겨찾기·설치파일·출력시스템·빔PC 전체 협의실 점검",
                    "노트북 주기 작업 및 인터넷 환경 점검(6/10)",
                    "입소 시 매뉴얼·명찰·생활안내문 세트 배부",
                    "협의실별 수정본·수정보완대조표·증빙자료 박스·결과보고서 박스·ID·PW 세팅(심의위원 협의실·윤문)",
                    "수정본 검토 전산 연수 실시(6/21, 워크숍 이후)"
                },
                e1: new[] {
                    "증빙자료 내 운영팀 보관자료 준비(노트북·USB·대외비·복사 대장, 심의회 게시판 자료 탑재)",
                    "결과보고서 박스 및 증빙자료 준비(수정본 검토 결과보고서 등)",
                    "수정본 검토 결과 보고(7/1)"
                },
                e2: new[] {
                    "수정본 검토 결과 작업(7/1~7/2)",
                    "수정본 검토 결과 우편 발송(운영지원부, 7/4 오전 9시 30분)",
                    "수정본 검토 결과 발표(7/5 오전 10시) 및 재수정보완권고서 출원사 송부"
                });

            // 견본 검수(실제 운영팀 합숙 체크리스트 원문, 24.8.6.)
            FillPhase(room, ReviewPhase.SampleInspection,
                d1: new[] {
                    "견본 검수 시스템·서식 최종 확인(8/5), 전산 매뉴얼 PPT 작성·탑재(8/6), 설문 문항(안) 탑재 준비(8/9)",
                    "수정본 검토·견본 검수 매뉴얼 동일 세팅 확인(8/6, 합숙 창고 PT박스 보관)",
                    "견본 온라인 등록(7/29~8/2)·접수(8/1~2) 진행, 견본검수대조표 업로드 오류(일련번호·양식) 확인",
                    "이상설홀 접수 장비 설치(7/31) 및 1층 접수 간판 부착"
                },
                d2: new[] {
                    "견본 심의회 세팅(총 10 Set: 보관본·접수번호·기획위원·윤문·여분·위원장·검정위원용 4Set)",
                    "견본 접수 상세 업무: 출원사 번호표 제공(21개 출판사), 라벨 부착·분류, USB 외장하드 제작",
                    "피티박스 분류·이상설홀 적재 및 시건 완료(8/2), 견본 합숙지 이동(8/6)",
                    "윤문 협의실 설치(표준국어대사전 시스템 반영·프로그램 전체 PC 설치, 8/7)"
                },
                mid: new[] {
                    "네트워크 점검(8/2~5): 수정본 검토 시스템·표준국어대사전·설치파일·출력시스템·빔PC 전체 협의실 점검",
                    "임차 노트북·갤럭시탭·아이패드 점검(자산태그 부착, 업데이트 실시)",
                    "견본 검수 입소 매뉴얼·명찰·생활안내문 세트 배부",
                    "심의위원 협의실 견본 등 세팅(심사본·수정본·수정보완대조표·견본·견본검수대조표·증빙자료 박스·ID·PW)",
                    "견본 검수 전산 연수(8/8 개별 입소 심의회, 8/10 오전)"
                },
                e1: new[] {
                    "증빙자료 내 운영팀 보관자료 준비, 결과보고서 박스·증빙자료 준비(8/4 완료 예정)",
                    "견본 검수 결과 보고(8/13)",
                    "적재 물품 정리(본원): 견본(404책·4,040권)·견본검수대조표, 기획팀 수정본 자료, 아이패드·갤럭시 각 20대"
                },
                e2: new[] {
                    "견본 검수 결과 작업(8/13~8/16)",
                    "견본 검수 결과 발표(8/21 오전 10시) 및 직권수정요청서 출원사 송부",
                    "생극창고 적재물품 반납: 심사본, 수정본·수정보완대조표, 사무용품 전체"
                });

            return room;
        }

        // ---- 인터넷 검색실 + 운영대표 집무실 (운영팀실 내 파티션) ----

        private static RoomData BuildOpsSearchRoom()
        {
            var room = NewRoom(
                "ops_search", "인터넷 검색실", "운영대표 · 인터넷 검색 지원",
                new Vector3(2.4f, 2.6f, 2.0f),
                new Color32(0x27, 0x52, 0x97, 0xff),
                new Vector4(0.2907f, 0.0455f, 0.1174f, 0.4546f),
                nestedIn: "operation");

            AddFurniture(room, FurnitureKind.Desks, new Vector3(-0.7f, 0f, -0.5f), "인터넷 연결 PC (2대, 위원 자료조사용)");
            AddFurniture(room, FurnitureKind.ExecDesk, new Vector3(0.7f, 0f, 0.6f), "운영대표 집무실 책상");

            return room;
        }

        // ---- 운영팀 창고 (운영팀실 내 파티션) ----

        private static RoomData BuildOpsStorage()
        {
            var room = NewRoom(
                "ops_storage", "운영팀 창고", "운영팀 · 심사본/임차물품 보관",
                new Vector3(2.4f, 2.6f, 2.0f),
                new Color32(0x27, 0x52, 0x97, 0xff),
                new Vector4(0.2907f, 0.5001f, 0.1174f, 0.4545f),
                nestedIn: "operation");

            AddFurniture(room, FurnitureKind.Crate, new Vector3(-0.5f, 0f, 0f), "심사본 보관 창고(PT박스)");
            AddFurniture(room, FurnitureKind.Crate, new Vector3(0.5f, 0f, 0f), "노트북 등 임차 물품 창고");

            return room;
        }

        // ---- 자료보관실 (운영팀실 내 파티션) ----

        private static RoomData BuildData()
        {
            var room = NewRoom(
                "data", "자료보관실", "자료팀장 · 운영위원(심사자료)",
                new Vector3(2.5f, 2.6f, 2.0f),
                new Color32(0x4f, 0x7f, 0x8f, 0xff),
                new Vector4(0.4081f, 0.0909f, 0.0769f, 0.8182f),
                nestedIn: "operation");

            AddFurniture(room, FurnitureKind.Shelf, new Vector3(0f, 0f, -0.81f), "자료 서가 (3조)");

            FillCommonSchedules(room,
                d1: new[] {
                    "운영팀 제공자료 공유폴더 세팅(심사본 PDF·매뉴얼·과목별 자료, 외래어·로마자 표기 용례집)",
                    "심사본 보관 박스(PT박스)별 라벨지 제작(보관본/전시본/기획위원/연구위원/검정위원 등)",
                    "심의위원별 서류(위촉장·업무이행약정서·사실확인서·개인정보동의서) 협의실 비치",
                    "결과보고서 박스·증빙자료 박스 준비"
                },
                d2: new[] {
                    "바코드 프린터 점검 및 심사본 전수 바코드 부착(접수-심사번호 이중관리 체계 반영)",
                    "심사본·매뉴얼 수량 최종 확인 및 비치, 진행·운영·외부 세 팀장 현장 업무 지원"
                },
                mid: new[] {
                    "심사 진행 중 서류 정리·보관(보안 유지 원칙, 관계자 외 열람 금지)",
                    "심사본 분실·훼손 발생 시 신고서 접수 지원 및 재배부 자료 준비",
                    "추가 자료 요청 대응 및 전달"
                },
                e1: new[] {
                    "결과보고서 박스·증빙자료·심사본·매뉴얼 수거품 분류 및 포장",
                    "심의위원장·전체 위원 날인 결과물 수합 지원(간사 경유)"
                },
                e2: new[] {
                    "심사 결과물(기초조사·본심사·이의신청·수정본·견본 보고서, 회의록 등) 외부인 통제 구역으로 통합 이전·보관",
                    "전체 서류·심사본 최종 반납 처리 및 공유폴더 자료 정리·백업"
                });

            return room;
        }

        // ---- 내외부 연결공간 (보안 면담실) ----
        //
        // 원래 "라운지/입소식 공간" 가정으로 만들었던 방인데, 사용자가 실제 구조를
        // "보안 합숙 공간 — 내부와 외부가 좁은 공간에서 창구로 면담하고, CCTV가 있고,
        // 벨을 눌러 소통하는 곳"이라고 정정해줘서 치수·가구를 이에 맞게 교체함. 입소식·
        // 결과발표는 실제로 크리스탈홀·이상설홀 같은 별도 대강당에서 열려 이 방과 무관하므로
        // (사용자 확인) 업무 목록에서도 제외하고, 이 방 자체의 면담 창구 기능으로 교체함.
        private static RoomData BuildHub()
        {
            var room = NewRoom(
                "hub", "내외부 연결공간", "보안 면담실 · 내부-외부 창구 면담",
                new Vector3(2.4f, 2.6f, 1.8f),
                new Color32(0xa9, 0x82, 0x3f, 0xff),
                new Vector4(0.5007f, 0.0455f, 0.1304f, 0.9091f));

            AddFurniture(room, FurnitureKind.Counter, new Vector3(0f, 0f, 0f), "면담 창구(내부-외부 칸막이, 유리창)");
            AddFurniture(room, FurnitureKind.CallBell, new Vector3(-0.9f, 1.1f, -0.85f), "호출벨(인터폰) — 내부/외부 소통");
            AddFurniture(room, FurnitureKind.Cctv, new Vector3(0f, 1.25f, -0.8f), "CCTV");

            FillCommonSchedules(room,
                d1: new[] { "면담 창구·호출벨·CCTV 작동 점검" },
                d2: new[] { "위원 입소 관련 내부-외부 연락 중계(필요 시 창구 면담 응대)" },
                mid: new[] { "내부·외부 연락 중계(필요 시 호출벨로 면담 요청 응대)" },
                e1: new[] { "퇴소 준비 관련 내부-외부 연락 중계" },
                e2: new[] { "퇴소 당일 내부-외부 연락 중계 마감" });

            return room;
        }

        // ---- 심의회실 ----

        private static RoomData BuildReview()
        {
            var room = NewRoom(
                "review", "심의회실", "심의위원 · 연구/검정위원",
                new Vector3(6.5f, 2.6f, 5.0f),
                new Color32(0x6a, 0x5a, 0x8a, 0xff),
                new Vector4(0.6389f, 0.0455f, 0.2086f, 0.9091f));

            AddFurniture(room, FurnitureKind.Table, new Vector3(-0.65f, 0f, -0.5f), "심의 회의테이블 (8인)");
            AddFurniture(room, FurnitureKind.Desks, new Vector3(1.83f, 0f, -2.08f), "개별 심사용 PC·듀얼모니터");
            AddFurniture(room, FurnitureKind.DocSet, new Vector3(1.4f, 0f, -1.6f), "서류봉투·노란색/파란색 파일홀더 (입소 전날까지 책상에 세팅)");
            AddFurniture(room, FurnitureKind.Shelf, new Vector3(-2.75f, 0f, 1.6f), "심사본 서가 (위원별로 분류되어 꽂혀 있음)");
            AddFurniture(room, FurnitureKind.Whiteboard, new Vector3(-2.59f, 1.3f, -2.12f), "화이트보드");
            AddFurniture(room, FurnitureKind.SideTable, new Vector3(2.75f, 0f, -2.0f), "보조 테이블(프린터용, 개별 PC 책상 옆)");
            AddFurniture(room, FurnitureKind.Printer, new Vector3(2.75f, 0.45f, -2.0f), "A4 프린터 (보조 테이블 위)");
            AddFurniture(room, FurnitureKind.Tv, new Vector3(-0.91f, 1.3f, -2.48f), "TV (PC 연결)");
            AddFurniture(room, FurnitureKind.SnackCounter, new Vector3(0f, 0f, 2.3f), "공용간식대(과자 등 구비, 심의회실 입구)");

            FillCommonSchedules(room,
                d1: new[] {
                    "집결지 인솔 입소 및 명찰 수령",
                    "위원 준수사항 고지 수령(검정 관련 사항 비공개, 외출·외박 금지, 공정성 훼손 행위 금지 등)",
                    "서약서·업무이행약정서 서명, 사실확인서·개인정보수집이용동의서 제출",
                    "(진행팀 세팅 확인) 책상 위 서류봉투·노란색/파란색 파일홀더가 입소 전날까지 놓여 있는지 확인"
                },
                d2: new[] {
                    "기초조사/본심사 연수 참여(교육과정·검정기준·편찬 유의점·보안교육 등)",
                    "심사본 인수·인계서에 날인 후 수령, 개인 보안 서약 및 PC 보안 조치"
                },
                mid: new[] {
                    "내용조사·표기표현조사 등 개별·합숙 조사 수행(1책 2인 조사 또는 영역별 협의)",
                    "심의회 토의·토론을 통한 보고서 종합, 본심사 1~3라운드 평정 수행",
                    "활동범위 합숙 장소 내 한정, 외부통화 차단 수칙 준수"
                },
                e1: new[] {
                    "최종 심의 완료 및 합의",
                    "결과보고서·수정보완권고서·불합격판정사유서 등 전원 날인"
                },
                e2: new[] {
                    "심의회별 지정 퇴소 장소에서 퇴소(타 심의회 위원과 분리 이동)",
                    "퇴소 전 설문조사 참여, 심사본·매뉴얼 반납"
                });

            return room;
        }

        // ---- 외부지원팀실 ----

        private static RoomData BuildExternal()
        {
            var room = NewRoom(
                "external", "외부지원팀실", "외부팀장 · 심사지원(외부지원)팀장",
                new Vector3(4.5f, 2.6f, 3.6f),
                new Color32(0xc0, 0x7a, 0x3e, 0xff),
                new Vector4(0.8553f, 0.0455f, 0.1369f, 0.9091f));

            AddFurniture(room, FurnitureKind.Desks, new Vector3(-1.39f, 0f, -0.68f), "데스크탑 PC (2대)");
            AddFurniture(room, FurnitureKind.Sofa, new Vector3(0.48f, 0f, -0.68f), "소파");
            AddFurniture(room, FurnitureKind.Shelf, new Vector3(-1.57f, 0f, -1.12f), "도서서가");
            AddFurniture(room, FurnitureKind.Printer, new Vector3(1.65f, 0f, -1.12f), "A4 프린터");
            AddFurniture(room, FurnitureKind.Crate, new Vector3(-1.34f, 0f, 1.27f), "보안·창고 물품");
            AddFurniture(room, FurnitureKind.Tv, new Vector3(0.27f, 1.3f, -1.79f), "TV (PC 연결)");

            FillCommonSchedules(room,
                d1: new[] {
                    "심의위원 인력풀 최종 확인, 집결지 운영 및 인솔요원·보안요원 편성 협조",
                    "서약서·증빙서류(가족관계증명서·재직증명서·졸업증명서 등) 최종 수합",
                    "합숙 장소 출입구 보안요원 배치, 출입자 명단 사전 접수(명단 외 인원 통제 준비)"
                },
                d2: new[] {
                    "위원 인솔 이동(이동경로 비공개, 통화 자제 안내) 및 입소 확인·서약 서명 지원",
                    "보안요원 24시간 근무 개시, 명찰 패용·외출외박 금지 등 보안수칙 안내",
                    "격리·위험보상비 수당 지급 준비"
                },
                mid: new[] {
                    "보안구역 정기 순찰 및 일일 출입자 현황·순찰일지 작성·본부 보고",
                    "위원 개별 요청(편의·건강) 지원, 운영 물품·자료 추가 조달",
                    "외부통화 차단 등 보안수칙 위반 여부 점검"
                },
                e1: new[] {
                    "심의회별 퇴소 장소 운영(타 심의회 출입 통제) 준비",
                    "위원 퇴소 교통 지원 준비, 수당(대체강사료·격리위험보상비) 정산 자료 취합"
                },
                e2: new[] {
                    "해당 심의회 위원에 한해 퇴소 지원 완료(출입 명단 대비 확인)",
                    "서약서·약정서 최종 보관 처리, 외부지원 비용 정산 마감"
                });

            // 수정본 검토·견본 검수: 외부지원팀 실제 합숙 체크리스트 원문은 "'24.2.1 합숙 업무
            // 체크리스트"(본심사 합숙 전/중 업무, 기간 미표기) 1건뿐이라 두 단계에 동일하게
            // 임시 적용. 보안·차량·입소 절차 자체는 단계가 바뀌어도 거의 동일하다는 전제.
            string[] extD1 = {
                "인솔자 임명 및 간담회 개최(2/6, 본원 341호)",
                "종합 심의회 대강료 지급 계획(안) 수립, 보건요원 임명",
                "합숙본부 승합차량 운영비용 지급 계획(안) 수립",
                "단가계약 발주(대형버스·화물차·승합차): 관리팀·기획팀·심의위원 및 간사 입소건"
            };
            string[] extD2 = {
                "심의위원·간사 입소안내문·대강료·집결지 조사 이메일·문자 발송",
                "심의위원·간사 집결지 현황 정리, 입소 관련 안내 문자 발송",
                "전산요원 위촉(상록에스), 출장명령서 작성(기획팀·관리팀·간사)"
            };
            string[] extMid = {
                "심의회 입소자 반납 휴대전화 충전 시작(내부 합숙자)",
                "내·외부 업무연락 상시 대응",
                "과목별 퇴소(심의위원·간사·기획팀) 관련 대형버스·승합차 임차 준비(퇴소건 발주)"
            };
            string[] extE1 = {
                "과목별 퇴소 개인휴대전화 퇴소지별 세팅",
                "파쇄업체 담당자 연락(사전 도착 일정 확인)"
            };
            string[] extE2 = {
                "대형버스·화물차 임차(관리팀 퇴소용) 최종 확인",
                "퇴소 완료 후 임차 차량·장비 반납 확인"
            };
            FillPhase(room, ReviewPhase.RevisionReview, extD1, extD2, extMid, extE1, extE2);
            FillPhase(room, ReviewPhase.SampleInspection, extD1, extD2, extMid, extE1, extE2);

            return room;
        }
    }
}
