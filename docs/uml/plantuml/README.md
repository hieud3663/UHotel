# Bộ sơ đồ PlantUML cho HotelManagement

Thư mục này chứa các sơ đồ được xây dựng từ kế hoạch trong `docs/uml/uml-diagram-planning.md` và kết quả rà soát lại bằng GitNexus MCP.

## Cấu trúc

```text
plantuml/
├── activity/
├── class/
├── sequence/
└── usecase/
```

## Activity Diagram

| File | Nội dung |
|---|---|
| `activity/Activity_TongQuan_QuyTrinhKhachSan.puml` | Quy trình tổng quát từ đăng nhập đến báo cáo. |
| `activity/Activity_Reservation_CreateCancel.puml` | Tạo và hủy đặt phòng. |
| `activity/Activity_CheckIn.puml` | Nhận phòng/check-in. |
| `activity/Activity_RoomService_Usage.puml` | Thêm, cập nhật, xóa dịch vụ phòng. |
| `activity/Activity_RoomMaintenanceCleaning.puml` | Quản lý bảo trì và dọn phòng. |
| `activity/Activity_CheckOut_CheckoutThenPay.puml` | Checkout trước, thanh toán sau. |
| `activity/Activity_CheckOut_PayThenCheckout.puml` | Thanh toán trước, checkout sau. |
| `activity/Activity_Background_RoomStatusUpdate.puml` | Cập nhật trạng thái phòng tự động. |

## Class Diagram

| File | Nội dung |
|---|---|
| `class/Class_TongQuan_DomainModel.puml` | Domain model tổng quát và quan hệ chính. |
| `class/Class_Reservation_CheckIn_CheckOut.puml` | Nhóm đặt phòng, check-in, check-out, invoice. |
| `class/Class_ServiceUsage.puml` | Nhóm dịch vụ phòng. |
| `class/Class_RoomMaintenanceCleaning.puml` | Nhóm bảo trì và dọn phòng. |
| `class/Class_Account_Authorization.puml` | Nhóm tài khoản và phân quyền. |

## Sequence Diagram

| File | Nội dung |
|---|---|
| `sequence/Sequence_TongQuan_ReservationToCheckout.puml` | Luồng tổng quát từ đặt phòng đến checkout. |
| `sequence/Sequence_Reservation_Create.puml` | Tạo đặt phòng. |
| `sequence/Sequence_CheckIn.puml` | Check-in. |
| `sequence/Sequence_RoomService_AddUpdateDelete.puml` | Thêm/cập nhật/xóa dịch vụ phòng. |
| `sequence/Sequence_RoomMaintenanceCleaning.puml` | Quản lý bảo trì và dọn phòng. |
| `sequence/Sequence_CheckOut_CheckoutThenPay.puml` | Checkout trước, thanh toán sau. |
| `sequence/Sequence_CheckOut_PayThenCheckout.puml` | Thanh toán trước, checkout sau. |
| `sequence/Sequence_Webhook_ConfirmPayment.puml` | Webhook xác nhận thanh toán chuyển khoản. |

## Use Case Diagram

| File | Nội dung |
|---|---|
| `usecase/UseCase_TongQuan_HeThongQuanLyKhachSan.puml` | Use case tổng quan toàn hệ thống. |
| `usecase/UseCase_Reservation.puml` | Use case đặt phòng. |
| `usecase/UseCase_CheckIn.puml` | Use case check-in. |
| `usecase/UseCase_RoomService.puml` | Use case dịch vụ phòng. |
| `usecase/UseCase_RoomMaintenanceCleaning.puml` | Use case bảo trì và dọn phòng. |
| `usecase/UseCase_CheckOutPayment.puml` | Use case check-out và thanh toán. |
| `usecase/UseCase_Report.puml` | Use case báo cáo. |

## Cách render nhanh

Có thể render bằng PlantUML extension trong VS Code hoặc bằng CLI:

```powershell
plantuml docs/uml/plantuml/**/*.puml
```

## Ghi chú

- Các sơ đồ ưu tiên biểu diễn luồng nghiệp vụ chính và các stored procedure quan trọng.
- Một số chi tiết cần tiếp tục xác minh với SQL script trước khi chốt báo cáo cuối: role `MANAGER`, receipt type `CHECKOUT`, các trường tổng tiền invoice và luồng đổi phòng.
