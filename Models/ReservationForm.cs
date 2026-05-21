using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ReservationForm")]
    public class ReservationForm
    {
        [Key]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        [Required]
        [Column("reservationDate")]
        [Display(Name = "Ngày đặt phòng")]
        public DateTime ReservationDate { get; set; }

        [Required]
        [Column("checkInDate")]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Required]
        [Column("checkOutDate")]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Column("employeeID")]
        [StringLength(15)]
        public string? EmployeeID { get; set; }

        [Column("roomID")]
        [StringLength(15)]
        public string? RoomID { get; set; }

        [Column("customerID")]
        [StringLength(15)]
        public string? CustomerID { get; set; }

        [Required]
        [Column("roomBookingDeposit")]
        [Display(Name = "Tiền đặt cọc")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền đặt cọc phải lớn hơn hoặc bằng 0")]
        public double RoomBookingDeposit { get; set; }

        [Required]
        [Column("priceUnit")]
        [StringLength(15)]
        [Display(Name = "Hình thức thuê")]
        public string PriceUnit { get; set; } = "DAY";

        [Required]
        [Column("unitPrice", TypeName = "decimal(18, 2)")]
        [Display(Name = "Đơn giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
        public decimal UnitPrice { get; set; }

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        [Column("reservationStatus")]
        [StringLength(20)]
        public string ReservationStatus { get; set; } = "BOOKED";

        // Navigation properties
        [ForeignKey("EmployeeID")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("RoomID")]
        public virtual Room? Room { get; set; }

        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        public virtual ICollection<RoomUsageService>? RoomUsageServices { get; set; }
        public virtual ICollection<RoomChangeHistory>? RoomChangeHistories { get; set; }
        public virtual HistoryCheckin? HistoryCheckin { get; set; }
        public virtual HistoryCheckOut? HistoryCheckOut { get; set; }
        public virtual ICollection<Invoice>? Invoices { get; set; }
    }
}
