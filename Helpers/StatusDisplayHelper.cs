using HotelManagement.Models;
using Microsoft.AspNetCore.Html;

namespace HotelManagement.Helpers
{
    public static class StatusDisplayHelper
    {
        public static string RoomStatusText(string? status)
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
                _ => string.IsNullOrWhiteSpace(status) ? "Không xác định" : status
            };
        }

        public static string RoomStatusCss(string? status)
        {
            return status switch
            {
                RoomOperationalStatuses.Available => "badge-success-modern",
                RoomOperationalStatuses.OnUse => "badge-warning-modern",
                RoomOperationalStatuses.Reserved => "badge-info-modern",
                RoomOperationalStatuses.Unavailable => "badge-danger-modern",
                RoomOperationalStatuses.Overdue => "badge-danger-modern",
                RoomOperationalStatuses.Dirty => "badge-warning-modern",
                RoomOperationalStatuses.Cleaning => "badge-info-modern",
                RoomOperationalStatuses.Maintenance => "badge-danger-modern",
                RoomOperationalStatuses.OutOfService => "badge-danger-modern",
                _ => "badge-secondary"
            };
        }

        public static string RoomStatusIcon(string? status)
        {
            return status switch
            {
                RoomOperationalStatuses.Available => "fas fa-check-circle",
                RoomOperationalStatuses.OnUse => "fas fa-user-check",
                RoomOperationalStatuses.Reserved => "fas fa-bookmark",
                RoomOperationalStatuses.Unavailable => "fas fa-ban",
                RoomOperationalStatuses.Overdue => "fas fa-clock",
                RoomOperationalStatuses.Dirty => "fas fa-broom",
                RoomOperationalStatuses.Cleaning => "fas fa-spray-can",
                RoomOperationalStatuses.Maintenance => "fas fa-tools",
                RoomOperationalStatuses.OutOfService => "fas fa-door-closed",
                _ => "fas fa-question-circle"
            };
        }

        public static string RoomStatusBootstrapCss(string? status)
        {
            return status switch
            {
                RoomOperationalStatuses.Available => "bg-success",
                RoomOperationalStatuses.OnUse => "bg-warning",
                RoomOperationalStatuses.Reserved => "bg-info",
                RoomOperationalStatuses.Unavailable => "bg-secondary",
                RoomOperationalStatuses.Overdue => "bg-danger",
                RoomOperationalStatuses.Dirty => "bg-warning",
                RoomOperationalStatuses.Cleaning => "bg-info",
                RoomOperationalStatuses.Maintenance => "bg-danger",
                RoomOperationalStatuses.OutOfService => "bg-dark",
                _ => "bg-secondary"
            };
        }

        public static IHtmlContent RoomStatusBadge(string? status)
        {
            return new HtmlString($"<span class='badge-modern {RoomStatusCss(status)}'><i class='{RoomStatusIcon(status)}'></i> {RoomStatusText(status)}</span>");
        }

        public static IHtmlContent RoomStatusBootstrapBadge(string? status)
        {
            return new HtmlString($"<span class='badge {RoomStatusBootstrapCss(status)}'>{RoomStatusText(status)}</span>");
        }

        public static string RoomTaskStatusText(string? status)
        {
            return status switch
            {
                RoomTaskStatuses.Pending => "Chờ xử lý",
                RoomTaskStatuses.Assigned => "Đã phân công",
                RoomTaskStatuses.InProgress => "Đang thực hiện",
                RoomTaskStatuses.Completed => "Hoàn thành",
                RoomTaskStatuses.Cancelled => "Đã hủy",
                _ => string.IsNullOrWhiteSpace(status) ? "-" : status
            };
        }

        public static string RoomTaskStatusCss(string? status)
        {
            return status switch
            {
                RoomTaskStatuses.Pending => "badge-warning-modern",
                RoomTaskStatuses.Assigned => "badge-info-modern",
                RoomTaskStatuses.InProgress => "badge-primary-modern",
                RoomTaskStatuses.Completed => "badge-success-modern",
                RoomTaskStatuses.Cancelled => "badge-danger-modern",
                _ => "badge-secondary"
            };
        }

        public static IHtmlContent RoomTaskStatusBadge(string? status)
        {
            return new HtmlString($"<span class='badge-modern {RoomTaskStatusCss(status)}'>{RoomTaskStatusText(status)}</span>");
        }

        public static string RoomTaskPriorityText(string? priority)
        {
            return priority switch
            {
                RoomTaskPriorities.Low => "Thấp",
                RoomTaskPriorities.Normal => "Bình thường",
                RoomTaskPriorities.High => "Cao",
                RoomTaskPriorities.Urgent => "Khẩn cấp",
                _ => string.IsNullOrWhiteSpace(priority) ? "Không xác định" : priority
            };
        }

        public static string ActivationText(string? status)
        {
            return status switch
            {
                "ACTIVATE" => "Đang hoạt động",
                "DEACTIVATE" => "Vô hiệu hóa",
                _ => string.IsNullOrWhiteSpace(status) ? "Không xác định" : status
            };
        }
    }
}
