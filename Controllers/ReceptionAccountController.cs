using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class ReceptionAccountController : BaseController
    {
        private readonly HotelManagementContext _context;

        public ReceptionAccountController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        private bool IsManagerOrAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "MANAGER" || role == "ADMIN";
        }

        // GET: /ReceptionAccount
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();

            var query = _context.Users
                .Include(u => u.Employee)
                .Where(u => u.Role != "ADMIN") // Hiển thị cả MANAGER và EMPLOYEE
                .OrderBy(u => u.Username);

            var users = await PagedList<User>.CreateAsync(query, page, pageSize);
            ViewBag.PageSize = pageSize;
            return View(users);
        }

        // GET: /ReceptionAccount/Create
        public async Task<IActionResult> Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();

            // Danh sách nhân viên đang hoạt động để liên kết tài khoản
            ViewBag.ActiveEmployees = await _context.Employees
                .Where(e => e.IsActivate == "ACTIVATE")
                .OrderBy(e => e.FullName)
                .Select(e => new { e.EmployeeID, e.FullName, e.Position })
                .ToListAsync();

            // Danh sách quyền có thể chọn (loại trừ ADMIN)
            ViewBag.AvailableRoles = new List<object>
            {
                new { Value = "EMPLOYEE", Text = "Nhân viên (EMPLOYEE)" },
                new { Value = "MANAGER", Text = "Quản lý (MANAGER)" }
            };

            return View();
        }

        // POST: /ReceptionAccount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string employeeId, string username, string password, string role)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(username) || 
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction(nameof(Create));
            }

            // Validate role
            if (role != "EMPLOYEE" && role != "MANAGER")
            {
                TempData["Error"] = "Quyền tài khoản không hợp lệ.";
                return RedirectToAction(nameof(Create));
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == username);
            if (exists)
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại.";
                return RedirectToAction(nameof(Create));
            }

            var employeeExists = await _context.Employees.AnyAsync(e => e.EmployeeID == employeeId && e.IsActivate == "ACTIVATE");
            if (!employeeExists)
            {
                TempData["Error"] = "Nhân viên không hợp lệ hoặc đã ngưng hoạt động.";
                return RedirectToAction(nameof(Create));
            }

            var user = new User
            {
                UserID = await _context.GenerateID("USR-", "User"),
                EmployeeID = employeeId,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActivate = "ACTIVATE"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tạo tài khoản {role.ToLower()} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /ReceptionAccount/Toggle/{id} (Activate/Deactivate)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == id && u.Role != "ADMIN");
            if (user == null) return NotFound();

            var currentUserID = HttpContext.Session.GetString("UserID");
            if (user.UserID == currentUserID)
            {
                TempData["Error"] = "Không thể khóa hoặc mở khóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActivate = user.IsActivate == "ACTIVATE" ? "DEACTIVATE" : "ACTIVATE";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActivate == "ACTIVATE" ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /ReceptionAccount/ResetPassword/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();
            if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Mật khẩu mới không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == id && u.Role != "ADMIN");
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt lại mật khẩu thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}

