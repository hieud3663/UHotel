# Kế hoạch triển khai module Quản lý bảo trì và dọn phòng

> Quyết định kỹ thuật: module Bảo trì & Dọn phòng của ứng dụng MVC dùng Entity Framework Core trực tiếp cho các thao tác tạo, phân công, bắt đầu, hoàn thành và hủy công việc. Các stored procedure RoomTask trong SQL script chỉ giữ vai trò tham khảo/đối chiếu nghiệp vụ, không phải luồng chính đang được controller gọi.

## 1. Mục tiêu tài liệu

Tài liệu này mô tả kế hoạch triển khai chi tiết cho module **Quản lý bảo trì và dọn phòng** đã được bổ sung vào bộ sơ đồ UML của hệ thống HotelManagement.

Module này nhằm làm hệ thống gần hơn với nghiệp vụ khách sạn thực tế, nơi phòng không chỉ có các trạng thái đặt/nhận/trả phòng mà còn cần được quản lý theo hoạt động vận hành nội bộ như dọn phòng, kiểm tra sau checkout, xử lý hỏng hóc và bảo trì định kỳ.

## 2. Lý do cần bổ sung module

Trong các hệ thống quản lý khách sạn thực tế, sau khi khách trả phòng hoặc trong quá trình lưu trú, phòng có thể phát sinh nhiều trạng thái vận hành:

- Phòng vừa checkout cần được dọn trước khi bán tiếp.
- Phòng đang có khách nhưng phát sinh yêu cầu sửa chữa.
- Phòng hỏng thiết bị cần khóa bán tạm thời.
- Nhân viên buồng phòng/kỹ thuật cần được giao việc rõ ràng.
- Quản lý cần theo dõi lịch sử xử lý và hiệu suất vận hành.

Hệ thống hiện tại đã có các nghiệp vụ chính như đặt phòng, nhận phòng, trả phòng, dịch vụ phòng, hóa đơn và báo cáo. Tuy nhiên, nếu chưa có module vận hành phòng thì vòng đời phòng sau checkout và khi phát sinh sự cố chưa được quản lý đầy đủ.

Vì vậy, module này được bổ sung để hoàn thiện chu trình:

```text
Đặt phòng -> Nhận phòng -> Sử dụng dịch vụ -> Trả phòng -> Dọn phòng/Bảo trì -> Phòng sẵn sàng bán lại
```

## 3. Phạm vi module

### 3.1. Tên module

**Quản lý bảo trì và dọn phòng**

Tên tiếng Anh đề xuất trong mã nguồn:

- `RoomTask`
- `RoomTaskHistory`
- `RoomMaintenanceCleaningController`
- `RoomTaskStatus`
- `RoomTaskType`

### 3.2. Phạm vi chức năng

Module bao gồm các nhóm chức năng chính:

1. Tạo yêu cầu dọn phòng.
2. Tạo yêu cầu bảo trì.
3. Phân công nhân viên xử lý.
4. Cập nhật trạng thái công việc.
5. Cập nhật trạng thái phòng sau khi hoàn thành.
6. Theo dõi lịch sử xử lý.
7. Thống kê công việc vận hành.

### 3.3. Ngoài phạm vi giai đoạn đầu

Các chức năng sau có thể để lại cho phiên bản nâng cấp:

- Lịch bảo trì định kỳ tự động.
- Tích hợp thông báo email/SMS cho nhân viên.
- Ứng dụng mobile riêng cho nhân viên buồng phòng.
- Chấm công/định vị nhân viên khi thực hiện công việc.
- Tính lương theo số lượng công việc hoàn thành.

## 4. Các tác nhân tham gia

| Tác nhân | Vai trò |
|---|---|
| Lễ tân | Tạo yêu cầu dọn phòng sau checkout hoặc ghi nhận sự cố từ khách |
| Quản lý | Theo dõi, phân công, hủy hoặc xác nhận công việc |
| Nhân viên buồng phòng | Thực hiện công việc dọn phòng |
| Nhân viên kỹ thuật | Thực hiện công việc bảo trì/sửa chữa |
| Hệ thống | Tự động gợi ý/tạo công việc sau checkout hoặc cập nhật trạng thái phòng |

## 5. Liên kết với các sơ đồ UML đã bổ sung

Module này đã được biểu diễn bằng 4 loại sơ đồ PlantUML:

| Loại sơ đồ | File | Mục đích |
|---|---|---|
| Use Case | `docs/uml/plantuml/usecase/UseCase_RoomMaintenanceCleaning.puml` | Mô tả các chức năng và tác nhân của module |
| Activity | `docs/uml/plantuml/activity/Activity_RoomMaintenanceCleaning.puml` | Mô tả luồng xử lý công việc dọn phòng/bảo trì |
| Class | `docs/uml/plantuml/class/Class_RoomMaintenanceCleaning.puml` | Mô tả các lớp dự kiến cần bổ sung |
| Sequence | `docs/uml/plantuml/sequence/Sequence_RoomMaintenanceCleaning.puml` | Mô tả tương tác giữa màn hình, controller, database và nhân viên |

Ngoài ra, các sơ đồ tổng quan cũng đã được cập nhật để thể hiện module này trong toàn hệ thống:

- `docs/uml/plantuml/usecase/UseCase_TongQuan_HeThongQuanLyKhachSan.puml`
- `docs/uml/plantuml/activity/Activity_TongQuan_QuyTrinhKhachSan.puml`
- `docs/uml/plantuml/class/Class_TongQuan_DomainModel.puml`
- `docs/uml/plantuml/sequence/Sequence_TongQuan_ReservationToCheckout.puml`

## 6. Mô hình nghiệp vụ đề xuất

### 6.1. Loại công việc

Module nên quản lý công việc theo hai loại chính:

| Mã | Tên | Ý nghĩa |
|---|---|---|
| `CLEANING` | Dọn phòng | Công việc vệ sinh phòng sau checkout hoặc theo yêu cầu |
| `MAINTENANCE` | Bảo trì | Công việc sửa chữa, kiểm tra, khắc phục sự cố |

### 6.2. Trạng thái công việc

| Mã | Tên hiển thị | Ý nghĩa |
|---|---|---|
| `PENDING` | Chờ xử lý | Công việc mới tạo, chưa giao hoặc chưa bắt đầu |
| `ASSIGNED` | Đã phân công | Đã có nhân viên phụ trách |
| `IN_PROGRESS` | Đang thực hiện | Nhân viên đang xử lý |
| `COMPLETED` | Hoàn thành | Công việc đã hoàn tất |
| `CANCELLED` | Đã hủy | Công việc bị hủy do tạo nhầm hoặc không cần xử lý |

### 6.3. Mức độ ưu tiên

| Mã | Ý nghĩa | Ví dụ |
|---|---|---|
| `LOW` | Ưu tiên thấp | Dọn phòng thông thường |
| `NORMAL` | Bình thường | Dọn phòng sau checkout |
| `HIGH` | Cao | Phòng sắp có khách check-in |
| `URGENT` | Khẩn cấp | Hỏng điện, hỏng nước, khách đang lưu trú bị ảnh hưởng |

## 7. Quy tắc nghiệp vụ

### 7.1. Quy tắc tạo công việc

1. Khi phòng checkout xong, hệ thống nên tự tạo công việc dọn phòng.
2. Khi lễ tân hoặc quản lý phát hiện phòng hỏng, có thể tạo công việc bảo trì thủ công.
3. Một phòng có thể có nhiều công việc trong lịch sử, nhưng không nên có nhiều công việc `PENDING` hoặc `IN_PROGRESS` cùng loại tại cùng thời điểm.
4. Công việc bảo trì nghiêm trọng có thể làm phòng chuyển sang trạng thái không khả dụng.

### 7.2. Quy tắc phân công

1. Chỉ quản lý hoặc tài khoản có quyền phù hợp mới được phân công nhân viên.
2. Công việc dọn phòng nên giao cho nhân viên buồng phòng.
3. Công việc bảo trì nên giao cho nhân viên kỹ thuật.
4. Một công việc chỉ có một nhân viên chính phụ trách trong phiên bản đầu.

### 7.3. Quy tắc cập nhật trạng thái

1. Công việc mới tạo có trạng thái mặc định là `PENDING`.
2. Sau khi phân công, trạng thái chuyển sang `ASSIGNED`.
3. Khi nhân viên bắt đầu xử lý, trạng thái chuyển sang `IN_PROGRESS`.
4. Khi hoàn tất, trạng thái chuyển sang `COMPLETED`.
5. Công việc chỉ được hủy khi chưa hoàn thành.
6. Mỗi lần thay đổi trạng thái phải ghi nhận vào lịch sử.

### 7.4. Quy tắc cập nhật trạng thái phòng

Tùy theo kết quả công việc, trạng thái phòng sẽ thay đổi:

| Tình huống | Trạng thái phòng đề xuất |
|---|---|
| Checkout xong, chưa dọn | Cần dọn |
| Đang dọn | Đang dọn |
| Dọn xong và phòng không lỗi | Trống/Sẵn sàng |
| Phát hiện hỏng hóc | Bảo trì |
| Bảo trì xong | Cần kiểm tra hoặc Trống/Sẵn sàng |

Nếu hệ thống hiện tại chưa có đủ các trạng thái này trong bảng `Room`, có thể bổ sung thêm giá trị trạng thái hoặc chuẩn hóa bằng bảng danh mục trạng thái phòng.

## 8. Thiết kế dữ liệu đề xuất

### 8.1. Bảng `RoomTasks`

Bảng này lưu công việc dọn phòng/bảo trì.

| Trường | Kiểu dữ liệu đề xuất | Bắt buộc | Ghi chú |
|---|---|---|---|
| `RoomTaskId` | `string` hoặc `int` | Có | Khóa chính |
| `RoomId` | `string` | Có | Phòng cần xử lý |
| `TaskType` | `string` | Có | `CLEANING`, `MAINTENANCE` |
| `Title` | `nvarchar(200)` | Có | Tiêu đề công việc |
| `Description` | `nvarchar(max)` | Không | Mô tả chi tiết |
| `Priority` | `string` | Có | `LOW`, `NORMAL`, `HIGH`, `URGENT` |
| `Status` | `string` | Có | Trạng thái công việc |
| `AssignedEmployeeId` | `string` | Không | Nhân viên được giao |
| `CreatedByEmployeeId` | `string` | Có | Người tạo công việc |
| `CreatedAt` | `datetime` | Có | Thời điểm tạo |
| `DueAt` | `datetime` | Không | Hạn xử lý/SLA dự kiến |
| `SlaMinutes` | `int` | Không | Số phút SLA dự kiến |
| `StartedAt` | `datetime` | Không | Thời điểm bắt đầu |
| `CompletedAt` | `datetime` | Không | Thời điểm hoàn thành |
| `CancelledAt` | `datetime` | Không | Thời điểm hủy |
| `CancelReason` | `nvarchar(500)` | Không | Lý do hủy |
| `Note` | `nvarchar(max)` | Không | Ghi chú khi xử lý |

### 8.2. Bảng `RoomTaskHistories`

Bảng này lưu lịch sử thay đổi trạng thái công việc.

| Trường | Kiểu dữ liệu đề xuất | Bắt buộc | Ghi chú |
|---|---|---|---|
| `RoomTaskHistoryId` | `string` hoặc `int` | Có | Khóa chính |
| `RoomTaskId` | `string` hoặc `int` | Có | Khóa ngoại đến `RoomTasks` |
| `OldStatus` | `string` | Không | Trạng thái trước đó |
| `NewStatus` | `string` | Có | Trạng thái mới |
| `ChangedByEmployeeId` | `string` | Có | Người thực hiện thay đổi |
| `ChangedAt` | `datetime` | Có | Thời điểm thay đổi |
| `Note` | `nvarchar(max)` | Không | Ghi chú thay đổi |

### 8.3. Quan hệ dữ liệu

```text
Room 1 -- * RoomTask
Employee 1 -- * RoomTask (CreatedByEmployeeId)
Employee 1 -- * RoomTask (AssignedEmployeeId)
RoomTask 1 -- * RoomTaskHistory
Employee 1 -- * RoomTaskHistory (ChangedByEmployeeId)
```

## 9. Thiết kế Model trong ASP.NET Core MVC

### 9.1. Model `RoomTask`

Thuộc tính đề xuất:

```csharp
public class RoomTask
{
    public string RoomTaskId { get; set; }
    public string RoomId { get; set; }
    public string TaskType { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public string? AssignedEmployeeId { get; set; }
    public string CreatedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public int? SlaMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? Note { get; set; }

    public Room Room { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public Employee CreatedByEmployee { get; set; }
    public ICollection<RoomTaskHistory> Histories { get; set; }
}
```

### 9.2. Model `RoomTaskHistory`

```csharp
public class RoomTaskHistory
{
    public string RoomTaskHistoryId { get; set; }
    public string RoomTaskId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; }
    public string ChangedByEmployeeId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }

    public RoomTask RoomTask { get; set; }
    public Employee ChangedByEmployee { get; set; }
}
```

### 9.3. Enum hoặc hằng số

Có thể dùng enum nếu muốn kiểm soát chặt hơn:

```csharp
public enum RoomTaskType
{
    Cleaning,
    Maintenance
}

public enum RoomTaskStatus
{
    Pending,
    Assigned,
    InProgress,
    Completed,
    Cancelled
}

public enum RoomTaskPriority
{
    Low,
    Normal,
    High,
    Urgent
}
```

Nếu database hiện tại đang lưu nhiều mã dạng chuỗi, có thể dùng class hằng số thay vì enum để dễ tương thích.

## 10. Cập nhật `HotelManagementContext`

Cần bổ sung `DbSet`:

```csharp
public DbSet<RoomTask> RoomTasks { get; set; }
public DbSet<RoomTaskHistory> RoomTaskHistories { get; set; }
```

Cần cấu hình quan hệ:

1. `RoomTask` liên kết với `Room`.
2. `RoomTask` liên kết với `Employee` qua `AssignedEmployeeId`.
3. `RoomTask` liên kết với `Employee` qua `CreatedByEmployeeId`.
4. `RoomTaskHistory` liên kết với `RoomTask`.
5. `RoomTaskHistory` liên kết với `Employee` qua `ChangedByEmployeeId`.

Nếu các khóa chính hiện tại trong hệ thống dùng kiểu `string`, nên dùng `string` để thống nhất. Nếu các bảng mới dùng identity `int`, cần chú ý cách đặt khóa ngoại với các bảng hiện có.

## 11. Thiết kế Controller

### 11.1. Controller đề xuất

Tên controller:

```text
RoomMaintenanceCleaningController
```

Route mặc định:

```text
/RoomMaintenanceCleaning
```

### 11.2. Danh sách action đề xuất

| Action | Method | Mục đích |
|---|---|---|
| `Index` | GET | Danh sách công việc dọn phòng/bảo trì |
| `Details` | GET | Xem chi tiết công việc và lịch sử xử lý |
| `CreateCleaningTask` | GET/POST | Tạo yêu cầu dọn phòng |
| `CreateMaintenanceTask` | GET/POST | Tạo yêu cầu bảo trì |
| `Assign` | GET/POST | Phân công nhân viên xử lý |
| `Start` | POST | Chuyển trạng thái sang đang thực hiện |
| `Complete` | POST | Hoàn thành công việc |
| `Cancel` | POST | Hủy công việc |
| `History` | GET | Xem lịch sử công việc theo phòng |

### 11.3. Phân quyền action

| Action | Lễ tân | Quản lý | Buồng phòng | Kỹ thuật |
|---|---:|---:|---:|---:|
| `Index` | Có | Có | Có | Có |
| `Details` | Có | Có | Có | Có |
| `CreateCleaningTask` | Có | Có | Không | Không |
| `CreateMaintenanceTask` | Có | Có | Không | Có |
| `Assign` | Không | Có | Không | Không |
| `Start` | Không | Có | Có | Có |
| `Complete` | Không | Có | Có | Có |
| `Cancel` | Không | Có | Không | Không |
| `History` | Có | Có | Có | Có |

## 12. Thiết kế View

### 12.1. Thư mục View

Tạo thư mục:

```text
Views/RoomMaintenanceCleaning/
```

### 12.2. Danh sách view

| View | Mục đích |
|---|---|
| `Index.cshtml` | Danh sách công việc, bộ lọc theo phòng, loại, trạng thái, nhân viên |
| `Details.cshtml` | Chi tiết công việc và lịch sử thay đổi |
| `CreateCleaningTask.cshtml` | Form tạo công việc dọn phòng |
| `CreateMaintenanceTask.cshtml` | Form tạo công việc bảo trì |
| `Assign.cshtml` | Form phân công nhân viên |
| `History.cshtml` | Lịch sử công việc theo phòng hoặc theo nhân viên |

### 12.3. Bộ lọc nên có trong màn hình danh sách

- Tìm theo mã phòng/tên phòng.
- Lọc theo loại công việc.
- Lọc theo trạng thái.
- Lọc theo mức độ ưu tiên.
- Lọc theo nhân viên được giao.
- Lọc theo khoảng ngày tạo.
- Lọc công việc quá hạn hoặc công việc khẩn cấp.

### 12.4. Cột dữ liệu trong `Index`

| Cột | Nội dung |
|---|---|
| Mã công việc | `RoomTaskId` |
| Phòng | Thông tin phòng |
| Loại | Dọn phòng/Bảo trì |
| Tiêu đề | Mô tả ngắn |
| Ưu tiên | Low/Normal/High/Urgent |
| Trạng thái | Pending/Assigned/InProgress/Completed/Cancelled |
| Nhân viên phụ trách | Tên nhân viên |
| Ngày tạo | `CreatedAt` |
| Thao tác | Xem, phân công, bắt đầu, hoàn thành, hủy |

## 13. Luồng xử lý chính

### 13.1. Luồng tự động tạo công việc dọn phòng sau checkout

1. Nhân viên hoàn tất checkout.
2. Hệ thống cập nhật trạng thái phòng sang cần dọn.
3. Hệ thống tạo `RoomTask` loại `CLEANING`.
4. Công việc có trạng thái `PENDING`.
5. Quản lý phân công nhân viên buồng phòng.
6. Nhân viên bắt đầu dọn phòng.
7. Nhân viên hoàn thành công việc.
8. Hệ thống cập nhật phòng sang trạng thái sẵn sàng.
9. Hệ thống ghi lịch sử xử lý.

### 13.2. Luồng tạo yêu cầu bảo trì thủ công

1. Lễ tân/Quản lý/Nhân viên kỹ thuật phát hiện sự cố.
2. Người dùng chọn phòng và nhập mô tả sự cố.
3. Hệ thống tạo `RoomTask` loại `MAINTENANCE`.
4. Nếu sự cố nghiêm trọng, phòng chuyển sang trạng thái bảo trì.
5. Quản lý phân công nhân viên kỹ thuật.
6. Nhân viên kỹ thuật xử lý.
7. Nhân viên cập nhật kết quả.
8. Quản lý hoặc hệ thống xác nhận phòng có thể sử dụng lại.
9. Hệ thống ghi lịch sử.

### 13.3. Luồng hủy công việc

1. Quản lý mở chi tiết công việc.
2. Chọn hủy công việc.
3. Nhập lý do hủy.
4. Hệ thống kiểm tra công việc chưa hoàn thành.
5. Cập nhật trạng thái `CANCELLED`.
6. Ghi lịch sử thay đổi.

## 14. Tích hợp với module hiện có

### 14.1. Tích hợp với module phòng

Module này cần đọc và cập nhật dữ liệu phòng:

- Lấy danh sách phòng.
- Hiển thị trạng thái phòng.
- Cập nhật trạng thái phòng khi dọn/bảo trì.
- Ngăn đặt phòng nếu phòng đang bảo trì.

### 14.2. Tích hợp với module checkout

Sau khi checkout hoàn tất:

- Tạo công việc dọn phòng tự động.
- Cập nhật phòng sang trạng thái cần dọn.
- Gắn công việc với phòng vừa checkout.

### 14.3. Tích hợp với module nhân viên

Cần sử dụng thông tin nhân viên để:

- Hiển thị người tạo công việc.
- Hiển thị người được phân công.
- Lọc danh sách nhân viên theo bộ phận/vai trò.
- Ghi nhận người thay đổi trạng thái.

### 14.4. Tích hợp với báo cáo

Có thể bổ sung báo cáo:

- Số công việc dọn phòng theo ngày/tháng.
- Số công việc bảo trì theo ngày/tháng.
- Thời gian xử lý trung bình.
- Nhân viên xử lý nhiều công việc nhất.
- Phòng phát sinh bảo trì nhiều nhất.

## 15. Gợi ý thay đổi trạng thái phòng

Nếu hệ thống hiện tại đang dùng trạng thái phòng đơn giản, nên mở rộng thành các trạng thái sau:

| Trạng thái | Ý nghĩa |
|---|---|
| `AVAILABLE` | Phòng trống, có thể đặt |
| `RESERVED` | Đã được đặt trước |
| `OCCUPIED` | Đang có khách ở |
| `DIRTY` | Cần dọn |
| `CLEANING` | Đang dọn |
| `MAINTENANCE` | Đang bảo trì |
| `OUT_OF_SERVICE` | Tạm ngưng sử dụng |

Với đồ án, có thể giữ tên trạng thái tiếng Việt trong giao diện và dùng mã tiếng Anh trong database/code.

## 16. Validation dữ liệu

### 16.1. Khi tạo công việc

- Phòng không được rỗng.
- Loại công việc phải hợp lệ.
- Tiêu đề không được rỗng.
- Mức độ ưu tiên phải hợp lệ.
- Không tạo trùng công việc đang mở cùng loại cho cùng phòng.

### 16.2. Khi phân công

- Công việc phải tồn tại.
- Công việc chưa hoàn thành/chưa hủy.
- Nhân viên được giao phải tồn tại.
- Nhân viên phải phù hợp với loại công việc.

### 16.3. Khi hoàn thành

- Công việc phải ở trạng thái `ASSIGNED` hoặc `IN_PROGRESS`.
- Nếu là bảo trì, cần nhập ghi chú kết quả.
- Cập nhật `CompletedAt`.
- Ghi lịch sử trạng thái.

## 17. Gợi ý stored procedure hoặc service method

Nếu dự án tiếp tục theo hướng sử dụng stored procedure, có thể bổ sung:

| Tên | Mục đích |
|---|---|
| `sp_CreateRoomTask` | Tạo công việc mới |
| `sp_AssignRoomTask` | Phân công nhân viên |
| `sp_UpdateRoomTaskStatus` | Cập nhật trạng thái công việc |
| `sp_CancelRoomTask` | Hủy công việc |
| `sp_GetRoomTaskList` | Lấy danh sách công việc có lọc |
| `sp_GetRoomTaskHistory` | Lấy lịch sử công việc |

Nếu muốn đơn giản hơn, có thể xử lý bằng Entity Framework Core trực tiếp trong controller hoặc service layer.

## 18. Kế hoạch triển khai theo giai đoạn

### Giai đoạn 1: Chuẩn bị dữ liệu

- Tạo model `RoomTask`.
- Tạo model `RoomTaskHistory`.
- Cập nhật `HotelManagementContext`.
- Tạo migration hoặc script SQL.
- Seed dữ liệu trạng thái/loại công việc nếu cần.

### Giai đoạn 2: Xây dựng chức năng CRUD cơ bản

- Tạo controller `RoomMaintenanceCleaningController`.
- Tạo màn hình danh sách công việc.
- Tạo form tạo công việc dọn phòng.
- Tạo form tạo công việc bảo trì.
- Tạo màn hình chi tiết công việc.

### Giai đoạn 3: Phân công và cập nhật trạng thái

- Thêm chức năng phân công nhân viên.
- Thêm chức năng bắt đầu xử lý.
- Thêm chức năng hoàn thành.
- Thêm chức năng hủy.
- Ghi lịch sử thay đổi trạng thái.

### Giai đoạn 4: Tích hợp với checkout và trạng thái phòng

- Sau checkout, tự động tạo công việc dọn phòng.
- Cập nhật trạng thái phòng sang cần dọn.
- Sau khi hoàn thành dọn phòng, cập nhật phòng sang sẵn sàng.
- Với bảo trì, cập nhật phòng sang bảo trì hoặc sẵn sàng tùy kết quả.

### Giai đoạn 5: Báo cáo và tối ưu giao diện

- Thêm bộ lọc nâng cao.
- Thêm thống kê công việc theo loại/trạng thái.
- Thêm báo cáo hiệu suất nhân viên.
- Thêm cảnh báo công việc khẩn cấp hoặc quá hạn.

## 19. Checklist triển khai chi tiết

### 19.1. Database

- [ ] Tạo bảng `RoomTasks`.
- [ ] Tạo bảng `RoomTaskHistories`.
- [ ] Tạo khóa ngoại đến `Rooms`.
- [ ] Tạo khóa ngoại đến `Employees`.
- [ ] Thêm index cho `RoomId`.
- [ ] Thêm index cho `Status`.
- [ ] Thêm index cho `TaskType`.
- [ ] Thêm index cho `AssignedEmployeeId`.
- [ ] Cập nhật script database trong `docs/database` nếu cần.

### 19.2. Model

- [ ] Tạo `Models/RoomTask.cs`.
- [ ] Tạo `Models/RoomTaskHistory.cs`.
- [ ] Tạo enum hoặc class hằng số cho loại/trạng thái/ưu tiên.
- [ ] Cập nhật navigation property nếu cần.

### 19.3. DbContext

- [ ] Thêm `DbSet<RoomTask>`.
- [ ] Thêm `DbSet<RoomTaskHistory>`.
- [ ] Cấu hình quan hệ trong `OnModelCreating` nếu cần.
- [ ] Kiểm tra khóa chính/khóa ngoại.

### 19.4. Controller

- [ ] Tạo `RoomMaintenanceCleaningController`.
- [ ] Tạo action `Index`.
- [ ] Tạo action `Details`.
- [ ] Tạo action `CreateCleaningTask`.
- [ ] Tạo action `CreateMaintenanceTask`.
- [ ] Tạo action `Assign`.
- [ ] Tạo action `Start`.
- [ ] Tạo action `Complete`.
- [ ] Tạo action `Cancel`.
- [ ] Tạo action `History`.

### 19.5. View

- [ ] Tạo `Views/RoomMaintenanceCleaning/Index.cshtml`.
- [ ] Tạo `Views/RoomMaintenanceCleaning/Details.cshtml`.
- [ ] Tạo `Views/RoomMaintenanceCleaning/CreateCleaningTask.cshtml`.
- [ ] Tạo `Views/RoomMaintenanceCleaning/CreateMaintenanceTask.cshtml`.
- [ ] Tạo `Views/RoomMaintenanceCleaning/Assign.cshtml`.
- [ ] Tạo `Views/RoomMaintenanceCleaning/History.cshtml`.
- [ ] Thêm liên kết menu/sidebar nếu dự án có layout chung.

### 19.6. Tích hợp nghiệp vụ

- [ ] Tích hợp với checkout để tạo công việc dọn phòng.
- [ ] Tích hợp với phòng để cập nhật trạng thái.
- [ ] Tích hợp với nhân viên để phân công.
- [ ] Tích hợp với báo cáo nếu cần.

### 19.7. Kiểm thử

- [ ] Tạo công việc dọn phòng thành công.
- [ ] Tạo công việc bảo trì thành công.
- [ ] Không cho tạo trùng công việc đang mở cùng loại cho cùng phòng.
- [ ] Phân công nhân viên thành công.
- [ ] Bắt đầu xử lý thành công.
- [ ] Hoàn thành công việc thành công.
- [ ] Hủy công việc thành công.
- [ ] Lịch sử trạng thái được ghi đúng.
- [ ] Trạng thái phòng được cập nhật đúng sau khi hoàn thành.
- [ ] Bộ lọc danh sách hoạt động đúng.

## 20. Rủi ro và lưu ý khi triển khai

| Rủi ro | Ảnh hưởng | Cách xử lý |
|---|---|---|
| Trạng thái phòng hiện tại chưa đủ chi tiết | Khó phản ánh đúng nghiệp vụ dọn/bảo trì | Bổ sung trạng thái phòng hoặc bảng trạng thái riêng |
| Dữ liệu nhân viên chưa phân loại bộ phận | Khó lọc nhân viên buồng phòng/kỹ thuật | Bổ sung vai trò/bộ phận hoặc quy ước tạm thời |
| Tạo trùng công việc | Gây rối danh sách xử lý | Thêm validation kiểm tra công việc đang mở |
| Cập nhật trạng thái không ghi lịch sử | Mất khả năng truy vết | Bắt buộc ghi `RoomTaskHistory` khi đổi trạng thái |
| Tích hợp checkout sai thời điểm | Tạo công việc dọn phòng khi checkout chưa hoàn tất | Chỉ tạo task sau khi checkout/payment thành công |

## 21. Ưu tiên triển khai cho đồ án

Nếu cần triển khai nhanh nhưng vẫn đủ giá trị demo, nên ưu tiên:

1. Tạo bảng/model `RoomTask`.
2. Tạo danh sách công việc dọn phòng/bảo trì.
3. Tạo công việc thủ công.
4. Phân công nhân viên.
5. Cập nhật trạng thái công việc.
6. Ghi lịch sử xử lý.
7. Tự động tạo công việc dọn phòng sau checkout.

Các phần báo cáo nâng cao, thống kê hiệu suất và lịch bảo trì định kỳ có thể triển khai sau.

## 22. Tiêu chí hoàn thành module

Module được xem là hoàn thành ở mức cơ bản khi đáp ứng các tiêu chí sau:

- Có thể tạo công việc dọn phòng và bảo trì.
- Có thể phân công nhân viên phụ trách.
- Có thể cập nhật trạng thái công việc theo luồng hợp lệ.
- Có thể xem lịch sử thay đổi trạng thái.
- Phòng sau checkout có thể được đưa vào luồng dọn phòng.
- Phòng đang bảo trì không nên được bán/đặt như phòng trống.
- Giao diện đủ để lễ tân/quản lý/nhân viên theo dõi công việc.

## 23. Đề xuất thứ tự code thực tế

Thứ tự triển khai nên đi từ dữ liệu đến giao diện:

1. Tạo model và bảng dữ liệu.
2. Cập nhật `HotelManagementContext`.
3. Tạo controller với chức năng `Index` và `Details`.
4. Tạo chức năng tạo công việc.
5. Tạo chức năng phân công.
6. Tạo chức năng cập nhật trạng thái.
7. Ghi lịch sử thay đổi.
8. Tích hợp với checkout.
9. Cập nhật menu/layout.
10. Kiểm thử toàn bộ luồng.

## 24. Kết luận

Module **Quản lý bảo trì và dọn phòng** là phần mở rộng hợp lý cho hệ thống HotelManagement vì nó bổ sung lớp nghiệp vụ vận hành thực tế của khách sạn. Khi triển khai module này, hệ thống không chỉ quản lý đặt phòng và thanh toán mà còn quản lý được vòng đời sử dụng phòng sau checkout, tình trạng phòng, phân công nhân viên và lịch sử xử lý nội bộ.

Đây là module có giá trị cao khi trình bày đồ án vì dễ giải thích, dễ demo và thể hiện rõ sự khác biệt giữa hệ thống quản lý phòng đơn giản với hệ thống quản lý khách sạn thực tế.
