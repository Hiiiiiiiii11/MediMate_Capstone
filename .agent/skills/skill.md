# MediMate – Project Flow Documentation

> Dựa theo cấu trúc code thực tế (Controllers / Services / Repositories)
> Stack: ASP.NET Core Web API · Entity Framework Core · PostgreSQL · PayOS · Firebase · Cloudinary · Agora

---

## Kiến trúc tổng quan

```
Client (Mobile/Web)
  └── MediMate (API Layer – Controllers)
        └── MediMateService (Business Logic – Services)
              └── MediMateRepository (Data Access – Repositories / EF Core)
                    └── PostgreSQL Database
```

**Shared libs:** `Share` – JWT, Common helpers, Constants, Cloudinary utils.  
**Background Jobs:** Hangfire (`ReminderJobService`, `MedicationStatusJobService`).  
**Real-time:** SignalR Hubs (`ChatHub`).

---

## FLOW 1 – Xác thực & Tài khoản (`/api/v1/auth`)

### 1.1 Đăng ký User
```
POST /api/v1/auth/register
  → AuthenticationService.RegisterAsync()
    → Tạo Users (role = User, IsActive = false)
    → Gửi OTP qua Email (EmailService)
```

### 1.2 Xác thực OTP / Kích hoạt tài khoản
```
POST /api/v1/auth/verify-otp
  → AuthenticationService.VerifyOtpAsync()
    → Kiểm tra OTP hợp lệ, chưa hết hạn
    → Set IsActive = true
    → Tạo JWT AccessToken
    → Lưu token vào HttpOnly Cookie
```

### 1.3 Đăng nhập User
```
POST /api/v1/auth/login/user
  → AuthenticationService.LoginUserAsync()
    → Kiểm tra email/password + IsActive
    → Tạo JWT (Claims: Id, Role=User)
    → Set Cookie "token"
```

### 1.4 Đăng nhập Dependent (thành viên phụ thuộc qua QR)
```
POST /api/v1/auth/login-dependent
  Body: { QrCode }
  → AuthenticationService.LoginDependentByQrAsync()
    → Tìm Member theo QrLoginCode
    → Tạo JWT (Claims: MemberId, Role=Dependent)
    → Set Cookie "token"
```

### 1.5 Đăng xuất
```
POST /api/v1/auth/logout
  → Đọc token từ Header hoặc Cookie
  → Giải mã token lấy AccountId, Role
  → AuthenticationService.LogoutAsync() – xóa FCM token, blacklist JWT
  → Xóa Cookie "token"
```

---

## FLOW 2 – Quản lý Gia đình (`/api/v1/families`)

### 2.1 Tạo chế độ Cá nhân
```
POST /api/v1/families/personal  [Authorize]
  → FamilyService.CreatePersonalFamilyAsync(userId)
    → Tạo Families (Type = Personal)
    → Tạo Member gắn với User (Role = Owner)
```

### 2.2 Tạo Gia đình dùng chung
```
POST /api/v1/families/shared    [Authorize]
  Body: { FamilyName }
  → FamilyService.CreateSharedFamilyAsync(userId, request)
    → Tạo Families (Type = Shared, Name = FamilyName)
    → Sinh JoinCode ngẫu nhiên
    → Tạo Member cho User (Role = Owner)
```

### 2.3 Lấy danh sách gia đình
```
GET /api/v1/families
  → FamilyService.GetMyFamiliesAsync(userId)
    → Trả về tất cả families mà user là thành viên
```

### 2.4 Cập nhật / Xóa gia đình
```
PUT  /api/v1/families/{id}   → FamilyService.UpdateFamilyAsync()
DELETE /api/v1/families/{id} → FamilyService.DeleteFamilyAsync()
```

### 2.5 Xem subscription của gia đình
```
GET /api/v1/families/{id}/subscription
  → FamilyService.GetFamilySubscriptionAsync(id)
```

---

## FLOW 3 – Quản lý Thành viên (`/api/v1/members`)

### 3.1 Tạo thành viên phụ thuộc (Dependent)
```
POST /api/v1/members/create-dependent-member  [Authorize]
  Body: { FamilyId, FullName, DateOfBirth, ... }
  → MemberService.CreateDependentMemberAsync(request, userId)
    → Kiểm tra userId là Owner của Family
    → Tạo Member (Type = Dependent, không có UserId)
    → Sinh QR code cho Dependent
```

### 3.2 Thêm User khác vào gia đình qua số điện thoại
```
POST /api/v1/members/add-user-member-by-phone  [Authorize]
  Body: { FamilyId, PhoneNumber }
  → MemberService.AddUserMemberToFamilyAsync(request, userId)
    → Tìm User theo PhoneNumber
    → Tạo Member liên kết User đó với Family
```

### 3.3 User tự tham gia gia đình qua JoinCode
```
POST /api/v1/members/user-join  [Authorize]
  Body: { JoinCode }
  → MemberService.JoinFamilyByJoinCodeAsync(request, userId)
    → Tìm Family theo JoinCode
    → Tạo Member cho userId trong Family đó
```

### 3.4 Tạo hồ sơ Dependent bằng JoinCode (không cần login)
```
POST /api/v1/members/dependent-join  [AllowAnonymous]
  Body: { JoinCode, FullName, ... }
  → MemberService.InitDependentProfileAsync(request, null)
    → Tạo Member (Dependent) trong Family tương ứng
    → Trả về QR code để đăng nhập sau
```

### 3.5 Tạo QR đăng nhập cho Dependent
```
POST /api/v1/members/generate-dependent-logincode?memberId=...  [Authorize]
  → MemberService.GenerateLoginQrForDependentAsync(memberId, userId)
    → Kiểm tra userId có quyền quản lý member
    → Sinh QrLoginCode mới, lưu vào Members
    → Trả về QR image/code
```

### 3.6 Cập nhật / Xóa / Rời nhóm
```
PUT    /api/v1/members/{id}         → UpdateMemberAsync()
PUT    /api/v1/members/remove/{id}  → RemoveMemberAsync()   (soft remove / rời nhóm)
DELETE /api/v1/members/delete/{id}  → DeleteMemberAsync()   (xóa hoàn toàn)
```

---

## FLOW 4 – Hồ sơ Sức khỏe (`/api/v1/health`)

### 4.1 Tạo hồ sơ sức khỏe cho Member
```
POST /api/v1/health/member/{memberId}
  Body: { Height, Weight, BloodType, ... }
  → HealthService.CreateHealthProfileAsync(memberId, userId, request)
    → Kiểm tra quyền truy cập (userId phải thuộc cùng family)
    → Tạo HealthProfiles
```

### 4.2 Xem / Cập nhật hồ sơ sức khỏe
```
GET /api/v1/health/member/{memberId}      → GetHealthProfileAsync()
PUT /api/v1/health/member/{memberId}      → UpdateHealthProfileAsync()
GET /api/v1/health/family/{familyId}      → GetHealthProfilesByFamilyIdAsync()
```

### 4.3 Quản lý bệnh án (Conditions)
```
POST   /api/v1/health/member/{memberId}/conditions  → AddConditionAsync()
GET    /api/v1/health/conditions/{conditionId}      → GetConditionByIdAsync()
PUT    /api/v1/health/conditions/{conditionId}      → UpdateConditionAsync()
DELETE /api/v1/health/conditions/{conditionId}      → RemoveConditionAsync()
```

---

## FLOW 5 – Đơn thuốc (`/api/v1/prescriptions`)

### 5.1 Tạo đơn thuốc (từ kết quả OCR)
```
POST /api/v1/prescriptions/member/{memberId}
  Body: { PrescriptionDate, DoctorName, Medicines[] }
  → PrescriptionService.CreatePrescriptionAsync()
    → Tạo Prescriptions
    → Tạo PrescriptionMedicines[]
```

### 5.2 Tạo đơn thuốc trống (nhập tay)
```
POST /api/v1/prescriptions/member/{memberId}/empty
  → PrescriptionService.CreateEmptyPrescriptionAsync()
```

### 5.3 Thêm/Sửa/Xóa thuốc trong đơn
```
POST   /api/v1/prescriptions/{id}/medicines              → AddMedicineAsync()
PUT    /api/v1/prescriptions/medicines/{medicineId}      → UpdateMedicineAsync()
DELETE /api/v1/prescriptions/medicines/{medicineId}      → DeleteMedicineAsync()
```

### 5.4 Upload ảnh đơn thuốc
```
POST /api/v1/prescriptions/{id}/images
  → PrescriptionService.AddImageToPrescriptionAsync()
    → Upload lên Cloudinary
    → Lưu URL vào PrescriptionImages
```

---

## FLOW 6 – Lịch uống thuốc & Nhắc nhở

### 6.1 Tạo lịch uống thuốc
```
POST /api/v1/members/{memberId}/schedules
  Body: { MedicineName, Frequency, Times[], StartDate, EndDate, ... }
  → MedicationSchedulesService.CreateScheduleAsync()
    → Tạo MedicationSchedules
    → Tạo MedicationScheduleDetails (từng thời điểm)
    → Tạo MedicationReminders (các nhắc nhở tương ứng)
    → Trigger ReminderJobService đăng ký Hangfire jobs
```

### 6.2 Tạo nhiều lịch cùng lúc (Bulk)
```
POST /api/v1/members/{memberId}/schedules/bulk
  → MedicationSchedulesService.CreateBulkSchedulesAsync()
```

### 6.3 Xem nhắc nhở hàng ngày
```
GET /api/v1/members/{memberId}/reminders/daily?date=...
  → MedicationSchedulesService.GetDailyRemindersAsync()

GET /api/v1/families/{familyId}/reminders/daily?date=...
  → GetFamilyDailyRemindersAsync()
```

### 6.4 Hành động với nhắc nhở (Uống / Bỏ qua)
```
PUT /api/v1/reminders/{reminderId}/action
  Body: { Action: "Taken" | "Skipped" | "Snoozed" }
  → MedicationSchedulesService.MarkReminderActionAsync()
    → Cập nhật MedicationReminders.Status
    → Tạo MedicationLogs (ghi nhận hành động)
```

### 6.5 Snooze nhắc nhở
```
POST /api/v1/reminders/{reminderId}/snooze
  Body: delayMinutes
  → MedicationSchedulesService.SnoozeReminderAsync()
    → Lùi ScheduledTime của Reminder
    → Reschedule Hangfire job
```

### 6.6 Background Job tự động
```
ReminderJobService (Hangfire)
  → Mỗi khi đến giờ: gửi push notification qua FirebaseNotificationService
  → MedicationStatusJobService: tự động đánh dấu "Missed" sau khi hết giờ
```

---

## FLOW 7 – Lịch sử uống thuốc (`/api/v1/medicationlogs`)

```
POST /api/v1/medicationlogs/action
  Body: { ReminderId, Action, ActualTime }
  → MedicationLogService.LogMedicationActionAsync()
    → Tạo MedicationLogs

GET /api/v1/medicationlogs/member/{memberId}?startDate=&endDate=
  → GetMemberLogsAsync()

GET /api/v1/medicationlogs/family/{familyId}?startDate=&endDate=
  → GetFamilyLogsAsync()

GET /api/v1/medicationlogs/stats/{scheduleId}
  → GetAdherenceStatsAsync()  (% tuân thủ)

GET /api/v1/medicationlogs/family/{familyId}/dashboard
  → GetFamilyAdherenceDashboardAsync()  (dashboard tổng hợp)
```

---

## FLOW 8 – Bác sĩ (`/api/v1/doctors`)

### 8.1 Admin tạo tài khoản Doctor
```
POST /api/v1/admin/doctors
  Body: { Email, PhoneNumber, FullName }
  → DoctorService.CreateDoctorAsync()
    → Tạo Users (role = Doctor, IsActive = false)
    → Tạo Doctors (Status = Inactive)
    → Gửi email thông báo + mã kích hoạt
```

### 8.2 Bác sĩ nộp hồ sơ (Submit Profile)
```
POST /api/v1/doctors/me/submit  [Authorize]
  Form: { FullName, Specialty, LicenseNumber, LicenseImage[], AvatarImage, ... }
  → Upload ảnh lên Cloudinary (UploadPhotoService)
  → DoctorDocumentService.CreateAsync() – lưu DoctorDocuments
  → DoctorService.SubmitPendingAsync()
    → Doctors.Status = Pending
```

### 8.3 Bác sĩ kích hoạt tài khoản
```
POST /api/v1/doctors/activate
  Body: { DoctorId, VerifyCode }
  → DoctorService.ActivateDoctorAsync()
    → Kiểm tra VerifyCode
    → Users.IsActive = true
    → Doctors.Status = Active
```

### 8.4 Cập nhật hồ sơ bác sĩ
```
PUT /api/v1/doctors/me  [Authorize]
  → DoctorService.UpdateMyProfileAsync()
    → Doctors.Status = Pending (cần duyệt lại)
```

### 8.5 Bác sĩ online/offline (Heartbeat)
```
PATCH /api/v1/doctors/me/online
  → DoctorService.HeartbeatAsync()
    → Cập nhật Doctors.IsOnline = true, LastSeenAt = now
```

### 8.6 Lấy danh sách bác sĩ (Public)
```
GET /api/v1/doctors?specialty=...  [AllowAnonymous]
  → DoctorService.GetPublicDoctorsAsync()
```

---

## FLOW 9 – Đặt lịch khám (`/api/v1/appointments`)

### 9.1 Xem slot trống của bác sĩ
```
GET /api/v1/appointments/doctors/{doctorId}/available-slots?date=...
  → AppointmentService.GetAvailableSlotsAsync()
    → Đọc DoctorAvailability, loại trừ slot đã có Appointment
```

### 9.2 Đặt lịch khám
```
POST /api/v1/appointments
  Body: { DoctorId, MemberId, AvailabilityId, AppointmentDate, AppointmentTime }
  → AppointmentService.CreateAppointmentAsync(userId, dto)
    → Kiểm tra slot còn trống
    → Kiểm tra FamilySubscription còn lượt khám (ConsultantLimit)
    → Tạo Appointments (Status = Pending)
    → Tạo ConsultationSession (Status = Scheduled)
    → Trừ lượt khám trong FamilySubscriptions
    → Gửi notification cho bác sĩ
```

### 9.3 Cập nhật trạng thái lịch hẹn
```
PUT /api/v1/appointments/{id}/status
  Body: { Status: "Confirmed" | "Completed" | "Cancelled" }
  → AppointmentService.UpdateAppointmentAsync()

PUT /api/v1/appointments/{id}/cancel
  Body: { Reason }
  → AppointmentService.CancelAppointmentAsync()
    → Appointments.Status = Cancelled
    → Hoàn trả ConsultantLimit vào FamilySubscriptions
```

---

## FLOW 10 – Phiên tư vấn (`/api/v1/sessions`)

### 10.1 Tham gia phiên tư vấn
```
POST /api/v1/sessions/{sessionId}/join
  Body: { Role: "user" | "doctor" }
  → ConsultationService.JoinSessionAsync()
    → Ghi nhận UserJoinedAt hoặc DoctorJoinedAt
    → Khi cả 2 join → Status = InProgress
    → Tạo Agora token (AgoraService) cho video call
```

### 10.2 Chat trong phiên tư vấn
```
POST /api/v1/chatdoctor/sessions/{sessionId}/messages
  Form: { Content, AttachmentFile? }
  → ChatDoctorService.SendMessageAsync()
    → Upload file lên Cloudinary (nếu có)
    → Tạo ChatDoctorMessages
    → Push realtime qua SignalR Hub

GET /api/v1/chatdoctor/sessions/{sessionId}/messages
  → GetSessionMessagesAsync()
```

### 10.3 Kết thúc phiên
```
POST /api/v1/sessions/{sessionId}/end
  → ConsultationService.EndSessionByUserAsync()
    → Sessions.Status = Ended
    → Appointments.Status = Completed
    → Tính phí bác sĩ → DoctorPayout
```

### 10.4 Bác sĩ gắn đơn thuốc vào session
```
POST /api/v1/sessions/{sessionId}/attach-prescription
  Body: { PrescriptionId }
  → ConsultationService.AttachPrescriptionAsync()
    → Gắn PrescriptionsByDoctor vào Session
```

### 10.5 Các tình huống đặc biệt
```
POST /api/v1/sessions/{sessionId}/doctor-late
  → MarkDoctorLateAsync() – ghi nhận bác sĩ đến trễ

PUT /api/v1/sessions/{sessionId}/cancel-no-show
  → CancelNoShowAsync()
    → Session bị huỷ, hoàn trả lượt khám
```

---

## FLOW 11 – Đánh giá bác sĩ (`/api/v1/ratings`)

```
POST /api/v1/ratings/session/{sessionId}
  Form: { Score, Comment, Image? }
  → RatingService.CreateRatingAsync(userId, sessionId, dto)
    → Kiểm tra Session đã Completed
    → Tạo Ratings
    → Cập nhật Doctors.AverageRating

GET /api/v1/ratings/doctor/{doctorId}  [AllowAnonymous]
  → GetDoctorReviewsAsync()

PUT /api/v1/ratings/{ratingId}   → UpdateRatingAsync()
DELETE /api/v1/ratings/{ratingId} → DeleteRatingAsync()
```

---

## FLOW 12 – Thanh toán & Gói dịch vụ

### 12.1 Xem gói membership
```
GET /api/v1/membership-packages
  → MembershipPackageService.GetAllAsync()
    → Trả về danh sách gói (Price, DurationDays, MemberLimit, ConsultantLimit, OcrLimit)
```

### 12.2 Tạo link thanh toán (PayOS)
```
POST /api/v1/payment/create  [Authorize]
  Body: { PackageId, FamilyId }
  → PayOSService.CreatePaymentLinkAsync(userId, request)
    → Tạo Payments (Status = Pending)
    → Tạo Transactions (Status = Pending)
    → Gọi PayOS API → trả về PaymentUrl
    → User thanh toán trên PayOS
```

### 12.3 Webhook xử lý kết quả thanh toán
```
POST /api/v1/payment/webhook  [Public – PayOS callback]
  → Xác thực chữ ký webhook
  → PayOSService.ProcessPaymentWebhookAsync(orderCode, isSuccess)
    → Cập nhật Payments.Status = Success/Failed
    → Cập nhật Transactions.Status
    → Nếu Success:
        → Tạo/Gia hạn FamilySubscriptions
        → Gửi notification cho User
```

### 12.4 Admin quản lý thanh toán bác sĩ (Payout)
```
GET  /api/v1/transactions/payouts/pending   [Admin/Manager]
  → GetPendingPayoutsAsync() – danh sách bác sĩ chờ nhận tiền

GET  /api/v1/transactions/payouts/paid      [Admin/Manager]
  → GetPaidPayoutsAsync()

POST /api/v1/transactions/payouts/{payoutId}/approve  [Admin/Manager]
  Form: { TransferImage? }
  → TransactionService.ApproveDoctorPayoutAsync()
    → DoctorPayout.Status = Paid
    → Upload chứng từ lên Cloudinary
```

---

## FLOW 13 – Tương tác thuốc bằng AI (`/api/v1/drug-interactions`)

```
POST /api/v1/drug-interactions/explain  [Authorize]
  Body: { NewDrugName, Conflicts: [{ DrugName, Reason }] }
  → DrugInteractionAIService.ExplainInteractionAsync()
    → RAG: tìm kiếm trong DrugBank database
    → Kết hợp bệnh sử của bệnh nhân (HealthConditions)
    → Gọi AI model → trả về giải thích chi tiết
```

---

## FLOW 14 – Thông báo (`/api/v1/notifications`)

```
GET /api/v1/notifications
  → NotificationService.GetUserNotificationsAsync(userId)

PUT /api/v1/notifications/{id}/read      → MarkAsReadAsync()
PUT /api/v1/notifications/read-all       → MarkAllAsReadAsync()

GET /api/v1/notifications/member/{memberId}
  → GetUserNotificationsAsync(memberId: memberId)  [Dependent]

PUT /api/v1/notifications/member/{memberId}/{notificationId}/read
PUT /api/v1/notifications/member/{memberId}/read-all
```

**Push notification:** `FirebaseNotificationService` – gửi FCM tới thiết bị.

---

## FLOW 15 – Admin

```
POST /api/v1/admin/doctors
  → Tạo tài khoản bác sĩ mới (xem Flow 8.1)

POST /api/v1/admin/doctor-managers
  → UserService.CreateDoctorManagerAsync() – tạo quản lý bác sĩ

GET  /api/v1/admin/family-subscriptions
  → FamilyService.GetAllFamilySubscriptionsAsync() – quản lý subscription

PUT  /api/v1/admin/family-subscriptions/{id}/status
  → FamilyService.UpdateFamilySubscriptionStatusAsync()
```

---

## Sơ đồ Flow Chính (Tổng hợp)

```
[Đăng ký] → [Xác thực OTP] → [Đăng nhập]
  └── [Tạo Family (Personal/Shared)]
        └── [Thêm Member (Dependent / User khác)]
              └── [Tạo HealthProfile cho Member]
              └── [Tạo Prescription (OCR / Nhập tay)]
                    └── [Tạo MedicationSchedule]
                          └── [Nhận Reminder hàng ngày]
                                └── [Đánh dấu Taken/Skipped → MedicationLog]
              └── [Mua gói Membership (PayOS)]
                    └── [Đặt lịch khám (Appointment)]
                          └── [Join Session (Video Call / Chat)]
                                └── [Bác sĩ gắn đơn thuốc]
                                └── [Kết thúc Session]
                                      └── [Đánh giá bác sĩ (Rating)]
```

---

## Danh sách Models chính

| Model | Mô tả |
|---|---|
| `Users` | Tài khoản đăng nhập (User/Doctor/Admin) |
| `Doctors` | Hồ sơ bác sĩ (Status: Inactive/Pending/Active/Rejected) |
| `Families` | Gia đình (Type: Personal/Shared) |
| `Members` | Thành viên trong gia đình |
| `HealthProfiles` | Hồ sơ sức khỏe của Member |
| `HealthConditions` | Bệnh án đính kèm HealthProfile |
| `Prescriptions` | Đơn thuốc |
| `PrescriptionMedicines` | Thuốc trong đơn |
| `MedicationSchedules` | Lịch uống thuốc |
| `MedicationScheduleDetails` | Chi tiết lịch (thời điểm uống) |
| `MedicationReminders` | Nhắc nhở (được Hangfire xử lý) |
| `MedicationLogs` | Lịch sử hành động uống/bỏ qua |
| `Appointments` | Lịch hẹn khám |
| `ConsultationSessions` | Phiên tư vấn video/chat |
| `ChatDoctorMessages` | Tin nhắn trong phiên tư vấn |
| `Ratings` | Đánh giá bác sĩ sau phiên |
| `MembershipPackages` | Các gói dịch vụ |
| `FamilySubscriptions` | Subscription của gia đình |
| `Payments` | Giao dịch thanh toán (PayOS) |
| `Transactions` | Lịch sử transaction hệ thống |
| `DoctorPayout` | Thống kê hoa hồng bác sĩ |
| `Notifications` | Thông báo đẩy |
| `Drug` + `DrugInteraction` | Cơ sở dữ liệu tương tác thuốc |
