using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    /// <summary>
    /// Model to receive sp_CreateReservation result
    /// </summary>
    public class ReservationResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string RoomID { get; set; } = string.Empty;
        public string RoomCategoryName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public double RoomBookingDeposit { get; set; }
        public int DaysBooked { get; set; }
    }

    /// <summary>
    /// Model to receive sp_QuickCheckin result
    /// </summary>
    public class CheckInResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public string HistoryCheckInID { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public string RoomID { get; set; } = string.Empty;
        public string CheckinStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_MarkReservationNoShow result
    /// </summary>
    public class NoShowResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public string RoomID { get; set; } = string.Empty;
        public string ReservationStatus { get; set; } = string.Empty;
        public string RoomStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_QuickCheckout result
    /// </summary>
    public class CheckOutResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public string HistoryCheckOutID { get; set; } = string.Empty;
        public DateTime CheckOutDate { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal ServicesCharge { get; set; }
        public decimal TotalDue { get; set; }
        public decimal NetDue { get; set; }
        public string CheckoutStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_AddRoomService result
    /// </summary>
    public class RoomServiceResult
    {
        public string RoomUsageServiceId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime DateAdded { get; set; }
        public string HotelServiceId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ReservationFormID { get; set; } = string.Empty;
        public string EmployeeID { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string? Action { get; set; }  // "INSERTED" hoặc "UPDATED"
        public int? QuantityAdded { get; set; }
        public int? PreviousQuantity { get; set; }
    }

    /// <summary>
    /// Model to receive sp_DeleteRoomService result
    /// </summary>
    public class DeletedRoomServiceResult
    {
        public string RoomUsageServiceId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string ReservationFormID { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
    }

    /// <summary>
    /// Model to receive sp_CreateInvoice_CheckoutThenPay result
    /// </summary>
    public class CheckoutThenPayResult
    {
        public string InvoiceID { get; set; } = string.Empty;
        public decimal RoomCharge { get; set; }
        public decimal ServicesCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public string CheckoutType { get; set; } = string.Empty;
        public string CheckOutID { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_CreateInvoice_PayThenCheckout result
    /// </summary>
    public class PayThenCheckoutResult
    {
        public string InvoiceID { get; set; } = string.Empty;
        public decimal RoomCharge { get; set; }
        public decimal ServicesCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CheckoutType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_ConfirmPayment result
    /// </summary>
    public class ConfirmPaymentResult
    {
        public string InvoiceID { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public DateTime? PaymentDate { get; set; } // Nullable để xử lý NULL từ database
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_ActualCheckout_AfterPrepayment result
    /// </summary>
    public class ActualCheckoutResult
    {
        public string CheckOutID { get; set; } = string.Empty;
        public DateTime CheckOutDate { get; set; }
        public DateTime CheckOutExpected { get; set; }
        public decimal AdditionalCharge { get; set; }
        public string CheckoutStatus { get; set; } = string.Empty;
        public string InvoiceID { get; set; } = string.Empty;
    }

    public static class DatabaseExtensions
    {
        /// <summary>
        /// Execute stored procedure sp_CreateReservation
        /// Returns result set with reservation details
        /// </summary>
        public static async Task<ReservationResult?> CreateReservationSP(
            this HotelManagementContext context,
            DateTime checkInDate,
            DateTime checkOutDate,
            string roomID,
            string customerID,
            string employeeID,
            double roomBookingDeposit,
            string priceUnit,
            decimal unitPrice)
        {
            var parameters = new[]
            {
                new SqlParameter("@checkInDate", checkInDate),
                new SqlParameter("@checkOutDate", checkOutDate),
                new SqlParameter("@roomID", roomID),
                new SqlParameter("@customerID", customerID),
                new SqlParameter("@employeeID", employeeID),
                new SqlParameter("@roomBookingDeposit", roomBookingDeposit),
                new SqlParameter("@priceUnit", priceUnit),
                new SqlParameter("@unitPrice", unitPrice)
            };

            var result = await context.Database
                .SqlQueryRaw<ReservationResult>(
                    "EXEC sp_CreateReservation @checkInDate, @checkOutDate, @roomID, @customerID, @employeeID, @roomBookingDeposit, @priceUnit, @unitPrice",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_UpdateReservationSchedule
        /// Cập nhật lịch/phòng và snapshot giá cho phiếu đặt phòng chưa check-in, có kiểm tra trùng lịch trong SQL.
        /// </summary>
        public static async Task UpdateReservationScheduleSP(
            this HotelManagementContext context,
            string reservationFormID,
            string roomID,
            DateTime checkInDate,
            DateTime checkOutDate,
            string employeeID,
            string priceUnit,
            decimal unitPrice,
            decimal roomBookingDeposit)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@roomID", roomID),
                new SqlParameter("@checkInDate", checkInDate),
                new SqlParameter("@checkOutDate", checkOutDate),
                new SqlParameter("@employeeID", employeeID),
                new SqlParameter("@priceUnit", priceUnit),
                new SqlParameter("@unitPrice", unitPrice),
                new SqlParameter("@roomBookingDeposit", roomBookingDeposit)
            };

            await context.Database.ExecuteSqlRawAsync(
                "EXEC sp_UpdateReservationSchedule @reservationFormID, @roomID, @checkInDate, @checkOutDate, @employeeID, @priceUnit, @unitPrice, @roomBookingDeposit",
                parameters);
        }

        /// <summary>
        /// Execute stored procedure sp_QuickCheckin
        /// Automatically generates historyCheckInID
        /// </summary>
        public static async Task<CheckInResult?> CheckInRoomSP(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@employeeID", employeeID)
            };

            var result = await context.Database
                .SqlQueryRaw<CheckInResult>(
                    "EXEC sp_QuickCheckin @reservationFormID, @employeeID",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_MarkReservationNoShow
        /// Đánh dấu phiếu khách không đến và giải phóng phòng nếu phù hợp.
        /// </summary>
        public static async Task<NoShowResult?> MarkReservationNoShowSP(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@employeeID", employeeID)
            };

            var result = await context.Database
                .SqlQueryRaw<NoShowResult>(
                    "EXEC sp_MarkReservationNoShow @reservationFormID, @employeeID",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_QuickCheckout
        /// Automatically generates historyCheckOutID and invoiceID
        /// </summary>
        public static async Task<CheckOutResult?> CheckOutRoomSP(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@employeeID", employeeID)
            };

            var result = await context.Database
                .SqlQueryRaw<CheckOutResult>(
                    "EXEC sp_QuickCheckout @reservationFormID, @employeeID",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_AddRoomService
        /// Add service to room (bypass trigger conflict)
        /// </summary>
        public static async Task<RoomServiceResult?> AddRoomServiceSP(
            this HotelManagementContext context,
            string reservationFormID,
            string hotelServiceId,
            int quantity,
            string employeeID)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@hotelServiceId", hotelServiceId),
                new SqlParameter("@quantity", quantity),
                new SqlParameter("@employeeID", employeeID)
            };

            var result = await context.Database
                .SqlQueryRaw<RoomServiceResult>(
                    "EXEC sp_AddRoomService @reservationFormID, @hotelServiceId, @quantity, @employeeID",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_DeleteRoomService
        /// Delete service from room
        /// </summary>
        public static async Task<DeletedRoomServiceResult?> DeleteRoomServiceSP(
            this HotelManagementContext context,
            string roomUsageServiceId)
        {
            var parameters = new[]
            {
                new SqlParameter("@roomUsageServiceId", roomUsageServiceId)
            };

            var result = await context.Database
                .SqlQueryRaw<DeletedRoomServiceResult>(
                    "EXEC sp_DeleteRoomService @roomUsageServiceId",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Generate ID using database function fn_GenerateID
        /// </summary>
        public static async Task<string> GenerateID(this HotelManagementContext context, string prefix, string tableName, int padLength = 6)
        {
            var connection = context.Database.GetDbConnection();
            bool shouldClose = false;

            // Chỉ mở connection nếu chưa mở
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldClose = true;
            }

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT dbo.fn_GenerateID(@prefix, @tableName, '', @padLength)";
                    command.Parameters.Add(new SqlParameter("@prefix", prefix));
                    command.Parameters.Add(new SqlParameter("@tableName", tableName));
                    command.Parameters.Add(new SqlParameter("@padLength", padLength));

                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString()    ?? string.Empty;
                }
            }
            finally
            {
                // Chỉ đóng connection nếu mình là người mở
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Execute stored procedure sp_CreateConfirmationReceipt
        /// Returns receipt details
        /// </summary>
        public static async Task<ConfirmationReceipt?> CreateConfirmationReceiptSP(
            this HotelManagementContext context,
            string receiptType,
            string reservationFormID,
            string? invoiceID,
            string employeeID)
        {
            var receiptTypeParam = new SqlParameter("@receiptType", receiptType);
            var reservationParam = new SqlParameter("@reservationFormID", reservationFormID);
            var invoiceParam = new SqlParameter("@invoiceID", (object?)invoiceID ?? DBNull.Value);
            var employeeParam = new SqlParameter("@employeeID", employeeID);

            var results = await context.ConfirmationReceipts
                .FromSqlRaw("EXEC sp_CreateConfirmationReceipt @receiptType, @reservationFormID, @invoiceID, @employeeID",
                    receiptTypeParam, reservationParam, invoiceParam, employeeParam)
                .ToListAsync();

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_CreateInvoice_CheckoutThenPay
        /// Checkout rồi thanh toán - Tính tiền theo thời gian thực tế
        /// </summary>
        public static async Task<CheckoutThenPayResult?> CreateInvoice_CheckoutThenPay(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var reservationParam = new SqlParameter("@reservationFormID", reservationFormID);
            var employeeParam = new SqlParameter("@employeeID", employeeID);

            var results = await context.Database
                .SqlQueryRaw<CheckoutThenPayResult>(
                    "EXEC sp_CreateInvoice_CheckoutThenPay @reservationFormID, @employeeID",
                    reservationParam, employeeParam)
                .ToListAsync();

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_CreateInvoice_PayThenCheckout
        /// Thanh toán trước - Tính tiền từ check-in thực tế đến check-out dự kiến
        /// </summary>
        public static async Task<PayThenCheckoutResult?> CreateInvoice_PayThenCheckout(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID,
            string paymentMethod = "CASH")
        {
            var reservationParam = new SqlParameter("@reservationFormID", reservationFormID);
            var employeeParam = new SqlParameter("@employeeID", employeeID);
            var paymentParam = new SqlParameter("@paymentMethod", paymentMethod);

            var results = await context.Database
                .SqlQueryRaw<PayThenCheckoutResult>(
                    "EXEC sp_CreateInvoice_PayThenCheckout @reservationFormID, @employeeID, @paymentMethod",
                    reservationParam, employeeParam, paymentParam)
                .ToListAsync();

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_ConfirmPayment
        /// Xác nhận thanh toán cho hóa đơn (dùng cho option Checkout Then Pay)
        /// </summary>
        public static async Task<ConfirmPaymentResult?> ConfirmPaymentSP(
            this HotelManagementContext context,
            string invoiceID,
            string paymentMethod,
            string employeeID)
        {
            var invoiceParam = new SqlParameter("@invoiceID", invoiceID);
            var paymentParam = new SqlParameter("@paymentMethod", paymentMethod);
            var employeeParam = new SqlParameter("@employeeID", employeeID);

            var results = await context.Database
                .SqlQueryRaw<ConfirmPaymentResult>(
                    "EXEC sp_ConfirmPayment @invoiceID, @paymentMethod, @employeeID",
                    invoiceParam, paymentParam, employeeParam)
                .ToListAsync();

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_ActualCheckout_AfterPrepayment
        /// Checkout thực tế sau khi đã thanh toán trước (với tính phụ phí nếu muộn)
        /// </summary>
        public static async Task<ActualCheckoutResult?> ActualCheckout_AfterPrepayment(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var reservationParam = new SqlParameter("@reservationFormID", reservationFormID);
            var employeeParam = new SqlParameter("@employeeID", employeeID);

            var results = await context.Database
                .SqlQueryRaw<ActualCheckoutResult>(
                    "EXEC sp_ActualCheckout_AfterPrepayment @reservationFormID, @employeeID",
                    reservationParam, employeeParam)
                .ToListAsync();

            return results.FirstOrDefault();
        }
    }
}
