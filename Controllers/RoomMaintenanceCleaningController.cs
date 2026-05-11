using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    public class RoomMaintenanceCleaningController : BaseController
    {
        private readonly HotelManagementContext _context;
        private readonly ILogger<RoomMaintenanceCleaningController> _logger;

        public RoomMaintenanceCleaningController(HotelManagementContext context, ILogger<RoomMaintenanceCleaningController> logger)
        {
            _context = context;
            _logger = logger;
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

        private string? CurrentEmployeeID => HttpContext.Session.GetString("EmployeeID");

        public async Task<IActionResult> Index(
            string? searchRoom = null,
            string? taskType = null,
            string? status = null,
            string? priority = null,
            string? assignedEmployeeID = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var query = _context.RoomTasks
                .Include(t => t.Room)
                    .ThenInclude(r => r!.RoomCategory)
                .Include(t => t.AssignedEmployee)
                .Include(t => t.CreatedByEmployee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchRoom))
            {
                query = query.Where(t => t.RoomID.Contains(searchRoom));
            }

            if (!string.IsNullOrWhiteSpace(taskType))
            {
                query = query.Where(t => t.TaskType == taskType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(assignedEmployeeID))
            {
                query = query.Where(t => t.AssignedEmployeeID == assignedEmployeeID);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt.Date <= toDate.Value.Date);
            }

            query = query
                .OrderBy(t => t.Status == RoomTaskStatuses.Completed || t.Status == RoomTaskStatuses.Cancelled)
                .ThenByDescending(t => t.Priority == RoomTaskPriorities.Urgent)
                .ThenByDescending(t => t.Priority == RoomTaskPriorities.High)
                .ThenByDescending(t => t.CreatedAt);

            await LoadFilterData(assignedEmployeeID);
            ViewBag.SearchRoom = searchRoom;
            ViewBag.TaskType = taskType;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.AssignedEmployeeID = assignedEmployeeID;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PageSize = pageSize;

            return View(await PagedList<RoomTask>.CreateAsync(query, page, pageSize));
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var roomTask = await _context.RoomTasks
                .Include(t => t.Room)
                    .ThenInclude(r => r!.RoomCategory)
                .Include(t => t.CreatedByEmployee)
                .Include(t => t.AssignedEmployee)
                .Include(t => t.Histories!)
                    .ThenInclude(h => h.ChangedByEmployee)
                .FirstOrDefaultAsync(t => t.RoomTaskID == id);

            if (roomTask == null)
            {
                return NotFound();
            }

            return View(roomTask);
        }

        public async Task<IActionResult> CreateCleaningTask(string? roomID = null)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            await LoadFormData(roomID);

            return View(new RoomTask
            {
                RoomID = roomID ?? string.Empty,
                TaskType = RoomTaskTypes.Cleaning,
                Priority = RoomTaskPriorities.Normal,
                Title = "Dọn phòng",
                CreatedAt = DateTime.UtcNow.AddHours(7)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCleaningTask(RoomTask roomTask)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                _logger.LogInformation(
                    "Bắt đầu tạo yêu cầu dọn phòng. RoomID={RoomID}, Title={Title}, Priority={Priority}, CurrentEmployeeID={EmployeeID}",
                    roomTask.RoomID,
                    roomTask.Title,
                    roomTask.Priority,
                    CurrentEmployeeID);

                roomTask.TaskType = RoomTaskTypes.Cleaning;
                roomTask.Status = RoomTaskStatuses.Pending;
                roomTask.CreatedByEmployeeID = CurrentEmployeeID ?? string.Empty;
                roomTask.CreatedAt = DateTime.UtcNow.AddHours(7);
                ClearServerManagedRoomTaskModelState();

                if (!await ValidateRoomTask(roomTask))
                {
                    LogModelStateErrors(nameof(CreateCleaningTask), roomTask);
                    await LoadFormData(roomTask.RoomID);
                    return View(roomTask);
                }

                await CreateRoomTask(roomTask, RoomOperationalStatuses.Dirty, "Tạo yêu cầu dọn phòng");
                _logger.LogInformation("Tạo yêu cầu dọn phòng thành công. RoomTaskID={RoomTaskID}, RoomID={RoomID}", roomTask.RoomTaskID, roomTask.RoomID);
                TempData["Success"] = $"Tạo yêu cầu dọn phòng {roomTask.RoomTaskID} thành công.";
                return RedirectToAction(nameof(Details), new { id = roomTask.RoomTaskID });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi tạo yêu cầu dọn phòng. RoomID={RoomID}, Title={Title}, Priority={Priority}, CurrentEmployeeID={EmployeeID}",
                    roomTask.RoomID,
                    roomTask.Title,
                    roomTask.Priority,
                    CurrentEmployeeID);

                ModelState.AddModelError(string.Empty, $"Không thể tạo yêu cầu dọn phòng: {ex.Message}");
                TempData["Error"] = $"Không thể tạo yêu cầu dọn phòng: {ex.Message}";
                await LoadFormData(roomTask.RoomID);
                return View(roomTask);
            }
        }

        public async Task<IActionResult> CreateMaintenanceTask(string? roomID = null)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            await LoadFormData(roomID);

            return View(new RoomTask
            {
                RoomID = roomID ?? string.Empty,
                TaskType = RoomTaskTypes.Maintenance,
                Priority = RoomTaskPriorities.High,
                Title = "Bảo trì phòng",
                CreatedAt = DateTime.UtcNow.AddHours(7)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaintenanceTask(RoomTask roomTask)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                _logger.LogInformation(
                    "Bắt đầu tạo yêu cầu bảo trì. RoomID={RoomID}, Title={Title}, Priority={Priority}, CurrentEmployeeID={EmployeeID}",
                    roomTask.RoomID,
                    roomTask.Title,
                    roomTask.Priority,
                    CurrentEmployeeID);

                roomTask.TaskType = RoomTaskTypes.Maintenance;
                roomTask.Status = RoomTaskStatuses.Pending;
                roomTask.CreatedByEmployeeID = CurrentEmployeeID ?? string.Empty;
                roomTask.CreatedAt = DateTime.UtcNow.AddHours(7);
                ClearServerManagedRoomTaskModelState();

                if (!await ValidateRoomTask(roomTask))
                {
                    LogModelStateErrors(nameof(CreateMaintenanceTask), roomTask);
                    await LoadFormData(roomTask.RoomID);
                    return View(roomTask);
                }

                await CreateRoomTask(roomTask, RoomOperationalStatuses.Maintenance, "Tạo yêu cầu bảo trì");
                _logger.LogInformation("Tạo yêu cầu bảo trì thành công. RoomTaskID={RoomTaskID}, RoomID={RoomID}", roomTask.RoomTaskID, roomTask.RoomID);
                TempData["Success"] = $"Tạo yêu cầu bảo trì {roomTask.RoomTaskID} thành công.";
                return RedirectToAction(nameof(Details), new { id = roomTask.RoomTaskID });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi tạo yêu cầu bảo trì. RoomID={RoomID}, Title={Title}, Priority={Priority}, CurrentEmployeeID={EmployeeID}",
                    roomTask.RoomID,
                    roomTask.Title,
                    roomTask.Priority,
                    CurrentEmployeeID);

                ModelState.AddModelError(string.Empty, $"Không thể tạo yêu cầu bảo trì: {ex.Message}");
                TempData["Error"] = $"Không thể tạo yêu cầu bảo trì: {ex.Message}";
                await LoadFormData(roomTask.RoomID);
                return View(roomTask);
            }
        }

        public async Task<IActionResult> Assign(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();

            var roomTask = await _context.RoomTasks
                .Include(t => t.Room)
                .Include(t => t.AssignedEmployee)
                .FirstOrDefaultAsync(t => t.RoomTaskID == id);

            if (roomTask == null)
            {
                return NotFound();
            }

            if (IsClosed(roomTask))
            {
                TempData["Warning"] = "Công việc đã kết thúc, không thể phân công.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await LoadEmployeeData(roomTask.AssignedEmployeeID);
            return View(roomTask);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(string id, string assignedEmployeeID, string? note)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();

            var roomTask = await _context.RoomTasks.FirstOrDefaultAsync(t => t.RoomTaskID == id);
            if (roomTask == null)
            {
                return NotFound();
            }

            if (IsClosed(roomTask))
            {
                TempData["Warning"] = "Công việc đã kết thúc, không thể phân công.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == assignedEmployeeID && e.IsActivate == "ACTIVATE");

            if (employee == null)
            {
                TempData["Error"] = "Nhân viên được phân công không hợp lệ.";
                return RedirectToAction(nameof(Assign), new { id });
            }

            var oldStatus = roomTask.Status;
            roomTask.AssignedEmployeeID = assignedEmployeeID;
            roomTask.Status = RoomTaskStatuses.Assigned;
            roomTask.Note = note;

            await AddHistory(roomTask.RoomTaskID, oldStatus, roomTask.Status, note ?? $"Phân công cho {employee.FullName}");
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã phân công công việc {roomTask.RoomTaskID} cho {employee.FullName}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var roomTask = await _context.RoomTasks
                .Include(t => t.Room)
                .FirstOrDefaultAsync(t => t.RoomTaskID == id);

            if (roomTask == null)
            {
                return NotFound();
            }

            if (IsClosed(roomTask))
            {
                TempData["Warning"] = "Công việc đã kết thúc, không thể bắt đầu.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (roomTask.Status != RoomTaskStatuses.Assigned || string.IsNullOrWhiteSpace(roomTask.AssignedEmployeeID))
            {
                TempData["Warning"] = "Cần phân công nhân viên trước khi bắt đầu xử lý công việc.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!CanOperateTask(roomTask))
            {
                TempData["Error"] = "Bạn không có quyền bắt đầu công việc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var oldStatus = roomTask.Status;
            roomTask.Status = RoomTaskStatuses.InProgress;
            roomTask.StartedAt ??= DateTime.UtcNow.AddHours(7);
            UpdateRoomStatusForTask(roomTask, isStarting: true);

            await AddHistory(roomTask.RoomTaskID, oldStatus, roomTask.Status, "Bắt đầu xử lý công việc");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã chuyển công việc sang trạng thái đang xử lý.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(string id, string? note, bool createCleaningAfterMaintenance = false)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var roomTask = await _context.RoomTasks
                .Include(t => t.Room)
                .FirstOrDefaultAsync(t => t.RoomTaskID == id);

            if (roomTask == null)
            {
                return NotFound();
            }

            if (IsClosed(roomTask))
            {
                TempData["Warning"] = "Công việc đã kết thúc.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (roomTask.Status != RoomTaskStatuses.InProgress)
            {
                TempData["Warning"] = "Cần bắt đầu xử lý công việc trước khi hoàn thành.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!CanOperateTask(roomTask))
            {
                TempData["Error"] = "Bạn không có quyền hoàn thành công việc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (roomTask.TaskType == RoomTaskTypes.Maintenance && string.IsNullOrWhiteSpace(note))
            {
                TempData["Error"] = "Cần nhập ghi chú kết quả khi hoàn thành công việc bảo trì.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var oldStatus = roomTask.Status;
            roomTask.Status = RoomTaskStatuses.Completed;
            roomTask.CompletedAt = DateTime.UtcNow.AddHours(7);
            roomTask.Note = note;

            var shouldCreateCleaningTask = createCleaningAfterMaintenance && roomTask.TaskType == RoomTaskTypes.Maintenance;

            if (roomTask.Room != null)
            {
                var hasOtherOpenTasks = await HasOpenRoomTasks(roomTask.RoomID, roomTask.RoomTaskID);
                roomTask.Room.RoomStatus = shouldCreateCleaningTask
                    ? RoomOperationalStatuses.Dirty
                    : hasOtherOpenTasks
                        ? roomTask.Room.RoomStatus
                        : RoomOperationalStatuses.Available;
            }

            await AddHistory(roomTask.RoomTaskID, oldStatus, roomTask.Status, note ?? "Hoàn thành công việc");

            if (shouldCreateCleaningTask)
            {
                var hasOpenCleaningTask = await _context.RoomTasks.AnyAsync(t =>
                    t.RoomID == roomTask.RoomID &&
                    t.RoomTaskID != roomTask.RoomTaskID &&
                    t.TaskType == RoomTaskTypes.Cleaning &&
                    RoomTaskStatuses.OpenStatuses.Contains(t.Status));

                if (!hasOpenCleaningTask)
                {
                    var cleaningTask = new RoomTask
                    {
                        RoomTaskID = await GenerateRoomTaskID(),
                        RoomID = roomTask.RoomID,
                        TaskType = RoomTaskTypes.Cleaning,
                        Title = "Dọn phòng sau bảo trì",
                        Description = $"Tự động tạo sau khi hoàn thành bảo trì {roomTask.RoomTaskID}.",
                        Priority = RoomTaskPriorities.Normal,
                        Status = RoomTaskStatuses.Pending,
                        CreatedByEmployeeID = CurrentEmployeeID ?? string.Empty,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    };

                    _context.RoomTasks.Add(cleaningTask);
                    await AddHistory(cleaningTask.RoomTaskID, null, RoomTaskStatuses.Pending, "Tạo yêu cầu dọn phòng sau bảo trì");
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hoàn thành công việc.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id, string cancelReason)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (!IsManagerOrAdmin()) return Forbid();

            var roomTask = await _context.RoomTasks
                .Include(t => t.Room)
                .FirstOrDefaultAsync(t => t.RoomTaskID == id);

            if (roomTask == null)
            {
                return NotFound();
            }

            if (roomTask.Status == RoomTaskStatuses.Completed)
            {
                TempData["Warning"] = "Công việc đã hoàn thành, không thể hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["Error"] = "Vui lòng nhập lý do hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var oldStatus = roomTask.Status;
            roomTask.Status = RoomTaskStatuses.Cancelled;
            roomTask.CancelReason = cancelReason;
            roomTask.CancelledAt = DateTime.UtcNow.AddHours(7);

            if (roomTask.Room != null && !await HasOpenRoomTasks(roomTask.RoomID, roomTask.RoomTaskID))
            {
                roomTask.Room.RoomStatus = RoomOperationalStatuses.Available;
            }

            await AddHistory(roomTask.RoomTaskID, oldStatus, roomTask.Status, cancelReason);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy công việc.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> History(string? roomID = null, string? employeeID = null, int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var query = _context.RoomTaskHistories
                .Include(h => h.RoomTask)
                    .ThenInclude(t => t!.Room)
                .Include(h => h.ChangedByEmployee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(roomID))
            {
                query = query.Where(h => h.RoomTask != null && h.RoomTask.RoomID == roomID);
            }

            if (!string.IsNullOrWhiteSpace(employeeID))
            {
                query = query.Where(h => h.ChangedByEmployeeID == employeeID);
            }

            query = query.OrderByDescending(h => h.ChangedAt);

            await LoadFilterData();
            ViewBag.RoomID = roomID;
            ViewBag.EmployeeID = employeeID;
            ViewBag.PageSize = pageSize;

            return View(await PagedList<RoomTaskHistory>.CreateAsync(query, page, pageSize));
        }

        private async Task<bool> ValidateRoomTask(RoomTask roomTask)
        {
            _logger.LogDebug(
                "Validate RoomTask. RoomID={RoomID}, TaskType={TaskType}, Priority={Priority}, CreatedByEmployeeID={CreatedByEmployeeID}",
                roomTask.RoomID,
                roomTask.TaskType,
                roomTask.Priority,
                roomTask.CreatedByEmployeeID);

            if (string.IsNullOrWhiteSpace(roomTask.CreatedByEmployeeID))
            {
                ModelState.AddModelError(string.Empty, "Không xác định được nhân viên tạo công việc.");
            }

            if (!RoomTaskTypes.All.Contains(roomTask.TaskType))
            {
                ModelState.AddModelError(nameof(RoomTask.TaskType), "Loại công việc không hợp lệ.");
            }

            if (!RoomTaskPriorities.All.Contains(roomTask.Priority))
            {
                ModelState.AddModelError(nameof(RoomTask.Priority), "Mức độ ưu tiên không hợp lệ.");
            }

            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomID == roomTask.RoomID && r.IsActivate == "ACTIVATE");
            if (!roomExists)
            {
                ModelState.AddModelError(nameof(RoomTask.RoomID), "Phòng không hợp lệ hoặc đã ngừng hoạt động.");
            }

            var hasOpenSameType = await _context.RoomTasks.AnyAsync(t =>
                t.RoomID == roomTask.RoomID &&
                t.TaskType == roomTask.TaskType &&
                RoomTaskStatuses.OpenStatuses.Contains(t.Status));

            if (hasOpenSameType)
            {
                ModelState.AddModelError(string.Empty, "Phòng đã có công việc cùng loại đang mở.");
            }

            _logger.LogDebug(
                "Kết quả validate RoomTask. IsValid={IsValid}, RoomID={RoomID}, TaskType={TaskType}",
                ModelState.IsValid,
                roomTask.RoomID,
                roomTask.TaskType);

            return ModelState.IsValid;
        }

        private async Task CreateRoomTask(RoomTask roomTask, string roomStatus, string historyNote)
        {
            roomTask.RoomTaskID = await GenerateRoomTaskID();

            _logger.LogDebug(
                "Chuẩn bị lưu RoomTask. RoomTaskID={RoomTaskID}, RoomID={RoomID}, TaskType={TaskType}, RoomStatus={RoomStatus}",
                roomTask.RoomTaskID,
                roomTask.RoomID,
                roomTask.TaskType,
                roomStatus);

            var room = await _context.Rooms.FirstAsync(r => r.RoomID == roomTask.RoomID);
            room.RoomStatus = roomStatus;

            _context.RoomTasks.Add(roomTask);
            await AddHistory(roomTask.RoomTaskID, null, roomTask.Status, historyNote);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Đã lưu RoomTask và cập nhật trạng thái phòng. RoomTaskID={RoomTaskID}, RoomID={RoomID}", roomTask.RoomTaskID, roomTask.RoomID);
        }

        private async Task AddHistory(string roomTaskID, string? oldStatus, string newStatus, string? note)
        {
            var history = new RoomTaskHistory
            {
                RoomTaskHistoryID = await GenerateRoomTaskHistoryID(),
                RoomTaskID = roomTaskID,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByEmployeeID = CurrentEmployeeID ?? string.Empty,
                ChangedAt = DateTime.UtcNow.AddHours(7),
                Note = note
            };

            _logger.LogDebug(
                "Thêm lịch sử RoomTask. RoomTaskHistoryID={RoomTaskHistoryID}, RoomTaskID={RoomTaskID}, OldStatus={OldStatus}, NewStatus={NewStatus}, ChangedByEmployeeID={EmployeeID}",
                history.RoomTaskHistoryID,
                history.RoomTaskID,
                history.OldStatus,
                history.NewStatus,
                history.ChangedByEmployeeID);

            _context.RoomTaskHistories.Add(history);
        }

        private void ClearServerManagedRoomTaskModelState()
        {
            ModelState.Remove(nameof(RoomTask.RoomTaskID));
            ModelState.Remove(nameof(RoomTask.TaskType));
            ModelState.Remove(nameof(RoomTask.Status));
            ModelState.Remove(nameof(RoomTask.CreatedByEmployeeID));
            ModelState.Remove(nameof(RoomTask.CreatedAt));
            ModelState.Remove(nameof(RoomTask.StartedAt));
            ModelState.Remove(nameof(RoomTask.CompletedAt));
            ModelState.Remove(nameof(RoomTask.CancelledAt));
            ModelState.Remove(nameof(RoomTask.CreatedByEmployee));
            ModelState.Remove(nameof(RoomTask.AssignedEmployee));
            ModelState.Remove(nameof(RoomTask.Room));
            ModelState.Remove(nameof(RoomTask.Histories));
        }

        private void LogModelStateErrors(string actionName, RoomTask roomTask)
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => new
                {
                    Field = string.IsNullOrWhiteSpace(entry.Key) ? "Model" : entry.Key,
                    Messages = entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray()
                })
                .ToArray();

            foreach (var error in errors)
            {
                _logger.LogWarning(
                    "Validation lỗi khi {ActionName}. Field={Field}, Messages={Messages}, RoomID={RoomID}, TaskType={TaskType}, CurrentEmployeeID={EmployeeID}",
                    actionName,
                    error.Field,
                    string.Join(" | ", error.Messages),
                    roomTask.RoomID,
                    roomTask.TaskType,
                    CurrentEmployeeID);
            }
        }

        private async Task<string> GenerateRoomTaskID()
        {
            var maxNumber = await _context.RoomTasks
                .Where(t => t.RoomTaskID.StartsWith("RT-"))
                .Select(t => t.RoomTaskID.Substring(3))
                .ToListAsync();

            var nextNumber = maxNumber
                .Select(value => int.TryParse(value, out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"RT-{nextNumber:D6}";
        }

        private async Task<string> GenerateRoomTaskHistoryID()
        {
            var persistedIds = await _context.RoomTaskHistories
                .Where(h => h.RoomTaskHistoryID.StartsWith("RTH-"))
                .Select(h => h.RoomTaskHistoryID.Substring(4))
                .ToListAsync();

            var trackedIds = _context.ChangeTracker
                .Entries<RoomTaskHistory>()
                .Where(entry => entry.Entity.RoomTaskHistoryID.StartsWith("RTH-"))
                .Select(entry => entry.Entity.RoomTaskHistoryID.Substring(4))
                .ToList();

            var nextNumber = persistedIds
                .Concat(trackedIds)
                .Select(value => int.TryParse(value, out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"RTH-{nextNumber:D6}";
        }

        private void UpdateRoomStatusForTask(RoomTask roomTask, bool isStarting)
        {
            if (roomTask.Room == null || !isStarting)
            {
                return;
            }

            roomTask.Room.RoomStatus = roomTask.TaskType == RoomTaskTypes.Cleaning
                ? RoomOperationalStatuses.Cleaning
                : RoomOperationalStatuses.Maintenance;
        }

        private async Task<bool> HasOpenRoomTasks(string roomID, string? exceptTaskID = null)
        {
            return await _context.RoomTasks.AnyAsync(t =>
                t.RoomID == roomID &&
                t.RoomTaskID != exceptTaskID &&
                RoomTaskStatuses.OpenStatuses.Contains(t.Status));
        }

        private bool IsClosed(RoomTask roomTask)
        {
            return roomTask.Status == RoomTaskStatuses.Completed || roomTask.Status == RoomTaskStatuses.Cancelled;
        }

        private bool CanOperateTask(RoomTask roomTask)
        {
            if (IsManagerOrAdmin())
            {
                return true;
            }

            var employeeID = CurrentEmployeeID;
            return !string.IsNullOrWhiteSpace(employeeID) && roomTask.AssignedEmployeeID == employeeID;
        }

        private async Task LoadFormData(string? selectedRoomID = null)
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .Where(r => r.IsActivate == "ACTIVATE")
                .OrderBy(r => r.RoomID)
                .ToListAsync();

            ViewBag.Rooms = new SelectList(
                rooms.Select(r => new
                {
                    r.RoomID,
                    DisplayName = $"{r.RoomID} - {r.RoomCategory?.RoomCategoryName ?? "Chưa có loại"} ({GetRoomStatusText(r.RoomStatus)})"
                }),
                "RoomID",
                "DisplayName",
                selectedRoomID);

            ViewBag.Priorities = BuildPrioritySelectList();
        }

        private async Task LoadEmployeeData(string? selectedEmployeeID = null)
        {
            var employees = await _context.Employees
                .Where(e => e.IsActivate == "ACTIVATE")
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.Employees = new SelectList(
                employees.Select(e => new
                {
                    e.EmployeeID,
                    DisplayName = $"{e.EmployeeID} - {e.FullName} ({e.Position})"
                }),
                "EmployeeID",
                "DisplayName",
                selectedEmployeeID);
        }

        private async Task LoadFilterData(string? selectedEmployeeID = null)
        {
            var employees = await _context.Employees
                .Where(e => e.IsActivate == "ACTIVATE")
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.TaskTypes = new SelectList(new[]
            {
                new { Value = RoomTaskTypes.Cleaning, Text = "Dọn phòng" },
                new { Value = RoomTaskTypes.Maintenance, Text = "Bảo trì" }
            }, "Value", "Text");

            ViewBag.Statuses = new SelectList(new[]
            {
                new { Value = RoomTaskStatuses.Pending, Text = "Chờ xử lý" },
                new { Value = RoomTaskStatuses.Assigned, Text = "Đã phân công" },
                new { Value = RoomTaskStatuses.InProgress, Text = "Đang thực hiện" },
                new { Value = RoomTaskStatuses.Completed, Text = "Hoàn thành" },
                new { Value = RoomTaskStatuses.Cancelled, Text = "Đã hủy" }
            }, "Value", "Text");

            ViewBag.Priorities = BuildPrioritySelectList();

            ViewBag.Employees = new SelectList(
                employees.Select(e => new
                {
                    e.EmployeeID,
                    DisplayName = $"{e.EmployeeID} - {e.FullName}"
                }),
                "EmployeeID",
                "DisplayName",
                selectedEmployeeID);
        }

        private static SelectList BuildPrioritySelectList()
        {
            return new SelectList(new[]
            {
                new { Value = RoomTaskPriorities.Low, Text = "Thấp" },
                new { Value = RoomTaskPriorities.Normal, Text = "Bình thường" },
                new { Value = RoomTaskPriorities.High, Text = "Cao" },
                new { Value = RoomTaskPriorities.Urgent, Text = "Khẩn cấp" }
            }, "Value", "Text");
        }

        private static string GetRoomStatusText(string status)
        {
            return status switch
            {
                RoomOperationalStatuses.Available => "Trống",
                RoomOperationalStatuses.OnUse => "Đang sử dụng",
                RoomOperationalStatuses.Reserved => "Đã đặt",
                RoomOperationalStatuses.Unavailable => "Không khả dụng",
                RoomOperationalStatuses.Overdue => "Quá hạn",
                RoomOperationalStatuses.Dirty => "Cần dọn",
                RoomOperationalStatuses.Cleaning => "Đang dọn",
                RoomOperationalStatuses.Maintenance => "Bảo trì",
                RoomOperationalStatuses.OutOfService => "Ngừng sử dụng",
                _ => status
            };
        }
    }
}
