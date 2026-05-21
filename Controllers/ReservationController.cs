using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class ReservationController : BaseController
    {
        private readonly HotelManagementContext _context;

        public ReservationController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        public async Task<IActionResult> Index(string? phoneNumber = null, string? customerName = null, string? reservationId = null, int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var query = _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .Include(r => r.HistoryCheckin)
                .Where(r => r.IsActivate == "ACTIVATE" || r.ReservationStatus == "NO_SHOW");

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                query = query.Where(r => r.Customer!.PhoneNumber.Contains(phoneNumber));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                query = query.Where(r => r.Customer!.FullName.Contains(customerName));
            }

            if (!string.IsNullOrEmpty(reservationId))
            {
                query = query.Where(r => r.ReservationFormID.Contains(reservationId));
            }
            
            var reservations = await PagedList<ReservationForm>.CreateAsync(query.OrderByDescending(r => r.ReservationDate), page, pageSize);
            var now = DateTime.UtcNow.AddHours(7);
            
            // Đánh dấu phiếu đặt phòng quá hạn nhưng chưa được xử lý no-show
            ViewBag.OverdueReservations = reservations
                .Where(r => r.CheckOutDate < now && r.HistoryCheckin == null && r.ReservationStatus != "NO_SHOW")
                .Select(r => r.ReservationFormID)
                .ToHashSet();
            
            ViewBag.NoShowReservations = reservations
                .Where(r => r.ReservationStatus == "NO_SHOW")
                .Select(r => r.ReservationFormID)
                .ToHashSet();
            
            ViewBag.PageSize = pageSize;
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.CustomerName = customerName;
            ViewBag.ReservationId = reservationId;
            return View(reservations);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .Include(r => r.HistoryCheckin)
                .Include(r => r.HistoryCheckOut)
                .Include(r => r.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .FirstOrDefaultAsync(m => m.ReservationFormID == id);
            
            if (reservation == null) return NotFound();

            var now = DateTime.UtcNow.AddHours(7);
            var isPendingCheckIn = reservation.IsActivate == "ACTIVATE" && reservation.HistoryCheckin == null;
            var lateCheckInThreshold = TimeSpan.FromHours(2);

            ViewBag.IsLateCheckIn = isPendingCheckIn && reservation.CheckInDate < now && reservation.CheckOutDate >= now;
            ViewBag.IsSeriousLateCheckIn = isPendingCheckIn && reservation.CheckInDate.Add(lateCheckInThreshold) < now && reservation.CheckOutDate >= now;
            ViewBag.IsExpired = isPendingCheckIn && reservation.CheckOutDate < now;
            ViewBag.IsRoomOccupied = false;

            if (isPendingCheckIn && !string.IsNullOrEmpty(reservation.RoomID))
            {
                ViewBag.IsRoomOccupied = await _context.HistoryCheckins
                    .Include(h => h.ReservationForm)
                    .AnyAsync(h => h.ReservationFormID != id
                                   && h.ReservationForm != null
                                   && h.ReservationForm.RoomID == reservation.RoomID
                                   && !_context.HistoryCheckOuts.Any(co => co.ReservationFormID == h.ReservationFormID));
            }

            return View(reservation);
        }

        public async Task<IActionResult> Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.Where(c => c.IsActivate == "ACTIVATE").ToListAsync(), 
                "CustomerID", "FullName");
            
            ViewData["RoomCategories"] = new SelectList(
                await _context.RoomCategories
                    .Where(rc => rc.IsActivate == "ACTIVATE")
                    .ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName");
            
            ViewData["RoomID"] = new SelectList(
                await _context.Rooms
                    .Include(r => r.RoomCategory)
                    .Where(r => r.IsActivate == "ACTIVATE"
                                && r.RoomStatus != "UNAVAILABLE"
                                && r.RoomStatus != "OVERDUE"
                                && r.RoomStatus != "MAINTENANCE"
                                && r.RoomStatus != "OUT_OF_SERVICE")
                    .ToListAsync(), 
                "RoomID", "RoomID");
            
            // Load all rooms with category info and pricing for grid display
            ViewBag.AllRooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Where(r => r.IsActivate == "ACTIVATE")
                .Select(r => new
                {
                    roomID = r.RoomID,
                    roomStatus = r.RoomStatus,
                    roomCategoryID = r.RoomCategoryID,
                    roomCategoryName = r.RoomCategory!.RoomCategoryName,
                    hourPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "HOUR")!.Price,
                    dayPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "DAY")!.Price
                })
                .ToListAsync();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CheckInDate,CheckOutDate,RoomID,CustomerID,RoomBookingDeposit,PriceUnit,UnitPrice")] ReservationForm reservation)
        {

            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            reservation.EmployeeID = HttpContext.Session.GetString("EmployeeID")!;

            // Kiểm tra ngày nhận và ngày trả
            if (reservation.CheckInDate < DateTime.UtcNow.AddHours(7))
            {
                ModelState.AddModelError("CheckInDate", "Ngày nhận phòng phải lớn hơn hoặc bằng ngày hiện tại.");
            }
            if (reservation.CheckOutDate <= reservation.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            }

            if (ModelState.IsValid)
            {
                try
                {

                    // Gọi stored procedure để tạo reservation
                    var result = await _context.CreateReservationSP(
                        reservation.CheckInDate,
                        reservation.CheckOutDate,
                        reservation.RoomID,
                        reservation.CustomerID,
                        reservation.EmployeeID,
                        reservation.RoomBookingDeposit,
                        reservation.PriceUnit,
                        reservation.UnitPrice
                    );

                    if (result != null)
                    {
                        // Tạo phiếu xác nhận đặt phòng tự động
                        try
                        {
                            var receipt = await _context.CreateConfirmationReceiptSP(
                                "RESERVATION",
                                result.ReservationFormID,
                                null,
                                reservation.EmployeeID
                            );
                            
                            if (receipt != null)
                            {
                                TempData["Success"] = $"Đặt phòng thành công! Mã đặt phòng: {result.ReservationFormID}<br/>Phiếu xác nhận: {receipt.ReceiptID}";
                                TempData["ReceiptID"] = receipt.ReceiptID; // Để redirect sang view phiếu
                            }
                        }
                        catch (Exception exReceipt)
                        {
                            // Nếu tạo phiếu lỗi thì vẫn báo đặt phòng thành công
                            TempData["Success"] = $"Đặt phòng thành công! Mã đặt phòng: {result.ReservationFormID}";
                            TempData["Warning"] = $"Không thể tạo phiếu xác nhận: {exReceipt.Message}";
                        }
                        
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Không thể tạo phiếu đặt phòng. Vui lòng thử lại.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
                }
            }

            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.Where(c => c.IsActivate == "ACTIVATE").ToListAsync(), 
                "CustomerID", "FullName");
            
            ViewData["RoomCategories"] = new SelectList(
                await _context.RoomCategories
                    .Where(rc => rc.IsActivate == "ACTIVATE")
                    .ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName");
            
            // Load all rooms with category info and pricing for grid display
            ViewBag.AllRooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Where(r => r.IsActivate == "ACTIVATE")
                .Select(r => new
                {
                    roomID = r.RoomID,
                    roomStatus = r.RoomStatus,
                    roomCategoryID = r.RoomCategoryID,
                    roomCategoryName = r.RoomCategory!.RoomCategoryName,
                    hourPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "HOUR")!.Price,
                    dayPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "DAY")!.Price
                })
                .ToListAsync();

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var reservation = await _context.ReservationForms
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationFormID == id);
            
            if (reservation != null)
            {
                // Kiểm tra đã check-in chưa
                var checkedIn = await _context.HistoryCheckins
                    .AnyAsync(h => h.ReservationFormID == id);
                
                if (checkedIn)
                {
                    TempData["Error"] = "Không thể hủy phiếu đặt phòng đã check-in!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Sử dụng ExecuteSqlRaw để tránh conflict với trigger
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE ReservationForm SET IsActivate = 'DEACTIVATE' WHERE ReservationFormID = {0}", 
                    id);

                // Cập nhật trạng thái phòng về AVAILABLE
                if (reservation.Room != null)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE Room SET RoomStatus = 'AVAILABLE' WHERE RoomID = {0}", 
                        reservation.Room.RoomID);
                }

                TempData["Success"] = "Hủy đặt phòng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> CalculateDeposit(string roomId, DateTime checkInDate, DateTime checkOutDate, string priceUnit = "DAY")
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return Json(new { success = false, message = "Vui lòng chọn phòng." });
            }

            if (checkOutDate <= checkInDate)
            {
                return Json(new { success = false, message = "Ngày trả phòng phải sau ngày nhận phòng." });
            }

            priceUnit = string.IsNullOrWhiteSpace(priceUnit) ? "DAY" : priceUnit.ToUpperInvariant();
            if (priceUnit != "DAY" && priceUnit != "HOUR")
            {
                return Json(new { success = false, message = "Hình thức thuê không hợp lệ." });
            }

            var room = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .FirstOrDefaultAsync(r => r.RoomID == roomId && r.IsActivate == "ACTIVATE");

            if (room == null)
            {
                return Json(new { success = false, message = "Phòng không tồn tại hoặc đã ngừng hoạt động." });
            }

            var unitPrice = room.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == priceUnit)?.Price ?? 0;
            if (unitPrice <= 0)
            {
                return Json(new { success = false, message = "Loại phòng chưa có bảng giá hợp lệ." });
            }

            var totalHours = Math.Max((decimal)(checkOutDate - checkInDate).TotalHours, 0.01m);
            var billingUnits = priceUnit == "HOUR"
                ? Math.Ceiling(totalHours)
                : Math.Ceiling(totalHours / 24m);
            var estimatedRoomCharge = unitPrice * billingUnits;
            var deposit = Math.Round(estimatedRoomCharge * 0.3m, 0, MidpointRounding.AwayFromZero); // 30% tiền phòng dự kiến

            return Json(new
            {
                success = true,
                deposit,
                billingUnits,
                unitPrice,
                priceUnit,
                estimatedRoomCharge
            });
        }

        // Xóa phiếu đặt phòng (soft delete)
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var reservation = await _context.ReservationForms
                    .Include(r => r.HistoryCheckin)
                    .FirstOrDefaultAsync(r => r.ReservationFormID == id);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu đặt phòng!";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem đã check-in chưa
                if (reservation.HistoryCheckin != null)
                {
                    TempData["ErrorMessage"] = "Không thể xóa phiếu đặt phòng đã check-in!";
                    return RedirectToAction("Index");
                }

                // Soft delete
                reservation.IsActivate = "DEACTIVATE";
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa phiếu đặt phòng {id} thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa phiếu đặt phòng: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
