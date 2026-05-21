# Báo cáo tổng thể luồng logic nghiệp vụ hệ thống HotelManagement

Ngày cập nhật: 22/05/2026

## 1. Giới thiệu hệ thống

HotelManagement là hệ thống quản lý khách sạn theo mô hình ASP.NET Core MVC. Hệ thống hỗ trợ các nghiệp vụ chính của một khách sạn từ quản lý dữ liệu nền, đặt phòng, nhận phòng, sử dụng dịch vụ, trả phòng, thanh toán, hóa đơn, phiếu xác nhận, báo cáo, cho đến vận hành dọn phòng và bảo trì phòng.

Điểm quan trọng của hệ thống là các luồng nghiệp vụ không chỉ được xử lý ở controller. Nhiều quy tắc quan trọng được đặt ở tầng cơ sở dữ liệu thông qua stored procedure, trigger, computed column và constraint. Điều này giúp hệ thống giữ tính nhất quán dữ liệu khi các thao tác quan trọng như đặt phòng, check-in, check-out, tính hóa đơn hoặc cập nhật trạng thái phòng được thực hiện.

Các thành phần chính:

| Thành phần | Vai trò |
| --- | --- |
| Controllers | Điều hướng request, kiểm tra session, chuẩn bị dữ liệu cho view, gọi EF Core hoặc stored procedure |
| Views | Giao diện Razor MVC cho nhân viên thao tác |
| Models | Mapping bảng SQL sang entity C# |
| HotelManagementContext | DbContext trung tâm, khai báo DbSet và quan hệ |
| DatabaseExtensions | Wrapper gọi stored procedure từ C# |
| HotelManagement_new.sql | Nguồn chính của cấu trúc bảng, trigger, procedure, ràng buộc và seed data |
| RoomStatusUpdateService | Background service tự động cập nhật trạng thái phòng sắp đến giờ nhận |

## 2. Vai trò người dùng và phân quyền

Hệ thống sử dụng đăng nhập bằng tài khoản trong bảng `User`, mật khẩu được xác thực bằng BCrypt. Sau khi đăng nhập thành công, hệ thống lưu thông tin vào Session gồm `UserID`, `Username`, `Role`, `EmployeeID` và `Position`.

Các vai trò chính:

| Vai trò | Chức năng chính |
| --- | --- |
| ADMIN | Quản trị toàn hệ thống, quản lý tài khoản, dữ liệu nền, báo cáo |
| MANAGER | Quản lý nghiệp vụ, báo cáo, phân công dọn phòng/bảo trì |
| EMPLOYEE | Lễ tân/nghiệp vụ: khách hàng, đặt phòng, check-in, dịch vụ, check-out, hóa đơn |
| CLEANER | Nhân viên dọn phòng, chỉ xem và xử lý công việc dọn phòng được giao |
| TECHNICIAN | Nhân viên kỹ thuật, chỉ xem và xử lý công việc bảo trì được giao |

Luồng đăng nhập:

1. Người dùng nhập tên đăng nhập và mật khẩu.
2. Hệ thống tìm tài khoản đang `ACTIVATE`.
3. Xác thực mật khẩu bằng BCrypt.
4. Xác định vai trò hiệu dụng:
   - Nếu user role là `EMPLOYEE` nhưng vị trí nhân viên là `CLEANER` hoặc `TECHNICIAN`, hệ thống dùng vị trí này làm role thao tác.
   - Các tài khoản còn lại giữ role trong bảng `User`.
5. Nếu là `CLEANER` hoặc `TECHNICIAN`, chuyển thẳng sang module dọn phòng/bảo trì.
6. Các vai trò còn lại chuyển sang Dashboard.

## 3. Dữ liệu nền của hệ thống

Dữ liệu nền là các thông tin được dùng xuyên suốt các nghiệp vụ chính:

| Nhóm dữ liệu | Bảng/Model | Mục đích |
| --- | --- | --- |
| Người dùng | User | Tài khoản đăng nhập và role |
| Nhân viên | Employee | Nhân sự thao tác nghiệp vụ |
| Khách hàng | Customer | Thông tin khách đặt và lưu trú |
| Loại phòng | RoomCategory | Phân loại phòng theo số giường, tên loại phòng |
| Bảng giá | Pricing | Giá thuê theo `DAY` hoặc `HOUR` cho từng loại phòng |
| Phòng | Room | Phòng cụ thể, trạng thái vận hành |
| Nhóm dịch vụ | ServiceCategory | Phân nhóm dịch vụ khách sạn |
| Dịch vụ | HotelService | Dịch vụ phát sinh như ăn uống, giặt ủi |

Phần lớn dữ liệu nền dùng cơ chế xóa mềm thông qua `isActivate` hoặc `IsActivate`:

- `ACTIVATE`: đang hoạt động.
- `DEACTIVATE`: ngưng hoạt động.

Cách này giúp không mất dữ liệu lịch sử, đặc biệt khi dữ liệu đã liên quan đến đặt phòng, hóa đơn hoặc báo cáo.

## 4. Trạng thái phòng và ý nghĩa nghiệp vụ

Trạng thái phòng là trục liên kết giữa đặt phòng, check-in, check-out và dọn phòng/bảo trì.

| Trạng thái | Ý nghĩa |
| --- | --- |
| AVAILABLE | Phòng trống, có thể khai thác |
| RESERVED | Phòng đã được giữ gần giờ nhận phòng |
| ON_USE | Khách đang sử dụng phòng |
| UNAVAILABLE | Phòng không khả dụng |
| OVERDUE | Phòng/quy trình bị quá hạn theo nghiệp vụ cũ |
| DIRTY | Phòng cần dọn sau trả phòng hoặc sau bảo trì |
| CLEANING | Phòng đang được dọn |
| MAINTENANCE | Phòng đang bảo trì |
| OUT_OF_SERVICE | Phòng ngưng phục vụ |

Vòng đời phổ biến của phòng:

```text
AVAILABLE
  -> RESERVED
  -> ON_USE
  -> DIRTY
  -> CLEANING
  -> AVAILABLE
```

Nếu có bảo trì:

```text
AVAILABLE/khác
  -> MAINTENANCE
  -> DIRTY nếu cần dọn sau bảo trì
  -> CLEANING
  -> AVAILABLE
```

## 5. Luồng đặt phòng

Luồng đặt phòng là điểm bắt đầu của nghiệp vụ lưu trú.

### 5.1. Các bước xử lý

1. Lễ tân chọn khách hàng.
2. Chọn phòng, loại giá thuê theo ngày hoặc theo giờ.
3. Nhập ngày nhận phòng, ngày trả phòng, tiền cọc.
4. Hệ thống kiểm tra dữ liệu đầu vào ở controller.
5. Controller gọi `sp_CreateReservation`.
6. Stored procedure kiểm tra lại toàn bộ điều kiện ở database.
7. Nếu hợp lệ, tạo phiếu đặt phòng `ReservationForm` với mã `RF-...`.
8. Hệ thống tạo phiếu xác nhận đặt phòng `ConfirmationReceipt` loại `RESERVATION`.
9. Phòng chưa chuyển `RESERVED` ngay. Phòng chỉ chuyển `RESERVED` khi còn gần giờ nhận phòng.

### 5.2. Điều kiện validate

Điều kiện ở controller:

- Ngày nhận phòng không được nhỏ hơn thời điểm hiện tại.
- Ngày trả phòng phải sau ngày nhận phòng.
- Có khách hàng, phòng, đơn giá, tiền cọc hợp lệ.

Điều kiện ở SQL:

- Phiếu đặt phòng không được thiếu thông tin.
- Check-in phải sau thời điểm hiện tại.
- Check-out phải sau check-in.
- Phòng phải tồn tại, đang hoạt động và không nằm trong trạng thái bị chặn như `UNAVAILABLE`, `OVERDUE`, `MAINTENANCE`, `OUT_OF_SERVICE`.
- Khách hàng phải tồn tại và đang hoạt động.
- Nhân viên phải tồn tại và đang hoạt động.
- Tiền cọc không âm.
- `priceUnit` chỉ nhận `DAY` hoặc `HOUR`.
- `unitPrice` phải lớn hơn 0.
- Không được trùng lịch với reservation khác của cùng phòng.

### 5.3. Ý nghĩa nghiệp vụ

Đặt phòng không chỉ là lưu thông tin khách và phòng. Hệ thống còn chụp lại đơn giá tại thời điểm đặt, tiền cọc, thời gian dự kiến và nhân viên phụ trách. Các dữ liệu này sẽ là cơ sở cho check-in, tính tiền, báo cáo công suất và xử lý no-show.

## 6. Luồng lịch đặt phòng

Lịch đặt phòng giúp lễ tân xem phòng theo thời gian, trạng thái reservation và các task dọn/bảo trì đang mở.

### 6.1. Các trạng thái reservation trên lịch

| Trạng thái | Ý nghĩa |
| --- | --- |
| BOOKED | Đã đặt, chưa check-in |
| OVERDUE_CHECKIN | Đã quá giờ nhận phòng nhưng khách chưa check-in |
| CHECKED_IN | Khách đã nhận phòng |
| CHECKED_OUT | Khách đã trả phòng |

Ngoài ra hệ thống có trạng thái `NO_SHOW` ở bảng `ReservationForm` để đánh dấu khách không đến.

### 6.2. Kéo thả thay đổi lịch

Khi kéo một booking sang ngày hoặc phòng khác:

1. JavaScript lấy reservation đang được kéo.
2. Giữ nguyên giờ nhận phòng ban đầu và thời lượng đặt phòng ban đầu.
3. Tính `newStart` và `newEnd`.
4. Gọi API `CheckAvailability`.
5. Nếu phòng khả dụng, gọi API preview giá.
6. Nếu đổi loại phòng hoặc đơn giá thay đổi, hệ thống yêu cầu xác nhận giá mới.
7. Sau khi xác nhận, gọi `MoveReservation`.
8. Controller gọi `sp_UpdateReservationSchedule` để lưu lịch mới.

### 6.3. Validate khi kéo thả

Validate ở JavaScript:

- Nếu resize ngày trả phòng, `newEnd` phải sau `start`.
- Nếu event không còn editable thì không cho kéo.

Validate ở controller:

- Không cho check-in date nằm trong quá khứ.
- Check-out phải sau check-in.
- Phòng phải tồn tại và đang hoạt động.
- Phòng không được ở trạng thái bị chặn.
- Không có task bảo trì đang mở.
- Không trùng lịch với booking khác.

Validate ở SQL:

- Phiếu đặt phòng phải đang hoạt động.
- Phiếu chưa check-in và chưa check-out.
- Phòng mới hợp lệ.
- Đơn giá xác nhận phải khớp bảng giá hiện tại.
- Nhân viên thao tác phải hợp lệ.
- Lịch mới không trùng với reservation khác.
- Phòng không có yêu cầu bảo trì đang mở.

Ý nghĩa: lịch kéo thả có 3 lớp bảo vệ. Frontend giúp trải nghiệm tốt, controller kiểm tra nhanh, SQL là lớp chốt cuối cùng bảo vệ dữ liệu.

## 7. Luồng khách không đến (No-show)

No-show dùng cho trường hợp phiếu đặt phòng đã quá giờ nhận nhưng khách không đến.

### 7.1. Điều kiện xử lý

Hệ thống chỉ cho đánh dấu no-show khi:

- Phiếu đặt phòng tồn tại.
- Phiếu chưa bị hủy.
- Phiếu chưa check-in.
- Nhân viên xử lý còn hoạt động.
- Đã đến hoặc quá giờ check-in dự kiến.

### 7.2. Kết quả xử lý

Khi đánh dấu no-show:

1. Cập nhật `reservationStatus = 'NO_SHOW'`.
2. Cập nhật `isActivate = 'DEACTIVATE'`.
3. Giải phóng phòng nếu phù hợp.
4. Nếu phòng có reservation kế tiếp trong vòng 5 giờ, giữ trạng thái `RESERVED`; nếu không thì chuyển về `AVAILABLE`.

### 7.3. Ý nghĩa nghiệp vụ

No-show giúp hệ thống phân biệt giữa phiếu bị hủy chủ động và phiếu khách không đến. Điều này có giá trị cho báo cáo vận hành, đánh giá tỷ lệ khách không đến, quản lý phòng trống thực tế và tránh khóa phòng không cần thiết.

## 8. Luồng check-in

Check-in là bước chuyển từ đặt phòng sang lưu trú thực tế.

### 8.1. Các bước xử lý

1. Lễ tân vào màn hình check-in.
2. Hệ thống hiển thị các phiếu đang hoạt động, chưa có `HistoryCheckin`.
3. Lễ tân chọn phiếu và xác nhận check-in.
4. Controller kiểm tra phiếu tồn tại, chưa bị hủy và chưa quá checkout dự kiến.
5. Controller gọi `sp_QuickCheckin`.
6. `sp_QuickCheckin` sinh mã `HCI-...` và gọi `sp_CheckinRoom`.
7. SQL tạo bản ghi `HistoryCheckin`.
8. SQL ghi `RoomChangeHistory`.
9. Trigger `TR_UpdateRoomStatus_OnCheckin` chuyển phòng sang `ON_USE`.
10. Hệ thống tạo phiếu xác nhận check-in.

### 8.2. Validate thời gian check-in

Hệ thống hiện kiểm tra:

- Nếu thời điểm hiện tại đã sau checkout dự kiến, không cho check-in.
- Nếu đã có khách khác đang ở cùng phòng và chưa checkout, không cho check-in.
- Phòng chỉ được check-in nếu đang `AVAILABLE` hoặc `RESERVED`.

Điểm cần lưu ý khi báo cáo:

- Hệ thống hiện cho phép check-in sớm nếu phòng sẵn sàng.
- Trong SQL có trigger `TR_HistoryCheckin_CheckTime` để chặn check-in sớm, nhưng trigger này đang bị disable. Vì vậy rule đang chạy thực tế là cho phép check-in sớm, miễn là phòng hợp lệ và chưa có khách khác đang ở.

### 8.3. Ý nghĩa nghiệp vụ

Check-in xác nhận khách thật sự nhận phòng. Từ thời điểm này:

- Phòng chuyển sang `ON_USE`.
- Reservation được xem là đang lưu trú.
- Hệ thống cho phép phát sinh dịch vụ phòng.
- Thời điểm check-in thực tế trở thành cơ sở tính tiền phòng khi checkout.

## 9. Luồng sử dụng dịch vụ phòng

Khách đang lưu trú có thể phát sinh dịch vụ khách sạn.

### 9.1. Các bước xử lý

1. Lễ tân mở màn hình dịch vụ theo mã phiếu đặt phòng.
2. Chọn nhóm dịch vụ, dịch vụ và số lượng.
3. Controller gọi `sp_AddRoomService`.
4. SQL kiểm tra reservation đang hoạt động và chưa checkout.
5. SQL kiểm tra dịch vụ tồn tại và đang hoạt động.
6. Nếu dịch vụ đã tồn tại trong reservation, cộng thêm số lượng.
7. Nếu chưa tồn tại, tạo bản ghi `RoomUsageService`.
8. Khi xóa dịch vụ, controller gọi `sp_DeleteRoomService`.

### 9.2. Quy tắc tính dịch vụ

Mỗi dòng dịch vụ có:

```text
totalPrice = quantity * unitPrice
```

Khi tính hóa đơn:

```text
servicesCharge = SUM(quantity * unitPrice)
```

Dịch vụ không được thêm hoặc xóa nếu reservation đã checkout.

## 10. Luồng checkout và thanh toán

Checkout là luồng phức tạp nhất vì liên quan đến thời gian thực tế, tiền phòng, dịch vụ, cọc, VAT, trạng thái thanh toán, hóa đơn và trạng thái phòng.

### 10.1. Màn hình chi tiết checkout

Khi mở chi tiết checkout, hệ thống tính trước hai phương án:

1. Checkout rồi thanh toán:
   - Tính từ check-in thực tế đến thời điểm hiện tại.
2. Thanh toán trước rồi checkout sau:
   - Tính từ check-in thực tế đến checkout dự kiến.

Các giá trị preview gồm:

- Tiền phòng.
- Tiền dịch vụ.
- Tổng trước VAT.
- VAT 10%.
- Tổng sau VAT.
- Tiền cọc.
- Số tiền còn phải thu.
- Số tiền cần hoàn nếu cọc lớn hơn tổng.

### 10.2. Luồng checkout rồi thanh toán

Các bước:

1. Khách trả phòng trước.
2. Controller gọi `sp_CreateInvoice_CheckoutThenPay`.
3. SQL kiểm tra:
   - Đã check-in.
   - Chưa checkout.
   - Chưa có invoice.
4. SQL tạo `HistoryCheckOut` với thời gian hiện tại.
5. SQL tạo invoice chưa thanh toán, `isPaid = 0`, `checkoutType = 'CHECKOUT_THEN_PAY'`.
6. Trigger invoice tự tính tiền phòng và tiền dịch vụ.
7. Lễ tân vào màn hình thanh toán.
8. Controller gọi `sp_ConfirmPayment`.
9. SQL cập nhật `isPaid = 1`, `paymentDate`, `paymentMethod`, `amountPaid`.
10. Sau thanh toán, controller tạo task dọn phòng sau checkout.
11. Phòng chuyển sang `DIRTY`.

### 10.3. Luồng thanh toán trước rồi checkout sau

Các bước:

1. Khách vẫn đang ở nhưng muốn thanh toán trước.
2. Controller gọi `sp_CreateInvoice_PayThenCheckout`.
3. SQL kiểm tra đã check-in và chưa có invoice.
4. SQL tạo invoice đã thanh toán, `isPaid = 1`, `checkoutType = 'PAY_THEN_CHECKOUT'`.
5. Trigger tính tiền từ check-in thực tế đến checkout dự kiến.
6. Phòng vẫn `ON_USE`.
7. Khi khách trả phòng thật, controller gọi `sp_ActualCheckout_AfterPrepayment`.
8. SQL tạo `HistoryCheckOut`.
9. Nếu khách checkout muộn, hệ thống đánh dấu cần thu bổ sung.
10. Nếu không phát sinh thêm, controller tạo task dọn phòng và phòng chuyển `DIRTY`.

Điểm kỹ thuật cần chú ý: trong trigger update hóa đơn, phần tính lại tiền phòng khi update invoice đang có đoạn logic bị comment. Vì vậy nếu thuyết trình sâu về trả muộn sau thanh toán trước, nên nói theo thiết kế nghiệp vụ là có kiểm tra phụ thu, đồng thời cần kiểm thử kỹ phần trigger tính lại để đảm bảo số tiền phát sinh được cập nhật đúng.

### 10.4. Chuyển thuê giờ sang thuê ngày

Hệ thống có luồng `sp_ConvertHourlyToDaily`.

Điều kiện:

- Reservation đang thuê theo giờ.
- Đã check-in.
- Thời gian ở lớn hơn 6 giờ.
- Chưa có invoice.

Kết quả:

- `priceUnit` chuyển từ `HOUR` sang `DAY`.
- `unitPrice` đổi sang giá ngày của loại phòng.
- Ghi `RoomChangeHistory`.

Ý nghĩa: tránh trường hợp khách ở dài nhưng vẫn tính theo giờ gây bất hợp lý hoặc chi phí quá cao so với giá ngày.

## 11. Cách tính tiền hóa đơn trong cơ sở dữ liệu

Phần tính tiền nằm chủ yếu trong bảng `Invoice` và trigger `TR_Invoice_ManageInsert`.

### 11.1. Các cột tiền chính

| Cột | Ý nghĩa |
| --- | --- |
| roomCharge | Tiền phòng |
| servicesCharge | Tiền dịch vụ |
| totalDue | Tổng trước VAT, computed bằng `roomCharge + servicesCharge` |
| taxRate | Thuế VAT, mặc định `0.1` |
| netDue | Tổng sau VAT, computed bằng `(roomCharge + servicesCharge) * 1.1` |
| roomBookingDeposit | Tiền cọc |
| totalAmount | Số tiền còn phải thu sau khi trừ cọc |
| amountPaid | Số tiền thực tế đã thu |

### 11.2. Công thức tổng quát

```text
totalDue = roomCharge + servicesCharge
netDue = totalDue * 1.1
totalAmount = max(totalDue * (1 + taxRate) - roomBookingDeposit, 0)
```

Trong đó `taxRate` mặc định là `0.1`, tức VAT 10%.

Nếu tiền cọc lớn hơn tổng tiền sau VAT:

```text
totalAmount = 0
refundAmount = roomBookingDeposit + amountPaid - netDue
```

Phần hoàn tiền được controller tính khi xác nhận thanh toán để hiển thị cho lễ tân.

### 11.3. Tính tiền phòng

Trigger lấy các thông tin:

- `priceUnit`: `DAY` hoặc `HOUR`.
- `unitPrice`: đơn giá đã chốt trong reservation.
- `checkInDateActual`: thời gian check-in thực tế.
- `checkOutDateActual`: thời gian checkout thực tế.
- `checkOutDateExpected`: thời gian checkout dự kiến.
- `checkoutType`: loại luồng thanh toán.

Xác định mốc thời gian:

- Nếu `CHECKOUT_THEN_PAY`: tính từ check-in thực tế đến checkout thực tế.
- Nếu `PAY_THEN_CHECKOUT` và chưa checkout thật: tính từ check-in thực tế đến checkout dự kiến.
- Nếu đã checkout thật: theo thiết kế sẽ tính lại theo checkout thực tế.

Công thức:

```text
bookingMinutes = DATEDIFF(MINUTE, effectiveCheckIn, effectiveCheckOut)
```

Nếu thuê theo ngày:

```text
timeUnits = CEILING(bookingMinutes / 1440.0)
roomCharge = timeUnits * unitPrice
```

Nếu thuê theo giờ:

```text
timeUnits = CEILING(bookingMinutes / 60.0)
roomCharge = timeUnits * unitPrice
```

Nếu thời lượng nhỏ hơn hoặc bằng 0, hệ thống vẫn tính tối thiểu 1 đơn vị.

### 11.4. Tính tiền dịch vụ

```text
servicesCharge = SUM(RoomUsageService.quantity * RoomUsageService.unitPrice)
```

Chỉ các dịch vụ gắn với reservation đó được tính vào hóa đơn.

### 11.5. Vai trò của tiền cọc

Tiền cọc được lưu từ lúc đặt phòng. Khi tính hóa đơn:

- Cọc không làm giảm `roomCharge` hoặc `servicesCharge`.
- Cọc được trừ ở bước tính `totalAmount`.
- Nếu cọc lớn hơn tổng tiền sau VAT, hệ thống hiển thị số tiền cần hoàn cho khách.

Ví dụ:

```text
Tiền phòng: 1.000.000
Tiền dịch vụ: 200.000
Tổng trước VAT: 1.200.000
VAT 10%: 120.000
Tổng sau VAT: 1.320.000
Cọc: 300.000
Khách còn phải trả: 1.020.000
```

## 12. Luồng hóa đơn và phiếu xác nhận

### 12.1. Hóa đơn

Hóa đơn được dùng để:

- Xem chi tiết tiền phòng, tiền dịch vụ, VAT, cọc, số tiền phải thu.
- Lọc theo trạng thái thanh toán.
- Lọc theo khoảng ngày.
- Tra cứu theo mã hóa đơn, mã reservation, khách hoặc phòng.
- Làm nguồn dữ liệu cho báo cáo doanh thu.

### 12.2. Phiếu xác nhận

Hệ thống có `ConfirmationReceipt` cho:

- Phiếu xác nhận đặt phòng.
- Phiếu xác nhận check-in.
- Phiếu xác nhận check-out theo controller.

Phiếu xác nhận lưu thông tin snapshot như khách hàng, phòng, loại phòng, ngày nhận/trả, đơn giá, cọc và nhân viên. Đây là chứng từ nghiệp vụ để đối chiếu với khách.

## 13. Luồng dọn phòng và bảo trì

Module dọn phòng/bảo trì quản lý công việc vận hành phòng sau checkout hoặc khi phát sinh sự cố.

### 13.1. Tạo công việc

Có hai loại công việc:

- `CLEANING`: dọn phòng.
- `MAINTENANCE`: bảo trì.

Nguồn tạo công việc:

- Tạo thủ công bởi lễ tân, quản lý hoặc admin.
- Tự động tạo sau checkout.
- Tự động tạo dọn phòng sau khi hoàn tất bảo trì nếu cần.

### 13.2. Trạng thái công việc

| Trạng thái | Ý nghĩa |
| --- | --- |
| PENDING | Chờ xử lý |
| ASSIGNED | Đã phân công |
| IN_PROGRESS | Đang thực hiện |
| COMPLETED | Hoàn thành |
| CANCELLED | Đã hủy |

### 13.3. Quy trình xử lý

1. Tạo task.
2. Phòng chuyển trạng thái theo loại task:
   - Dọn phòng: `DIRTY`.
   - Bảo trì: `MAINTENANCE`.
3. Manager/Admin phân công nhân viên.
4. Nhân viên bắt đầu:
   - Task sang `IN_PROGRESS`.
   - Phòng sang `CLEANING` hoặc `MAINTENANCE`.
5. Nhân viên hoàn thành:
   - Task sang `COMPLETED`.
   - Nếu không còn task mở khác, phòng về `AVAILABLE`.
6. Mọi thay đổi được ghi vào `RoomTaskHistory`.

### 13.4. Phân quyền module

- Manager/Admin: xem, tạo, phân công, hủy, báo cáo.
- Employee: có thể truy cập và tạo task theo nghiệp vụ.
- Cleaner: chỉ thấy task dọn phòng được giao cho mình.
- Technician: chỉ thấy task bảo trì được giao cho mình.

## 14. Tự động cập nhật trạng thái phòng

`RoomStatusUpdateService` chạy nền và gọi `sp_UpdateRoomStatusToReserved`.

Nghiệp vụ:

1. Tìm reservation active chưa check-in.
2. Nếu còn tối đa 5 giờ đến giờ check-in.
3. Nếu phòng đang `AVAILABLE`.
4. Chuyển phòng sang `RESERVED`.

Ý nghĩa:

- Phòng không bị giữ quá sớm ngay khi khách đặt.
- Gần giờ khách đến, hệ thống tự động khóa phòng để tránh nhầm lẫn.
- Nếu khách không đến, luồng no-show có thể giải phóng phòng.

Lưu ý kỹ thuật: comment trong `Program.cs` nói chạy mỗi 30 phút, nhưng service hiện đặt interval 5 phút. Khi báo cáo có thể nói hệ thống có background service định kỳ; nếu cần chính xác tuyệt đối thì nêu hiện tại đang cấu hình 5 phút.

## 15. Dashboard và báo cáo

### 15.1. Dashboard

Dashboard cung cấp số liệu nhanh:

- Tổng số phòng active.
- Số phòng trống.
- Số phòng đang sử dụng.
- Tổng khách hàng.
- Tổng nhân viên.
- Số đặt phòng hôm nay.
- Doanh thu tháng hiện tại.

Dashboard giúp người quản lý nắm tình hình vận hành ngay khi đăng nhập.

### 15.2. Báo cáo doanh thu

Báo cáo doanh thu cho biết:

- Tổng doanh thu trong khoảng ngày.
- Doanh thu tiền phòng.
- Doanh thu dịch vụ.
- Số hóa đơn.
- Doanh thu trung bình.
- Doanh thu theo ngày.
- Doanh thu theo loại phòng.

Mục đích:

- Theo dõi hiệu quả kinh doanh.
- Xác định loại phòng đem lại doanh thu cao.
- So sánh doanh thu giữa các giai đoạn.
- Hỗ trợ ra quyết định điều chỉnh giá hoặc khuyến mãi.

### 15.3. Báo cáo công suất phòng

Báo cáo công suất phòng dùng để đo mức độ khai thác phòng.

Cách tính chính:

```text
totalRoomDays = tổng số phòng * số ngày trong kỳ
occupiedDays = tổng số ngày hoặc phần ngày có khách ở
occupancyRate = occupiedDays / totalRoomDays * 100
```

Báo cáo còn thống kê:

- Tổng số lượt check-in.
- Công suất theo loại phòng.
- Công suất theo ngày.
- Danh sách reservation đã lưu trú, số lượng ngày/giờ sử dụng.

Mục đích nghiệp vụ:

- Biết khách sạn đang dùng bao nhiêu phần trăm năng lực phòng.
- Phát hiện mùa cao điểm/thấp điểm.
- Biết loại phòng nào được khách chọn nhiều.
- Hỗ trợ lên kế hoạch nhân sự dọn phòng và lễ tân.
- Hỗ trợ điều chỉnh giá, chính sách đặt cọc và chương trình khuyến mãi.
- Kết hợp với doanh thu để đánh giá hiệu quả: công suất cao nhưng doanh thu thấp có thể do giá thấp; công suất thấp nhưng doanh thu cao có thể do giá cao hoặc khách ít nhưng giá trị lớn.

### 15.4. Báo cáo hiệu suất nhân viên

Báo cáo hiệu suất nhân viên thống kê các nghiệp vụ gắn với nhân viên:

- Số phiếu đặt phòng đã tạo.
- Số lượt check-in.
- Số lượt checkout.
- Hiệu quả xử lý nghiệp vụ theo thời gian.

Mục đích:

- Đánh giá khối lượng công việc.
- Hỗ trợ phân công ca làm.
- Xác định nhân viên xử lý nhiều booking hoặc giao dịch.

### 15.5. Báo cáo dọn phòng/bảo trì

Module dọn phòng/bảo trì có báo cáo riêng:

- Tổng task.
- Task đang chờ, đang xử lý, hoàn thành, hủy.
- Task quá hạn.
- Thời gian xử lý trung bình.
- Thống kê theo nhân viên.
- Thống kê theo phòng.

Mục đích:

- Đánh giá hiệu quả vận hành buồng phòng.
- Biết phòng nào hay phát sinh bảo trì hoặc dọn phòng.
- Kiểm soát SLA xử lý task.

## 16. Tổng hợp các luồng nghiệp vụ chính

### 16.1. Luồng lưu trú chuẩn

```text
Tạo khách hàng
  -> Tạo đặt phòng
  -> Gần giờ nhận phòng, phòng chuyển RESERVED
  -> Khách đến check-in
  -> Phòng chuyển ON_USE
  -> Khách sử dụng dịch vụ
  -> Checkout
  -> Tạo hóa đơn
  -> Thanh toán
  -> Tạo task dọn phòng
  -> Phòng chuyển DIRTY
  -> Nhân viên dọn phòng xử lý task
  -> Phòng về AVAILABLE
```

### 16.2. Luồng khách không đến

```text
Tạo đặt phòng
  -> Quá giờ check-in
  -> Khách không đến
  -> Đánh dấu NO_SHOW
  -> Phiếu bị deactivate
  -> Phòng được giải phóng hoặc giữ RESERVED cho booking kế tiếp gần giờ
```

### 16.3. Luồng thanh toán trước

```text
Check-in
  -> Khách đang ở
  -> Tạo hóa đơn thanh toán trước
  -> Tính tiền đến checkout dự kiến
  -> Khách tiếp tục ở
  -> Checkout thực tế
  -> Nếu trả muộn, phát sinh thu bổ sung
  -> Nếu đúng giờ, hoàn tất và tạo task dọn phòng
```

### 16.4. Luồng bảo trì phòng

```text
Phát hiện phòng cần bảo trì
  -> Tạo task MAINTENANCE
  -> Phòng chuyển MAINTENANCE
  -> Quản lý phân công kỹ thuật viên
  -> Kỹ thuật viên bắt đầu xử lý
  -> Hoàn thành bảo trì
  -> Có thể tạo task CLEANING sau bảo trì
  -> Phòng về AVAILABLE sau khi hết task mở
```

## 17. Các điểm nổi bật khi trình bày với giảng viên

1. Hệ thống không chỉ CRUD dữ liệu, mà có chuỗi nghiệp vụ đầy đủ của khách sạn.
2. Các nghiệp vụ quan trọng được bảo vệ bằng stored procedure và trigger ở database.
3. Check-in, checkout và hóa đơn dựa trên thời gian thực tế, không chỉ thời gian dự kiến.
4. Hóa đơn có tính tiền phòng, dịch vụ, VAT, tiền cọc và số tiền còn phải thu.
5. Có hai luồng thanh toán: checkout rồi thanh toán và thanh toán trước rồi checkout sau.
6. Có background service tự động chuyển phòng gần giờ nhận sang `RESERVED`.
7. Có no-show để xử lý khách không đến và giải phóng phòng.
8. Sau checkout, hệ thống tự động nối sang vận hành dọn phòng.
9. Module dọn phòng/bảo trì có phân quyền theo nhân viên buồng phòng và kỹ thuật viên.
10. Báo cáo không chỉ thống kê doanh thu mà còn đo công suất phòng và hiệu suất nhân viên.

## 18. Một số điểm cần lưu ý khi demo hoặc bảo vệ

### 18.1. Luồng check-in sớm

SQL có trigger chặn check-in sớm nhưng đang bị disable. Vì vậy demo hiện tại có thể check-in sớm nếu phòng đang `AVAILABLE` hoặc `RESERVED`. Nếu giảng viên hỏi, có thể giải thích rằng hệ thống ưu tiên linh hoạt nghiệp vụ lễ tân: khách đến sớm vẫn có thể nhận phòng nếu phòng trống.

### 18.2. Luồng trả muộn sau thanh toán trước

Thiết kế nghiệp vụ có hỗ trợ kiểm tra trả muộn và phụ thu, nhưng cần kiểm thử kỹ phần trigger update hóa đơn vì phần tính lại tiền phòng trong `TR_Invoice_ManageUpdate` đang được comment. Nếu muốn bảo vệ chắc chắn, nên chuẩn bị một test case thực tế để chứng minh số tiền phát sinh có được cập nhật đúng.

### 18.3. Đồng bộ script database

`HotelManagement_new.sql` đã có `reservationStatus` và `sp_MarkReservationNoShow`. File `AddReservationNoShow.sql` là script bổ sung cho database đã tạo trước đó. Khi cài mới từ đầu thì dùng `HotelManagement_new.sql`; khi nâng cấp database cũ thì chạy thêm script bổ sung.

### 18.4. Khoảng thời gian background service

Comment có chỗ ghi 30 phút, nhưng service hiện cấu hình interval 5 phút. Khi trình bày nên nói chung là hệ thống chạy định kỳ để cập nhật phòng gần giờ nhận, hoặc cập nhật comment/code cho thống nhất trước khi nộp chính thức.

## 19. Kết luận

HotelManagement là hệ thống quản lý khách sạn có đầy đủ vòng đời nghiệp vụ từ chuẩn bị dữ liệu, đặt phòng, nhận phòng, lưu trú, dịch vụ phát sinh, trả phòng, thanh toán, hóa đơn, báo cáo đến vận hành phòng sau checkout. Điểm mạnh của hệ thống là có sự kết hợp giữa MVC để xử lý giao diện/người dùng và SQL để đảm bảo tính đúng đắn của nghiệp vụ cốt lõi.

Khi báo cáo, nên nhấn mạnh rằng mỗi reservation đi qua nhiều trạng thái và tác động đến nhiều phân hệ:

- Reservation ảnh hưởng đến lịch phòng.
- Check-in ảnh hưởng đến trạng thái phòng.
- Dịch vụ ảnh hưởng đến hóa đơn.
- Checkout ảnh hưởng đến hóa đơn, thanh toán và task dọn phòng.
- Dọn phòng/bảo trì ảnh hưởng đến khả năng tiếp tục kinh doanh phòng.
- Báo cáo lấy dữ liệu từ toàn bộ chuỗi nghiệp vụ để hỗ trợ quản lý ra quyết định.

