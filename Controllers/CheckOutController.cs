using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class CheckOutController : BaseController
    {
        private readonly HotelManagementContext _context;

        public CheckOutController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        private static decimal CalculateGrossTotal(Invoice invoice)
        {
            return invoice.NetDue ?? invoice.RoomCharge + invoice.ServicesCharge + invoice.TaxAmount;
        }

        private static decimal CalculateAmountToCollect(Invoice invoice)
        {
            var paidAmount = Math.Max(invoice.AmountPaid, 0);
            var balance = CalculateGrossTotal(invoice) - invoice.Deposit - paidAmount;
            return Math.Round(Math.Max(balance, 0), 0);
        }

        private static decimal CalculateRefundAmount(Invoice invoice)
        {
            var paidAmount = Math.Max(invoice.AmountPaid, 0);
            var balance = CalculateGrossTotal(invoice) - invoice.Deposit - paidAmount;
            return Math.Round(Math.Max(-balance, 0), 0);
        }

        // REMOVED: CalculateHourlyFee() - Không còn cần tính phí theo bậc thang

        public async Task<IActionResult> Index(string? phoneNumber = null, string? customerName = null, string? reservationId = null, int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            // Lấy danh sách phòng đã check-in nhưng chưa check-out
            var query = _context.HistoryCheckins
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Invoices)
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckOut)
                .Where(h => !_context.HistoryCheckOuts.Any(co => co.ReservationFormID == h.ReservationFormID));

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                query = query.Where(h => h.ReservationForm!.Customer!.PhoneNumber.Contains(phoneNumber));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                query = query.Where(h => h.ReservationForm!.Customer!.FullName.Contains(customerName));
            }

            if (!string.IsNullOrEmpty(reservationId))
            {
                query = query.Where(h => h.ReservationFormID.Contains(reservationId));
            }

            query = query.OrderByDescending(h => h.CheckInDate);

            var checkedInReservations = await PagedList<HistoryCheckin>.CreateAsync(query, page, pageSize);

            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.CustomerName = customerName;
            ViewBag.ReservationId = reservationId;
            ViewBag.PageSize = pageSize;

            return View(checkedInReservations);
        }

        public async Task<IActionResult> Details(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Include(r => r.HistoryCheckin)
                .Include(r => r.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);

            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ReservationFormID == reservationFormID);

            if (reservation == null)
            {
                return NotFound();
            }


            var actualCheckInDate = reservation.HistoryCheckin?.CheckInDate ?? reservation.CheckInDate;
            var actualCheckOutDate = DateTime.UtcNow.AddHours(7);

            var unitPrice = reservation.UnitPrice;
            var priceUnit = reservation.PriceUnit;

            var actualMinutes = (actualCheckOutDate - actualCheckInDate).TotalMinutes;

            decimal timeUnits;
            double actualDuration; 
            
            if (priceUnit == "DAY")
            {
                timeUnits = (decimal)Math.Ceiling(actualMinutes / 1440.0);
                actualDuration = Math.Ceiling(actualMinutes / 1440.0); 
            }
            else // HOUR
            {
                timeUnits = (decimal)Math.Ceiling(actualMinutes / 60.0);
                actualDuration = Math.Ceiling(actualMinutes / 60.0); 
            }

            if (timeUnits < 1) timeUnits = 1;
            if (actualDuration < 1) actualDuration = 1; 

            decimal roomCharge = unitPrice * timeUnits;

            // ============================================================================
            // TÍNH TIỀN CHO THANH TOÁN TRƯỚC 
            // ============================================================================
            var expectedCheckOutDate = reservation.CheckOutDate;
            var expectedMinutes = (expectedCheckOutDate - actualCheckInDate).TotalMinutes;

            decimal expectedTimeUnits;
            if (priceUnit == "DAY")
            {
                expectedTimeUnits = (decimal)Math.Ceiling(expectedMinutes / 1440.0);
            }
            else // HOUR
            {
                expectedTimeUnits = (decimal)Math.Ceiling(expectedMinutes / 60.0);
            }

            if (expectedTimeUnits < 1) expectedTimeUnits = 1;

            decimal expectedRoomCharge = unitPrice * expectedTimeUnits;

            var servicesCharge = reservation.RoomUsageServices?.Sum(s => s.Quantity * s.UnitPrice) ?? 0;

            var expectedSubTotal = expectedRoomCharge + servicesCharge;
            var expectedTaxAmount = expectedSubTotal * 0.1m;
            var expectedTotalAmount = expectedSubTotal + expectedTaxAmount;
            var expectedAmountDue = expectedTotalAmount - (decimal)reservation.RoomBookingDeposit;

            ViewBag.UnitPrice = unitPrice;
            ViewBag.PriceUnit = priceUnit;
            ViewBag.TimeUnits = timeUnits;
            ViewBag.ActualCheckInDate = actualCheckInDate;
            ViewBag.ActualCheckOutDate = actualCheckOutDate;
            ViewBag.ActualDuration = actualDuration;

            var subTotal = roomCharge + servicesCharge;
            var taxAmount = subTotal * 0.1m; // VAT 10%
            var totalAmount = subTotal + taxAmount;
            var balanceAfterDeposit = totalAmount - (decimal)reservation.RoomBookingDeposit;
            var amountDue = Math.Max(balanceAfterDeposit, 0);
            var refundAmount = Math.Max(-balanceAfterDeposit, 0);

            // ViewBag cho CHECKOUT_THEN_PAY (tính đến hiện tại)
            ViewBag.RoomCharge = Math.Round(roomCharge, 0);
            ViewBag.ServiceCharge = Math.Round(servicesCharge, 0);
            ViewBag.SubTotal = Math.Round(subTotal, 0);
            ViewBag.TaxAmount = Math.Round(taxAmount, 0);
            ViewBag.TotalAmount = Math.Round(totalAmount, 0);
            ViewBag.Deposit = Math.Round((decimal)reservation.RoomBookingDeposit, 0);
            ViewBag.AmountDue = Math.Round(amountDue, 0);
            ViewBag.RefundAmount = Math.Round(refundAmount, 0);

            // ViewBag cho PAY_THEN_CHECKOUT (tính theo dự kiến)
            ViewBag.ExpectedRoomCharge = Math.Round(expectedRoomCharge, 0);
            ViewBag.ExpectedTimeUnits = expectedTimeUnits;
            ViewBag.ExpectedSubTotal = Math.Round(expectedSubTotal, 0);
            ViewBag.ExpectedTaxAmount = Math.Round(expectedTaxAmount, 0);
            ViewBag.ExpectedTotalAmount = Math.Round(expectedTotalAmount, 0);
            ViewBag.ExpectedAmountDue = Math.Round(expectedAmountDue, 0);

            ViewBag.ExistingInvoice = existingInvoice;
            
            // Kiểm tra có thể chuyển đổi sang giá ngày không
            ViewBag.CanConvertToDaily = CanConvertToDaily(reservation, actualCheckInDate);
            ViewBag.HoursStayed = (actualCheckOutDate - actualCheckInDate).TotalHours;
            
            return View(reservation);
        }

        // Kiểm tra xem có thể chuyển đổi từ giá giờ sang giá ngày không
        private bool CanConvertToDaily(ReservationForm reservation, DateTime actualCheckInDate)
        {
            // Chỉ cho phép chuyển đổi nếu:
            // 1. Đang thuê theo giờ
            // 2. Đã ở quá 6 giờ
            // 3. Chưa có invoice
            if (reservation.PriceUnit != "HOUR") return false;
            
            var hoursStayed = (DateTime.UtcNow.AddHours(7) - actualCheckInDate).TotalHours;
            if (hoursStayed <= 6) return false;
            
            var hasInvoice = _context.Invoices.Any(i => i.ReservationFormID == reservation.ReservationFormID);
            if (hasInvoice) return false;
            
            return true;
        }

        // Chuyển đổi từ giá giờ sang giá ngày
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToDaily(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");

                // Gọi stored procedure để chuyển đổi
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_ConvertHourlyToDaily";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@reservationFormID", reservationFormID));
                        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@employeeID", employeeID ?? ""));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                
                                if (status == "SUCCESS")
                                {
                                    var oldPriceUnit = reader["OldPriceUnit"].ToString();
                                    var newPriceUnit = reader["NewPriceUnit"].ToString();
                                    var newUnitPrice = reader["NewUnitPrice"];
                                    var hoursStayed = reader["HoursStayed"];
                                    
                                    TempData["Success"] = $"Chuyển đổi thành công từ giá {oldPriceUnit} sang giá {newPriceUnit}! " +
                                                         $"Đã ở {hoursStayed} giờ. Đơn giá mới: {newUnitPrice:N0} VNĐ/ngày";
                                }
                                else
                                {
                                    TempData["Error"] = "Không thể chuyển đổi. " + status;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction("Details", new { reservationFormID });
        }

        // LUỒNG 1: TRẢ PHÒNG VÀ THANH TOÁN (Checkout Then Pay)
        // Bước 1: Trả phòng và tạo hóa đơn chưa thanh toán (tính theo thời gian THỰC TẾ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutThenPay(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");

                // var existingInvoice = await _context.Invoices
                //     .FirstOrDefaultAsync(i => i.ReservationFormID == reservationFormID && !i.IsPaid);

                var result = await _context.CreateInvoice_CheckoutThenPay(reservationFormID, employeeID!);


                if (result != null && result.Status == "CHECKOUT_THEN_PAY")
                {
                    TempData["Success"] = "Trả phòng thành công! Vui lòng thanh toán hóa đơn.";
                    return RedirectToAction("Details", "Invoice", new { id = result.InvoiceID });
                }
                else
                {
                    TempData["Error"] = result?.Status ?? "Không thể trả phòng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Bước 2: Hiển thị trang thanh toán cho invoice chưa thanh toán
        public async Task<IActionResult> Payment(string invoiceID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var invoice = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceID);

            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.IsPaid)
            {
                TempData["Success"] = $"Hóa đơn {invoiceID} đã được thanh toán thành công qua {invoice.PaymentMethod} lúc {invoice.PaymentDate:dd/MM/yyyy HH:mm}!";
                return RedirectToAction("Details", "Invoice", new { id = invoiceID });
            }

            return View(invoice);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentStatus(string invoiceID)
        {
            if (!CheckAuth())
            {
                return Unauthorized(new { isAuthenticated = false });
            }

            if (string.IsNullOrWhiteSpace(invoiceID))
            {
                return BadRequest(new { exists = false, message = "Thiếu mã hóa đơn." });
            }

            var invoice = await _context.Invoices
                .AsNoTracking()
                .Where(i => i.InvoiceID == invoiceID)
                .Select(i => new
                {
                    i.InvoiceID,
                    i.IsPaid,
                    i.PaymentMethod,
                    i.PaymentDate
                })
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                return NotFound(new { exists = false, message = "Không tìm thấy hóa đơn." });
            }

            return Json(new
            {
                exists = true,
                isPaid = invoice.IsPaid,
                paymentMethod = invoice.PaymentMethod,
                paymentDate = invoice.PaymentDate.HasValue
                    ? invoice.PaymentDate.Value.ToString("dd/MM/yyyy HH:mm")
                    : null,
                redirectUrl = invoice.IsPaid
                    ? Url.Action("Details", "Invoice", new { id = invoice.InvoiceID })
                    : null
            });
        }

        // Bước 3: Xác nhận thanh toán cho invoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(string invoiceID, string paymentMethod)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                var invoiceBeforePayment = await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.InvoiceID == invoiceID);
                var amountToCollectBeforePayment = invoiceBeforePayment == null
                    ? 0
                    : CalculateAmountToCollect(invoiceBeforePayment);
                var refundAmountBeforePayment = invoiceBeforePayment == null
                    ? 0
                    : CalculateRefundAmount(invoiceBeforePayment);

                // Gọi SP để xác nhận thanh toán
                // SP sẽ cập nhật isPaid = 1, paymentDate, paymentMethod
                var result = await _context.ConfirmPaymentSP(invoiceID, paymentMethod, employeeID!);

                if (result != null && result.Status == "PAYMENT_CONFIRMED")
                {
                    var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceID == invoiceID);
                    if (invoice != null)
                    {
                        if (invoice.AmountPaid != amountToCollectBeforePayment)
                        {
                            invoice.AmountPaid = amountToCollectBeforePayment;
                            await _context.SaveChangesAsync();
                        }
                    }

                    await CreateCleaningTaskAfterCheckout(invoiceID: invoiceID, employeeID: employeeID);
                    TempData["Success"] = refundAmountBeforePayment > 0
                        ? $"Đã hoàn tất hóa đơn. Không thu thêm tiền; cần hoàn lại cho khách {refundAmountBeforePayment:N0} VNĐ. Phòng đã được chuyển sang trạng thái cần dọn."
                        : amountToCollectBeforePayment == 0
                            ? "Đã hoàn tất hóa đơn 0đ. Phòng đã được chuyển sang trạng thái cần dọn."
                            : "Thanh toán thành công! Phòng đã được chuyển sang trạng thái cần dọn.";
                    return RedirectToAction("Details", "Invoice", new { id = invoiceID });
                }
                else
                {
                    TempData["Error"] = result?.Status ?? "Không thể xác nhận thanh toán. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction("Payment", new { invoiceID });
        }

        public record WebHookRequest(decimal transferAmount, string code);

        // Xác nhận thanh toán cho invoice -- webhook
        [HttpPost("/api/webhook/confirm-payment")]  // ← Route tuyệt đối
        public async Task<IActionResult> WebHookConfirmPayment([FromBody] WebHookRequest request)
        {
            var api_key = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            api_key = api_key?.Replace("Apikey ", ""); // Loại bỏ "Apikey " nếu có
            var expected_api_key = Environment.GetEnvironmentVariable("WEBHOOK_API_KEY") ?? "123456789"; // Lấy từ biến môi trường hoặc fallback giá trị mặc định

            if (api_key != expected_api_key)
            {
                return Unauthorized(new
                {
                    status = "failed",
                    message = "Invalid API Key",
                    success = false
                });
            }

            try
            {
                // Tìm mã invoice dạng INV001, INV-001, RF001, RF-001, etc.
                var match = System.Text.RegularExpressions.Regex.Match(
                    request.code,
                    @"(INV)[-]?(\d{6})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (!match.Success)
                {
                    return BadRequest(new
                    {
                        status = "failed",
                        message = "Invoice ID not found in transfer content",
                        success = false,
                        hint = "Content should contain invoice ID (e.g., 'INV-000001' or 'INV000001')"
                    });
                }

                // Tạo mã chuẩn: INV-000001
                var prefix = match.Groups[1].Value.ToUpper();
                var number = match.Groups[2].Value;
                var invoiceID = $"{prefix}-{number}";

                var paymentMethod = "TRANSFER"; // Mặc định là chuyển khoản
                // string employeeID = extractCode[1].Trim();
                decimal amount = request.transferAmount;

                // Lấy thông tin hóa đơn
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.InvoiceID == invoiceID);

                if (invoice == null)
                {
                    return BadRequest(new
                    {
                        status = "failed",
                        message = "Invoice not found",
                        success = false
                    });
                }

                var amountToCollect = CalculateAmountToCollect(invoice);

                // Kiểm tra số tiền thanh toán
                if (amountToCollect <= 0 || amount <= 0 || amount != amountToCollect)
                {
                    return BadRequest(new
                    {
                        status = "failed",
                        message = "Invalid payment amount",
                        success = false
                    });
                }

                // Gọi SP để xác nhận thanh toán
                var result = await _context.ConfirmPaymentSP(invoiceID, paymentMethod, employeeID: "");

                if (result != null && result.Status == "PAYMENT_CONFIRMED")
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE Invoice SET amountPaid = {0} WHERE invoiceID = {1} AND amountPaid <> {0}",
                        amountToCollect,
                        invoiceID);

                    await CreateCleaningTaskAfterCheckout(invoiceID: invoiceID, employeeID: null);

                    return Ok(new
                    {
                        status = "success",
                        message = "Payment confirmed successfully",
                        success = true,
                        invoiceID = invoiceID
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        status = "failed",
                        message = result?.Status ?? "Cannot confirm payment",
                        success = false
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message,
                    success = false
                });
            }
        }


        // LUỒNG 2: THANH TOÁN TRƯỚC (Pay Then Checkout)
        // Bước 1: Thanh toán ngay (tính theo thời gian DỰ KIẾN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayThenCheckout(string reservationFormID, string paymentMethod)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");

                // Gọi SP để tạo invoice ĐÃ THANH TOÁN
                // SP tính tiền dựa trên thời gian DỰ KIẾN (expected checkout)
                // Phòng vẫn giữ trạng thái ON_USE
                var result = await _context.CreateInvoice_PayThenCheckout(reservationFormID, employeeID!, paymentMethod);

                if (result != null && result.Status == "SUCCESS")
                {
                    TempData["Success"] = $"Thanh toán trước thành công! Khách có thể ở đến {result.PaymentDate:dd/MM/yyyy HH:mm}.";
                    return RedirectToAction("Details", "Invoice", new { id = result.InvoiceID });
                }
                else
                {
                    TempData["Error"] = result?.Status ?? "Không thể thanh toán trước. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Bước 2: Trả phòng thực tế sau khi đã thanh toán trước
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualCheckout(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");

                // Gọi SP để checkout thực tế
                // SP sẽ tính phí phụ thu nếu checkout muộn hơn dự kiến
                var result = await _context.ActualCheckout_AfterPrepayment(reservationFormID, employeeID!);

                if (result != null)
                {
                    if (result.AdditionalCharge > 0)
                    {
                        TempData["Warning"] = $"Trả phòng muộn! Phí phụ thu: {result.AdditionalCharge:N0} VNĐ. Vui lòng thanh toán bổ sung.";
                        return RedirectToAction("Payment", new { invoiceID = result.InvoiceID });
                    }
                    else
                    {
                        await CreateCleaningTaskAfterCheckout(reservationFormID: reservationFormID, employeeID: employeeID);
                        TempData["Success"] = "Trả phòng đúng giờ! Phòng đã được chuyển sang trạng thái cần dọn.";
                        return RedirectToAction("Details", "Invoice", new { id = result.InvoiceID });
                    }
                }
                else
                {
                    TempData["Error"] = "Không thể trả phòng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // LEGACY: Giữ lại action cũ để tương thích ngược (nếu cần)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ProcessCheckOut")]
        public async Task<IActionResult> CheckOut(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");

                // Gọi stored procedure để check-out
                // SP sẽ tự động:
                // - Kiểm tra điều kiện (đã check-in, chưa check-out)
                // - Tạo ID cho check-out và invoice
                // - Tính toán phí phòng + dịch vụ + phí trả muộn
                // - Cập nhật trạng thái phòng về AVAILABLE
                // - Tạo hoặc cập nhật invoice
                var result = await _context.CheckOutRoomSP(reservationFormID, employeeID!);

                if (result != null)
                {
                    await CreateCleaningTaskAfterCheckout(reservationFormID: reservationFormID, employeeID: employeeID);
                    TempData["Success"] = $"Check-out thành công! Phòng đã được chuyển sang trạng thái cần dọn. {result.CheckoutStatus}";

                    // Lấy invoice vừa tạo để redirect
                    var invoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.ReservationFormID == reservationFormID);

                    if (invoice != null)
                    {
                        return RedirectToAction("Invoice", "Invoice", new { id = invoice.InvoiceID });
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Không thể check-out. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                // Lỗi từ stored procedure
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CreateCleaningTaskAfterCheckout(string? reservationFormID = null, string? invoiceID = null, string? employeeID = null)
        {
            ReservationForm? reservation = null;

            if (!string.IsNullOrWhiteSpace(reservationFormID))
            {
                reservation = await _context.ReservationForms
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);
            }
            else if (!string.IsNullOrWhiteSpace(invoiceID))
            {
                reservation = await _context.Invoices
                    .Where(i => i.InvoiceID == invoiceID)
                    .Include(i => i.ReservationForm)
                        .ThenInclude(r => r!.Room)
                    .Select(i => i.ReservationForm)
                    .FirstOrDefaultAsync();
            }

            if (reservation?.Room == null)
            {
                return;
            }

            var creatorEmployeeID = employeeID;

            if (string.IsNullOrWhiteSpace(creatorEmployeeID))
            {
                creatorEmployeeID = reservation.EmployeeID;
            }

            if (string.IsNullOrWhiteSpace(creatorEmployeeID))
            {
                creatorEmployeeID = await _context.HistoryCheckOuts
                    .Where(h => h.ReservationFormID == reservation.ReservationFormID && h.EmployeeID != null && h.EmployeeID != "")
                    .OrderByDescending(h => h.CheckOutDate)
                    .Select(h => h.EmployeeID!)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(creatorEmployeeID))
            {
                creatorEmployeeID = await _context.Employees
                    .Where(e => e.IsActivate == "ACTIVATE")
                    .OrderBy(e => e.EmployeeID)
                    .Select(e => e.EmployeeID)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(creatorEmployeeID))
            {
                return;
            }

            var hasOpenCleaningTask = await _context.RoomTasks.AnyAsync(t =>
                t.RoomID == reservation.Room.RoomID &&
                t.TaskType == RoomTaskTypes.Cleaning &&
                RoomTaskStatuses.OpenStatuses.Contains(t.Status));

            if (hasOpenCleaningTask)
            {
                reservation.Room.RoomStatus = RoomOperationalStatuses.Dirty;
                await _context.SaveChangesAsync();
                return;
            }

            var roomTask = new RoomTask
            {
                RoomTaskID = await GenerateRoomTaskID(),
                RoomID = reservation.Room.RoomID,
                TaskType = RoomTaskTypes.Cleaning,
                Title = "Dọn phòng sau trả phòng",
                Description = $"Tự động tạo sau khi khách trả phòng từ phiếu {reservation.ReservationFormID}.",
                Priority = RoomTaskPriorities.Normal,
                Status = RoomTaskStatuses.Pending,
                CreatedByEmployeeID = creatorEmployeeID,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            reservation.Room.RoomStatus = RoomOperationalStatuses.Dirty;
            _context.RoomTasks.Add(roomTask);
            _context.RoomTaskHistories.Add(new RoomTaskHistory
            {
                RoomTaskHistoryID = await GenerateRoomTaskHistoryID(),
                RoomTaskID = roomTask.RoomTaskID,
                OldStatus = null,
                NewStatus = RoomTaskStatuses.Pending,
                ChangedByEmployeeID = creatorEmployeeID,
                ChangedAt = DateTime.UtcNow.AddHours(7),
                Note = "Tự động tạo yêu cầu dọn phòng sau trả phòng"
            });

            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateRoomTaskID()
        {
            var ids = await _context.RoomTasks
                .Where(t => t.RoomTaskID.StartsWith("RT-"))
                .Select(t => t.RoomTaskID.Substring(3))
                .ToListAsync();

            var nextNumber = ids
                .Select(value => int.TryParse(value, out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"RT-{nextNumber:D6}";
        }

        private async Task<string> GenerateRoomTaskHistoryID()
        {
            var ids = await _context.RoomTaskHistories
                .Where(h => h.RoomTaskHistoryID.StartsWith("RTH-"))
                .Select(h => h.RoomTaskHistoryID.Substring(4))
                .ToListAsync();

            var nextNumber = ids
                .Select(value => int.TryParse(value, out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"RTH-{nextNumber:D6}";
        }
    }
}
