# Kế hoạch xây dựng View Quản lý lịch đặt phòng dạng Calendar

## 1. Mục tiêu

Xây dựng màn hình **Quản lý lịch đặt phòng dạng calendar** để lễ tân/quản lý xem trực quan tình trạng đặt phòng theo ngày, hỗ trợ thao tác nhanh với phiếu đặt phòng và hạn chế đặt trùng lịch.

Phạm vi chính:

- Xem phòng theo ngày/tuần/tháng trên giao diện lịch.
- Hiển thị các khoảng đặt phòng theo từng phòng.
- Kéo thả hoặc resize lịch đặt phòng để đổi ngày nhận/trả phòng khi còn hợp lệ.
- Kiểm tra tránh trùng lịch trước khi tạo/cập nhật đặt phòng.
- Không tạo bảng database calendar riêng nếu chỉ dùng để hiển thị lịch.

## 2. Nguyên tắc thiết kế

### 2.1 Không thêm bảng calendar riêng

Calendar là **view tổng hợp dữ liệu**, lấy từ các bảng hiện có:

- `ReservationForm`: nguồn chính cho lịch đặt phòng.
- `Room`: danh sách phòng và trạng thái hiện tại.
- `RoomCategory`: loại phòng để nhóm/lọc.
- `Customer`: tên khách hiển thị trên lịch.
- `HistoryCheckin`: xác định đặt phòng đã check-in.
- `HistoryCheckOut`: xác định đặt phòng đã check-out.
- `RoomTask`: nếu muốn hiển thị phòng đang dọn/bảo trì.

Chỉ cân nhắc bảng mới nếu sau này cần lưu sự kiện độc lập như khóa phòng thủ công, ghi chú calendar, block phòng ngoài quy trình đặt phòng.

### 2.2 Giữ nguyên luồng nghiệp vụ hiện tại

- Tạo đặt phòng vẫn dùng `sp_CreateReservation` qua `CreateReservationSP`.
- Không bypass stored procedure khi tạo phiếu đặt phòng.
- Check-in/check-out vẫn theo controller hiện có.
- Calendar chỉ bổ sung giao diện xem và thao tác nhanh.

### 2.3 Không đổi mã trạng thái database

Các trạng thái vẫn giữ mã tiếng Anh trong database:

- Room: `AVAILABLE`, `RESERVED`, `ON_USE`, `DIRTY`, `CLEANING`, `MAINTENANCE`, `OUT_OF_SERVICE`, ...
- Reservation activation: `ACTIVATE`, `DEACTIVATE`.

UI phải mapping sang tiếng Việt khi hiển thị.

## 3. Chức năng đề xuất

## 3.1 Xem lịch đặt phòng

### Mô tả

Màn hình hiển thị danh sách phòng theo hàng dọc và timeline theo ngày/giờ ở chiều ngang.

Gợi ý chế độ xem:

- Theo ngày: phù hợp khách thuê giờ hoặc lễ tân xử lý trong ngày.
- Theo tuần: phù hợp xem công suất ngắn hạn.
- Theo tháng: phù hợp quản lý tổng quan.

### Dữ liệu hiển thị trên mỗi sự kiện

Mỗi block đặt phòng nên hiển thị:

- Mã phiếu đặt phòng.
- Tên khách hàng.
- Mã phòng.
- Loại phòng.
- Thời gian nhận/trả phòng.
- Trạng thái nghiệp vụ:
  - Đã đặt.
  - Đã check-in.
  - Đã check-out.
  - Quá hạn nhận phòng.
  - Đã hủy.

### Bộ lọc

Nên có:

- Từ ngày / đến ngày.
- Loại phòng.
- Trạng thái phòng.
- Trạng thái đặt phòng.
- Từ khóa khách hàng hoặc mã phiếu.

## 3.2 Kéo thả đặt phòng

### Mô tả

Cho phép kéo block đặt phòng sang ngày khác hoặc phòng khác để điều chỉnh lịch.

### Điều kiện được kéo thả

Chỉ cho phép kéo thả nếu:

- Phiếu đặt phòng còn `ACTIVATE`.
- Phiếu chưa check-in.
- Phiếu chưa check-out.
- Phòng đích đang hoạt động.
- Khoảng thời gian mới không trùng phiếu đặt phòng khác.
- Phòng đích không đang bảo trì/dọn phòng nếu khoảng đó không phù hợp.

### Điều kiện không cho kéo thả

Không cho kéo thả nếu:

- Phiếu đã check-in.
- Phiếu đã check-out.
- Phiếu đã hủy.
- Khoảng ngày mới nằm trong quá khứ.
- Phòng mới trùng lịch.
- Phòng mới đang `MAINTENANCE`, `OUT_OF_SERVICE` hoặc có task mở.

### Hành vi UI

Khi người dùng kéo thả:

1. UI gửi yêu cầu kiểm tra hợp lệ.
2. Server kiểm tra trùng lịch.
3. Nếu hợp lệ, hiển thị popup xác nhận.
4. Sau khi xác nhận, cập nhật phiếu đặt phòng.
5. Reload lại event trên calendar.
6. Nếu không hợp lệ, trả block về vị trí cũ và hiển thị lỗi.

## 3.3 Resize thời gian đặt phòng

### Mô tả

Cho phép kéo cạnh block lịch để đổi ngày/giờ check-in hoặc check-out.

### Điều kiện

Tương tự kéo thả:

- Chỉ áp dụng phiếu chưa check-in.
- `CheckOutDate` phải lớn hơn `CheckInDate`.
- Không được trùng lịch.
- Không được chuyển check-in về quá khứ.

## 3.4 Tạo đặt phòng nhanh từ calendar

### Mô tả

Người dùng chọn một vùng trống trên calendar để mở form tạo đặt phòng với dữ liệu đã điền sẵn:

- Phòng.
- Ngày nhận phòng.
- Ngày trả phòng.
- Loại giá gợi ý.

### Luồng đề xuất

1. Người dùng chọn vùng trống trên lịch.
2. Mở modal chọn khách hàng, loại thuê, tiền cọc.
3. Gọi API kiểm tra khả dụng.
4. Gửi tạo đặt phòng qua action hiện có hoặc action mới dùng `CreateReservationSP`.
5. Tạo phiếu xác nhận nếu cần.
6. Cập nhật lại calendar.

Có thể triển khai giai đoạn đầu bằng cách redirect sang màn hình tạo đặt phòng hiện tại kèm query string:

```text
/Reservation/Create?roomID=R001&checkInDate=...&checkOutDate=...
```

Sau này mới nâng cấp thành modal AJAX.

## 4. Thiết kế dữ liệu trả về calendar

## 4.1 DTO event

Tạo DTO không map database, ví dụ trong `Data/DatabaseExtensions.cs` hoặc file riêng nếu muốn gọn hơn:

```csharp
public class BookingCalendarEventDto
{
    public string Id { get; set; } = string.Empty;
    public string ReservationFormID { get; set; } = string.Empty;
    public string RoomID { get; set; } = string.Empty;
    public string? RoomCategoryID { get; set; }
    public string? RoomCategoryName { get; set; }
    public string? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool Editable { get; set; }
}
```

## 4.2 DTO resource phòng

Nếu dùng calendar dạng resource timeline, cần DTO phòng:

```csharp
public class BookingCalendarRoomResourceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RoomStatus { get; set; } = string.Empty;
    public string RoomStatusText { get; set; } = string.Empty;
    public string? RoomCategoryName { get; set; }
}
```

## 5. Controller/API đề xuất

Có thể tạo controller mới để tách khỏi `ReservationController`:

```text
Controllers/BookingCalendarController.cs
Views/BookingCalendar/Index.cshtml
```

## 5.1 `Index()`

Mục đích:

- Trả về view calendar.
- Load filter ban đầu: loại phòng, trạng thái phòng.

Quyền truy cập:

- Yêu cầu đăng nhập.
- Lễ tân, quản lý, admin đều xem được.

## 5.2 `Events(DateTime start, DateTime end, string? roomCategoryID, string? status)`

Mục đích:

- Trả JSON danh sách event đặt phòng trong khoảng thời gian.

Query gợi ý:

- Lấy `ReservationForms` có `IsActivate = 'ACTIVATE'`.
- Có giao với khoảng `[start, end]`:

```csharp
reservation.CheckInDate < end && reservation.CheckOutDate > start
```

Include:

- `Room`
- `Room.RoomCategory`
- `Customer`
- `HistoryCheckin`
- `HistoryCheckOut`

## 5.3 `Rooms(string? roomCategoryID, string? roomStatus)`

Mục đích:

- Trả danh sách phòng làm resource cho calendar.

Điều kiện:

- `IsActivate = 'ACTIVATE'`.
- Có thể lọc theo loại phòng/trạng thái.

## 5.4 `CheckAvailability(string roomID, DateTime checkInDate, DateTime checkOutDate, string? excludeReservationFormID = null)`

Mục đích:

- Kiểm tra phòng có trùng lịch hay không.

Điều kiện trùng lịch:

```csharp
r.RoomID == roomID
&& r.IsActivate == "ACTIVATE"
&& r.ReservationFormID != excludeReservationFormID
&& r.CheckInDate < checkOutDate
&& r.CheckOutDate > checkInDate
```

Cần loại trừ hoặc chặn tùy nghiệp vụ:

- Phiếu đã check-out thực tế.
- Phiếu bị hủy `DEACTIVATE`.
- Room task đang mở.
- Phòng đang bảo trì/ngừng sử dụng.

## 5.5 `MoveReservation(...)`

Mục đích:

- Cập nhật phòng/ngày nhận/ngày trả sau khi kéo thả.

Input đề xuất:

```csharp
public class MoveReservationRequest
{
    public string ReservationFormID { get; set; } = string.Empty;
    public string RoomID { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
}
```

Validation bắt buộc:

- Có đăng nhập.
- Có quyền sửa lịch.
- Phiếu tồn tại và `ACTIVATE`.
- Phiếu chưa check-in/check-out.
- Ngày hợp lệ.
- Không trùng lịch.
- Phòng đích hợp lệ.

Ghi chú quan trọng:

- Hiện hệ thống có tạo đặt phòng qua stored procedure, nhưng chưa có stored procedure cập nhật lịch đặt phòng.
- Nếu muốn an toàn và nhất quán, nên bổ sung stored procedure `sp_UpdateReservationSchedule` trong SQL.
- Nếu chưa thêm stored procedure, có thể cập nhật EF trực tiếp ở giai đoạn prototype, nhưng cần ghi chú rõ trong code.

## 6. Stored procedure khuyến nghị

Để tránh trùng logic giữa UI calendar và database, nên thêm stored procedure:

```sql
sp_UpdateReservationSchedule
```

Tham số:

```sql
@reservationFormID NVARCHAR(15),
@roomID NVARCHAR(15),
@checkInDate DATETIME,
@checkOutDate DATETIME,
@employeeID NVARCHAR(15)
```

Nhiệm vụ:

1. Kiểm tra phiếu tồn tại và đang active.
2. Chặn nếu đã check-in hoặc check-out.
3. Kiểm tra ngày hợp lệ.
4. Kiểm tra phòng tồn tại và active.
5. Kiểm tra phòng không bị bảo trì/ngừng sử dụng.
6. Kiểm tra không trùng lịch với reservation active khác.
7. Cập nhật `ReservationForm`.
8. Cập nhật trạng thái phòng cũ/phòng mới nếu cần.
9. Có thể ghi lịch sử thay đổi phòng vào `RoomChangeHistory` nếu phù hợp.

## 7. Luật chống trùng lịch

## 7.1 Công thức overlap chuẩn

Hai khoảng thời gian trùng nhau nếu:

```text
A.Start < B.End AND A.End > B.Start
```

Áp dụng cho reservation:

```csharp
existing.CheckInDate < newCheckOutDate
&& existing.CheckOutDate > newCheckInDate
```

## 7.2 Trường hợp không trùng

Không trùng nếu khách A trả phòng đúng lúc khách B nhận phòng:

```text
A.CheckOutDate == B.CheckInDate
```

Ví dụ:

- Phiếu A: 10/05 12:00 → 12/05 12:00
- Phiếu B: 12/05 12:00 → 14/05 12:00

Hai phiếu này không trùng.

## 7.3 Các phiếu cần bỏ qua khi check trùng

- Phiếu đang bị hủy: `IsActivate = 'DEACTIVATE'`.
- Chính phiếu đang được kéo thả.
- Có thể bỏ qua phiếu đã check-out thực tế nếu nghiệp vụ cho phép dùng lại phòng sau checkout.

## 8. Giao diện đề xuất

## 8.1 Thư viện UI

Có thể dùng một trong hai hướng:

### Hướng A: FullCalendar

Ưu điểm:

- Dễ triển khai.
- Có event drag/drop/resize.
- Hỗ trợ timeline/resource nếu dùng plugin phù hợp.

Nhược điểm:

- Resource timeline bản chính thức có thể liên quan license tùy bản/plugin.
- Cần kiểm tra kỹ giấy phép nếu dùng trong đồ án hoặc sản phẩm.

### Hướng B: Tự dựng grid Razor + JavaScript

Ưu điểm:

- Không phụ thuộc license timeline.
- Chủ động giao diện theo nghiệp vụ khách sạn.
- Phù hợp đồ án nếu chỉ cần ngày/tuần/tháng cơ bản.

Nhược điểm:

- Tốn công xử lý kéo thả/resize.
- Cần tự xử lý scale thời gian và scroll.

Khuyến nghị cho dự án hiện tại:

- Giai đoạn 1: dùng view dạng grid ngày/tuần tự dựng, click để xem/tạo/sửa.
- Giai đoạn 2: thêm drag/drop đơn giản theo ngày.
- Giai đoạn 3: nếu cần UX mạnh hơn, cân nhắc FullCalendar.

## 8.2 Layout màn hình

Đề xuất bố cục:

```text
[Tiêu đề: Lịch đặt phòng]
[Toolbar: Hôm nay | Trước | Sau | Chế độ Ngày/Tuần/Tháng]
[Bộ lọc: Loại phòng | Trạng thái phòng | Trạng thái đặt phòng | Từ khóa]

-------------------------------------------------------
| Phòng       | 11/05 | 12/05 | 13/05 | 14/05 | ... |
-------------------------------------------------------
| P101 Deluxe | RF001=========         RF005====       |
| P102 Single |        RF002=====                     |
| P103 Suite  | MAINTENANCE                           |
-------------------------------------------------------
```

## 8.3 Màu sắc event

Gợi ý:

- Đã đặt: xanh dương.
- Đã check-in: vàng/cam.
- Đã check-out: xanh lá hoặc xám.
- Quá hạn nhận phòng: đỏ.
- Dọn phòng: cam.
- Bảo trì: đỏ đậm.
- Ngừng sử dụng: xám đậm.

## 9. Phân quyền

Đề xuất:

- `ADMIN`: xem, tạo, kéo thả, resize, hủy/điều chỉnh.
- `MANAGER`: xem, tạo, kéo thả, resize, hủy/điều chỉnh.
- `EMPLOYEE`: xem, tạo đặt phòng nhanh; kéo thả có thể cho phép hoặc chặn tùy quy định.

Nếu muốn an toàn:

- Giai đoạn đầu chỉ cho `ADMIN`/`MANAGER` kéo thả/resize.
- `EMPLOYEE` chỉ xem và tạo đặt phòng theo form chuẩn.

## 10. Các bước triển khai đề xuất

## Giai đoạn 1: Calendar read-only

Mục tiêu:

- Xem lịch đặt phòng theo ngày/tuần.
- Không kéo thả.
- Không cập nhật dữ liệu.

Công việc:

1. Tạo `BookingCalendarController`.
2. Tạo `Views/BookingCalendar/Index.cshtml`.
3. Tạo endpoint `Events` trả JSON reservation.
4. Tạo endpoint `Rooms` trả JSON phòng.
5. Render grid calendar.
6. Click event mở modal xem nhanh:
   - Mã phiếu.
   - Khách hàng.
   - Phòng.
   - Thời gian.
   - Link sang chi tiết reservation.
7. Thêm menu trong `_Layout.cshtml`.

Validation:

- Calendar hiển thị đúng reservation active.
- Bộ lọc loại phòng hoạt động.
- Không hiển thị phiếu đã hủy.

## Giai đoạn 2: Kiểm tra khả dụng và tạo nhanh

Mục tiêu:

- Chọn vùng trống để tạo đặt phòng nhanh hoặc redirect sang form hiện có.

Công việc:

1. Thêm endpoint `CheckAvailability`.
2. Khi chọn slot, gọi kiểm tra khả dụng.
3. Nếu hợp lệ, redirect sang `Reservation/Create` với query string hoặc mở modal tạo nhanh.
4. Nếu dùng modal, gọi lại `CreateReservationSP` để tạo.
5. Reload calendar sau khi tạo.

Validation:

- Không tạo được khi trùng lịch.
- Không tạo được ngày quá khứ.
- Không tạo được với phòng bảo trì/ngừng sử dụng.

## Giai đoạn 3: Kéo thả/resize điều chỉnh lịch

Mục tiêu:

- Điều chỉnh ngày/phòng trực tiếp trên calendar.

Công việc:

1. Bổ sung stored procedure `sp_UpdateReservationSchedule`.
2. Thêm wrapper C# trong `DatabaseExtensions.cs`.
3. Thêm endpoint `MoveReservation`.
4. Thêm endpoint `ResizeReservation` hoặc dùng chung `MoveReservation`.
5. Frontend xử lý drag/drop.
6. Frontend xử lý revert khi server trả lỗi.
7. Hiển thị confirm trước khi lưu.

Validation:

- Không kéo được phiếu đã check-in.
- Không kéo sang phòng đang bận.
- Không resize làm ngày trả nhỏ hơn ngày nhận.
- Sau cập nhật, lịch reload đúng.

## Giai đoạn 4: Tích hợp room task và trạng thái phòng

Mục tiêu:

- Calendar thể hiện thêm dọn phòng/bảo trì.

Công việc:

1. Query `RoomTasks` đang mở.
2. Render block task hoặc marker trên phòng.
3. Chặn tạo/kéo reservation vào phòng đang `MAINTENANCE` hoặc `OUT_OF_SERVICE`.
4. Có thể cảnh báo nếu phòng đang `DIRTY`/`CLEANING`.

Validation:

- Phòng đang bảo trì không cho đặt.
- Phòng cần dọn hiển thị cảnh báo rõ.

## 11. Rủi ro và lưu ý

### 11.1 Trạng thái phòng hiện tại không đủ đại diện lịch tương lai

`Room.RoomStatus` chỉ phản ánh trạng thái hiện tại, không đại diện cho toàn bộ lịch tương lai.

Ví dụ:

- Phòng hiện `AVAILABLE`, nhưng đã có reservation vào tuần sau.
- Calendar phải dựa vào `ReservationForm`, không chỉ dựa vào `RoomStatus`.

### 11.2 Không nên chỉ check phòng `AVAILABLE`

Khi tạo đặt phòng tương lai, phòng có thể đang `ON_USE` hôm nay nhưng vẫn trống trong tương lai.

Do đó check khả dụng phải dựa vào overlap reservation, không chỉ `RoomStatus`.

### 11.3 Kéo thả cần transaction

Cập nhật lịch đặt phòng nên dùng transaction hoặc stored procedure để tránh race condition khi nhiều lễ tân cùng thao tác.

### 11.4 Cần đồng bộ với stored procedure hiện có

`sp_CreateReservation` hiện là nguồn tạo đặt phòng chính. Nếu thêm cập nhật lịch, nên đưa validation tương tự vào `sp_UpdateReservationSchedule`.

## 12. Tiêu chí hoàn thành

Giai đoạn read-only hoàn thành khi:

- Có màn hình lịch đặt phòng.
- Hiển thị đúng phòng và reservation active.
- Có bộ lọc cơ bản.
- Click event xem được chi tiết.

Giai đoạn tương tác hoàn thành khi:

- Có kiểm tra trùng lịch ở server.
- Tạo nhanh không cho trùng.
- Kéo thả/resize không cho trùng.
- Không điều chỉnh được phiếu đã check-in/check-out.
- UI rollback nếu server từ chối cập nhật.

## 13. Danh sách file dự kiến

File mới:

- `Controllers/BookingCalendarController.cs`
- `Views/BookingCalendar/Index.cshtml`
- `wwwroot/js/booking-calendar.js`
- `wwwroot/css/booking-calendar.css`

File có thể sửa:

- `Views/Shared/_Layout.cshtml`
- `Data/DatabaseExtensions.cs`
- `docs/database/HotelManagement_new.sql`
- `Views/Reservation/Create.cshtml` nếu hỗ trợ query string tạo nhanh.

## 14. Gợi ý cập nhật BFD sau này

Nếu đưa calendar vào BFD, nên thêm dưới nhóm đặt phòng:

```text
** 2. Quản lý Đặt phòng
*** 2.1 Tiếp nhận đặt phòng
*** 2.2 Điều chỉnh / Hủy đặt phòng
*** 2.3 Tra cứu phòng trống & tính cọc gợi ý
*** 2.4 Xem lịch đặt phòng dạng calendar
*** 2.5 Điều chỉnh lịch đặt phòng bằng kéo thả
```

Không nên tách calendar thành module database riêng vì bản chất là view quản lý lịch đặt phòng.
