using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        [Column("employeeID")]
        [StringLength(15)]
        public string EmployeeID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Column("fullName")]
        [StringLength(50)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Column("phoneNumber")]
        [StringLength(10)]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [Column("email")]
        [StringLength(50)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Column("address")]
        [StringLength(100)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Required]
        [Column("gender")]
        [StringLength(6)]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = "MALE";

        [Required(ErrorMessage = "Số CMND/CCCD không được để trống")]
        [Column("idCardNumber")]
        [StringLength(12)]
        [Display(Name = "Số CMND/CCCD")]
        public string IdCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [Column("dob")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime Dob { get; set; }

        [Required]
        [Column("position")]
        [StringLength(15)]
        [Display(Name = "Vị trí")]
        public string Position { get; set; } = "RECEPTIONIST";

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        public virtual ICollection<ReservationForm>? ReservationForms { get; set; }
        public virtual ICollection<HistoryCheckin>? HistoryCheckins { get; set; }
        public virtual ICollection<HistoryCheckOut>? HistoryCheckOuts { get; set; }
        public virtual ICollection<RoomChangeHistory>? RoomChangeHistories { get; set; }
        public virtual ICollection<RoomUsageService>? RoomUsageServices { get; set; }
        public virtual ICollection<RoomTask>? CreatedRoomTasks { get; set; }
        public virtual ICollection<RoomTask>? AssignedRoomTasks { get; set; }
        public virtual ICollection<RoomTaskHistory>? RoomTaskHistories { get; set; }
    }
}
