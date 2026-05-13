using HotelManagement.Data;
using HotelManagement.Helpers;
using HotelManagement.Models;
using HotelManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    public class BookingCalendarController : BaseController
    {
        private readonly HotelManagementContext _context;

        public BookingCalendarController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        private bool CanEditSchedule()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "ADMIN" || role == "MANAGER" || role == "EMPLOYEE";
        }

        public async Task<IActionResult> Index(
            DateTime? startDate,
            string? viewMode,
            string? roomCategoryID,
            string? roomStatus,
            string? reservationStatus,
            string? keyword)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var model = new BookingCalendarViewModel
            {
                StartDate = (startDate ?? DateTime.UtcNow.AddHours(7).Date).Date,
                ViewMode = BookingCalendarViewModes.All.Contains(viewMode) ? viewMode! : BookingCalendarViewModes.Week,
                RoomCategoryID = roomCategoryID,
                RoomStatus = roomStatus,
                ReservationStatus = reservationStatus,
                Keyword = keyword,
                CanEditSchedule = CanEditSchedule(),
                RoomCategories = await BuildRoomCategoryOptions(roomCategoryID),
                RoomStatuses = BuildRoomStatusOptions(roomStatus),
                ReservationStatuses = BuildReservationStatusOptions(reservationStatus)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Rooms(string? roomCategoryID, string? roomStatus)
        {
            if (!CheckAuth()) return Unauthorized();

            var openTaskStatuses = RoomTaskStatuses.OpenStatuses;
            var query = _context.Rooms
                .Include(r => r.RoomCategory)
                .Where(r => r.IsActivate == "ACTIVATE");

            if (!string.IsNullOrWhiteSpace(roomCategoryID))
            {
                query = query.Where(r => r.RoomCategoryID == roomCategoryID);
            }

            if (!string.IsNullOrWhiteSpace(roomStatus))
            {
                query = query.Where(r => r.RoomStatus == roomStatus);
            }

            var rooms = await query
                .OrderBy(r => r.RoomID)
                .Select(r => new BookingCalendarRoomResourceDto
                {
                    Id = r.RoomID,
                    Title = r.RoomID + " - " + (r.RoomCategory != null ? r.RoomCategory.RoomCategoryName : "Chưa có loại phòng"),
                    RoomStatus = r.RoomStatus,
                    RoomStatusText = StatusDisplayHelper.RoomStatusText(r.RoomStatus),
                    RoomCategoryID = r.RoomCategoryID,
                    RoomCategoryName = r.RoomCategory != null ? r.RoomCategory.RoomCategoryName : null,
                    HasOpenTask = _context.RoomTasks.Any(t => t.RoomID == r.RoomID && openTaskStatuses.Contains(t.Status)),
                    OpenTaskText = _context.RoomTasks
                        .Where(t => t.RoomID == r.RoomID && openTaskStatuses.Contains(t.Status))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.TaskType == RoomTaskTypes.Cleaning ? "Có yêu cầu dọn phòng" : "Có yêu cầu bảo trì")
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> Events(
            DateTime start,
            DateTime end,
            string? roomCategoryID,
            string? roomStatus,
            string? reservationStatus,
            string? keyword)
        {
            if (!CheckAuth()) return Unauthorized();

            if (end <= start)
            {
                return BadRequest(new { message = "Khoảng thời gian xem lịch không hợp lệ." });
            }

            var now = DateTime.UtcNow.AddHours(7);
            var canEdit = CanEditSchedule();
            var query = _context.ReservationForms
                .Include(r => r.Room)
                .ThenInclude(room => room!.RoomCategory)
                .Include(r => r.Customer)
                .Include(r => r.HistoryCheckin)
                .Include(r => r.HistoryCheckOut)
                .Where(r => r.IsActivate == "ACTIVATE")
                .Where(r => r.CheckInDate < end &&
                    (r.HistoryCheckOut != null ? r.HistoryCheckOut.CheckOutDate : r.CheckOutDate) > start);

            if (!string.IsNullOrWhiteSpace(roomCategoryID))
            {
                query = query.Where(r => r.Room != null && r.Room.RoomCategoryID == roomCategoryID);
            }

            if (!string.IsNullOrWhiteSpace(roomStatus))
            {
                query = query.Where(r => r.Room != null && r.Room.RoomStatus == roomStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(r =>
                    r.ReservationFormID.Contains(normalizedKeyword) ||
                    (r.Customer != null && r.Customer.FullName.Contains(normalizedKeyword)) ||
                    (r.RoomID != null && r.RoomID.Contains(normalizedKeyword)));
            }

            var reservations = await query
                .OrderBy(r => r.CheckInDate)
                .ToListAsync();

            var events = reservations
                .Select(r => BuildCalendarEvent(r, now, canEdit))
                .Where(e => string.IsNullOrWhiteSpace(reservationStatus) || e.Status == reservationStatus)
                .ToList();

            return Json(events);
        }

        [HttpGet]
        public async Task<IActionResult> Tasks(DateTime start, DateTime end, string? roomCategoryID, string? roomStatus)
        {
            if (!CheckAuth()) return Unauthorized();

            if (end <= start)
            {
                return BadRequest(new { message = "Khoảng thời gian xem lịch không hợp lệ." });
            }

            var openStatuses = RoomTaskStatuses.OpenStatuses;
            var query = _context.RoomTasks
                .Include(t => t.Room)
                .ThenInclude(r => r!.RoomCategory)
                .Where(t => openStatuses.Contains(t.Status))
                .Where(t => t.CreatedAt < end && (t.CompletedAt ?? t.CancelledAt ?? t.DueAt ?? end) >= start);

            if (!string.IsNullOrWhiteSpace(roomCategoryID))
            {
                query = query.Where(t => t.Room != null && t.Room.RoomCategoryID == roomCategoryID);
            }

            if (!string.IsNullOrWhiteSpace(roomStatus))
            {
                query = query.Where(t => t.Room != null && t.Room.RoomStatus == roomStatus);
            }

            var tasks = await query
                .OrderBy(t => t.RoomID)
                .ThenBy(t => t.DueAt == null)
                .ThenBy(t => t.DueAt)
                .Select(t => new
                {
                    id = t.RoomTaskID,
                    roomID = t.RoomID,
                    roomCategoryName = t.Room != null && t.Room.RoomCategory != null ? t.Room.RoomCategory.RoomCategoryName : null,
                    start = t.CreatedAt,
                    end = t.DueAt ?? t.CreatedAt.AddHours(2),
                    taskType = t.TaskType,
                    status = t.Status,
                    title = t.TaskType == RoomTaskTypes.Cleaning ? "Dọn phòng" : "Bảo trì",
                    color = t.TaskType == RoomTaskTypes.Cleaning ? "#f59e0b" : "#dc2626",
                    detailsUrl = Url.Action("Details", "RoomMaintenanceCleaning", new { id = t.RoomTaskID })
                })
                .ToListAsync();

            return Json(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(string roomID, DateTime checkInDate, DateTime checkOutDate, string? excludeReservationFormID = null)
        {
            if (!CheckAuth()) return Unauthorized();

            var result = await ValidateAvailability(roomID, checkInDate, checkOutDate, excludeReservationFormID);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> PreviewPriceChange(string reservationFormID, string roomID, DateTime checkInDate, DateTime checkOutDate)
        {
            if (!CheckAuth()) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (!CanEditSchedule())
            {
                return Forbid();
            }

            try
            {
                var preview = await BuildPricePreview(reservationFormID, roomID, checkInDate, checkOutDate);
                return Json(preview);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveReservation([FromBody] MoveReservationRequest request)
        {
            if (!CheckAuth()) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (!CanEditSchedule())
            {
                return Forbid();
            }

            var employeeID = HttpContext.Session.GetString("EmployeeID");
            if (string.IsNullOrWhiteSpace(employeeID))
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin nhân viên trong phiên đăng nhập." });
            }

            try
            {
                var pricePreview = await BuildPricePreview(
                    request.ReservationFormID,
                    request.RoomID,
                    request.CheckInDate,
                    request.CheckOutDate);

                if (pricePreview.RequiresPriceConfirmation && !request.ConfirmPriceChange)
                {
                    return BadRequest(new
                    {
                        success = false,
                        requiresPriceConfirmation = true,
                        message = "Đơn giá phòng thay đổi. Vui lòng xác nhận giá mới trước khi lưu.",
                        pricePreview
                    });
                }

                await _context.UpdateReservationScheduleSP(
                    request.ReservationFormID,
                    request.RoomID,
                    request.CheckInDate,
                    request.CheckOutDate,
                    employeeID,
                    pricePreview.PriceUnit,
                    pricePreview.NewUnitPrice,
                    pricePreview.SuggestedDeposit);

                return Json(new { success = true, message = "Đã cập nhật lịch đặt phòng và đơn giá liên quan." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        private async Task<BookingCalendarPricePreviewDto> BuildPricePreview(
            string reservationFormID,
            string roomID,
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            if (string.IsNullOrWhiteSpace(reservationFormID) || string.IsNullOrWhiteSpace(roomID))
            {
                throw new InvalidOperationException("Thông tin phiếu đặt phòng hoặc phòng mới không hợp lệ.");
            }

            if (checkOutDate <= checkInDate)
            {
                throw new InvalidOperationException("Ngày trả phòng phải sau ngày nhận phòng.");
            }

            var reservation = await _context.ReservationForms
                .Include(r => r.Room)
                .ThenInclude(room => room!.RoomCategory)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID && r.IsActivate == "ACTIVATE");

            if (reservation == null)
            {
                throw new InvalidOperationException("Phiếu đặt phòng không tồn tại hoặc đã bị hủy.");
            }

            if (reservation.HistoryCheckin != null || reservation.HistoryCheckOut != null)
            {
                throw new InvalidOperationException("Không thể điều chỉnh giá cho phiếu đã check-in hoặc check-out.");
            }

            var newRoom = await _context.Rooms
                .Include(r => r.RoomCategory)
                .FirstOrDefaultAsync(r => r.RoomID == roomID && r.IsActivate == "ACTIVATE");

            if (newRoom == null)
            {
                throw new InvalidOperationException("Phòng mới không tồn tại hoặc đã ngừng hoạt động.");
            }

            var priceUnit = string.IsNullOrWhiteSpace(reservation.PriceUnit) ? "DAY" : reservation.PriceUnit;
            var newPricing = await _context.Pricings
                .FirstOrDefaultAsync(p => p.RoomCategoryID == newRoom.RoomCategoryID && p.PriceUnit == priceUnit);

            if (newPricing == null || newPricing.Price <= 0)
            {
                throw new InvalidOperationException($"Loại phòng mới chưa có bảng giá hợp lệ cho đơn vị {PriceUnitText(priceUnit)}.");
            }

            var oldTimeUnits = CalculateBillingUnits(reservation.CheckInDate, reservation.CheckOutDate, priceUnit);
            var newTimeUnits = CalculateBillingUnits(checkInDate, checkOutDate, priceUnit);
            var currentEstimatedRoomCharge = reservation.UnitPrice * oldTimeUnits;
            var newEstimatedRoomCharge = newPricing.Price * newTimeUnits;
            var currentDeposit = Math.Round((decimal)reservation.RoomBookingDeposit, 0, MidpointRounding.AwayFromZero);
            var hasRoomCategoryChanged = reservation.Room?.RoomCategoryID != newRoom.RoomCategoryID;
            var hasUnitPriceChanged = reservation.UnitPrice != newPricing.Price;
            var suggestedDeposit = hasRoomCategoryChanged
                ? Math.Round(newEstimatedRoomCharge * 0.3m, 0, MidpointRounding.AwayFromZero)
                : currentDeposit;
            var requiresConfirmation = hasRoomCategoryChanged || hasUnitPriceChanged;

            return new BookingCalendarPricePreviewDto
            {
                RequiresPriceConfirmation = requiresConfirmation,
                ReservationFormID = reservation.ReservationFormID,
                PriceUnit = priceUnit,
                CurrentRoomID = reservation.RoomID,
                CurrentRoomCategoryName = reservation.Room?.RoomCategory?.RoomCategoryName,
                NewRoomID = newRoom.RoomID,
                NewRoomCategoryName = newRoom.RoomCategory?.RoomCategoryName,
                CurrentUnitPrice = reservation.UnitPrice,
                NewUnitPrice = newPricing.Price,
                CurrentEstimatedRoomCharge = currentEstimatedRoomCharge,
                NewEstimatedRoomCharge = newEstimatedRoomCharge,
                EstimatedDifference = newEstimatedRoomCharge - currentEstimatedRoomCharge,
                CurrentDeposit = reservation.RoomBookingDeposit,
                SuggestedDeposit = suggestedDeposit,
                Message = hasRoomCategoryChanged
                    ? "Đổi sang loại phòng khác làm thay đổi đơn giá và tiền cọc đề xuất. Cần xác nhận trước khi cập nhật lịch đặt phòng."
                    : requiresConfirmation
                        ? "Đơn giá thay đổi. Tiền cọc hiện tại được giữ nguyên. Cần xác nhận trước khi cập nhật lịch đặt phòng."
                        : "Đơn giá và tiền cọc giữ nguyên."
            };
        }

        private static decimal CalculateBillingUnits(DateTime checkInDate, DateTime checkOutDate, string priceUnit)
        {
            var totalHours = Math.Max((decimal)(checkOutDate - checkInDate).TotalHours, 0.01m);
            return priceUnit == "HOUR"
                ? Math.Ceiling(totalHours)
                : Math.Ceiling(totalHours / 24m);
        }

        private static string PriceUnitText(string priceUnit)
        {
            return priceUnit == "HOUR" ? "giờ" : "ngày";
        }

        private async Task<BookingCalendarAvailabilityResult> ValidateAvailability(
            string roomID,
            DateTime checkInDate,
            DateTime checkOutDate,
            string? excludeReservationFormID)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return new BookingCalendarAvailabilityResult { Available = false, Message = "Vui lòng chọn phòng." };
            }

            if (checkInDate < DateTime.UtcNow.AddHours(7))
            {
                return new BookingCalendarAvailabilityResult { Available = false, Message = "Ngày nhận phòng không được nằm trong quá khứ." };
            }

            if (checkOutDate <= checkInDate)
            {
                return new BookingCalendarAvailabilityResult { Available = false, Message = "Ngày trả phòng phải sau ngày nhận phòng." };
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomID == roomID && r.IsActivate == "ACTIVATE");
            if (room == null)
            {
                return new BookingCalendarAvailabilityResult { Available = false, Message = "Phòng không tồn tại hoặc đã ngừng hoạt động." };
            }

            var blockedStatuses = new[]
            {
                RoomOperationalStatuses.Maintenance,
                RoomOperationalStatuses.OutOfService,
                RoomOperationalStatuses.Unavailable
            };

            if (blockedStatuses.Contains(room.RoomStatus))
            {
                return new BookingCalendarAvailabilityResult
                {
                    Available = false,
                    Message = $"Phòng đang ở trạng thái {StatusDisplayHelper.RoomStatusText(room.RoomStatus)}, không thể đặt trong lúc này.",
                    RoomStatus = room.RoomStatus,
                    RoomStatusText = StatusDisplayHelper.RoomStatusText(room.RoomStatus)
                };
            }

            var openStatuses = RoomTaskStatuses.OpenStatuses;
            var hasOpenBlockingTask = await _context.RoomTasks.AnyAsync(t =>
                t.RoomID == roomID &&
                openStatuses.Contains(t.Status) &&
                t.TaskType == RoomTaskTypes.Maintenance);

            if (hasOpenBlockingTask)
            {
                return new BookingCalendarAvailabilityResult
                {
                    Available = false,
                    Message = "Phòng đang có yêu cầu bảo trì mở, không thể đặt phòng."
                };
            }

            var hasOverlap = await _context.ReservationForms.AnyAsync(r =>
                r.RoomID == roomID &&
                r.IsActivate == "ACTIVATE" &&
                (string.IsNullOrWhiteSpace(excludeReservationFormID) || r.ReservationFormID != excludeReservationFormID) &&
                r.CheckInDate < checkOutDate &&
                (r.HistoryCheckOut != null ? r.HistoryCheckOut.CheckOutDate : r.CheckOutDate) > checkInDate);

            if (hasOverlap)
            {
                return new BookingCalendarAvailabilityResult { Available = false, Message = "Phòng đã được đặt trong khoảng thời gian này." };
            }

            return new BookingCalendarAvailabilityResult
            {
                Available = true,
                Message = room.RoomStatus is RoomOperationalStatuses.Dirty or RoomOperationalStatuses.Cleaning
                    ? $"Phòng đang ở trạng thái {StatusDisplayHelper.RoomStatusText(room.RoomStatus)}. Có thể đặt nhưng cần kiểm tra dọn phòng trước khi khách nhận."
                    : "Phòng khả dụng trong khoảng thời gian đã chọn.",
                RoomStatus = room.RoomStatus,
                RoomStatusText = StatusDisplayHelper.RoomStatusText(room.RoomStatus)
            };
        }

        private BookingCalendarEventDto BuildCalendarEvent(ReservationForm reservation, DateTime now, bool canEdit)
        {
            var status = GetReservationStatus(reservation, now);
            var actualCheckOutDate = reservation.HistoryCheckOut?.CheckOutDate;
            var effectiveEnd = actualCheckOutDate ?? reservation.CheckOutDate;

            return new BookingCalendarEventDto
            {
                Id = reservation.ReservationFormID,
                ReservationFormID = reservation.ReservationFormID,
                RoomID = reservation.RoomID ?? string.Empty,
                RoomCategoryID = reservation.Room?.RoomCategoryID,
                RoomCategoryName = reservation.Room?.RoomCategory?.RoomCategoryName,
                CustomerID = reservation.CustomerID,
                CustomerName = reservation.Customer?.FullName,
                Start = reservation.CheckInDate,
                End = effectiveEnd,
                ExpectedCheckOutDate = reservation.CheckOutDate,
                ActualCheckOutDate = actualCheckOutDate,
                CheckoutTimingText = BuildCheckoutTimingText(actualCheckOutDate, reservation.CheckOutDate),
                Status = status,
                StatusText = ReservationStatusText(status),
                Color = ReservationStatusColor(status),
                Editable = canEdit && status == "BOOKED" && reservation.CheckInDate > now,
                HasCheckedIn = reservation.HistoryCheckin != null,
                HasCheckedOut = reservation.HistoryCheckOut != null,
                PriceUnit = reservation.PriceUnit,
                UnitPrice = reservation.UnitPrice,
                RoomBookingDeposit = reservation.RoomBookingDeposit,
                DetailsUrl = Url.Action("Details", "Reservation", new { id = reservation.ReservationFormID })
            };
        }

        private static string? BuildCheckoutTimingText(DateTime? actualCheckOutDate, DateTime expectedCheckOutDate)
        {
            if (!actualCheckOutDate.HasValue)
            {
                return null;
            }

            if (actualCheckOutDate.Value < expectedCheckOutDate)
            {
                return "Khách trả phòng sớm hơn dự kiến";
            }

            if (actualCheckOutDate.Value > expectedCheckOutDate)
            {
                return "Khách trả phòng trễ hơn dự kiến";
            }

            return "Khách trả phòng đúng dự kiến";
        }

        private static string GetReservationStatus(ReservationForm reservation, DateTime now)
        {
            if (reservation.HistoryCheckOut != null)
            {
                return "CHECKED_OUT";
            }

            if (reservation.HistoryCheckin != null)
            {
                return "CHECKED_IN";
            }

            return reservation.CheckInDate < now ? "OVERDUE_CHECKIN" : "BOOKED";
        }

        private static string ReservationStatusText(string status)
        {
            return status switch
            {
                "BOOKED" => "Đã đặt",
                "CHECKED_IN" => "Đã check-in",
                "CHECKED_OUT" => "Đã check-out",
                "OVERDUE_CHECKIN" => "Quá hạn nhận phòng",
                _ => "Không xác định"
            };
        }

        private static string ReservationStatusColor(string status)
        {
            return status switch
            {
                "BOOKED" => "#2563eb",
                "CHECKED_IN" => "#f59e0b",
                "CHECKED_OUT" => "#16a34a",
                "OVERDUE_CHECKIN" => "#dc2626",
                _ => "#6b7280"
            };
        }

        private async Task<List<SelectListItem>> BuildRoomCategoryOptions(string? selectedValue)
        {
            var categories = await _context.RoomCategories
                .Where(c => c.IsActivate == "ACTIVATE")
                .OrderBy(c => c.RoomCategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.RoomCategoryID,
                    Text = c.RoomCategoryName,
                    Selected = c.RoomCategoryID == selectedValue
                })
                .ToListAsync();

            categories.Insert(0, new SelectListItem { Value = string.Empty, Text = "Tất cả loại phòng", Selected = string.IsNullOrWhiteSpace(selectedValue) });
            return categories;
        }

        private static List<SelectListItem> BuildRoomStatusOptions(string? selectedValue)
        {
            var statuses = new[]
            {
                RoomOperationalStatuses.Available,
                RoomOperationalStatuses.Reserved,
                RoomOperationalStatuses.OnUse,
                RoomOperationalStatuses.Dirty,
                RoomOperationalStatuses.Cleaning,
                RoomOperationalStatuses.Maintenance,
                RoomOperationalStatuses.OutOfService,
                RoomOperationalStatuses.Unavailable,
                RoomOperationalStatuses.Overdue
            };

            var options = statuses
                .Select(status => new SelectListItem
                {
                    Value = status,
                    Text = StatusDisplayHelper.RoomStatusText(status),
                    Selected = status == selectedValue
                })
                .ToList();

            options.Insert(0, new SelectListItem { Value = string.Empty, Text = "Tất cả trạng thái phòng", Selected = string.IsNullOrWhiteSpace(selectedValue) });
            return options;
        }

        private static List<SelectListItem> BuildReservationStatusOptions(string? selectedValue)
        {
            var statuses = new[] { "BOOKED", "CHECKED_IN", "CHECKED_OUT", "OVERDUE_CHECKIN" };
            var options = statuses
                .Select(status => new SelectListItem
                {
                    Value = status,
                    Text = ReservationStatusText(status),
                    Selected = status == selectedValue
                })
                .ToList();

            options.Insert(0, new SelectListItem { Value = string.Empty, Text = "Tất cả trạng thái đặt phòng", Selected = string.IsNullOrWhiteSpace(selectedValue) });
            return options;
        }
    }
}
