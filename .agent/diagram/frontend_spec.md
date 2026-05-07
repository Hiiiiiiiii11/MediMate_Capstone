# MediMate Frontend – Specification Cập Nhật

> Tài liệu này mô tả các thay đổi cần thực hiện trên ứng dụng Mobile (User App) và Admin App để khớp với backend hiện tại.  
> **Nguyên tắc:** Tái sử dụng flow đã có (ví dụ: WebView thanh toán PayOS, màn hình thành công chung), không viết lại từ đầu.

---

## I. THÊM / CẬP NHẬT API CALLS

### 1. Đặt Lịch & Thanh Toán

| Hành động | Method | Endpoint | Ghi chú |
|-----------|--------|----------|---------|
| Tạo lịch hẹn | `POST` | `/api/v1/appointments` | Trả về `{ appointmentId, checkoutUrl, orderCode }` |
| Xem chi tiết lịch | `GET` | `/api/v1/appointments/detail/{appointmentId}` | |
| Xem lịch của Doctor | `GET` | `/api/v1/appointments/doctor/me` | |
| Xem lịch của Member | `GET` | `/api/v1/appointments/member/{memberId}` | |
| Cập nhật trạng thái (Doctor) | `PUT` | `/api/v1/appointments/{id}/status` | Body: `{ status }` |
| Hủy lịch | `PUT` | `/api/v1/appointments/{id}/cancel` | Body: `{ cancelReason }` |
| Cập nhật PaymentStatus | `PUT` | `/api/v1/appointments/{id}/payment-status` | Dùng khi PayOS return về |
| Hủy slot chưa thanh toán | `DELETE` | `/api/v1/appointments/{id}/unpaid` | Gọi khi User chủ động thoát/hủy ở màn checkout PayOS |
| Lấy lịch chờ refund (Admin) | `GET` | `/api/v1/appointments/refundable` | |
| Hoàn tất refund (Admin) | `PUT` | `/api/v1/appointments/{id}/complete-refund` | **multipart/form-data**, field: `TransferImage` |

### 2. Phiên Tư Vấn (ConsultationSession)

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Lấy session theo appointment | `GET` | `/api/v1/sessions/by-appointment/{appointmentId}` |
| Lấy session của Doctor hiện tại | `GET` | `/api/v1/sessions/me` |
| Join phiên | `POST` | `/api/v1/sessions/{sessionId}/join` | Body: `{ role: "user" \| "doctor" }` |
| Báo bác sĩ trễ | `POST` | `/api/v1/sessions/{sessionId}/doctor-late` | Body: `{ lateMinutes }` |
| Hủy do no-show | `PUT` | `/api/v1/sessions/{sessionId}/cancel-no-show` | |
| Kết thúc phiên | `POST` | `/api/v1/sessions/{sessionId}/end` | |
| Gắn đơn thuốc | `POST` | `/api/v1/sessions/{sessionId}/attach-prescription` | |
| Xem URL ghi hình | `GET` | `/api/v1/sessions/{sessionId}/recording` | Chỉ Doctor & chủ hộ |

### 3. Payout (Admin App)

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Xem tổng công nợ | `GET` | `/api/v1/payout/summary` |
| Xem chi tiết payout | `GET` | `/api/v1/payout?clinicId=&status=&pageNumber=&pageSize=` |
| Tất toán cho clinic | `POST` | `/api/v1/payout/clinic/{clinicId}/process` | **multipart/form-data**: `TransferImage`, `ReportFile`, `Note` |

### 4. Đơn Thuốc

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Tạo đơn thuốc (User - sau OCR) | `POST` | `/api/v1/prescriptions/member/{memberId}` |
| Tạo đơn trống | `POST` | `/api/v1/prescriptions/member/{memberId}/empty` |
| Xem danh sách đơn thuốc | `GET` | `/api/v1/prescriptions/member/{memberId}` |
| Xem chi tiết đơn | `GET` | `/api/v1/prescriptions/{id}` |
| Tạo đơn thuốc (Doctor - trong session) | `POST` | `/api/v1/prescriptions-by-doctor/session/{sessionId}` |

### 5. Lịch Uống Thuốc

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Tạo lịch uống | `POST` | `/api/v1/members/{memberId}/schedules` |
| Tạo nhiều lịch | `POST` | `/api/v1/members/{memberId}/schedules/bulk` |
| Xem lịch theo ngày | `GET` | `/api/v1/members/{memberId}/schedules?date=YYYY-MM-DD` |
| Ghi nhận đã uống | `POST` | `/api/v1/medication-logs` | Body: `{ scheduleId, status: "Taken"\|"Skipped" }` |

### 6. Gia Đình & Hồ Sơ Sức Khỏe

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Tạo hộ cá nhân | `POST` | `/api/v1/families/personal` |
| Tạo hộ chia sẻ | `POST` | `/api/v1/families/shared` |
| Xem danh sách gia đình | `GET` | `/api/v1/families` |
| Xem hồ sơ sức khỏe | `GET` | `/api/v1/health/member/{memberId}` |
| Tạo hồ sơ sức khỏe | `POST` | `/api/v1/health/member/{memberId}` |
| Cập nhật hồ sơ | `PUT` | `/api/v1/health/member/{memberId}` |
| Thêm bệnh nền | `POST` | `/api/v1/health/member/{memberId}/conditions` |
| Thêm dị ứng | `POST` | `/api/v1/health/member/{memberId}/allergies` |

### 7. Gói Dịch Vụ (Subscription)

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Xem danh sách gói | `GET` | `/api/v1/membership-packages` |
| Tạo link thanh toán gói | `POST` | `/api/v1/payment/create` | Body: `{ subscriptionId, returnUrl, cancelUrl }` |

### 8. Tài Khoản Ngân Hàng User

| Hành động | Method | Endpoint |
|-----------|--------|----------|
| Xem thông tin ngân hàng | `GET` | `/api/v1/user/bank-account` |
| Thêm tài khoản ngân hàng | `POST` | `/api/v1/user/bank-account` |
| Cập nhật | `PUT` | `/api/v1/user/bank-account` |
| Xóa | `DELETE` | `/api/v1/user/bank-account` |

---

## II. MÀNG HÌNH CẦN THÊM MỚI

### A. User App

#### 1. Màn hình Thanh toán lịch khám (PaymentScreen)
**Trigger:** Sau khi user chọn giờ khám, bấm "Đặt lịch"  
**Flow:**
```
CreateAppointment (POST) → nhận checkoutUrl
→ Mở WebView PayOS với checkoutUrl
→ PayOS redirect về returnUrl (deep link)
→ App nhận deep link → gọi GET /appointments/detail/{id}
→ Nếu PaymentStatus == "Paid" → chuyển sang màn Đặt lịch thành công
→ Nếu Cancelled → hiện thông báo thất bại
```
> **Tái sử dụng:** Dùng lại WebView và màn thành công chung đang dùng cho Subscription

#### 2. Màn hình Chi tiết Lịch hẹn (AppointmentDetailScreen)
**Fields cần hiển thị:**
- Tên bác sĩ, phòng khám, chuyên khoa
- Tên bệnh nhân (Member)
- Ngày giờ khám
- `Status` + `PaymentStatus` (badge màu)
- Nút **"Vào phòng khám"** (khi Status=Approved và đến giờ)
- Nút **"Hủy lịch"** (khi Status=Pending/Approved và chưa vào phòng)
- Nút **"Xem video ghi hình"** (khi Session ended, chỉ chủ hộ)

#### 3. Màn hình Phiên Tư Vấn - Chờ kết nối (SessionWaitingScreen)
**Trigger:** User bấm "Vào phòng khám"  
**Flow:**
```
GET /sessions/by-appointment/{appointmentId} → lấy sessionId
→ POST /sessions/{sessionId}/join { role: "user" }
→ Hiển thị màn chờ (spinner): "Đang chờ bác sĩ..."
→ Nhận SignalR event "SessionStarted" hoặc poll session status
→ Khi Session.Status == "InProgress" → vào màn video call Agora
```
**Nút hành động:**
- "Báo bác sĩ trễ" → `POST /sessions/{sessionId}/doctor-late`
- "Thoát - Bác sĩ không tham gia" (sau N phút) → `PUT /sessions/{sessionId}/cancel-no-show`

#### 4. Màn hình sau Phiên tư vấn (PostSessionScreen)
**Trigger:** Khi session kết thúc (Doctor end session)  
**Hiển thị:**
- Thời lượng cuộc gọi
- Link xem lại video (nếu có RecordingUrl)
- Đơn thuốc bác sĩ gửi (nếu có)
- Nút **"Tạo lịch uống thuốc"** từ đơn thuốc này
- Nút **"Xem lại video"** (nếu có gói Subscription)

#### 5. Màn hình Hủy lịch & Trạng thái Refund (RefundStatusScreen)
**Trigger:** User hủy lịch đã thanh toán  
**Hiển thị:**
- Thông báo "Lịch đã hủy thành công"
- Status: `PaymentStatus = "Refunded"` → "Đang chờ hoàn tiền"
- Thông tin tài khoản ngân hàng đã đăng ký (để admin chuyển tiền về)
- Nút **"Cập nhật tài khoản ngân hàng"** (nếu chưa có)

#### 6. Màn hình Tài khoản Ngân hàng (BankAccountScreen)
**Path:** Cài đặt > Tài khoản nhận hoàn tiền  
**Fields:** Tên ngân hàng, Số tài khoản, Chủ tài khoản  
**Lưu ý:** Mỗi User chỉ có 1 tài khoản

#### 7. Màn hình Lịch sử Sử dụng Gói (SubscriptionHistoryScreen)
**Path:** Gói dịch vụ > Lịch sử sử dụng  
**Hiển thị:** Số lượt OCR còn lại, lịch sử trừ lượt

---

### B. Doctor App

#### 1. Màn hình Danh sách Lịch hẹn chờ duyệt (PendingAppointmentsScreen)
**Status filter:** `Status=Pending, PaymentStatus=Paid`  
**Hiển thị mỗi item:**
- Tên bệnh nhân (Member.FullName)
- Ngày giờ khám
- Số tiền (Amount)
**Nút:** "Chấp nhận" → `PUT /status { status: "Approved" }` | "Từ chối" → Cancel

#### 2. Màn hình Đơn thuốc trong Session (PrescriptionInSessionScreen)
**Trigger:** Doctor bấm "Kê đơn" trong phiên tư vấn  
**Flow:**
```
POST /prescriptions-by-doctor/session/{sessionId}
→ Đơn thuốc được lưu và gửi vào chat
→ Nút "Kết thúc phiên" → POST /sessions/{sessionId}/end
```

---

### C. Admin App

#### 1. Màn hình Tổng quan Công nợ Phòng khám (PayoutDashboardScreen)
**Data source:** `GET /api/v1/payout/summary`  
**Hiển thị:**
- Danh sách phòng khám + Tổng tiền đang ReadyToPay
- Badge đếm số lượt chờ tất toán

#### 2. Màn hình Chi tiết Công nợ (PayoutDetailScreen)
**Data source:** `GET /api/v1/payout?clinicId=&status=ReadyToPay`  
**Hiển thị mỗi item:**
- Ngày giờ khám (`AppointmentDate`, `AppointmentTime`)
- Tên bệnh nhân (`PatientName`)
- Tên bác sĩ (`DoctorName`)
- Trạng thái thanh toán (`PaymentStatus`)
- Tên người đặt lịch (`PayerName`, `PayerPhoneNumber`)
- **Thông tin ngân hàng hoàn tiền:** `PayerBankName`, `PayerBankAccountNumber`, `PayerBankAccountHolder`
- Số tiền (`Amount`)

#### 3. Màn hình Tất toán (ProcessPayoutScreen)
**Trigger:** Admin bấm "Tất toán cho [Clinic Name]"  
**Fields:**
- Upload ảnh chứng minh chuyển khoản (TransferImage) *(tùy chọn)*
- Upload file báo cáo PDF/Excel (ReportFile) *(tùy chọn)*
- Ghi chú (Note)
**API:** `POST /payout/clinic/{clinicId}/process` (multipart/form-data)

#### 4. Màn hình Quản lý Hoàn tiền (RefundManagementScreen)
**Data source:** `GET /api/v1/appointments/refundable`  
**Hiển thị:**
- Danh sách lịch hẹn có `PaymentStatus = "Refunded"`
- Thông tin ngân hàng của người đặt lịch để chuyển tiền
**Nút:** "Đã chuyển tiền" → upload ảnh → `PUT /appointments/{id}/complete-refund`

---

## III. CẬP NHẬT CÁC MÀN HÌNH HIỆN CÓ

### 1. Màn hình Danh sách Lịch hẹn (AppointmentListScreen)
**Thêm:**
- Filter tab: `Tất cả | Chờ xác nhận | Đã xác nhận | Hoàn thành | Đã hủy`
- Badge `PaymentStatus` bên cạnh badge `Status`
- Item có `PaymentStatus = "Refunded"` → hiện nhãn "Đang chờ hoàn tiền" màu vàng
- Item có `PaymentStatus = "RefundCompleted"` → hiện nhãn "Đã hoàn tiền" màu xanh

### 2. Màn hình Profile Bác sĩ (DoctorProfileScreen)
**Thêm:**
- Tên phòng khám (ClinicName)
- Phí khám (ConsultationFee / giá từ Clinic)
- Nút "Đặt lịch" → redirect sang Booking Flow → **PayOS Payment** (không bỏ qua thanh toán)

### 3. Màn hình Subscription / Gói dịch vụ (SubscriptionScreen)
**Bỏ:** Đặc quyền "lượt tư vấn" (ConsultantLimit) khỏi mô tả gói — tư vấn hiện tính phí riêng  
**Giữ:** Lượt OCR, Video replay  
**Thêm:** Hiển thị số OCR còn lại (`RemainingOcrCount`)

### 4. Màn hình Hồ sơ thành viên (MemberProfileScreen)
**Thêm tab "Sức khỏe":**
- HealthProfile (chiều cao, cân nặng, nhóm máu)
- Bệnh nền (MedicalConditions)
- Dị ứng (Allergies)
- Nút chỉnh sửa từng mục

### 5. Màn hình Video Call (VideoCallScreen)
**Thêm:**
- Nút "Kê đơn thuốc" (Doctor only) trong call
- Khi Doctor end → gọi `POST /sessions/{sessionId}/end` → navigate sang PostSessionScreen
- Khi User nhận SignalR "SessionEnded" → tự động navigate về PostSessionScreen

---

## IV. BỎ / VÔ HIỆU HÓA

| Tính năng | Lý do |
|-----------|-------|
| Đánh giá / Rating sau khám | Chưa có backend logic hoàn chỉnh (Rating controller rỗng) |
| Đặt lịch không cần thanh toán | Backend yêu cầu bắt buộc thanh toán PayOS trước |
| Lượt tư vấn từ Subscription | Tư vấn đã đổi sang Pay-per-booking |
| Bác sĩ tự do (Freelance) | Admin chỉ quản lý bác sĩ qua Clinic |

---

## V. FLOW THANH TOÁN - PHẢI TÁI SỬ DỤNG

```
[Mua Subscription hiện tại]
SubscriptionScreen
  → POST /payment/create { subscriptionId }
  → WebView (checkoutUrl PayOS)
  → Deep link callback /?orderCode=&status=PAID
  → GET /payment/info/{orderCode}
  → Nếu success → SuccessScreen (có sẵn)

[ĐẶT LỊCH - tái sử dụng hoàn toàn flow trên]
BookingScreen
  → POST /appointments { doctorId, memberId, date, time }
  → Nhận { checkoutUrl, appointmentId }
  → WebView (checkoutUrl PayOS)  ← DÙNG LẠI
  → Deep link callback /?orderCode=&status=PAID
  → GET /appointments/detail/{appointmentId}
  → Nếu PaymentStatus == "Paid" → AppointmentSuccessScreen
  → Nếu Cancelled/Failed → hiển thị lỗi
```

---

## VI. SIGNALR EVENTS CẦN LẮNG NGHE

| Event | Payload | Xử lý trên Frontend |
|-------|---------|---------------------|
| `AppointmentStatusUpdated` | `{ appointmentId, status, paymentStatus }` | Refresh chi tiết lịch hẹn |
| `SessionStarted` (custom) | `{ sessionId }` | Chuyển sang màn Video Call |
| `SessionEnded` | `{ sessionId }` | Chuyển sang PostSessionScreen |
| `ForceLogout` | `{ message }` | Logout + về màn Login |
| `NewAppointmentNotification` | - | Bác sĩ: refresh danh sách lịch chờ |

---

## VII. STATUS BADGE MAPPING

| Status | Màu | Label |
|--------|-----|-------|
| Appointment: `Pending` | Vàng | Chờ xác nhận |
| Appointment: `Approved` | Xanh dương | Đã xác nhận |
| Appointment: `Completed` | Xanh lá | Hoàn thành |
| Appointment: `Cancelled` | Đỏ | Đã hủy |
| PaymentStatus: `Pending` | Xám | Chờ thanh toán |
| PaymentStatus: `Paid` | Xanh lá | Đã thanh toán |
| PaymentStatus: `Refunded` | Vàng | Đang hoàn tiền |
| PaymentStatus: `RefundCompleted` | Xanh lá nhạt | Đã hoàn tiền |
| DoctorPayout: `Hold` | Xám | Tạm giữ |
| DoctorPayout: `ReadyToPay` | Cam | Chờ tất toán |
| DoctorPayout: `Paid` | Xanh lá | Đã tất toán |
