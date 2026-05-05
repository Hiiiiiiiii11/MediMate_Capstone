# MediMate – Danh Sách Các Flow Chính (End-to-End Workflows)

---

## 1. Flow Đăng Ký & Kích Hoạt Tài Khoản

**Bắt đầu:** User mới muốn tham gia hệ thống  
**Kết thúc:** Tài khoản được kích hoạt, nhận JWT Token

1. User nhập **số điện thoại, email, mật khẩu** → gọi `POST /api/v1/auth/register`
2. Hệ thống kiểm tra trùng SĐT/email, hash mật khẩu, tạo User với `IsActive = false`
3. Gửi **mã OTP 6 số** qua Email (hiệu lực 30 phút)
4. User nhập OTP → gọi `POST /api/v1/auth/verify-otp`
5. Hệ thống kích hoạt tài khoản `IsActive = true`, trả về **JWT Token**

---

## 2. Flow Đăng Nhập & Quản Lý Phiên

**Bắt đầu:** User có tài khoản muốn vào app  
**Kết thúc:** Nhận JWT Token, sẵn sàng sử dụng

1. User nhập **số điện thoại/email + mật khẩu** → gọi `POST /api/v1/auth/login`
2. Hệ thống xác thực BCrypt, kiểm tra `IsActive`
3. Gửi **ForceLogout** SignalR đến phiên cũ (nếu có)
4. Lưu **FCM Token** (push notification) & **CurrentSessionToken**
5. Trả về **JWT Token** → User bắt đầu sử dụng

**Biến thể – Đăng nhập Dependent qua QR:**
1. User (chủ hộ) tạo QR → app sinh **SyncToken**
2. Dependent scan QR → gọi `POST /api/v1/auth/login-qr { qrData }`
3. Hệ thống validate SyncToken + thời hạn, xóa token, sinh **JWT Dependent**

---

## 3. Flow Tạo Gia Đình & Mời Thành Viên

**Bắt đầu:** User muốn quản lý sức khỏe cho nhiều người  
**Kết thúc:** Gia đình được tạo, thành viên tham gia

1. User chọn loại hộ: **Cá nhân** (`POST /families/personal`) hoặc **Chia sẻ** (`POST /families/shared`)
2. Hệ thống tạo **Family** + tạo **Member** đầu tiên (chủ hộ)
3. *(Chia sẻ)* Chủ hộ mời thêm người → gọi `POST /members/families/{id}/invite`
4. Người được mời xác nhận → **Member** được kích hoạt trong Family
5. Chủ hộ tạo **QR Code** cho Dependent → Dependent đăng nhập bằng QR (flow 2)

---

## 4. Flow Tạo & Quản Lý Hồ Sơ Sức Khỏe

**Bắt đầu:** Thành viên cần lưu thông tin sức khỏe  
**Kết thúc:** Hồ sơ sức khỏe đầy đủ, sẵn sàng cho bác sĩ xem khi khám

1. User/Dependent gọi `POST /health/member/{memberId}` → tạo **HealthProfile** (chiều cao, cân nặng, nhóm máu)
2. Thêm **bệnh nền** → `POST /health/member/{memberId}/conditions` (tiểu đường, tim mạch, ...)
3. Thêm **dị ứng** → `POST /health/member/{memberId}/allergies`
4. Cập nhật theo thời gian → `PUT /health/member/{memberId}` (cân nặng, chiều cao)
5. Bác sĩ truy cập hồ sơ trước khi khám để nắm bệnh sử

---

## 5. Flow Mua Gói Dịch Vụ Gia Đình (Subscription)

**Bắt đầu:** User muốn nâng cấp để dùng tính năng cao cấp (OCR,xem lại video call)  
**Kết thúc:** Gói được kích hoạt, gia đình dùng được các tính năng tương ứng

1. User xem danh sách gói → `GET /membership-packages`
2. Chọn gói → hệ thống tạo **FamilySubscription** (Status=Pending)
3. Gọi `POST /payment/create { subscriptionId }` → PayOSService tạo link thanh toán
4. User redirect sang **PayOS** thanh toán thực tế
5. **PayOS Webhook** callback → hệ thống xác nhận
6. Cập nhật: `Payment.Status = "Success"`, `Transaction.Status = "Success"`
7. Kích hoạt gói: `FamilySubscription.Status = "Active"`, set StartDate/EndDate, RemainingOcrCount
8. Hủy gói cũ nếu còn hiệu lực

---

## 6. Flow Đặt Lịch Khám & Thanh Toán

**Bắt đầu:** User muốn đặt lịch khám cho thành viên gia đình  
**Kết thúc:** Lịch hẹn được xác nhận, bác sĩ nhận thông báo

1. User chọn **bác sĩ** → xem lịch trống `GET /doctors/{id}/availabilities`
2. Chọn ngày/giờ → gọi `POST /appointments { doctorId, memberId, date, time }`
3. Hệ thống kiểm tra **DoctorAvailability** (chưa bị đặt trùng)
4. Tạo **Appointment** (Status=Pending) + **Payment** (Status=Pending)
5. Tạo link thanh toán **PayOS** → trả về `{ checkoutUrl }`
6. User redirect → thanh toán thực tế trên PayOS
7. **PayOS Webhook** callback:
   - `Payment.Status = "Success"`, `Transaction.Status = "Success"`
   - `Appointment.PaymentStatus = "Paid"`, `Appointment.Status = "Approved"`
   - Tạo **DoctorPayout** (Status=Hold)
   - Tạo **ConsultationSession** (Status=Scheduled)
8. Bác sĩ nhận **Push Notification** + **SignalR** thông báo có lịch mới

---

## 7. Flow Tư Vấn Trực Tuyến (Video Call)

**Bắt đầu:** Đến giờ hẹn, cả bác sĩ và bệnh nhân vào phiên  
**Kết thúc:** Phiên kết thúc, đơn thuốc được tạo, tiền được giải phóng

1. Đến giờ hẹn (T-15 phút): HangFire kích hoạt **Reminder Notification** cho bệnh nhân
2. User & Doctor vào app → gọi `POST /sessions/{sessionId}/join { role }`
3. Khi **cả 2 join**: `ConsultationSession.Status = "InProgress"` + **Agora Video** bắt đầu
4. Trong khi tư vấn: Doctor chat qua `ChatDoctorController`, trao đổi thông tin
5. Bác sĩ kết thúc → `POST /sessions/{sessionId}/end`
   - `Session.Status = "Ended"`, `Session.EndedAt = now`
   - `Appointment.Status = "Completed"`
   - `DoctorPayout.Status = "ReadyToPay"` *(tiền chờ admin tất toán)*
6. Bác sĩ tạo **đơn thuốc** → `POST /sessions/{sessionId}/attach-prescription`
7. User xem lại **video recording** → `GET /sessions/{sessionId}/recording`

**Xử lý ngoại lệ:**
- Bác sĩ trễ: User báo → `POST /sessions/{sessionId}/doctor-late { lateMinutes }`
- Bác sĩ không tham gia: User hủy → `PUT /sessions/{sessionId}/cancel-no-show` → hoàn tiền tự động

---

## 8. Flow OCR Đơn Thuốc & Lưu Trữ

**Bắt đầu:** User có đơn thuốc giấy muốn số hóa  
**Kết thúc:** Đơn thuốc lưu trong hệ thống, sẵn sàng tạo lịch uống

1. User chụp ảnh đơn thuốc → upload lên Cloudinary `POST /upload/image`
2. App (Mobile) thực hiện **OCR** bằng AI → trích xuất thông tin thuốc
3. User xem lại kết quả OCR, chỉnh sửa nếu cần
4. Gọi `POST /prescriptions/member/{memberId}` với `{ imageUrl, medications[] }`
5. Hệ thống lưu **Prescription** + các **PrescriptionItems** (tên thuốc, liều, tần suất)
6. User tạo **lịch uống thuốc** từ đơn → sang Flow 9

---

## 9. Flow Quản Lý Lịch Uống Thuốc

**Bắt đầu:** Có đơn thuốc, cần nhắc uống đúng giờ  
**Kết thúc:** Toàn bộ liệu trình được theo dõi đầy đủ

1. User/Chủ hộ tạo lịch `POST /members/{memberId}/schedules` hoặc bulk `schedules/bulk`
   - Chỉ định: tên thuốc, giờ uống, ngày bắt đầu/kết thúc
2. HangFire tạo **Scheduled Jobs** → push notification đúng giờ uống
3. Đến giờ → User/Dependent nhận thông báo "Đến giờ uống thuốc X"
4. Sau khi uống → ghi nhận: `POST /medication-logs { scheduleId, status: "Taken" }`
5. Nếu quên → `status: "Skipped"`
6. Chủ hộ xem lịch sử tuân thủ của cả gia đình → `GET /members/{memberId}/schedules?date=`

---

## 10. Flow Hủy Lịch Hẹn & Hoàn Tiền

**Bắt đầu:** User hoặc bác sĩ muốn hủy lịch đã đặt  
**Kết thúc:** Lịch bị hủy, tiền được hoàn trả cho user

1. User gọi `PUT /appointments/{id}/cancel { cancelReason }`
2. Hệ thống cập nhật `Appointment.Status = "Cancelled"`
3. **Nếu đã thanh toán**: `Appointment.PaymentStatus = "Refunded"`
4. Hủy DoctorPayout liên quan: `DoctorPayout.Status = "Cancelled"`
5. Bác sĩ nhận **Notification** thông báo lịch bị hủy
6. Admin thấy lịch trong danh sách chờ hoàn tiền `GET /appointments/refundable`
7. Admin chuyển tiền cho User qua ngân hàng thủ công
8. Admin upload ảnh chứng minh chuyển khoản → `PUT /appointments/{id}/complete-refund [Form: transferImage]`
9. Hệ thống tạo **Payment OUT** + **Transaction OUT** (TransactionType=MoneySent)
10. `Appointment.PaymentStatus = "RefundCompleted"` → kết thúc

---

## 11. Flow Admin Tất Toán Công Nợ Phòng Khám

**Bắt đầu:** Cuối kỳ, admin cần thanh toán cho phòng khám  
**Kết thúc:** Phòng khám nhận tiền, có báo cáo đính kèm

1. Admin xem tổng hợp công nợ → `GET /payout/summary`
   - Thấy danh sách phòng khám + tổng tiền đang chờ (ReadyToPay)
2. Admin drill-down từng lịch khám → `GET /payout?clinicId=&status=ReadyToPay`
   - Xem đầy đủ: tên bệnh nhân, bác sĩ, ngày khám
   - Thông tin người thanh toán: tên, SĐT, **tên ngân hàng, số tài khoản**
3. Admin thực hiện chuyển khoản thực tế bên ngoài hệ thống
4. Admin upload **ảnh ủy nhiệm chi** + **file báo cáo** (PDF/Excel)
5. Gọi `POST /payout/clinic/{clinicId}/process [Form: TransferImage, ReportFile, Note]`
6. Hệ thống upload lên **Cloudinary**, cập nhật tất cả DoctorPayout: `Status = "Paid"`
7. Gửi **Email** tự động cho Admin phòng khám với link ảnh + báo cáo

---

## 12. Flow Quản Lý Bác Sĩ & Lịch Làm Việc (Admin Phòng Khám)

**Bắt đầu:** Phòng khám cần đăng ký bác sĩ và setup lịch  
**Kết thúc:** Bác sĩ xuất hiện trên app, user có thể đặt lịch

1. Admin phòng khám tạo tài khoản bác sĩ → `POST /management/doctors`
2. Bác sĩ cập nhật **hồ sơ** (ảnh, mô tả, chuyên khoa) → `PUT /doctors/{id}`
3. Bác sĩ upload **bằng cấp/chứng chỉ** → `POST /doctor-documents`
4. Bác sĩ thiết lập **lịch làm việc tuần** → `POST /doctor-availabilities { dayOfWeek, startTime, endTime }`
5. Bác sĩ thêm **ngày nghỉ/ngoại lệ** → `POST /doctor-availability-exceptions { date, reason }`
6. User có thể tìm kiếm bác sĩ và xem lịch trống → đặt lịch (Flow 6)

---

## Tổng Quan Vòng Đời Dữ Liệu Chính

```
User đăng ký (Flow 1)
    └─> Tạo Family + Member (Flow 3)
            ├─> Tạo HealthProfile (Flow 4)
            ├─> Mua Subscription (Flow 5)
            │       └─> FamilySubscription.Status = "Active"
            └─> Đặt lịch khám (Flow 6)
                    └─> Thanh toán PayOS
                            ├─> Payment.Status = "Success"
                            ├─> DoctorPayout.Status = "Hold"
                            └─> ConsultationSession.Status = "Scheduled"
                                    └─> Tư vấn trực tuyến (Flow 7)
                                            ├─> Session.Status = "Ended"
                                            ├─> DoctorPayout.Status = "ReadyToPay"
                                            └─> Prescription → Lịch uống thuốc (Flow 8, 9)

Admin cuối kỳ:
    Tất toán công nợ (Flow 11)
        └─> DoctorPayout.Status = "Paid"
            └─> Email thông báo Phòng khám

Nếu User hủy:
    Flow 10 (Hủy & Hoàn tiền)
        └─> PaymentStatus = "Refunded" → "RefundCompleted"
```
