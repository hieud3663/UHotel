using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class RoomController : BaseController
    {
        private readonly HotelManagementContext _context;

        public RoomController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var query = _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Where(r => r.IsActivate == "ACTIVATE")
                .OrderBy(r => r.RoomID);

            var rooms = await PagedList<Room>.CreateAsync(query, page, pageSize);

            // Lấy thông tin khách hàng đang ở phòng cho từng phòng trong trang hiện tại
            var roomsWithCustomer = new List<object>();
            foreach (var room in rooms)
            {
                string? customerName = null;

                // Nếu phòng đang sử dụng, lấy thông tin khách hàng từ HistoryCheckin
                if (room.RoomStatus == "ON_USE")
                {
                    var checkin = await _context.HistoryCheckins
                        .Include(h => h.ReservationForm)
                        .ThenInclude(rf => rf!.Customer)
                        .Where(h => h.ReservationForm!.RoomID == room.RoomID && h.ReservationForm.IsActivate == "ACTIVATE")
                        .OrderByDescending(h => h.CheckInDate)
                        .FirstOrDefaultAsync();

                    customerName = checkin?.ReservationForm?.Customer?.FullName;
                }
                // Nếu phòng đã đặt, lấy thông tin khách hàng từ ReservationForm
                else if (room.RoomStatus == "RESERVED")
                {
                    var reservation = await _context.ReservationForms
                        .Include(rf => rf.Customer)
                        .Where(rf => rf.RoomID == room.RoomID && rf.IsActivate == "ACTIVATE")
                        .OrderByDescending(rf => rf.ReservationDate)
                        .FirstOrDefaultAsync();

                    customerName = reservation?.Customer?.FullName;
                }

                roomsWithCustomer.Add(new
                {
                    Room = room,
                    CustomerName = customerName,
                    HourPrice = room.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price,
                    DayPrice = room.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price
                });
            }

            ViewBag.RoomsWithCustomer = roomsWithCustomer;
            ViewBag.PageSize = pageSize;
            return View(rooms);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .FirstOrDefaultAsync(m => m.RoomID == id);
            if (room == null) return NotFound();

            return View(room);
        }

        public async Task<IActionResult> Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                "RoomCategoryID", "RoomCategoryName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomID, RoomCategoryID")] Room room)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {

                //Kiểm tra mã phòng đã tồn tại
                var existingRoom = await _context.Rooms.FindAsync(room.RoomID);
                if (existingRoom != null)
                {
                    TempData["Error"] = "Mã phòng đã tồn tại! Vui lòng nhập mã khác.";
                    ViewData["RoomCategoryID"] = new SelectList(
                        await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                        "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
                    return View(room);
                }


                // Lấy thông tin loại phòng để tạo mã phòng
                var roomCategory = await _context.RoomCategories.FindAsync(room.RoomCategoryID);
                if (roomCategory == null)
                {
                    TempData["Error"] = "Loại phòng không tồn tại!";
                    ViewData["RoomCategoryID"] = new SelectList(
                        await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                        "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
                    return View(room);
                }

                room.IsActivate = "ACTIVATE";
                room.DateOfCreation = DateTime.UtcNow.AddHours(7).AddMinutes(-1);
                // Console.WriteLine(room.DateOfCreation);
                // Console.ReadLine();
                room.RoomStatus = "AVAILABLE";

                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Thêm phòng thành công! Mã phòng: {room.RoomID}";
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
            return View(room);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RoomID,RoomStatus,DateOfCreation,RoomCategoryID,IsActivate")] Room room)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id != room.RoomID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật phòng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
            return View(room);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomCategory)
                .FirstOrDefaultAsync(m => m.RoomID == id);
            if (room == null) return NotFound();

            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                room.IsActivate = "DEACTIVATE";
                _context.Update(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa phòng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(string id)
        {
            return _context.Rooms.Any(e => e.RoomID == id);
        }

        // API để lấy phòng trống theo loại phòng và thời gian
        [HttpGet]
        public async Task<JsonResult> GetAvailableRoomsByCategory(string categoryId, DateTime checkInDate, DateTime checkOutDate)
        {
            var availableRooms = await _context.Rooms
                .Where(r => (string.IsNullOrWhiteSpace(categoryId) || r.RoomCategoryID == categoryId) && r.RoomStatus == "AVAILABLE" && r.IsActivate == "ACTIVATE")
                .Where(r => !_context.ReservationForms.Any(rf =>
                    rf.RoomID == r.RoomID &&
                    rf.IsActivate == "ACTIVATE" &&
                    rf.CheckInDate < checkOutDate &&
                    rf.CheckOutDate > checkInDate)
                )
                .Select(r => new
                {
                    roomID = r.RoomID
                })
                .ToListAsync();

            return Json(availableRooms);
        }

        // API để lấy các tùy chọn giá theo loại phòng
        [HttpGet]
        public async Task<JsonResult> GetPricingOptions(string categoryId)
        {
            var pricings = await _context.Pricings
                .Where(p => p.RoomCategoryID == categoryId)
                .Select(p => new
                {
                    priceUnit = p.PriceUnit,
                    amount = p.Price
                })
                .ToListAsync();

            return Json(pricings);
        }

        // API để lấy phòng trống
        [HttpGet]
        public async Task<JsonResult> GetAvailableRooms(DateTime checkInDate, DateTime checkOutDate)
        {
            var availableRooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Where(r => r.RoomStatus == "AVAILABLE" && r.IsActivate == "ACTIVATE")
                .Where(r => !_context.ReservationForms.Any(rf =>
                    rf.RoomID == r.RoomID &&
                    rf.IsActivate == "ACTIVATE" &&
                    rf.CheckInDate < checkOutDate &&
                    rf.CheckOutDate > checkInDate
                ))
                .Select(r => new
                {
                    roomID = r.RoomID,
                    categoryName = r.RoomCategory!.RoomCategoryName,
                    numberOfBed = r.RoomCategory.NumberOfBed,
                    dayPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "DAY")!.Price,
                    hourPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "HOUR")!.Price
                })
                .ToListAsync();

            return Json(availableRooms);
        }

        // API mới: Lấy tất cả phòng với thông tin reservation sắp đến
        [HttpGet]
        public async Task<JsonResult> GetRoomsWithReservations(string? categoryId = null, DateTime checkInDate = default, DateTime checkOutDate = default)
        {
            List<object> roomsWithInfo = new();
            var blockedStatuses = new[] { "UNAVAILABLE", "OVERDUE", "MAINTENANCE", "OUT_OF_SERVICE" };

            if (!string.IsNullOrEmpty(categoryId))
            {
                // Lấy phòng AVAILABLE, không bị trùng với khoảng thời gian
                var availableRooms = await _context.Rooms
                    .Include(r => r.RoomCategory)
                    .ThenInclude(rc => rc!.Pricings)
                    .Where(r => r.RoomCategoryID == categoryId
                                && r.IsActivate == "ACTIVATE"
                                && !blockedStatuses.Contains(r.RoomStatus))
                    .Where(r => !_context.ReservationForms.Any(rf =>
                        rf.RoomID == r.RoomID &&
                        rf.IsActivate == "ACTIVATE" &&
                        rf.CheckInDate < checkOutDate &&
                        ((_context.HistoryCheckOuts
                            .Where(ho => ho.ReservationFormID == rf.ReservationFormID)
                            .Max(ho => (DateTime?)ho.CheckOutDate) ?? rf.CheckOutDate) > checkInDate)
                    ))
                    .ToListAsync();

                foreach (var room in availableRooms)
                {
                    roomsWithInfo.Add(new
                    {
                        roomID = room.RoomID,
                        roomStatus = room.RoomStatus,
                        isBookable = true,
                        roomCategoryID = room.RoomCategoryID,
                        roomCategoryName = room.RoomCategory!.RoomCategoryName,
                        hourPrice = room.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price,
                        dayPrice = room.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price,
                        upcomingReservation = new {}
                    });
                }
            }
            else
            {
                // Lấy tất cả phòng, kèm reservation sắp đến (nếu có)
                var allRooms = await _context.Rooms
                    .Include(r => r.RoomCategory)
                    .ThenInclude(rc => rc!.Pricings)
                    .Where(r => r.IsActivate == "ACTIVATE")
                    .ToListAsync();

                foreach (var room in allRooms)
                {
                    var hasOverlappingReservation = await _context.ReservationForms.AnyAsync(rf =>
                        rf.RoomID == room.RoomID &&
                        rf.IsActivate == "ACTIVATE" &&
                        rf.CheckInDate < checkOutDate &&
                        ((_context.HistoryCheckOuts
                            .Where(ho => ho.ReservationFormID == rf.ReservationFormID)
                            .Max(ho => (DateTime?)ho.CheckOutDate) ?? rf.CheckOutDate) > checkInDate));

                    var isBookable = !blockedStatuses.Contains(room.RoomStatus) && !hasOverlappingReservation;

                    var upcomingReservation = await _context.ReservationForms
                        .Include(rf => rf.Customer)
                        .Where(rf => rf.RoomID == room.RoomID && rf.IsActivate == "ACTIVATE")
                        .Where(rf => !_context.HistoryCheckins.Any(hc => hc.ReservationFormID == rf.ReservationFormID))
                        .Where(rf => rf.CheckInDate > DateTime.UtcNow.AddHours(7))
                        .OrderBy(rf => rf.CheckInDate)
                        .FirstOrDefaultAsync();

                    double? hoursUntilCheckIn = upcomingReservation != null
                        ? (upcomingReservation.CheckInDate - DateTime.UtcNow.AddHours(7)).TotalHours
                        : null;

                    roomsWithInfo.Add(new
                    {
                        roomID = room.RoomID,
                        roomStatus = room.RoomStatus,
                        isBookable,
                        roomCategoryID = room.RoomCategoryID,
                        roomCategoryName = room.RoomCategory!.RoomCategoryName,
                        hourPrice = room.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price,
                        dayPrice = room.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price,
                        upcomingReservation = upcomingReservation != null ? new
                        {
                            reservationFormID = upcomingReservation.ReservationFormID,
                            checkInDate = upcomingReservation.CheckInDate,
                            customerName = upcomingReservation.Customer?.FullName,
                            hoursUntilCheckIn = hoursUntilCheckIn,
                            isNearCheckIn = hoursUntilCheckIn <= 5 // Còn <= 5 giờ
                        } : null
                    });
                }
            }

            return Json(roomsWithInfo);
        }
    }
    
}
