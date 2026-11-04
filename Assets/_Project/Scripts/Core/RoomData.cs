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

        [Header("표시")]
        public Color accentColor = Color.white;

        public List<FurnitureEntry> furniture = new List<FurnitureEntry>();
        public List<DailyTask> tasksByDay = new List<DailyTask>();
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
