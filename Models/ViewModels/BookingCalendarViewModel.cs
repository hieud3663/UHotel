using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Models.ViewModels
{
    public class BookingCalendarViewModel
    {
        public DateTime StartDate { get; set; }
        public string ViewMode { get; set; } = BookingCalendarViewModes.Week;
        public string? RoomCategoryID { get; set; }
        public string? RoomStatus { get; set; }
        public string? ReservationStatus { get; set; }
        public string? Keyword { get; set; }
        public bool CanEditSchedule { get; set; }
        public List<SelectListItem> RoomCategories { get; set; } = new();
        public List<SelectListItem> RoomStatuses { get; set; } = new();
        public List<SelectListItem> ReservationStatuses { get; set; } = new();
    }

    public static class BookingCalendarViewModes
    {
        public const string Day = "day";
        public const string Week = "week";
        public const string Month = "month";

        public static readonly string[] All = { Day, Week, Month };
    }

    public class BookingCalendarEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string ReservationFormID { get; set; } = string.Empty;
        public string RoomID { get; set; } = string.Empty;
        public string? RoomCategoryID { get; set; }
        public string? RoomCategoryName { get; set; }
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime ExpectedCheckOutDate { get; set; }
        public DateTime? ActualCheckOutDate { get; set; }
        public string? CheckoutTimingText { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool Editable { get; set; }
        public bool HasCheckedIn { get; set; }
        public bool HasCheckedOut { get; set; }
        public string PriceUnit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public double RoomBookingDeposit { get; set; }
        public string? DetailsUrl { get; set; }
    }

    public class BookingCalendarRoomResourceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RoomStatus { get; set; } = string.Empty;
        public string RoomStatusText { get; set; } = string.Empty;
        public string? RoomCategoryID { get; set; }
        public string? RoomCategoryName { get; set; }
        public bool HasOpenTask { get; set; }
        public string? OpenTaskText { get; set; }
    }

    public class BookingCalendarAvailabilityResult
    {
        public bool Available { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RoomStatus { get; set; }
        public string? RoomStatusText { get; set; }
    }

    public class BookingCalendarPricePreviewDto
    {
        public bool RequiresPriceConfirmation { get; set; }
        public string ReservationFormID { get; set; } = string.Empty;
        public string PriceUnit { get; set; } = string.Empty;
        public string? CurrentRoomID { get; set; }
        public string? CurrentRoomCategoryName { get; set; }
        public string? NewRoomID { get; set; }
        public string? NewRoomCategoryName { get; set; }
        public decimal CurrentUnitPrice { get; set; }
        public decimal NewUnitPrice { get; set; }
        public decimal CurrentEstimatedRoomCharge { get; set; }
        public decimal NewEstimatedRoomCharge { get; set; }
        public decimal EstimatedDifference { get; set; }
        public double CurrentDeposit { get; set; }
        public decimal SuggestedDeposit { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MoveReservationRequest
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public string RoomID { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public bool ConfirmPriceChange { get; set; }
    }
}
