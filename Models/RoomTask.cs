using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("RoomTask")]
    public class RoomTask
    {
        [Key]
        [Column("roomTaskID")]
        [StringLength(15)]
        public string RoomTaskID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phòng không được để trống")]
        [Column("roomID")]
        [StringLength(15)]
        [Display(Name = "Phòng")]
        public string RoomID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại công việc không được để trống")]
        [Column("taskType")]
        [StringLength(20)]
        [Display(Name = "Loại công việc")]
        public string TaskType { get; set; } = RoomTaskTypes.Cleaning;

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [Column("title")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Mức độ ưu tiên không được để trống")]
        [Column("priority")]
        [StringLength(20)]
        [Display(Name = "Mức độ ưu tiên")]
        public string Priority { get; set; } = RoomTaskPriorities.Normal;

        [Required]
        [Column("status")]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = RoomTaskStatuses.Pending;

        [Column("assignedEmployeeID")]
        [StringLength(15)]
        [Display(Name = "Nhân viên phụ trách")]
        public string? AssignedEmployeeID { get; set; }

        [Required]
        [Column("createdByEmployeeID")]
        [StringLength(15)]
        [Display(Name = "Người tạo")]
        public string CreatedByEmployeeID { get; set; } = string.Empty;

        [Required]
        [Column("createdAt")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [Column("startedAt")]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartedAt { get; set; }

        [Column("completedAt")]
        [Display(Name = "Ngày hoàn thành")]
        public DateTime? CompletedAt { get; set; }

        [Column("cancelledAt")]
        [Display(Name = "Ngày hủy")]
        public DateTime? CancelledAt { get; set; }

        [Column("cancelReason")]
        [StringLength(500)]
        [Display(Name = "Lý do hủy")]
        public string? CancelReason { get; set; }

        [Column("note")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Column("dueAt")]
        [Display(Name = "Hạn xử lý")]
        public DateTime? DueAt { get; set; }

        [Column("slaMinutes")]
        [Display(Name = "SLA dự kiến (phút)")]
        public int? SlaMinutes { get; set; }

        [ForeignKey("RoomID")]
        public virtual Room? Room { get; set; }

        [ForeignKey("AssignedEmployeeID")]
        public virtual Employee? AssignedEmployee { get; set; }

        [ForeignKey("CreatedByEmployeeID")]
        public virtual Employee? CreatedByEmployee { get; set; }

        public virtual ICollection<RoomTaskHistory>? Histories { get; set; }
    }

    public static class RoomTaskTypes
    {
        public const string Cleaning = "CLEANING";
        public const string Maintenance = "MAINTENANCE";

        public static readonly string[] All = { Cleaning, Maintenance };
    }

    public static class RoomTaskStatuses
    {
        public const string Pending = "PENDING";
        public const string Assigned = "ASSIGNED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";

        public static readonly string[] OpenStatuses = { Pending, Assigned, InProgress };
        public static readonly string[] All = { Pending, Assigned, InProgress, Completed, Cancelled };
    }

    public static class RoomTaskPriorities
    {
        public const string Low = "LOW";
        public const string Normal = "NORMAL";
        public const string High = "HIGH";
        public const string Urgent = "URGENT";

        public static readonly string[] All = { Low, Normal, High, Urgent };
    }

    public static class RoomOperationalStatuses
    {
        public const string Available = "AVAILABLE";
        public const string OnUse = "ON_USE";
        public const string Unavailable = "UNAVAILABLE";
        public const string Overdue = "OVERDUE";
        public const string Reserved = "RESERVED";
        public const string Dirty = "DIRTY";
        public const string Cleaning = "CLEANING";
        public const string Maintenance = "MAINTENANCE";
        public const string OutOfService = "OUT_OF_SERVICE";
    }
}
