using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;
using BCrypt.Net;

namespace HotelManagement.Controllers
{
    public class AuthController : BaseController
    {
        private readonly HotelManagementContext _context;

        public AuthController(HotelManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserID") != null)
            {
                var currentRole = HttpContext.Session.GetString("Role");
                if (currentRole == EmployeePositions.Cleaner || currentRole == EmployeePositions.Technician)
                {
                    return RedirectToAction("Index", "RoomMaintenanceCleaning");
                }

                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu";
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActivate == "ACTIVATE");

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                var effectiveRole = ResolveEffectiveRole(user);

                HttpContext.Session.SetString("UserID", user.UserID);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", effectiveRole);
                HttpContext.Session.SetString("EmployeeID", user.EmployeeID);
                if (!string.IsNullOrWhiteSpace(user.Employee?.Position))
                {
                    HttpContext.Session.SetString("Position", user.Employee.Position);
                }

                if (effectiveRole == EmployeePositions.Cleaner || effectiveRole == EmployeePositions.Technician)
                {
                    return RedirectToAction("Index", "RoomMaintenanceCleaning");
                }

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View();
        }

        private static string ResolveEffectiveRole(User user)
        {
            if (user.Role == "EMPLOYEE" &&
                (user.Employee?.Position == EmployeePositions.Cleaner ||
                 user.Employee?.Position == EmployeePositions.Technician))
            {
                return user.Employee.Position;
            }

            return user.Role;
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
