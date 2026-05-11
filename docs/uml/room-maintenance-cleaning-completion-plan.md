# Kế hoạch hoàn thiện module Bảo trì & Dọn phòng

## 1. Mục tiêu

Tài liệu này lập kế hoạch triển khai các phần còn thiếu của module **Bảo trì & Dọn phòng** sau khi đã có luồng lõi:

```text
Tạo công việc -> Phân công -> Bắt đầu xử lý -> Hoàn thành/Hủy -> Ghi lịch sử -> Cập nhật trạng thái phòng
```

Mục tiêu hoàn thiện:

- Tăng độ an toàn nghiệp vụ khi tạo công việc làm thay đổi trạng thái phòng.
- Cải thiện UX theo đúng quyền và nhân viên phụ trách.
- Lọc/gợi ý nhân viên phù hợp với loại công việc.
- Bổ sung thống kê đúng theo toàn bộ dữ liệu, không chỉ theo trang hiện tại.
- Bổ sung hạn xử lý/SLA/quá hạn công việc.
- Bổ sung báo cáo vận hành cơ bản.
- Chuẩn hóa quyết định kỹ thuật: **module này dùng Entity Framework Core trực tiếp**, không dùng stored procedure cho luồng xử lý chính.

## 2. Quyết định kỹ thuật chính

### 2.1. Dùng EF Core trực tiếp cho module này

Module **Bảo trì & Dọn phòng** sẽ xử lý nghiệp vụ bằng EF Core trong controller hoặc service nội bộ, không gọi stored procedure cho các thao tác chính.

Lý do:

- Luồng module này mới, chưa phụ thuộc quy trình stored procedure cũ như đặt phòng/check-in/check-out.
- Logic cần kiểm tra nhiều trạng thái UI, quyền nhân viên, task mở, room status, SLA nên xử lý trong C# dễ đọc và dễ bảo trì hơn.
- Các thao tác chủ yếu là CRUD/lifecycle đơn giản, phù hợp EF Core.
- Tránh phải duy trì song song cùng một logic ở cả SQL stored procedure và C#.

### 2.2. Vai trò của stored procedure hiện có trong SQL script

Các stored procedure đã có trong database script như:

- `sp_CreateRoomTask`
- `sp_AssignRoomTask`
- `sp_UpdateRoomTaskStatus`
- `sp_CancelRoomTask`

Sẽ được xem là **tham khảo/không dùng cho ứng dụng MVC hiện tại**, trừ khi sau này có yêu cầu chuyển toàn bộ module sang stored procedure.

Khi triển khai hoàn thiện nên chọn một trong hai hướng tài liệu hóa:

1. Giữ stored procedure trong SQL script và ghi chú rõ: hiện app dùng EF Core trực tiếp.
2. Hoặc loại bỏ các stored procedure này khỏi script nếu muốn tránh nhầm lẫn.

Khuyến nghị: **giữ lại nhưng thêm comment rõ ràng** để phục vụ báo cáo/đồ án và đối chiếu nghiệp vụ.

## 3. Phạm vi phần còn thiếu

| Mã | Hạng mục | Mức ưu tiên | Mục tiêu |
|---|---|---:|---|
| MC-01 | Hiển thị nút thao tác đúng quyền | Cao | Không cho người không phụ trách thấy nút bắt đầu/hoàn thành |
| MC-02 | Cảnh báo/chặn đổi trạng thái phòng nhạy cảm | Cao | Tránh tạo task làm sai trạng thái phòng đang có khách/đã đặt |
| MC-03 | Lọc nhân viên theo loại công việc | Cao | Dọn phòng giao đúng nhóm buồng phòng, bảo trì giao đúng kỹ thuật |
| MC-04 | Thống kê toàn bộ theo query riêng | Trung bình | Dashboard chính xác theo bộ lọc/toàn hệ thống |
| MC-05 | Bổ sung hạn xử lý/SLA/quá hạn | Trung bình | Theo dõi việc trễ hạn, ưu tiên xử lý |
| MC-06 | Báo cáo vận hành | Trung bình | Báo cáo theo nhân viên, phòng, loại việc, thời gian xử lý |
| MC-07 | Chuẩn hóa EF trực tiếp trong docs/code comment | Trung bình | Tránh nhầm với stored procedure |
| MC-08 | Cải thiện xác nhận thao tác | Thấp | UX an toàn hơn khi hoàn thành/hủy công việc |

## 4. MC-01: Hiển thị nút thao tác đúng quyền

### 4.1. Vấn đề hiện tại

Controller đã chặn quyền khi xử lý `Start` và `Complete`, nhưng UI chi tiết có thể vẫn hiển thị nút nếu công việc đã phân công hoặc đang xử lý.

Điều này gây UX chưa tốt:

- Nhân viên không được giao vẫn thấy nút.
- Người dùng bấm xong mới bị báo lỗi.
- Giao diện không phản ánh đúng quyền thao tác.

### 4.2. Quy tắc đề xuất

Một người được thao tác công việc nếu:

```text
Là ADMIN hoặc MANAGER
hoặc
EmployeeID trong session == AssignedEmployeeID của công việc
```

Áp dụng cho:

- Nút **Bắt đầu xử lý**.
- Nút **Hoàn thành**.
- Thông báo hướng dẫn trong khung thao tác.

### 4.3. File cần sửa

- `Views/RoomMaintenanceCleaning/Details.cshtml`
- `Views/RoomMaintenanceCleaning/Index.cshtml` nếu sau này thêm nút start/complete nhanh ở danh sách.
- `Controllers/RoomMaintenanceCleaningController.cs` giữ nguyên logic chặn quyền hiện tại.

### 4.4. Cách triển khai

Trong view chi tiết, bổ sung biến:

```csharp
var currentEmployeeID = Context.Session.GetString("EmployeeID");
var canOperate = canManage || (!string.IsNullOrWhiteSpace(currentEmployeeID) && currentEmployeeID == Model.AssignedEmployeeID);
var canStart = isAssigned && hasAssignedEmployee && canOperate;
var canComplete = isInProgress && canOperate;
```

Nếu `!canOperate` nhưng công việc đã phân công cho người khác, hiển thị alert:

```text
Công việc này đã được phân công cho nhân viên khác. Bạn chỉ có quyền xem chi tiết.
```

### 4.5. Tiêu chí hoàn thành

- Nhân viên không được giao không thấy nút bắt đầu/hoàn thành.
- Nhân viên được giao thấy nút đúng trạng thái.
- Manager/Admin vẫn thao tác được mọi công việc chưa đóng.
- Controller vẫn giữ kiểm tra quyền để đảm bảo an toàn phía server.

## 5. MC-02: Cảnh báo/chặn đổi trạng thái phòng nhạy cảm

### 5.1. Vấn đề hiện tại

Form tạo dọn phòng/bảo trì đang cho chọn tất cả phòng active. Nếu chọn phòng đang `ON_USE` hoặc `RESERVED`, việc tạo task có thể làm phòng chuyển ngay sang `DIRTY` hoặc `MAINTENANCE`.

Rủi ro:

- Phòng đang có khách bị chuyển trạng thái sai.
- Phòng đã đặt trước bị đánh dấu bảo trì/cần dọn mà không có cảnh báo.
- Nhân viên có thể tạo nhầm task cho phòng đang không phù hợp.

### 5.2. Quy tắc đề xuất theo loại công việc

#### Dọn phòng thủ công

Cho phép tạo bình thường khi phòng thuộc:

- `AVAILABLE`
- `DIRTY`
- `CLEANING`

Cần cảnh báo/xác nhận khi phòng thuộc:

- `ON_USE`
- `RESERVED`
- `OVERDUE`

Không nên cho tạo khi phòng thuộc:

- `MAINTENANCE`
- `OUT_OF_SERVICE`

#### Bảo trì thủ công

Cho phép tạo khi phòng thuộc:

- `AVAILABLE`
- `DIRTY`
- `CLEANING`
- `UNAVAILABLE`

Cần cảnh báo/xác nhận khi phòng thuộc:

- `ON_USE`
- `RESERVED`
- `OVERDUE`

Không nên cho tạo nếu phòng đã:

- `MAINTENANCE` và đã có maintenance task mở.
- `OUT_OF_SERVICE`, trừ khi manager/admin xác nhận đây là công việc kiểm tra lại.

### 5.3. Cách triển khai giai đoạn đầu

Không thêm bảng mới. Bổ sung kiểm tra trong C#:

- Tạo helper `ValidateRoomStatusForTask(Room room, string taskType, bool forceCreate)`.
- Thêm checkbox hoặc hidden field `forceCreate` trong form.
- Nếu phòng nhạy cảm và `forceCreate = false`, trả về view kèm cảnh báo.
- Nếu user xác nhận, submit lại với `forceCreate = true`.

### 5.4. UI đề xuất

Khi chọn phòng, hiển thị trạng thái phòng trong dropdown như hiện tại.

Khi submit gặp phòng nhạy cảm, hiển thị alert:

```text
Phòng P101 hiện đang có trạng thái Đang sử dụng. Việc tạo yêu cầu bảo trì sẽ chuyển phòng sang Bảo trì và có thể ảnh hưởng đến khách đang lưu trú. Vui lòng xác nhận nếu vẫn muốn tiếp tục.
```

Nút xác nhận:

```text
Vẫn tạo yêu cầu
```

### 5.5. File cần sửa

- `Controllers/RoomMaintenanceCleaningController.cs`
- `Views/RoomMaintenanceCleaning/CreateCleaningTask.cshtml`
- `Views/RoomMaintenanceCleaning/CreateMaintenanceTask.cshtml`

### 5.6. Tiêu chí hoàn thành

- Tạo dọn phòng cho phòng bảo trì/ngừng sử dụng bị chặn hoặc cảnh báo đúng.
- Tạo bảo trì cho phòng đang có khách phải có cảnh báo xác nhận.
- Không ảnh hưởng luồng tự động tạo dọn phòng sau checkout.
- Không tạo trùng task cùng loại đang mở.

## 6. MC-03: Lọc nhân viên theo loại công việc

### 6.1. Vấn đề hiện tại

Màn hình phân công đang lấy tất cả nhân viên active. Điều này đủ chạy nhưng chưa đúng nghiệp vụ.

### 6.2. Quy tắc đề xuất

Do hệ thống hiện có trường `Position`, giai đoạn đầu có thể lọc theo từ khóa vị trí.

Gợi ý mapping:

| Loại công việc | Nhân viên ưu tiên |
|---|---|
| `CLEANING` | Vị trí chứa `buồng`, `phòng`, `dọn`, `housekeeping`, `clean` |
| `MAINTENANCE` | Vị trí chứa `kỹ thuật`, `bảo trì`, `sửa`, `maintenance`, `technical` |

Nếu không tìm thấy nhân viên phù hợp:

- Vẫn hiển thị toàn bộ nhân viên active.
- Hiển thị cảnh báo: `Chưa tìm thấy nhân viên đúng nhóm, đang hiển thị toàn bộ nhân viên hoạt động.`

### 6.3. Cách triển khai

Sửa helper load nhân viên:

```csharp
private async Task LoadEmployeeData(string taskType, string? selectedEmployeeID = null)
```

Luồng:

1. Query toàn bộ nhân viên active.
2. Lọc nhân viên phù hợp theo `taskType` và `Position`.
3. Nếu có kết quả, bind danh sách phù hợp.
4. Nếu không có kết quả, bind toàn bộ và set `ViewBag.EmployeeFilterWarning`.

Trong POST `Assign`, kiểm tra lại:

- Nếu nhân viên không đúng nhóm, manager/admin vẫn có thể phân công nhưng cần ghi cảnh báo hoặc yêu cầu xác nhận.
- Giai đoạn đầu nên cảnh báo ở UI, chưa cần chặn cứng để tránh kẹt dữ liệu mẫu.

### 6.4. File cần sửa

- `Controllers/RoomMaintenanceCleaningController.cs`
- `Views/RoomMaintenanceCleaning/Assign.cshtml`

### 6.5. Tiêu chí hoàn thành

- Task dọn phòng ưu tiên hiển thị nhân viên buồng phòng/dọn phòng.
- Task bảo trì ưu tiên hiển thị nhân viên kỹ thuật/bảo trì.
- Không có nhân viên phù hợp thì vẫn dùng được module.
- Người dùng hiểu vì sao danh sách bị lọc.

## 7. MC-04: Thống kê toàn bộ bằng query riêng

### 7.1. Vấn đề hiện tại

Các thẻ thống kê trên trang danh sách đang đếm trên `Model` hiện tại. Nếu `Model` là dữ liệu phân trang, số liệu chỉ phản ánh trang hiện tại, không phải toàn bộ danh sách.

### 7.2. Mục tiêu

Thống kê phải đếm trên query đầy đủ sau khi áp dụng bộ lọc, trước phân trang.

### 7.3. Chỉ số cần có

Giai đoạn đầu:

- Tổng công việc chờ xử lý.
- Tổng công việc đã phân công.
- Tổng công việc đang thực hiện.
- Tổng công việc hoàn thành.
- Tổng công việc đã hủy.
- Tổng công việc khẩn cấp.

Giai đoạn nâng cấp:

- Tổng công việc quá hạn.
- Thời gian xử lý trung bình.
- Tỷ lệ hoàn thành.

### 7.4. Cách triển khai

Tạo ViewModel:

```text
Models/ViewModels/RoomTaskIndexViewModel.cs
```

Nội dung đề xuất:

```csharp
public class RoomTaskIndexViewModel
{
    public PagedList<RoomTask> Tasks { get; set; }
    public RoomTaskSummaryViewModel Summary { get; set; }
}

public class RoomTaskSummaryViewModel
{
    public int PendingCount { get; set; }
    public int AssignedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int UrgentCount { get; set; }
    public int OverdueCount { get; set; }
}
```

Trong `Index`:

1. Build query gốc.
2. Apply filter.
3. Tính summary bằng `CountAsync` trên query đã lọc.
4. Apply sort + pagination.
5. Trả ViewModel thay vì chỉ `PagedList<RoomTask>`.

### 7.5. File cần sửa/tạo

- Tạo `Models/ViewModels/RoomTaskIndexViewModel.cs`
- Sửa `Controllers/RoomMaintenanceCleaningController.cs`
- Sửa `Views/RoomMaintenanceCleaning/Index.cshtml`

### 7.6. Tiêu chí hoàn thành

- Thống kê không thay đổi sai khi chuyển trang.
- Bộ lọc ảnh hưởng đúng đến số liệu summary.
- Không làm mất phân trang hiện có.

## 8. MC-05: Bổ sung hạn xử lý/SLA/quá hạn

### 8.1. Lý do cần bổ sung

Khách sạn cần biết công việc nào cần xử lý gấp, công việc nào đã trễ. Hiện tại chỉ có priority nhưng chưa có hạn hoàn thành cụ thể.

### 8.2. Data field đề xuất

Bổ sung vào `RoomTask`:

| Field C# | Column SQL | Ý nghĩa |
|---|---|---|
| `DueAt` | `dueAt` | Hạn hoàn thành công việc |
| `SlaMinutes` | `slaMinutes` | Thời lượng SLA dự kiến theo phút |

Giai đoạn đầu có thể chỉ cần `DueAt`. `SlaMinutes` là tùy chọn.

### 8.3. Quy tắc mặc định hạn xử lý

Nếu người dùng không nhập hạn xử lý, hệ thống tự gợi ý theo loại và độ ưu tiên:

| Loại | Ưu tiên | Hạn gợi ý |
|---|---|---|
| Dọn phòng | Low | 8 giờ |
| Dọn phòng | Normal | 4 giờ |
| Dọn phòng | High | 2 giờ |
| Dọn phòng | Urgent | 1 giờ |
| Bảo trì | Low | 48 giờ |
| Bảo trì | Normal | 24 giờ |
| Bảo trì | High | 8 giờ |
| Bảo trì | Urgent | 2 giờ |

### 8.4. Quy tắc quá hạn

Một task được xem là quá hạn khi:

```text
DueAt < thời điểm hiện tại
và Status thuộc PENDING / ASSIGNED / IN_PROGRESS
```

Không tạo thêm trạng thái database `OVERDUE` cho task ở giai đoạn đầu để tránh phình state machine. Quá hạn là trạng thái tính toán trên UI/query.

### 8.5. UI cần bổ sung

Ở danh sách:

- Cột `Hạn xử lý`.
- Badge `Quá hạn` nếu đã quá hạn.
- Bộ lọc `Chỉ hiển thị quá hạn`.

Ở chi tiết:

- Hiển thị hạn xử lý.
- Hiển thị thời gian còn lại hoặc trễ hạn.

Ở form tạo:

- Cho nhập `DueAt` tùy chọn.
- Nếu bỏ trống, dùng gợi ý tự động.

### 8.6. File cần sửa

- `Models/RoomTask.cs`
- `Data/HotelManagementContext.cs` nếu cần cấu hình thêm.
- `docs/database/HotelManagement_new.sql`
- `Controllers/RoomMaintenanceCleaningController.cs`
- `Views/RoomMaintenanceCleaning/Index.cshtml`
- `Views/RoomMaintenanceCleaning/Details.cshtml`
- `Views/RoomMaintenanceCleaning/CreateCleaningTask.cshtml`
- `Views/RoomMaintenanceCleaning/CreateMaintenanceTask.cshtml`

### 8.7. Tiêu chí hoàn thành

- Task mới có hạn xử lý.
- Task cũ không lỗi nếu `DueAt` null.
- Danh sách hiển thị rõ task quá hạn.
- Có thể lọc task quá hạn.
- Summary có số task quá hạn.

## 9. MC-06: Báo cáo vận hành

### 9.1. Mục tiêu

Bổ sung báo cáo phục vụ quản lý vận hành phòng.

### 9.2. Báo cáo giai đoạn đầu

Tạo action/view mới trong controller hiện tại:

```text
/RoomMaintenanceCleaning/Report
```

Bộ lọc:

- Từ ngày.
- Đến ngày.
- Loại công việc.
- Nhân viên.
- Phòng.

Chỉ số:

- Tổng số công việc.
- Số công việc hoàn thành.
- Số công việc hủy.
- Số công việc quá hạn.
- Thời gian xử lý trung bình.
- Top nhân viên xử lý nhiều nhất.
- Top phòng phát sinh bảo trì nhiều nhất.

### 9.3. DTO/ViewModel đề xuất

Tạo file:

```text
Models/ViewModels/RoomTaskReportViewModel.cs
```

Nội dung:

```csharp
public class RoomTaskReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? TaskType { get; set; }
    public string? EmployeeID { get; set; }
    public string? RoomID { get; set; }
    public RoomTaskReportSummary Summary { get; set; }
    public List<RoomTaskEmployeeReportItem> EmployeeItems { get; set; }
    public List<RoomTaskRoomReportItem> RoomItems { get; set; }
}
```

### 9.4. Cách tính thời gian xử lý

Với task hoàn thành:

```text
CompletedAt - StartedAt
```

Nếu `StartedAt` null:

```text
CompletedAt - CreatedAt
```

Chỉ tính trung bình với task `COMPLETED`.

### 9.5. File cần sửa/tạo

- `Controllers/RoomMaintenanceCleaningController.cs`
- `Models/ViewModels/RoomTaskReportViewModel.cs`
- `Views/RoomMaintenanceCleaning/Report.cshtml`
- `Views/RoomMaintenanceCleaning/Index.cshtml` thêm nút báo cáo.
- Có thể cập nhật `Views/Shared/_Layout.cshtml` nếu muốn menu riêng.

### 9.6. Tiêu chí hoàn thành

- Manager/Admin xem được báo cáo.
- Employee thường có thể bị chặn hoặc chỉ xem thống kê cá nhân tùy quyết định.
- Báo cáo lọc đúng theo ngày/nhân viên/phòng/loại việc.
- Không làm chậm trang danh sách chính.

## 10. MC-07: Chuẩn hóa tài liệu EF trực tiếp

### 10.1. Mục tiêu

Tránh nhầm lẫn vì SQL script có stored procedure nhưng controller dùng EF Core trực tiếp.

### 10.2. Việc cần làm

Trong tài liệu module, bổ sung ghi chú:

```text
Quyết định triển khai: Module Bảo trì & Dọn phòng dùng Entity Framework Core trực tiếp cho các thao tác nghiệp vụ. Stored procedure trong SQL script chỉ là phương án tham khảo/đối chiếu, không phải luồng app hiện tại.
```

Trong SQL script, thêm comment trước nhóm procedure room task:

```sql
-- Lưu ý: Ứng dụng MVC hiện tại xử lý module RoomTask bằng EF Core trực tiếp.
-- Các procedure dưới đây là phương án tham khảo, chưa được controller gọi.
```

### 10.3. File cần sửa

- `docs/uml/room-maintenance-cleaning-implementation-plan.md`
- `docs/database/HotelManagement_new.sql`

### 10.4. Tiêu chí hoàn thành

- Người đọc code hiểu rõ app đang dùng EF Core.
- Không có yêu cầu bắt buộc tạo wrapper stored procedure trong `DatabaseExtensions.cs` cho module này.

## 11. MC-08: Cải thiện xác nhận thao tác

### 11.1. Vấn đề hiện tại

Hủy công việc có confirm đơn giản. Hoàn thành công việc chưa có confirm rõ ràng.

### 11.2. UX đề xuất

Bổ sung confirm cho:

- Bắt đầu xử lý.
- Hoàn thành.
- Hủy.
- Tạo dọn phòng/bảo trì cho phòng trạng thái nhạy cảm.

Giai đoạn đầu dùng `confirm()` JavaScript đơn giản để phù hợp style hiện tại.

Giai đoạn sau có thể thay bằng Bootstrap modal.

### 11.3. Tiêu chí hoàn thành

- Người dùng không thể vô tình hoàn thành/hủy task chỉ bằng một click nhầm.
- Confirm message có nêu rõ tác động đến trạng thái phòng.

## 12. Kế hoạch triển khai theo giai đoạn

## Giai đoạn 1: Hoàn thiện UX quyền và cảnh báo phòng

Mục tiêu:

- Sửa UX nút thao tác.
- Thêm kiểm tra/cảnh báo trạng thái phòng khi tạo task.
- Lọc nhân viên theo loại việc.

Công việc:

1. Chạy impact analysis trước khi sửa `RoomMaintenanceCleaningController` và các view liên quan.
2. Sửa logic hiển thị nút trong `Details.cshtml`.
3. Bổ sung helper validate trạng thái phòng trong controller.
4. Bổ sung `forceCreate` cho form tạo task nếu cần xác nhận.
5. Sửa `LoadEmployeeData` để nhận `taskType` và lọc nhân viên phù hợp.
6. Sửa `Assign.cshtml` để hiển thị cảnh báo lọc nhân viên.
7. Chạy `dotnet build`.

Kết quả mong muốn:

- Module an toàn hơn khi thao tác nhầm.
- UI phản ánh đúng quyền.
- Phân công sát nghiệp vụ hơn.

## Giai đoạn 2: Thống kê chính xác và view model

Mục tiêu:

- Không dùng `Model.Count(...)` trên trang hiện tại để làm summary.
- Chuyển trang Index sang ViewModel rõ ràng.

Công việc:

1. Tạo `RoomTaskIndexViewModel`.
2. Sửa action `Index` để tính summary bằng query riêng.
3. Sửa view `Index.cshtml` dùng `Model.Summary` và `Model.Tasks`.
4. Kiểm tra phân trang và bộ lọc.
5. Chạy `dotnet build`.

Kết quả mong muốn:

- Số liệu dashboard đúng với toàn bộ query.
- Code dễ mở rộng cho quá hạn và báo cáo.

## Giai đoạn 3: Bổ sung SLA/quá hạn

Mục tiêu:

- Task có hạn xử lý.
- UI hiển thị/quản lý công việc quá hạn.

Công việc:

1. Bổ sung `DueAt` vào `RoomTask`.
2. Cập nhật SQL script thêm cột `dueAt`.
3. Bổ sung helper tính hạn mặc định theo loại việc/ưu tiên.
4. Cập nhật form tạo dọn phòng/bảo trì.
5. Cập nhật danh sách và chi tiết hiển thị hạn xử lý.
6. Cập nhật filter `overdueOnly`.
7. Cập nhật summary thêm `OverdueCount`.
8. Chạy `dotnet build`.

Kết quả mong muốn:

- Quản lý biết task nào trễ hạn.
- Nhân viên biết việc nào cần ưu tiên xử lý.

## Giai đoạn 4: Báo cáo vận hành

Mục tiêu:

- Có màn hình báo cáo riêng cho module.

Công việc:

1. Tạo ViewModel báo cáo.
2. Thêm action `Report` trong controller.
3. Tạo view `Report.cshtml`.
4. Thêm nút vào trang Index.
5. Tính các chỉ số theo query EF Core.
6. Chạy `dotnet build`.

Kết quả mong muốn:

- Quản lý xem được hiệu suất vận hành phòng.
- Có số liệu phục vụ báo cáo đồ án.

## Giai đoạn 5: Chuẩn hóa tài liệu và kiểm thử

Mục tiêu:

- Tài liệu phản ánh đúng triển khai EF Core trực tiếp.
- Module được kiểm thử theo luồng chính.

Công việc:

1. Cập nhật plan cũ về quyết định EF Core.
2. Thêm comment vào SQL script trước các stored procedure room task.
3. Cập nhật UML nếu thay đổi data model có `DueAt`.
4. Chạy `dotnet build`.
5. Kiểm thử thủ công toàn bộ checklist.
6. Chạy GitNexus detect changes trước khi commit.

## 13. Checklist kiểm thử thủ công

### 13.1. Quyền và nút thao tác

- [ ] Manager/Admin thấy nút phân công với task `PENDING`/`ASSIGNED`.
- [ ] Nhân viên không được giao không thấy nút bắt đầu.
- [ ] Nhân viên được giao thấy nút bắt đầu khi task `ASSIGNED`.
- [ ] Nhân viên được giao thấy nút hoàn thành khi task `IN_PROGRESS`.
- [ ] Task `COMPLETED`/`CANCELLED` không hiện thao tác xử lý.

### 13.2. Tạo task và trạng thái phòng

- [ ] Tạo dọn phòng cho phòng `AVAILABLE` thành công.
- [ ] Tạo dọn phòng cho phòng `MAINTENANCE` bị chặn hoặc cảnh báo đúng.
- [ ] Tạo bảo trì cho phòng `ON_USE` yêu cầu xác nhận.
- [ ] Không tạo được task cùng loại đang mở cho cùng phòng.
- [ ] Sau tạo cleaning, phòng sang `DIRTY`.
- [ ] Sau start cleaning, phòng sang `CLEANING`.
- [ ] Sau complete cleaning và không còn task mở, phòng sang `AVAILABLE`.

### 13.3. Phân công

- [ ] Task cleaning ưu tiên danh sách nhân viên buồng phòng/dọn phòng.
- [ ] Task maintenance ưu tiên danh sách nhân viên kỹ thuật/bảo trì.
- [ ] Không có nhân viên phù hợp vẫn có fallback danh sách nhân viên active.
- [ ] Nhân viên inactive không xuất hiện.

### 13.4. Thống kê

- [ ] Summary không đổi sai khi chuyển trang.
- [ ] Summary đổi đúng khi lọc trạng thái.
- [ ] Summary đổi đúng khi lọc nhân viên.
- [ ] Urgent count đúng theo toàn bộ query.

### 13.5. SLA/quá hạn

- [ ] Task mới có `DueAt` mặc định nếu không nhập.
- [ ] Task quá hạn có badge/cảnh báo.
- [ ] Bộ lọc quá hạn hoạt động.
- [ ] Task hoàn thành không còn bị tính quá hạn.

### 13.6. Báo cáo

- [ ] Báo cáo lọc theo khoảng ngày.
- [ ] Báo cáo lọc theo nhân viên.
- [ ] Báo cáo tính số việc hoàn thành/hủy đúng.
- [ ] Báo cáo tính thời gian xử lý trung bình đúng.

## 14. Danh sách file dự kiến thay đổi

File hiện có cần sửa:

- `Controllers/RoomMaintenanceCleaningController.cs`
- `Models/RoomTask.cs`
- `Data/HotelManagementContext.cs`
- `Views/RoomMaintenanceCleaning/Index.cshtml`
- `Views/RoomMaintenanceCleaning/Details.cshtml`
- `Views/RoomMaintenanceCleaning/CreateCleaningTask.cshtml`
- `Views/RoomMaintenanceCleaning/CreateMaintenanceTask.cshtml`
- `Views/RoomMaintenanceCleaning/Assign.cshtml`
- `docs/database/HotelManagement_new.sql`
- `docs/uml/room-maintenance-cleaning-implementation-plan.md`

File mới dự kiến:

- `Models/ViewModels/RoomTaskIndexViewModel.cs`
- `Models/ViewModels/RoomTaskReportViewModel.cs`
- `Views/RoomMaintenanceCleaning/Report.cshtml`

Có thể cập nhật nếu thêm menu riêng:

- `Views/Shared/_Layout.cshtml`

## 15. Thứ tự ưu tiên khuyến nghị

Nên triển khai theo thứ tự:

1. MC-01: Sửa UX quyền nút thao tác.
2. MC-02: Cảnh báo/chặn trạng thái phòng nhạy cảm.
3. MC-03: Lọc nhân viên theo loại việc.
4. MC-04: Tính thống kê đúng bằng query riêng.
5. MC-07: Ghi rõ quyết định EF trực tiếp.
6. MC-05: Thêm SLA/quá hạn.
7. MC-06: Báo cáo vận hành.
8. MC-08: Nâng cấp confirm/modal nếu còn thời gian.

## 16. Tiêu chí hoàn thành tổng thể

Module được xem là hoàn thiện hơn khi đáp ứng:

- Luồng lõi không bị phá vỡ.
- UI chỉ hiển thị thao tác người dùng có quyền thực hiện.
- Tạo task không vô tình làm sai trạng thái phòng đang có khách/đã đặt.
- Phân công nhân viên sát nghiệp vụ hơn.
- Dashboard thống kê chính xác theo toàn bộ query.
- Có theo dõi quá hạn công việc.
- Có báo cáo vận hành cơ bản cho quản lý.
- Tài liệu/code thống nhất rằng module dùng EF Core trực tiếp.
- `dotnet build` thành công sau khi triển khai.
