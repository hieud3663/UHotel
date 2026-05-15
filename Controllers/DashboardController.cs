using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;

namespace HotelManagement.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly HotelManagementContext _context;

        public DashboardController(HotelManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserID") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Background Service đã tự động cập nhật trạng thái phòng mỗi 30 phút
            // Không cần gọi thủ công nữa

            // Thống kê tổng quan
            ViewBag.TotalRooms = await _context.Rooms.CountAsync(r => r.IsActivate == "ACTIVATE");
            ViewBag.AvailableRooms = await _context.Rooms.CountAsync(r => r.RoomStatus == "AVAILABLE" && r.IsActivate == "ACTIVATE");
            ViewBag.OccupiedRooms = await _context.Rooms.CountAsync(r => r.RoomStatus == "ON_USE" && r.IsActivate == "ACTIVATE");
            ViewBag.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActivate == "ACTIVATE");
            ViewBag.TotalEmployees = await _context.Employees.CountAsync(e => e.IsActivate == "ACTIVATE");
            
            // Đặt phòng hôm nay
            var today = DateTime.Today;
            ViewBag.TodayReservations = await _context.ReservationForms
                .Where(r => r.ReservationDate.Date == today && r.IsActivate == "ACTIVATE")
                .CountAsync();

            // Doanh thu tháng này
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.AddHours(7).Year, DateTime.UtcNow.AddHours(7).Month, 1);
            var totalRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate >= firstDayOfMonth)
                .SumAsync(i => i.NetDue ?? 0);
            ViewBag.MonthlyRevenue = totalRevenue;

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            return View();
        }
    }
}
