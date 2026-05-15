using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class InvoiceController : BaseController
    {
        private readonly HotelManagementContext _context;

        public InvoiceController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        public async Task<IActionResult> Index(string searchTerm, string paymentStatus, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var query = _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .AsQueryable();

            // Apply search filter if searchTerm is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(i =>
                    i.InvoiceID.ToLower().Contains(searchTerm) ||
                    (i.ReservationForm!.RoomID!.ToLower().Contains(searchTerm)) ||
                    (i.ReservationForm!.Customer!.FullName!.ToLower().Contains(searchTerm)) ||
                    (i.ReservationFormID!.ToLower().Contains(searchTerm))
                );
                
                ViewBag.SearchTerm = searchTerm;
            }

            // Apply payment status filter
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                if (paymentStatus.ToLower() == "paid")
                {
                    query = query.Where(i => i.IsPaid == true);
                }
                else if (paymentStatus.ToLower() == "unpaid")
                {
                    query = query.Where(i => i.IsPaid == false);
                }
                
                ViewBag.PaymentStatus = paymentStatus.ToLower();
            }

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(i => i.InvoiceDate >= startDate);
                ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                var endDateExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(i => i.InvoiceDate < endDateExclusive);
                ViewBag.ToDate = toDate.Value.Date.ToString("yyyy-MM-dd");
            }

            ViewBag.TotalInvoices = await query.CountAsync();
            ViewBag.PaidInvoices = await query.CountAsync(i => i.IsPaid);
            ViewBag.UnpaidInvoices = await query.CountAsync(i => !i.IsPaid);
            ViewBag.TotalRevenue = await query.SumAsync(i => (decimal?)i.NetDue) ?? 0;
            ViewBag.PaidRevenue = await query.Where(i => i.IsPaid).SumAsync(i => (decimal?)i.NetDue) ?? 0;
            ViewBag.UnpaidRevenue = await query.Where(i => !i.IsPaid).SumAsync(i => (decimal?)i.NetDue) ?? 0;

            // Apply ordering at the end
            query = query.OrderByDescending(i => i.InvoiceDate);

            var invoices = await PagedList<Invoice>.CreateAsync(query, page, pageSize);
            ViewBag.PageSize = pageSize;
            return View(invoices);
        }

        public async Task<IActionResult> Invoice(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckin)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckOut)
                .FirstOrDefaultAsync(m => m.InvoiceID == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // Alias for Invoice action to support /Invoice/Details/{id} route
        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckin)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckOut)
                .FirstOrDefaultAsync(m => m.InvoiceID == id);

            if (invoice == null) return NotFound();

            return View("Invoice", invoice);
        }
    }
}
