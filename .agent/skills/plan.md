# Kế hoạch Chỉnh sửa Flow – Bác sĩ Gia đình (Family Doctor)

> Ngày: 2026-04-23  
> Mục tiêu: Bỏ flow đặt lịch khám truyền thống, thay bằng mô hình Bác sĩ Gia đình gắn trực tiếp với Family.

---

## Tổng quan thay đổi

| Cũ (Appointment Flow) | Mới (Family Doctor Flow) |
|---|---|
| User đặt lịch khám → chờ bác sĩ duyệt | User mua gói → thêm bác sĩ gia đình vào Family |
| Mỗi lần khám tốn 1 lượt (ConsultantLimit) | Bác sĩ gắn với Family suốt thời gian gói còn hiệu lực |
| Bác sĩ nhận tiền theo từng phiên | Bác sĩ nhận tiền theo tỷ lệ từ gói khi tham gia Family |
| Session tạm thời, 1 lần | Session cố định tồn tại suốt thời gian bác sĩ trong Family |
| Không có khái niệm bác sĩ "của" gia đình | 1 Family có thể có 1..N bác sĩ tùy gói |

---

## Quy tắc nghiệp vụ mới

1. **Gói dịch vụ quyết định số lượng bác sĩ:** Mỗi `MembershipPackage` có thêm field `DoctorSlots` (số bác sĩ tối đa được gắn vào 1 Family).
2. **Giới hạn bác sĩ:** Mỗi bác sĩ chỉ được tham gia tối đa **5 Family** cùng lúc.
3. **Thanh toán cho bác sĩ:** Khi bác sĩ được thêm vào Family, hệ thống tính ngay phần hoa hồng từ gói hiện tại → tạo `DoctorPayout` trạng thái Pending.
4. **FCM Key của bác sĩ:** Khi tham gia Family, bác sĩ đăng ký FCM token → nhận push notification khi có khẩn cấp hoặc yêu cầu tư vấn từ Family đó.
5. **Session cố định:** Mỗi cặp (Family ↔ Bác sĩ) có 1 `ConsultationSession` cố định (dạng "Permanent"), không bị đóng sau call. Call video / chat đều dùng session này.
6. **Kết thúc quan hệ:** Khi gói hết hạn hoặc bác sĩ bị xóa khỏi Family → Session chuyển sang Inactive, FCM token của bác sĩ không còn nhận thông báo từ Family đó.

---

## Thay đổi Database (Models)

### Bảng mới: `FamilyDoctors`
```
FamilyDoctorId  (Guid, PK)
FamilyId        (Guid, FK → Families)
DoctorId        (Guid, FK → Doctors)
JoinedAt        (DateTime)
Status          (string) – Active | Inactive | Removed
DoctorFcmToken  (string?) – FCM token bác sĩ đăng ký cho Family này
SessionId       (Guid, FK → ConsultationSessions) – session cố định
PayoutId        (Guid?, FK → DoctorPayout) – thanh toán tương ứng
```

### Sửa `MembershipPackages` – thêm field:
```
DoctorSlots     (int) – số bác sĩ tối đa được gắn vào 1 Family
DoctorPayoutRate (decimal) – % hoặc số tiền cố định trả cho bác sĩ khi join
```

### Sửa `ConsultationSessions` – thêm field:
```
SessionType     (string) – "Permanent" | "OneTime" (cũ)
IsActive        (bool)   – true: còn hiệu lực
FamilyDoctorId  (Guid?)  – liên kết với FamilyDoctor record
```

### Bảng `Appointments` – **Giữ lại nhưng không bắt buộc**
> Có thể dùng cho trường hợp đặt lịch nội bộ trong Family (bác sĩ hẹn khám định kỳ). Không còn dùng cho việc booking từ ngoài vào.

### Sửa `Doctors` – thêm field:
```
ActiveFamilyCount (int)  – số Family đang tham gia (max 5)
```

---

## Flow mới chi tiết

### FLOW A – Mua gói & Tạo FamilySubscription (giữ nguyên)
```
User mua gói → PayOS → Webhook
  → Tạo FamilySubscriptions
  → Cấp DoctorSlots cho Family theo gói
```

### FLOW B – Thêm bác sĩ gia đình vào Family
```
POST /api/v1/families/{familyId}/doctors
  Body: { DoctorId }
  [Authorize – chỉ Owner của Family]

  → FamilyDoctorService.AddDoctorToFamilyAsync(familyId, doctorId, userId)
    1. Kiểm tra userId là Owner của Family
    2. Kiểm tra FamilySubscription còn hiệu lực
    3. Kiểm tra Family chưa đầy DoctorSlots
    4. Kiểm tra Doctor.ActiveFamilyCount < 5
    5. Tạo FamilyDoctors record (Status = Active)
    6. Tạo ConsultationSession (Type = Permanent, Status = Active)
       → Lưu SessionId vào FamilyDoctors
    7. Tính DoctorPayout = Package.Price * DoctorPayoutRate
       → Tạo DoctorPayout (Status = Pending)
    8. Cập nhật Doctor.ActiveFamilyCount += 1
    9. Gửi notification cho bác sĩ: "Bạn đã được thêm vào gia đình [FamilyName]"
```

### FLOW C – Bác sĩ đăng ký FCM Token cho Family
```
POST /api/v1/families/{familyId}/doctors/fcm-token
  Body: { FcmToken }
  [Authorize – Role = Doctor]

  → FamilyDoctorService.RegisterDoctorFcmTokenAsync(familyId, doctorId, fcmToken)
    1. Tìm FamilyDoctors record (familyId + doctorId + Status=Active)
    2. Cập nhật DoctorFcmToken
```

### FLOW D – Yêu cầu tư vấn / Khẩn cấp từ Family
```
POST /api/v1/families/{familyId}/doctors/{doctorId}/consult-request
  Body: { MemberId, Type: "Consultation" | "Emergency", Note? }
  [Authorize]

  → FamilyDoctorService.RequestConsultationAsync(...)
    1. Lấy FamilyDoctors.DoctorFcmToken
    2. Gửi FCM push notification đến bác sĩ:
       - Type = Emergency → High priority notification
       - Type = Consultation → Normal notification
    3. Tạo Notification record cho bác sĩ
    4. Trả về SessionId (session cố định) để cả 2 bên join call
```

### FLOW E – Join Video Call trong Session cố định
```
POST /api/v1/sessions/{sessionId}/join
  Body: { Role: "user" | "doctor" }

  → ConsultationService.JoinSessionAsync()  (giữ nguyên logic cũ)
    → Tạo Agora RTC token cho sessionId
    → Khi bác sĩ join → push notification cho members của Family
```

### FLOW F – Xóa bác sĩ khỏi Family
```
DELETE /api/v1/families/{familyId}/doctors/{doctorId}
  [Authorize – Owner hoặc Admin]

  → FamilyDoctorService.RemoveDoctorFromFamilyAsync(...)
    1. FamilyDoctors.Status = Removed
    2. ConsultationSessions.IsActive = false (session cố định → Inactive)
    3. Xóa DoctorFcmToken khỏi record
    4. Doctor.ActiveFamilyCount -= 1
    5. Gửi notification cho bác sĩ: "Bạn đã bị xóa khỏi gia đình [FamilyName]"
```

### FLOW G – Hết hạn gói (Auto)
```
Hangfire Job: FamilySubscriptionExpiryJob (chạy hàng ngày)
  → Tìm FamilySubscriptions hết hạn
  → Với mỗi Family hết hạn:
      → FamilyDoctors của Family → Status = Inactive
      → ConsultationSessions tương ứng → IsActive = false
      → Doctor.ActiveFamilyCount -= n
      → Gửi notification cho Owner: "Gói dịch vụ đã hết hạn, bác sĩ gia đình bị ngắt kết nối"
      → Gửi notification cho Bác sĩ: "Gói của gia đình [X] đã hết hạn"
```

---

## Endpoints mới cần tạo

| Method | Endpoint | Mô tả |
|---|---|---|
| `POST` | `/api/v1/families/{familyId}/doctors` | Thêm bác sĩ vào Family |
| `DELETE` | `/api/v1/families/{familyId}/doctors/{doctorId}` | Xóa bác sĩ khỏi Family |
| `GET` | `/api/v1/families/{familyId}/doctors` | Lấy danh sách bác sĩ của Family |
| `POST` | `/api/v1/families/{familyId}/doctors/fcm-token` | Bác sĩ đăng ký FCM token |
| `POST` | `/api/v1/families/{familyId}/doctors/{doctorId}/consult-request` | Gửi yêu cầu tư vấn / khẩn cấp |
| `GET` | `/api/v1/doctors/me/families` | Bác sĩ xem danh sách Family đang quản lý |
| `GET` | `/api/v1/doctors/me/families/{familyId}/session` | Lấy session cố định với Family |

---

## Endpoints cũ cần bỏ / điều chỉnh

| Endpoint | Hành động |
|---|---|
| `POST /api/v1/appointments` | **Bỏ** – không đặt lịch từ bên ngoài nữa |
| `GET /api/v1/appointments/doctors/{doctorId}/available-slots` | **Bỏ** |
| `PUT /api/v1/appointments/{id}/status` | **Bỏ** |
| `PUT /api/v1/appointments/{id}/cancel` | **Bỏ** |
| `POST /api/v1/sessions/{sessionId}/end` | **Giữ** nhưng Session Permanent không bị close, chỉ kết thúc call |
| `MembershipPackages.ConsultantLimit` | **Bỏ** field này |
| `FamilySubscriptions.ConsultantLimit` | **Bỏ** field này |

---

## Danh sách file cần tạo mới

### Repository
- `IFamilyDoctorRepository.cs`
- `Implementations/FamilyDoctorRepository.cs`

### Model
- `FamilyDoctors.cs`

### Service
- `IFamilyDoctorService.cs`
- `Implementations/FamilyDoctorService.cs`

### Controller
- `FamilyDoctorController.cs`

### DTO
- `FamilyDoctorDTO.cs`
  - `AddDoctorToFamilyRequest`
  - `FamilyDoctorResponse`
  - `ConsultRequestDto`
  - `RegisterDoctorFcmRequest`

### Migration
- Thêm bảng `FamilyDoctors`
- Sửa `MembershipPackages` (+`DoctorSlots`, +`DoctorPayoutRate`)
- Sửa `ConsultationSessions` (+`SessionType`, +`IsActive`, +`FamilyDoctorId`)
- Sửa `Doctors` (+`ActiveFamilyCount`)

### Hangfire Job
- `FamilySubscriptionExpiryJob.cs` (mới)

---

## Sơ đồ quan hệ mới

```
MembershipPackages
  └── DoctorSlots (số bác sĩ tối đa)
  └── DoctorPayoutRate (% hoa hồng)

Families ──── FamilySubscriptions ──── MembershipPackages
  └── FamilyDoctors (1..N theo DoctorSlots)
        ├── DoctorId ──── Doctors (max 5 families)
        ├── DoctorFcmToken
        ├── SessionId ──── ConsultationSessions (Permanent)
        └── PayoutId  ──── DoctorPayout (Pending → Paid)
```

---

## Thứ tự triển khai (Implementation Order)

1. **Migration** – Thêm bảng & fields mới
2. **Model** – `FamilyDoctors.cs`
3. **Repository** – `IFamilyDoctorRepository` + Implementation
4. **DTO** – `FamilyDoctorDTO.cs`
5. **Service** – `IFamilyDoctorService` + `FamilyDoctorService`
   - `AddDoctorToFamilyAsync`
   - `RemoveDoctorFromFamilyAsync`
   - `RegisterDoctorFcmTokenAsync`
   - `RequestConsultationAsync`
   - `GetDoctorsByFamilyAsync`
   - `GetFamiliesByDoctorAsync`
6. **Controller** – `FamilyDoctorController`
7. **Sửa `PayOSService`** – bỏ ConsultantLimit, thêm logic DoctorSlots sau khi thanh toán thành công
8. **Sửa `ConsultationService`** – hỗ trợ SessionType = Permanent (không close sau call)
9. **Sửa `MembershipPackageService`** – thêm DoctorSlots, DoctorPayoutRate
10. **Hangfire Job** – `FamilySubscriptionExpiryJob`
11. **Xóa / Deprecate** – AppointmentsController, available-slots logic
