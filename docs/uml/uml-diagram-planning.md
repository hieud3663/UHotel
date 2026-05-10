# Phân tích nghiệp vụ và kế hoạch xây dựng sơ đồ UML cho HotelManagement

> Tài liệu này chỉ dùng để **mô tả luồng hiện tại và lập kế hoạch xây dựng sơ đồ**. Chưa tiến hành vẽ Activity Diagram, Class Diagram, Sequence Diagram hoặc Use Case Diagram trong tài liệu này.

## 1. Mục tiêu tài liệu

- Tổng hợp cấu trúc, module, actor, luồng nghiệp vụ và quan hệ dữ liệu của dự án HotelManagement.
- Xác định phạm vi cần biểu diễn khi xây dựng các sơ đồ:
  - Sơ đồ Activity tổng quát và chi tiết từng chức năng.
  - Sơ đồ Class.
  - Sơ đồ Sequence tổng quát và chi tiết từng chức năng.
  - Sơ đồ Use Case tổng quát và chi tiết từng chức năng.
- Đưa ra kế hoạch triển khai sơ đồ theo thứ tự ưu tiên, nguồn tham chiếu, đầu vào, đầu ra và các điểm cần xác minh.

## 2. Phạm vi khảo sát

### 2.1. Nguồn mã đã dùng để phân tích

- `Program.cs`: cấu hình MVC, DbContext, Session, Authentication/Authorization, HostedService.
- `Controllers/`: toàn bộ controller xử lý nghiệp vụ MVC.
- `Models/`: toàn bộ entity/domain model.
- `Data/HotelManagementContext.cs`: DbSet, quan hệ EF Core, computed column.
- `Data/DatabaseExtensions.cs`: các wrapper gọi stored procedure.
- `Services/RoomStatusUpdateService.cs`: service nền cập nhật trạng thái phòng.
- `docs/database/HotelManagement_new.sql`: schema database, constraints, trigger/function/procedure nghiệp vụ.
- `docs/report/description.txt`: mô tả nghiệp vụ tổng quát.
- `README.md`: mô tả tính năng, quy trình sử dụng và yêu cầu hệ thống.
- GitNexus MCP repo `HotelManagementWeb`: dùng để rà soát lại controller/action, model, DbSet, quan hệ kế thừa và các symbol nghiệp vụ chính.

### 2.2. Nguyên tắc phân tích

- Ưu tiên luồng thực tế trong controller/action và stored procedure được gọi từ C#.
- Tách rõ nghiệp vụ do MVC xử lý và nghiệp vụ nằm trong SQL stored procedure.
- Chỉ lập kế hoạch sơ đồ, chưa tạo nội dung PlantUML/Mermaid/XML cho sơ đồ.
- Với các điểm chưa thống nhất giữa model, controller và SQL, ghi nhận là điểm cần xác minh trước khi vẽ chi tiết.

### 2.3. Kết quả rà soát lại bằng GitNexus MCP

- Repo được GitNexus nhận diện là `HotelManagementWeb`, gồm 123 files, 937 nodes, 1656 relationships, 22 communities.
- GitNexus hiện báo 0 execution flows, vì vậy phần phân tích luồng vẫn cần dựa chủ yếu trên controller/action, `DatabaseExtensions` và stored procedure SQL thay vì process trace tự động.
- Graph xác nhận có 18 controller classes, bao gồm cả `HomeController` và `ServiceStatusController`.
- Graph xác nhận 17 model classes trong `Models/`, trong đó `ErrorViewModel` và `PagedList` là lớp hỗ trợ, không phải entity nghiệp vụ lõi.
- Graph xác nhận `HotelManagementContext` có 15 DbSet nghiệp vụ: Users, Employees, Customers, RoomCategories, Rooms, Pricings, ServiceCategories, HotelServices, ReservationForms, HistoryCheckins, HistoryCheckOuts, RoomChangeHistories, RoomUsageServices, Invoices, ConfirmationReceipts.
- Graph xác nhận các controller chính đều kế thừa `BaseController`; vì vậy Class Diagram kiến trúc có thể biểu diễn `BaseController` là lớp cha dùng chung cho controller.
- Graph xác nhận nhóm luồng lõi cần ưu tiên vẫn là `ReservationController`, `CheckInController`, `RoomServiceController`, `CheckOutController`, `InvoiceController`, `ConfirmationReceiptController` và `DatabaseExtensions`.

## 3. Tổng quan kiến trúc hệ thống

Dự án là hệ thống quản lý khách sạn sử dụng kiến trúc ASP.NET Core MVC:

- **View**: Razor Pages trong thư mục `Views/`, cung cấp giao diện thao tác.
- **Controller**: nhận request, kiểm tra session, gọi EF Core hoặc stored procedure, trả View/Redirect/JSON.
- **Model**: entity nghiệp vụ ánh xạ database.
- **Data layer**:
  - `HotelManagementContext`: quản lý DbSet và quan hệ EF Core.
  - `DatabaseExtensions`: đóng vai trò service/helper để gọi stored procedure.
- **SQL Server**: chứa database schema, constraint, function sinh ID, stored procedure cho các nghiệp vụ phức tạp.
- **Background service**: tự động cập nhật trạng thái phòng sang `RESERVED` trước giờ check-in.

## 4. Actor và vai trò nghiệp vụ

| Actor | Mô tả | Chức năng liên quan |
|---|---|---|
| Khách hàng | Người đặt phòng, nhận phòng, sử dụng dịch vụ, trả phòng, thanh toán. Không đăng nhập trực tiếp trong hệ thống hiện tại. | Cung cấp thông tin, gắn với reservation, check-in, dịch vụ, invoice, receipt. |
| Nhân viên/Lễ tân (`EMPLOYEE`) | Người vận hành nghiệp vụ hằng ngày. | Quản lý khách hàng, đặt phòng, check-in, thêm dịch vụ, check-out, xác nhận thanh toán, xem hóa đơn/phiếu. |
| Quản lý (`MANAGER`) | Người quản lý nghiệp vụ và tài khoản nhân viên. | Có các chức năng vận hành; thêm quản lý tài khoản nhân viên/quản lý. |
| Admin (`ADMIN`) | Vai trò hệ thống cao nhất. | Quản trị tài khoản; có quyền truy cập module tài khoản theo logic hiện tại. |
| Hệ thống thanh toán/chuyển khoản | Hệ thống ngoài gửi webhook xác nhận chuyển khoản. | Gọi endpoint xác nhận payment theo invoice. |
| Background Service | Tác nhân tự động nội bộ theo thời gian. | Gọi stored procedure cập nhật phòng sang `RESERVED`. |
| SQL Server/Stored Procedure | Thành phần xử lý nghiệp vụ lõi. | Sinh ID, tạo đặt phòng, check-in, check-out, invoice, payment, dịch vụ phòng. |

## 5. Danh sách module/chức năng chính

| Nhóm module | Controller/Thành phần | Chức năng chính |
|---|---|---|
| Xác thực | `AuthController`, `BaseController`, `Program.cs` | Đăng nhập, đăng xuất, session, kiểm tra quyền cơ bản. |
| Dashboard | `DashboardController` | Thống kê phòng, khách hàng, nhân viên, đặt phòng hôm nay, doanh thu tháng. |
| Khách hàng | `CustomerController` | CRUD, tạo nhanh AJAX, soft delete. |
| Nhân viên | `EmployeeController` | CRUD, thêm/sửa qua stored procedure, soft delete. |
| Tài khoản | `ReceptionAccountController` | Tạo user, phân quyền, khóa/mở khóa, reset mật khẩu. |
| Loại phòng & giá | `RoomCategoryController` | CRUD loại phòng, giá theo giờ/ngày, bật/tắt active. |
| Phòng | `RoomController` | CRUD phòng, trạng thái phòng, API phòng trống/giá/phòng kèm reservation. |
| Dịch vụ khách sạn | `HotelServiceController` | CRUD dịch vụ, danh mục dịch vụ, bật/tắt active. |
| Đặt phòng | `ReservationController` | Tạo phiếu đặt, tính cọc, xem chi tiết, hủy/xóa mềm. |
| Check-in | `CheckInController` | Danh sách chờ nhận phòng, check-in, danh sách đang ở. |
| Dịch vụ phòng | `RoomServiceController` | Thêm/cập nhật/xóa dịch vụ sử dụng theo reservation. |
| Check-out/Thanh toán | `CheckOutController` | Tính tiền, đổi thuê giờ sang ngày, checkout rồi thanh toán, thanh toán trước rồi checkout, webhook. |
| Hóa đơn | `InvoiceController` | Danh sách, lọc, xem/in hóa đơn. |
| Phiếu xác nhận | `ConfirmationReceiptController` | Xem/in phiếu đặt phòng, check-in, check-out. |
| Báo cáo | `ReportController` | Doanh thu, công suất phòng, hiệu suất nhân viên, dữ liệu biểu đồ. |
| Trang tĩnh/lỗi | `HomeController` | Trang home/privacy/error mặc định, ít liên quan nghiệp vụ chính. |
| Trạng thái phòng tự động | `RoomStatusUpdateService`, `ServiceStatusController` | Cập nhật tự động/thủ công trạng thái phòng sắp check-in. |

## 6. Luồng nghiệp vụ tổng quát

### 6.1. Luồng end-to-end chính

1. Người dùng đăng nhập.
2. Hệ thống xác thực tài khoản active bằng BCrypt password hash.
3. Hệ thống lưu session: `UserID`, `Username`, `Role`, `EmployeeID`.
4. Người dùng truy cập dashboard hoặc các module nghiệp vụ.
5. Nhân viên/quản lý chuẩn bị dữ liệu nền:
   - Khách hàng.
   - Nhân viên và tài khoản.
   - Loại phòng, bảng giá, phòng.
   - Danh mục dịch vụ, dịch vụ khách sạn.
6. Nhân viên tiếp nhận đặt phòng:
   - Chọn khách hàng.
   - Chọn thời gian check-in/check-out.
   - Chọn loại phòng/phòng.
   - Chọn đơn vị giá `HOUR` hoặc `DAY`.
   - Nhập hoặc tính gợi ý tiền cọc.
   - Hệ thống gọi stored procedure tạo reservation.
   - Hệ thống tạo phiếu xác nhận đặt phòng.
7. Trước giờ nhận phòng:
   - Background service gọi stored procedure chuyển phòng sang `RESERVED` nếu đủ điều kiện.
8. Khi khách đến:
   - Nhân viên chọn reservation hợp lệ.
   - Hệ thống check-in qua stored procedure.
   - Tạo lịch sử check-in.
   - Cập nhật phòng sang `ON_USE`.
   - Tạo phiếu xác nhận check-in.
9. Trong thời gian lưu trú:
   - Nhân viên ghi nhận dịch vụ sử dụng.
   - Nếu dịch vụ đã tồn tại thì cập nhật/cộng dồn số lượng.
   - Hệ thống tính thành tiền dịch vụ.
10. Khi khách trả phòng:
    - Hệ thống hiển thị chi tiết chi phí.
    - Tính tiền phòng, tiền dịch vụ, VAT, tiền cọc, số tiền cần thanh toán.
    - Có thể chuyển thuê giờ sang thuê ngày nếu thỏa điều kiện.
11. Hệ thống xử lý một trong các nhánh thanh toán:
    - Checkout trước, thanh toán sau.
    - Thanh toán trước, checkout sau.
    - Webhook xác nhận chuyển khoản.
12. Sau khi hoàn tất:
    - Tạo/cập nhật hóa đơn.
    - Tạo lịch sử check-out.
    - Giải phóng phòng.
    - In hóa đơn hoặc phiếu xác nhận check-out.
13. Quản lý xem báo cáo doanh thu, công suất phòng, hiệu suất nhân viên.

### 6.2. Trạng thái chính cần thể hiện

#### Phòng (`Room.RoomStatus`)

- `AVAILABLE`: phòng trống.
- `RESERVED`: phòng đã được đặt và gần đến giờ check-in.
- `ON_USE`: phòng đang có khách lưu trú.
- `UNAVAILABLE`: phòng không khả dụng/bảo trì.
- `OVERDUE`: phòng quá hạn trả.

#### Kích hoạt dữ liệu (`IsActivate`)

- `ACTIVATE`: bản ghi còn hiệu lực.
- `DEACTIVATE`: bản ghi đã bị vô hiệu hóa/hủy mềm.

#### Hóa đơn (`Invoice`)

- `isPaid = false`: chưa thanh toán.
- `isPaid = true`: đã thanh toán.
- `checkoutType = CHECKOUT_THEN_PAY`: trả phòng rồi thanh toán.
- `checkoutType = PAY_THEN_CHECKOUT`: thanh toán trước rồi trả phòng thực tế sau.

## 7. Luồng chi tiết theo chức năng

### 7.1. Xác thực

- GET Login:
  - Nếu đã có session thì chuyển dashboard.
  - Nếu chưa có session thì hiển thị form đăng nhập.
- POST Login:
  - Kiểm tra username/password.
  - Tìm user active.
  - Verify password bằng BCrypt.
  - Lưu session.
  - Chuyển dashboard.
- Logout:
  - Xóa session.
  - Chuyển về login.

### 7.2. Dashboard

- Kiểm tra đăng nhập.
- Thống kê:
  - Tổng phòng.
  - Phòng trống.
  - Phòng đang sử dụng.
  - Tổng khách hàng active.
  - Tổng nhân viên active.
  - Đặt phòng hôm nay.
  - Doanh thu tháng hiện tại.

### 7.3. Quản lý khách hàng

- Danh sách khách hàng active, phân trang.
- Xem chi tiết.
- Tạo khách hàng:
  - Kiểm tra trùng số điện thoại/CCCD/email.
  - Sinh mã khách hàng.
  - Lưu active.
- Sửa thông tin khách hàng.
- Xóa mềm bằng `DEACTIVATE`.
- Tạo nhanh qua AJAX khi đặt phòng.

### 7.4. Quản lý nhân viên

- Danh sách nhân viên active.
- Xem chi tiết.
- Tạo nhân viên:
  - Sinh mã nhân viên.
  - Gọi stored procedure insert.
- Sửa nhân viên:
  - Gọi stored procedure update.
- Xóa mềm bằng `DEACTIVATE`.

### 7.5. Quản lý tài khoản

- Chỉ cho `MANAGER` hoặc `ADMIN` theo logic controller.
- Danh sách user không phải admin.
- Tạo tài khoản:
  - Chọn employee active.
  - Chọn role.
  - Kiểm tra username trùng.
  - Hash mật khẩu.
  - Lưu user.
- Khóa/mở khóa user.
- Reset mật khẩu.

### 7.6. Quản lý loại phòng và giá

- Danh sách loại phòng kèm giá và phòng.
- Tạo loại phòng:
  - Sinh mã loại phòng.
  - Tạo giá theo giờ/ngày nếu nhập.
- Sửa loại phòng:
  - Cập nhật thông tin loại phòng.
  - Cập nhật hoặc thêm giá theo `HOUR`/`DAY`.
- Xóa loại phòng:
  - Chỉ xóa nếu chưa có phòng phụ thuộc.
  - Xóa pricing liên quan nếu đủ điều kiện.
- Bật/tắt active.

### 7.7. Quản lý phòng

- Danh sách phòng active, kèm loại phòng, giá, khách đang/sắp ở.
- Xem chi tiết phòng.
- Tạo phòng:
  - Kiểm tra mã phòng trùng.
  - Kiểm tra loại phòng tồn tại.
  - Tạo phòng trạng thái `AVAILABLE`.
- Sửa phòng:
  - Cập nhật loại phòng, trạng thái, active.
- Xóa mềm phòng.
- API hỗ trợ đặt phòng:
  - Lấy phòng trống theo loại và khoảng ngày.
  - Lấy bảng giá theo loại phòng.
  - Lấy phòng trống kèm thông tin giá.
  - Lấy phòng kèm reservation sắp tới.

### 7.8. Quản lý dịch vụ khách sạn

- Danh sách dịch vụ, tìm kiếm/lọc theo danh mục.
- Xem chi tiết dịch vụ.
- Tạo dịch vụ:
  - Sinh mã dịch vụ.
  - Gắn danh mục.
  - Lưu giá và trạng thái active.
- Sửa dịch vụ.
- Xóa dịch vụ:
  - Chỉ xóa thật nếu chưa phát sinh trong `RoomUsageService`.
- Bật/tắt active.
- Tạo danh mục dịch vụ qua AJAX.

### 7.9. Đặt phòng

- Danh sách reservation active.
- Xem chi tiết reservation kèm khách hàng, phòng, check-in/out, dịch vụ, invoice.
- Tạo đặt phòng:
  - Load khách hàng, loại phòng, phòng, giá.
  - Validate ngày check-in/check-out.
  - Lấy `EmployeeID` từ session.
  - Gọi `sp_CreateReservation`.
  - Tạo phiếu xác nhận `RESERVATION`.
- Hủy đặt phòng:
  - Không cho hủy nếu đã check-in.
  - Set reservation `DEACTIVATE`.
  - Set phòng về `AVAILABLE` nếu phù hợp.
- Tính cọc gợi ý:
  - Theo giá ngày, số ngày, tỷ lệ 30%.
- Xóa mềm reservation nếu chưa check-in.

### 7.10. Check-in

- Danh sách reservation active chưa có `HistoryCheckin`.
- Đánh dấu reservation quá hạn check-in nếu quá thời gian dự kiến.
- Thực hiện check-in:
  - Kiểm tra reservation tồn tại và active.
  - Lấy employee từ session.
  - Gọi `sp_QuickCheckin`.
  - Tạo phiếu xác nhận `CHECKIN`.
- Xóa/hủy mềm reservation chưa check-in từ màn check-in.
- Danh sách phòng/reservation đã check-in nhưng chưa check-out.

### 7.11. Dịch vụ phòng

- Mở màn hình dịch vụ theo reservation.
- Load dịch vụ đã dùng, danh mục dịch vụ, dịch vụ active.
- Thêm dịch vụ:
  - Validate service và quantity.
  - Gọi `sp_AddRoomService`.
  - Nếu đã tồn tại thì cập nhật/cộng dồn.
  - Nếu chưa có thì thêm mới.
- Cập nhật số lượng dịch vụ.
- Xóa dịch vụ qua `sp_DeleteRoomService`.
- API lấy giá dịch vụ và dịch vụ theo danh mục.

### 7.12. Check-out và thanh toán

- Danh sách reservation đã check-in chưa check-out.
- Xem chi tiết check-out:
  - Load reservation, customer, room, pricing, check-in, service usage.
  - Tính tiền phòng theo thực tế đến hiện tại.
  - Tính tiền theo thời gian dự kiến cho nhánh thanh toán trước.
  - Tính dịch vụ, VAT, cọc, số cần thanh toán.
  - Xác định có thể đổi thuê giờ sang thuê ngày không.
- Đổi thuê giờ sang thuê ngày:
  - Chỉ khi thuê `HOUR`, ở quá ngưỡng và chưa có invoice.
  - Gọi `sp_ConvertHourlyToDaily`.
- Nhánh 1: checkout rồi thanh toán:
  - Gọi `sp_CreateInvoice_CheckoutThenPay`.
  - Tạo check-out và invoice chưa paid.
  - Người dùng vào trang thanh toán.
  - Gọi `sp_ConfirmPayment` để xác nhận.
- Nhánh 2: thanh toán trước rồi checkout:
  - Gọi `sp_CreateInvoice_PayThenCheckout`.
  - Tạo invoice đã paid theo thời gian dự kiến.
  - Khi khách trả phòng thực tế, gọi `sp_ActualCheckout_AfterPrepayment`.
  - Nếu phát sinh phụ thu thì chuyển sang payment.
- Webhook chuyển khoản:
  - Kiểm tra API key.
  - Parse invoice code từ nội dung chuyển khoản.
  - Kiểm tra amount.
  - Gọi `sp_ConfirmPayment` nếu hợp lệ.
- Legacy checkout:
  - `ProcessCheckOut` gọi `sp_QuickCheckout`.
  - Cần quyết định có đưa vào sơ đồ chính hay chỉ ghi là luồng cũ/phụ.

### 7.13. Hóa đơn

- Danh sách hóa đơn.
- Tìm kiếm theo invoice, phòng, khách hàng, reservation.
- Lọc paid/unpaid.
- Xem chi tiết invoice kèm reservation, customer, room, dịch vụ, check-in/out.
- In hóa đơn.

### 7.14. Phiếu xác nhận

- Danh sách phiếu, lọc theo loại.
- Xem chi tiết phiếu.
- In/tạo phiếu đặt phòng `RESERVATION`.
- In/tạo phiếu check-in `CHECKIN`.
- In/tạo phiếu check-out `CHECKOUT`.
- Cần xác minh tránh tạo trùng khi bấm in nhiều lần.

### 7.15. Báo cáo

- Dashboard báo cáo tổng hợp.
- Báo cáo doanh thu theo ngày/khoảng ngày/loại phòng.
- Báo cáo công suất phòng.
- Báo cáo hiệu suất nhân viên.
- API dữ liệu biểu đồ doanh thu, occupancy, booking trend, employee performance.

### 7.16. Cập nhật trạng thái phòng tự động

- Hosted service chạy sau khi ứng dụng khởi động.
- Lặp định kỳ 5 phút theo code hiện tại.
- Gọi `sp_UpdateRoomStatusToReserved`.
- Ghi log số phòng đang `RESERVED`.
- Có controller hỗ trợ cập nhật thủ công và xem reservation sắp đến.

## 8. Thực thể/domain model dự kiến cho Class Diagram

### 8.1. Nhóm tài khoản và nhân sự

- `User`
  - Thuộc tính chính: `UserID`, `EmployeeID`, `Username`, `PasswordHash`, `Role`, `IsActivate`.
  - Quan hệ: gắn với `Employee`.
- `Employee`
  - Thuộc tính chính: `EmployeeID`, `FullName`, `PhoneNumber`, `Email`, `Address`, `Gender`, `IDCardNumber`, `Dob`, `Position`, `IsActivate`.
  - Quan hệ:
    - Một employee có nhiều reservation.
    - Một employee có nhiều check-in/check-out.
    - Một employee có nhiều room usage service.
    - Một employee có thể có user account.

### 8.2. Nhóm khách hàng và đặt phòng

- `Customer`
  - Một customer có nhiều reservation.
- `ReservationForm`
  - Trung tâm nghiệp vụ.
  - Quan hệ:
    - Thuộc một customer.
    - Thuộc một room.
    - Được tạo bởi một employee.
    - Có tối đa một `HistoryCheckin`.
    - Có tối đa một `HistoryCheckOut`.
    - Có nhiều `RoomUsageService`.
    - Có nhiều `Invoice`.
    - Có nhiều `RoomChangeHistory`.
    - Có nhiều `ConfirmationReceipt`.

### 8.3. Nhóm phòng và giá

- `RoomCategory`
  - Một loại phòng có nhiều phòng.
  - Một loại phòng có nhiều giá.
- `Pricing`
  - Thuộc một loại phòng.
  - Phân biệt `HOUR` và `DAY`.
- `Room`
  - Thuộc một loại phòng.
  - Có nhiều reservation.
  - Có nhiều lịch sử đổi/sử dụng phòng.

### 8.4. Nhóm dịch vụ

- `ServiceCategory`
  - Một danh mục có nhiều hotel service.
- `HotelService`
  - Thuộc một danh mục dịch vụ.
  - Có nhiều `RoomUsageService`.
- `RoomUsageService`
  - Thuộc một reservation.
  - Thuộc một hotel service.
  - Gắn với employee ghi nhận.
  - `TotalPrice = Quantity * UnitPrice`.

### 8.5. Nhóm lưu trú, hóa đơn và phiếu

- `HistoryCheckin`
  - Một reservation chỉ có một check-in.
  - Có employee thực hiện.
- `HistoryCheckOut`
  - Một reservation chỉ có một check-out.
  - Có employee thực hiện.
  - Có thể gắn invoice.
- `Invoice`
  - Thuộc một reservation.
  - Gồm tiền phòng, tiền dịch vụ, thuế, cọc, tổng tiền, trạng thái thanh toán.
- `ConfirmationReceipt`
  - Snapshot thông tin đặt phòng/check-in/check-out.
  - Có thể gắn reservation và invoice.
- `RoomChangeHistory`
  - Ghi lịch sử đổi phòng/sử dụng phòng theo reservation.

## 9. Kế hoạch xây dựng Activity Diagram

### 9.1. Mục tiêu Activity Diagram

- Mô tả luồng xử lý nghiệp vụ theo góc nhìn hành động.
- Tách rõ actor, hệ thống MVC, database/stored procedure và hệ thống ngoài nếu có.
- Thể hiện decision/branch, validation, success/failure và kết quả cập nhật dữ liệu.

### 9.2. Activity Diagram tổng quát

Tên dự kiến: `Activity_TongQuan_QuyTrinhKhachSan`

Phạm vi:

1. Đăng nhập.
2. Chuẩn bị dữ liệu nền.
3. Tạo đặt phòng.
4. Cập nhật trạng thái phòng trước check-in.
5. Check-in.
6. Ghi nhận dịch vụ.
7. Check-out và thanh toán.
8. In hóa đơn/phiếu.
9. Báo cáo.

Swimlane dự kiến:

- Khách hàng.
- Nhân viên/Lễ tân.
- Hệ thống MVC.
- SQL Server/Stored Procedure.
- Background Service.
- Cổng thanh toán/Webhook.

Các decision quan trọng:

- Đăng nhập hợp lệ?
- Phòng còn trống trong khoảng ngày?
- Reservation đã check-in chưa?
- Có phát sinh dịch vụ không?
- Chọn checkout rồi thanh toán hay thanh toán trước?
- Thanh toán hợp lệ không?
- Có phụ thu khi checkout thực tế không?

### 9.3. Activity Diagram chi tiết từng chức năng

| STT | Tên sơ đồ dự kiến | Phạm vi chính | Nguồn tham chiếu | Mức ưu tiên |
|---:|---|---|---|---|
| 1 | `Activity_Auth_LoginLogout` | Login, logout, session | `AuthController` | Cao |
| 2 | `Activity_Customer_CRUD` | Tạo/sửa/xóa mềm khách hàng, tạo nhanh | `CustomerController` | Trung bình |
| 3 | `Activity_Employee_AccountManagement` | Nhân viên và tài khoản | `EmployeeController`, `ReceptionAccountController` | Trung bình |
| 4 | `Activity_RoomCategory_Pricing` | Loại phòng và bảng giá | `RoomCategoryController` | Trung bình |
| 5 | `Activity_Room_ManagementAvailability` | CRUD phòng, lấy phòng trống | `RoomController` | Cao |
| 6 | `Activity_HotelService_Management` | Dịch vụ và danh mục dịch vụ | `HotelServiceController` | Trung bình |
| 7 | `Activity_Reservation_CreateCancel` | Tạo/hủy đặt phòng, tính cọc | `ReservationController`, `sp_CreateReservation` | Rất cao |
| 8 | `Activity_CheckIn` | Nhận phòng | `CheckInController`, `sp_QuickCheckin` | Rất cao |
| 9 | `Activity_RoomService_Usage` | Thêm/cập nhật/xóa dịch vụ phòng | `RoomServiceController`, `sp_AddRoomService`, `sp_DeleteRoomService` | Cao |
| 10 | `Activity_CheckOut_CheckoutThenPay` | Checkout trước, thanh toán sau | `CheckOutController`, `sp_CreateInvoice_CheckoutThenPay`, `sp_ConfirmPayment` | Rất cao |
| 11 | `Activity_CheckOut_PayThenCheckout` | Thanh toán trước, checkout sau | `CheckOutController`, `sp_CreateInvoice_PayThenCheckout`, `sp_ActualCheckout_AfterPrepayment` | Rất cao |
| 12 | `Activity_CheckOut_WebhookPayment` | Xác nhận chuyển khoản qua webhook | `CheckOutController.WebHookConfirmPayment` | Cao |
| 13 | `Activity_Invoice_Receipt_Printing` | Xem/in hóa đơn và phiếu | `InvoiceController`, `ConfirmationReceiptController` | Trung bình |
| 14 | `Activity_Report` | Báo cáo thống kê | `ReportController` | Thấp-Trung bình |
| 15 | `Activity_Background_RoomStatusUpdate` | Cập nhật trạng thái phòng tự động | `RoomStatusUpdateService`, `ServiceStatusController` | Cao |

### 9.4. Checklist trước khi vẽ Activity Diagram

- Xác định rõ action bắt đầu và kết thúc của từng chức năng.
- Đối chiếu controller với stored procedure tương ứng.
- Ghi rõ nhánh lỗi: validation fail, not found, unauthorized, duplicate, invalid state.
- Phân biệt soft delete và hard delete.
- Phân biệt thao tác của nhân viên và xử lý tự động của hệ thống.

## 10. Kế hoạch xây dựng Class Diagram

### 10.1. Mục tiêu Class Diagram

- Thể hiện cấu trúc domain model và quan hệ dữ liệu chính.
- Có thể bổ sung các lớp controller/service/data access ở mức kiến trúc nếu cần.
- Không đưa toàn bộ View vào class diagram nghiệp vụ, trừ khi cần biểu diễn MVC package.

### 10.2. Phạm vi Class Diagram tổng quát

Tên dự kiến: `Class_TongQuan_DomainModel`

Các nhóm class:

1. Tài khoản - nhân sự:
   - `User`
   - `Employee`
2. Khách hàng - đặt phòng:
   - `Customer`
   - `ReservationForm`
3. Phòng - loại phòng - giá:
   - `Room`
   - `RoomCategory`
   - `Pricing`
4. Dịch vụ:
   - `ServiceCategory`
   - `HotelService`
   - `RoomUsageService`
5. Lưu trú - hóa đơn - phiếu:
   - `HistoryCheckin`
   - `HistoryCheckOut`
   - `Invoice`
   - `ConfirmationReceipt`
   - `RoomChangeHistory`
6. Hạ tầng:
   - `BaseController`
   - Các controller chính: `AuthController`, `ReservationController`, `CheckInController`, `RoomServiceController`, `CheckOutController`, `InvoiceController`, `ConfirmationReceiptController`, `ReportController`, `ServiceStatusController`.
   - `HotelManagementContext`
   - `DatabaseExtensions`
   - `RoomStatusUpdateService`

### 10.3. Quan hệ cần thể hiện

| Quan hệ | Bội số dự kiến |
|---|---|
| `Employee` - `User` | 1 - 0..1 hoặc 1 - 0..n, cần xác minh theo nghiệp vụ |
| `Customer` - `ReservationForm` | 1 - 0..n |
| `Employee` - `ReservationForm` | 1 - 0..n |
| `Room` - `ReservationForm` | 1 - 0..n |
| `RoomCategory` - `Room` | 1 - 0..n |
| `RoomCategory` - `Pricing` | 1 - 0..n |
| `ServiceCategory` - `HotelService` | 1 - 0..n |
| `ReservationForm` - `HistoryCheckin` | 1 - 0..1 |
| `ReservationForm` - `HistoryCheckOut` | 1 - 0..1 |
| `ReservationForm` - `RoomUsageService` | 1 - 0..n |
| `HotelService` - `RoomUsageService` | 1 - 0..n |
| `ReservationForm` - `Invoice` | 1 - 0..n |
| `HistoryCheckOut` - `Invoice` | 0..1 - 0..1, cần xác minh nghiệp vụ |
| `ReservationForm` - `ConfirmationReceipt` | 1 - 0..n |
| `Invoice` - `ConfirmationReceipt` | 0..1 - 0..n |
| `ReservationForm` - `RoomChangeHistory` | 1 - 0..n |

### 10.4. Class Diagram chi tiết/biến thể nên chuẩn bị

| Sơ đồ | Nội dung | Mục đích |
|---|---|---|
| `Class_DomainModel_Core` | Entity chính và quan hệ | Dùng cho báo cáo/phân tích dữ liệu |
| `Class_Reservation_CheckIn_CheckOut` | Reservation, Room, Customer, History, Invoice | Tập trung quy trình lõi |
| `Class_ServiceUsage` | ServiceCategory, HotelService, RoomUsageService | Tập trung dịch vụ phát sinh |
| `Class_Account_Authorization` | User, Employee, role/session liên quan | Tập trung tài khoản/phân quyền |
| `Class_Infrastructure_MVC` | Controller, DbContext, DatabaseExtensions, HostedService | Tập trung kiến trúc triển khai |

### 10.5. Checklist trước khi vẽ Class Diagram

- Đối chiếu model C# với schema SQL vì có thể lệch field/constraint.
- Xác định có đưa thuộc tính đầy đủ hay chỉ thuộc tính quan trọng.
- Chuẩn hóa tên class/property theo C# model hay theo database column.
- Xác minh `User.Role` vì SQL hiện có check `ADMIN`, `EMPLOYEE`, trong khi controller dùng thêm `MANAGER`.
- Xác minh `ConfirmationReceipt.receiptType` vì SQL ban đầu check `RESERVATION`, `CHECKIN`, nhưng controller có dùng `CHECKOUT`.

## 11. Kế hoạch xây dựng Sequence Diagram

### 11.1. Mục tiêu Sequence Diagram

- Mô tả tương tác theo thời gian giữa actor, controller, DbContext, stored procedure, entity/database và hệ thống ngoài.
- Tách luồng happy path và alternative/error path.
- Với các luồng dùng stored procedure, biểu diễn rõ thông điệp từ controller tới `DatabaseExtensions` và SQL procedure.

### 11.2. Sequence Diagram tổng quát

Tên dự kiến: `Sequence_TongQuan_ReservationToCheckout`

Participants dự kiến:

- `NhanVien/LeTan`
- `Browser/View`
- `AuthController`
- `ReservationController`
- `CheckInController`
- `RoomServiceController`
- `CheckOutController`
- `InvoiceController`
- `ConfirmationReceiptController`
- `HotelManagementContext`
- `DatabaseExtensions`
- `SQL Server/Stored Procedures`
- `Payment Webhook` nếu có

Thông điệp chính:

1. Login.
2. Tạo reservation.
3. Tạo confirmation receipt.
4. Check-in.
5. Thêm dịch vụ.
6. Xem chi tiết checkout.
7. Tạo invoice/check-out.
8. Xác nhận thanh toán.
9. In hóa đơn/phiếu.

### 11.3. Sequence Diagram chi tiết từng chức năng

| STT | Tên sơ đồ dự kiến | Participants chính | Luồng cần thể hiện |
|---:|---|---|---|
| 1 | `Sequence_Auth_Login` | User, View, AuthController, DbContext, User table | Submit login, verify BCrypt, set session |
| 2 | `Sequence_Customer_QuickCreate` | Employee, View, CustomerController, DbContext | Tạo nhanh khách hàng khi đặt phòng |
| 3 | `Sequence_Reservation_Create` | Employee, View, ReservationController, DatabaseExtensions, `sp_CreateReservation`, `sp_CreateConfirmationReceipt` | Tạo đặt phòng và phiếu xác nhận |
| 4 | `Sequence_Room_AvailabilityLookup` | View, RoomController, DbContext, Room/Pricing/Reservation | Lấy phòng trống và giá |
| 5 | `Sequence_CheckIn` | Employee, View, CheckInController, DatabaseExtensions, `sp_QuickCheckin`, `sp_CreateConfirmationReceipt` | Nhận phòng |
| 6 | `Sequence_RoomService_AddUpdateDelete` | Employee, View, RoomServiceController, `sp_AddRoomService`, `sp_DeleteRoomService` | Quản lý dịch vụ phòng |
| 7 | `Sequence_CheckOut_Details` | Employee, View, CheckOutController, DbContext | Load chi tiết và tính phí preview |
| 8 | `Sequence_CheckOut_ConvertHourlyToDaily` | Employee, CheckOutController, SQL Procedure | Đổi đơn vị giá thuê |
| 9 | `Sequence_CheckOut_CheckoutThenPay` | Employee, CheckOutController, `sp_CreateInvoice_CheckoutThenPay`, Invoice, Payment View, `sp_ConfirmPayment` | Checkout trước, thanh toán sau |
| 10 | `Sequence_CheckOut_PayThenCheckout` | Employee, CheckOutController, `sp_CreateInvoice_PayThenCheckout`, `sp_ActualCheckout_AfterPrepayment` | Thanh toán trước, checkout thực tế |
| 11 | `Sequence_Webhook_ConfirmPayment` | Bank/Payment Gateway, CheckOutController API, DbContext, `sp_ConfirmPayment` | Xác nhận chuyển khoản |
| 12 | `Sequence_Invoice_ViewPrint` | Employee, InvoiceController, DbContext, View | Xem/in hóa đơn |
| 13 | `Sequence_Receipt_Print` | Employee, ConfirmationReceiptController, DbContext/SP, View | In phiếu xác nhận |
| 14 | `Sequence_Report_RevenueOccupancy` | Manager, ReportController, DbContext, Invoice/Reservation/Room | Truy vấn báo cáo |
| 15 | `Sequence_Background_UpdateRoomStatus` | HostedService, ServiceScope, DbContext, `sp_UpdateRoomStatusToReserved` | Cập nhật trạng thái tự động |

### 11.4. Checklist trước khi vẽ Sequence Diagram

- Xác định participant là class/controller thật hay thành phần logic.
- Với mỗi action POST, ghi rõ redirect/view/json response.
- Thêm `alt` cho lỗi validation, not found, unauthorized, duplicate, state invalid.
- Thêm `loop` cho background service chạy định kỳ.
- Thêm `opt` cho in phiếu/hóa đơn và webhook.

## 12. Kế hoạch xây dựng Use Case Diagram

### 12.1. Mục tiêu Use Case Diagram

- Mô tả chức năng hệ thống theo góc nhìn actor.
- Tách tổng quan toàn hệ thống và chi tiết từng nhóm chức năng.
- Dùng quan hệ `include` cho chức năng bắt buộc dùng lại, `extend` cho nhánh tùy chọn/phụ thuộc điều kiện.

### 12.2. Use Case Diagram tổng quát

Tên dự kiến: `UseCase_TongQuan_HeThongQuanLyKhachSan`

Actors:

- Khách hàng.
- Nhân viên/Lễ tân.
- Quản lý.
- Admin.
- Hệ thống thanh toán.
- Background Service.

Use cases tổng quan:

- Đăng nhập/Đăng xuất.
- Quản lý khách hàng.
- Quản lý nhân viên.
- Quản lý tài khoản.
- Quản lý phòng.
- Quản lý loại phòng và giá.
- Quản lý dịch vụ khách sạn.
- Đặt phòng.
- Hủy đặt phòng.
- Check-in.
- Ghi nhận dịch vụ phòng.
- Check-out.
- Thanh toán.
- Xem/in hóa đơn.
- Xem/in phiếu xác nhận.
- Xem báo cáo.
- Cập nhật trạng thái phòng tự động.

### 12.3. Use Case Diagram chi tiết từng nhóm

| Nhóm | Sơ đồ dự kiến | Actor chính | Use case chi tiết |
|---|---|---|---|
| Auth | `UseCase_Auth` | Nhân viên, Quản lý, Admin | Đăng nhập, đăng xuất, kiểm tra phiên |
| Dữ liệu nền | `UseCase_MasterData` | Nhân viên, Quản lý | CRUD khách hàng, nhân viên, phòng, loại phòng, giá, dịch vụ |
| Tài khoản | `UseCase_AccountManagement` | Quản lý, Admin | Tạo tài khoản, đổi trạng thái, reset password |
| Đặt phòng | `UseCase_Reservation` | Khách hàng, Nhân viên | Tra cứu phòng, tính cọc, tạo đặt phòng, tạo phiếu đặt phòng, hủy đặt phòng |
| Check-in | `UseCase_CheckIn` | Khách hàng, Nhân viên | Xem danh sách chờ, nhận phòng, tạo phiếu check-in |
| Dịch vụ phòng | `UseCase_RoomService` | Khách hàng, Nhân viên | Thêm dịch vụ, cập nhật số lượng, xóa dịch vụ, tính phí dịch vụ |
| Check-out/Payment | `UseCase_CheckOutPayment` | Khách hàng, Nhân viên, Payment Gateway | Xem chi phí, đổi thuê giờ sang ngày, checkout rồi thanh toán, thanh toán trước, xác nhận chuyển khoản, xử lý phụ thu |
| Invoice/Receipt | `UseCase_InvoiceReceipt` | Nhân viên, Quản lý, Khách hàng | Xem/in hóa đơn, xem/in phiếu đặt/check-in/check-out |
| Báo cáo | `UseCase_Report` | Quản lý, Admin | Xem doanh thu, công suất phòng, hiệu suất nhân viên, biểu đồ |
| Tự động trạng thái phòng | `UseCase_RoomStatusAutomation` | Background Service, Nhân viên | Cập nhật tự động, cập nhật thủ công, xem reservation sắp đến |

### 12.4. Quan hệ include/extend gợi ý

- `Tạo đặt phòng` include:
  - `Tra cứu phòng trống`.
  - `Kiểm tra thông tin khách hàng`.
  - `Tính/nhập tiền cọc`.
  - `Tạo phiếu xác nhận đặt phòng`.
- `Check-in` include:
  - `Kiểm tra reservation hợp lệ`.
  - `Tạo lịch sử check-in`.
  - `Cập nhật trạng thái phòng`.
  - `Tạo phiếu xác nhận check-in`.
- `Check-out` include:
  - `Kiểm tra đã check-in`.
  - `Tính tiền phòng`.
  - `Tính tiền dịch vụ`.
  - `Tạo lịch sử check-out`.
  - `Tạo/cập nhật hóa đơn`.
- `Check-out` extend:
  - `Đổi thuê giờ sang thuê ngày`.
  - `Xử lý phụ thu trả muộn`.
- `Thanh toán` extend:
  - `Xác nhận tiền mặt/thẻ`.
  - `Xác nhận chuyển khoản qua webhook`.
- `In phiếu xác nhận` extend:
  - `In phiếu đặt phòng`.
  - `In phiếu check-in`.
  - `In phiếu check-out`.

## 13. Thứ tự triển khai sơ đồ đề xuất

### Giai đoạn 1: Chuẩn hóa phạm vi và ký hiệu

1. Chốt công cụ vẽ sơ đồ: PlantUML, draw.io, Mermaid, StarUML hoặc Visual Paradigm.
2. Chốt quy ước đặt tên file sơ đồ.
3. Chốt quy ước actor, boundary/control/entity trong sequence.
4. Chốt trạng thái phòng, reservation, invoice.
5. Đối chiếu model C# với SQL schema.

### Giai đoạn 2: Xây dựng sơ đồ tổng quát

1. Use Case tổng quát.
2. Activity tổng quát.
3. Sequence tổng quát đặt phòng đến check-out.
4. Class Diagram domain tổng quát.

Lý do: các sơ đồ tổng quát giúp thống nhất phạm vi trước khi đi vào chi tiết.

### Giai đoạn 3: Xây dựng sơ đồ chi tiết luồng lõi

Ưu tiên cao nhất:

1. Đặt phòng.
2. Check-in.
3. Dịch vụ phòng.
4. Check-out rồi thanh toán.
5. Thanh toán trước rồi checkout.
6. Webhook xác nhận thanh toán.
7. Cập nhật trạng thái phòng tự động.

### Giai đoạn 4: Xây dựng sơ đồ chi tiết module quản trị

1. Khách hàng.
2. Nhân viên.
3. Tài khoản.
4. Loại phòng và giá.
5. Phòng.
6. Dịch vụ khách sạn.

### Giai đoạn 5: Xây dựng sơ đồ phụ trợ

1. Hóa đơn.
2. Phiếu xác nhận.
3. Báo cáo.
4. Dashboard.

### Giai đoạn 6: Review và đồng bộ

1. So sánh sơ đồ với controller/action thực tế.
2. So sánh sơ đồ với stored procedure.
3. Kiểm tra tên class, thuộc tính, quan hệ.
4. Kiểm tra actor và phân quyền.
5. Kiểm tra luồng lỗi/alternative path.
6. Xuất sơ đồ sang ảnh/PDF nếu cần.

## 14. Danh sách file sơ đồ dự kiến

Nếu dùng PlantUML, có thể lưu trong `docs/uml/plantuml/`:

```text
docs/uml/plantuml/
├── activity/
│   ├── Activity_TongQuan_QuyTrinhKhachSan.puml
│   ├── Activity_Reservation_CreateCancel.puml
│   ├── Activity_CheckIn.puml
│   ├── Activity_RoomService_Usage.puml
│   ├── Activity_CheckOut_CheckoutThenPay.puml
│   ├── Activity_CheckOut_PayThenCheckout.puml
│   └── Activity_Background_RoomStatusUpdate.puml
├── class/
│   ├── Class_TongQuan_DomainModel.puml
│   ├── Class_Reservation_CheckIn_CheckOut.puml
│   ├── Class_ServiceUsage.puml
│   └── Class_Account_Authorization.puml
├── sequence/
│   ├── Sequence_TongQuan_ReservationToCheckout.puml
│   ├── Sequence_Reservation_Create.puml
│   ├── Sequence_CheckIn.puml
│   ├── Sequence_RoomService_AddUpdateDelete.puml
│   ├── Sequence_CheckOut_CheckoutThenPay.puml
│   ├── Sequence_CheckOut_PayThenCheckout.puml
│   └── Sequence_Webhook_ConfirmPayment.puml
└── usecase/
    ├── UseCase_TongQuan_HeThongQuanLyKhachSan.puml
    ├── UseCase_Reservation.puml
    ├── UseCase_CheckIn.puml
    ├── UseCase_RoomService.puml
    ├── UseCase_CheckOutPayment.puml
    └── UseCase_Report.puml
```

Nếu dùng draw.io hoặc Visual Paradigm, vẫn nên giữ cùng cách đặt tên để dễ đối chiếu.

## 15. Điểm cần xác minh trước khi vẽ chi tiết

1. **Stored procedure là nguồn nghiệp vụ lõi**
   - Cần đọc kỹ từng procedure trong `docs/database/HotelManagement_new.sql` trước khi vẽ chi tiết:
     - `sp_CreateReservation`
     - `sp_QuickCheckin`
     - `sp_CheckinRoom`
     - `sp_QuickCheckout`
     - `sp_CheckoutRoom`
     - `sp_CreateInvoice_CheckoutThenPay`
     - `sp_CreateInvoice_PayThenCheckout`
     - `sp_ConfirmPayment`
     - `sp_ActualCheckout_AfterPrepayment`
     - `sp_AddRoomService`
     - `sp_DeleteRoomService`
     - `sp_UpdateRoomStatusToReserved`
     - `sp_ConvertHourlyToDaily`

2. **Phân quyền chưa hoàn toàn đồng nhất**
   - `Program.cs` có policy `EMPLOYEE`, `MANAGER`, `ADMIN`.
   - Nhiều controller chủ yếu kiểm tra session thủ công.
   - `ReceptionAccountController` kiểm tra `MANAGER`/`ADMIN` rõ ràng.
   - SQL ban đầu có constraint role chỉ `ADMIN`, `EMPLOYEE`, nhưng controller có `MANAGER`.

3. **Receipt type có khả năng lệch**
   - Controller có sử dụng `CHECKOUT`.
   - SQL đoạn tạo bảng ban đầu chỉ check `RESERVATION`, `CHECKIN`.
   - Cần kiểm tra script sau đó có alter constraint hay không.

4. **Invoice amount cần thống nhất**
   - Có `TotalDue`, `NetDue`, `TotalAmount`, `AmountPaid`, `RoomBookingDeposit`, `TaxRate`.
   - Cần xác định trường nào là số tiền khách phải trả cuối cùng trong sơ đồ payment.

5. **Trạng thái reservation**
   - Model dùng `IsActivate`.
   - Một số raw SQL có thể dùng khái niệm status/pending.
   - Cần xác minh tên trường thực tế trước khi vẽ trạng thái reservation.

6. **Luồng đổi phòng**
   - Có model `RoomChangeHistory` và README nhắc xử lý đổi phòng.
   - Chưa thấy controller nghiệp vụ đổi phòng rõ ràng.
   - Nếu yêu cầu có sơ đồ đổi phòng, cần xác định có chức năng UI/Controller hay chỉ là thiết kế DB.

7. **Legacy checkout**
   - `ProcessCheckOut`/`sp_QuickCheckout` tồn tại như luồng cũ.
   - Nên quyết định biểu diễn là use case phụ hay loại khỏi sơ đồ chính.

8. **Thời gian hệ thống**
   - Nhiều đoạn dùng `DateTime.UtcNow.AddHours(7)`.
   - Một số báo cáo dùng `DateTime.Today`.
   - Cần chuẩn hóa mô tả timezone khi vẽ luồng hạn check-in/check-out.

9. **Tần suất background service**
   - Comment nói chạy mỗi 30 phút.
   - Code hiện tại đặt `_updateInterval = TimeSpan.FromMinutes(5)`.
   - Cần chốt mô tả theo code hoặc theo yêu cầu nghiệp vụ.

10. **Tạo phiếu xác nhận trùng**
    - Phiếu có thể được tạo khi đặt/check-in qua stored procedure và cũng có action print tạo trực tiếp.
    - Cần xác minh logic chống trùng trước khi vẽ luồng in phiếu.

## 16. Mẫu thông tin cần chuẩn bị cho mỗi sơ đồ chi tiết

Mỗi sơ đồ chi tiết nên có một phiếu mô tả trước khi vẽ:

```text
Tên sơ đồ:
Loại sơ đồ: Activity / Sequence / Use Case / Class
Mục tiêu:
Actor/Participant/Class liên quan:
Tiền điều kiện:
Hậu điều kiện:
Luồng chính:
Luồng thay thế/lỗi:
Dữ liệu đọc:
Dữ liệu ghi/cập nhật:
Stored procedure liên quan:
Controller/action liên quan:
View/API liên quan:
Điểm cần xác minh:
```

## 17. Kết luận kế hoạch

Dự án có quy trình nghiệp vụ chính xoay quanh vòng đời đặt phòng:

```text
Đăng nhập -> Quản lý dữ liệu nền -> Đặt phòng -> Check-in -> Sử dụng dịch vụ -> Check-out/Thanh toán -> Hóa đơn/Phiếu -> Báo cáo
```

Khi xây dựng sơ đồ, nên ưu tiên các sơ đồ tổng quát trước, sau đó triển khai chi tiết các luồng lõi: đặt phòng, check-in, dịch vụ phòng, check-out và thanh toán. Class Diagram cần được dựng song song với việc đối chiếu C# model và SQL schema vì một số ràng buộc/trạng thái nằm dưới database. Activity và Sequence Diagram chi tiết cần đặc biệt chú ý các stored procedure do đây là nơi xử lý phần lớn nghiệp vụ quan trọng.
