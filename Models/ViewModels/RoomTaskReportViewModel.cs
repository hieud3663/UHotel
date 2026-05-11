namespace HotelManagement.Models.ViewModels
{
    public class RoomTaskReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TaskType { get; set; }
        public string? EmployeeID { get; set; }
        public string? RoomID { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double AverageProcessingHours { get; set; }
        public List<RoomTaskEmployeeReportItem> EmployeeItems { get; set; } = new();
        public List<RoomTaskRoomReportItem> RoomItems { get; set; } = new();
    }

    public class RoomTaskEmployeeReportItem
    {
        public string EmployeeID { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int AssignedCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double AverageProcessingHours { get; set; }
    }

    public class RoomTaskRoomReportItem
    {
        public string RoomID { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int CleaningCount { get; set; }
        public int MaintenanceCount { get; set; }
        public int OverdueCount { get; set; }
        public int TotalTasks { get; set; }
        public int CleaningTasks { get; set; }
        public int MaintenanceTasks { get; set; }
        public int OverdueTasks { get; set; }
    }
}
