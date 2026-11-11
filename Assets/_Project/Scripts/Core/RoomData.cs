using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualOffice.Core
{
    /// One space in the handover facility (심의회실, 운영팀 등). Holds its real-world-ish
    /// size, its furniture layout, and its per-day task checklist — the single source of
    /// truth both the 3D scene and any UI panel reads from.
    [CreateAssetMenu(menuName = "VirtualOffice/Room Data", fileName = "RoomData")]
    public class RoomData : ScriptableObject
    {
        [Header("식별")]
        public string roomId;
        public string label;
        [TextArea] public string roleDescription;

        [Header("치수 (m)")]
        public Vector3 size = new Vector3(5f, 2.6f, 4f);

        [Header("중첩 (예: 자료보관실 → operation)")]
        public string nestedInRoomId;

        [Header("씬")]
        public string sceneName;

        [Header("텔레포트 스폰 지점 (허브/미니맵에서 이 방 클릭 시)")]
        public Vector3 spawnPosition;
        public float spawnYaw;

        [Header("표시")]
        public Color accentColor = Color.white;

        [Header("허브 평면도상 위치 (정규화 0~1: x,y,w,h)")]
        public Vector4 planRect = new Vector4(0f, 0f, 0.1f, 0.9f);

        public List<FurnitureEntry> furniture = new List<FurnitureEntry>();

        [Header("심사 단계별 일자 업무 (기초조사·본심사는 별도 합숙, 수정본검토·견본검수는 각각 다른 시기)")]
        public List<PhaseSchedule> schedules = new List<PhaseSchedule>();
    }

    [Serializable]
    public class PhaseSchedule
    {
        public ReviewPhase phase;
        public List<DailyTask> tasksByDay = new List<DailyTask>();
    }

    /// 실제 사업계획서 Ⅷ장(검정심사 방법) 2·3·5·6절 기준 4단계. 진행 시기와 세부 업무는 단계마다
    /// 다르지만(기초조사·본심사는 1월 연속 합숙, 수정본 검토는 6~7월, 견본 검수는 8월), "개설(설치)
    /// → 운영(중간) → 철거(퇴소) 준비" 절차 자체는 4단계 모두 동일하다고 확인됨 — 그래서 RoomData의
    /// 공간·CampDay 구조는 공유하고 schedules(업무 내용)만 단계별로 따로 채운다(개발명세서 11절).
    public enum ReviewPhase
    {
        BasicSurvey,      // 기초조사
        MainReview,       // 본심사
        RevisionReview,   // 수정본 검토
        SampleInspection  // 견본 검수
    }

    [Serializable]
    public class FurnitureEntry
    {
        public FurnitureKind kind;
        public Vector3 localPosition;
        public string label;
        public bool interactable = true;
    }

    public enum FurnitureKind
    {
        Desks,
        Table,
        Shelf,
        Tv,
        Whiteboard,
        Printer,
        Crate,
        Sofa,
        Cooler,
        ExecDesk,      // 운영대표 집무실 등 — 일반 Desks보다 큰 1인 책상 + 캐비닛
        Cctv,          // 내외부 연결공간(보안 면담실) 천장 CCTV
        CallBell,      // 내외부 연결공간 호출벨/인터폰
        Counter,       // 내외부 연결공간 면담 창구(반칸막이+유리창)
        DocSet,        // 심의회실 책상 위 서류봉투 + 노란/파란 파일홀더
        SnackCounter,  // 공용간식대
        SideTable      // 작은 보조 테이블(프린터 등 올려두는 용도)
    }

    [Serializable]
    public class DailyTask
    {
        public CampDay day;
        public List<string> tasks = new List<string>();
    }

    public enum CampDay
    {
        Setup1,    // 설치1일차
        Setup2,    // 설치2일차
        Mid,       // 중간
        CheckOut1, // 퇴소1일차
        CheckOut2  // 퇴소2일차
    }
}
