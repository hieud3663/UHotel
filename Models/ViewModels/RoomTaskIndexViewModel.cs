namespace HotelManagement.Models.ViewModels
{
    public class RoomTaskIndexViewModel
    {
        public PagedList<RoomTask> Tasks { get; set; } = new(new List<RoomTask>(), 0, 1, 10);
        public RoomTaskSummaryViewModel Summary { get; set; } = new();
    }

    public class RoomTaskSummaryViewModel
    {
        public int PendingCount { get; set; }
        public int AssignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int UrgentCount { get; set; }
        public int OverdueCount { get; set; }
    }
}
