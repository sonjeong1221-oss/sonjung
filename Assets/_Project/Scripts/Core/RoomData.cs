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

    /// 실제 사업계획서 Ⅷ장(검정심사 방법) 2·3·5·6절 기준 4단계.
    /// 기초조사·본심사는 합숙 연수/재택조사(심사)/심의회 개최로 구성된 다단계 일정이고,
    /// 수정본 검토·견본 검수는 연중 별도 시기(예: 6~7월, 8월)에 진행되는 사무 검토 성격이 강해
    /// 같은 "7개 공간" 합숙 구조를 그대로 쓸지는 운영팀 확인이 필요하다(개발명세서 9·11절).
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
        Cooler
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
