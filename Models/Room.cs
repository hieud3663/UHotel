using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("Room")]
    public class Room
    {
        [Key]
        [Column("roomID")]
        [StringLength(15)]
        public string RoomID { get; set; } = string.Empty;

        [Required]
        [Column("roomStatus")]
        [StringLength(20)]
        [Display(Name = "Trạng thái phòng")]
        public string RoomStatus { get; set; } = "AVAILABLE";

        [Required]
        [Column("dateOfCreation")]
        [Display(Name = "Ngày tạo")]
        public DateTime DateOfCreation { get; set; }

        [Required]
        [Column("roomCategoryID")]
        [StringLength(15)]
        [Display(Name = "Loại phòng")]
        public string RoomCategoryID { get; set; } = string.Empty;

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        [ForeignKey("RoomCategoryID")]
        public virtual RoomCategory? RoomCategory { get; set; }
        public virtual ICollection<ReservationForm>? ReservationForms { get; set; }
        public virtual ICollection<RoomChangeHistory>? RoomChangeHistories { get; set; }
        public virtual ICollection<RoomTask>? RoomTasks { get; set; }
    }
}
