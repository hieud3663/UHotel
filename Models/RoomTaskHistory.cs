using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("RoomTaskHistory")]
    public class RoomTaskHistory
    {
        [Key]
        [Column("roomTaskHistoryID")]
        [StringLength(15)]
        public string RoomTaskHistoryID { get; set; } = string.Empty;

        [Required]
        [Column("roomTaskID")]
        [StringLength(15)]
        public string RoomTaskID { get; set; } = string.Empty;

        [Column("oldStatus")]
        [StringLength(20)]
        [Display(Name = "Trạng thái cũ")]
        public string? OldStatus { get; set; }

        [Required]
        [Column("newStatus")]
        [StringLength(20)]
        [Display(Name = "Trạng thái mới")]
        public string NewStatus { get; set; } = string.Empty;

        [Required]
        [Column("changedByEmployeeID")]
        [StringLength(15)]
        [Display(Name = "Người thay đổi")]
        public string ChangedByEmployeeID { get; set; } = string.Empty;

        [Required]
        [Column("changedAt")]
        [Display(Name = "Thời điểm thay đổi")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [Column("note")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [ForeignKey("RoomTaskID")]
        public virtual RoomTask? RoomTask { get; set; }

        [ForeignKey("ChangedByEmployeeID")]
        public virtual Employee? ChangedByEmployee { get; set; }
    }
}
