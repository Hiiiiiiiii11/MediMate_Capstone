# MediMate – Danh Sách Flow Cho Sequence & Class Diagram

---

## Tổng quan các Flow

| # | Flow | Actor chính |
|---|------|------------|
| 1 | Quản lý Family & Health Profile | User, Member, HealthService |
| 2 | Đặt lịch khám & Thanh toán (PayOS) | User, PayOS |
| 3 | PayOS Webhook xử lý kết quả | PayOS, System |
| 4 | Bác sĩ cập nhật trạng thái lịch hẹn | Doctor |
| 5 | Hủy lịch & Refund | User, Admin |
| 6 | Tham gia phiên tư vấn trực tuyến | User, Doctor, Agora |
| 7 | Quản lý Prescription & OCR | User, AI OCR |
| 8 | Quản lý lịch uống thuốc | User, Dependent |
| 9 | Mua gói gia đình (Subscription) | User, PayOS |
| 10 | Admin thanh toán công nợ phòng khám | Admin, Clinic, Cloudinary |

---

## Flow 1 – Quản Lý Family & Health Profile

### Sequence Diagram

```
User -> FamilyController: POST /api/v1/families/personal  (tạo hồ sơ cá nhân)
FamilyController -> FamilyService: CreatePersonalFamilyAsync(userId)
FamilyService -> DB: INSERT Families + INSERT Members (chủ hộ)
FamilyController --> User: 200 { FamilyResponse }

User -> FamilyController: POST /api/v1/families/shared { familyName }
FamilyService -> DB: INSERT Families (type=Shared) + INSERT Members
FamilyController --> User: 200 { FamilyResponse }

User -> MemberController: POST /api/v1/members/families/{familyId}/invite (mời thành viên)
MemberService -> DB: INSERT Members (pending)

User -> HealthController: POST /api/v1/health/member/{memberId} { height, weight, bloodType, ... }
HealthController -> HealthService: CreateHealthProfileAsync(memberId, userId, request)
HealthService -> DB: Validate caller owns this member
HealthService -> DB: INSERT HealthProfile
HealthController --> User: 201 { HealthProfileResponse }

User -> HealthController: PUT /api/v1/health/member/{memberId}/conditions
HealthService -> DB: INSERT MedicalConditions
User -> HealthController: PUT /api/v1/health/member/{memberId}/allergies
HealthService -> DB: INSERT Allergies
```

### Class Diagram

```
┌───────────────┐    ┌──────────────────┐    ┌──────────────────┐
│ FamilyControl │    │  FamilyService   │    │   Families       │
│ +CreatePersonal    │ +CreatePersonal()│    │  FamilyId        │
│ +CreateShared │    │ +CreateShared()  │    │  Name            │
│ +GetMyFamilies│    │ +GetMyFamilies() │    │  Type            │
└───────┬───────┘    └────────┬─────────┘    └──────┬───────────┘
        │                     │                     │ 1:N
┌───────▼───────┐    ┌────────▼─────────┐    ┌──────▼───────────┐
│ HealthControl │    │  HealthService   │    │    Members       │
│ +GetProfile() │    │ +CreateProfile() │    │  MemberId        │
│ +CreateProfile│    │ +UpdateProfile() │    │  FamilyId        │
│ +AddCondition │    │ +AddCondition()  │    │  FullName        │
└───────────────┘    └─────────────────┘    └──────┬───────────┘
                                                   │ 1:1
                                            ┌──────▼──────────┐
                                            │  HealthProfile  │
                                            │  Height, Weight │
                                            │  BloodType      │
                                            │  Conditions[]   │
                                            │  Allergies[]    │
                                            └─────────────────┘
```

---

## Flow 2 – Đặt Lịch Khám & Thanh Toán (PayOS)

### Sequence Diagram

```
User -> AppointmentsController: POST /api/v1/appointments { doctorId, memberId, date, time }
AppointmentService -> DB: Validate DoctorAvailability (chưa bị đặt)
AppointmentService -> DB: INSERT Appointments (Status=Pending, PaymentStatus=Pending)
AppointmentService -> DB: INSERT Payments (Status=Pending, UserId=chủ Family)
AppointmentService -> PayOSService: CreatePaymentUrlAsync(orderCode, amount)
PayOSService -> PayOS API: POST /v2/payment-requests
PayOS API --> PayOSService: { checkoutUrl, orderCode }
PayOSService -> DB: INSERT Transactions (Status=Pending, GatewayName="PayOS")
AppointmentsController --> User: 200 { appointmentId, checkoutUrl }

User -> PayOS: Redirect & thanh toán trực tiếp
```

### Class Diagram

```
┌──────────────────────┐    ┌──────────────────────┐
│ AppointmentService   │───>│    PayOSService       │
│ +CreateAppointment() │    │ +CreatePaymentUrl()   │
└──────┬───────────────┘    └──────┬────────────────┘
       │                           │
  ┌────┴────────┐           ┌──────┴──────┐
  │Appointments │           │Transactions │
  │ Status      │           │ OrderCode   │
  │ PaymentStat │           │ Status      │
  └──────┬──────┘           └─────────────┘
         │ 1:N
  ┌──────▼──────┐
  │  Payments   │
  │  UserId     │
  │  Amount     │
  │  Status     │
  └─────────────┘
```

---

## Flow 3 – PayOS Webhook Xử Lý Kết Quả Thanh Toán

### Sequence Diagram

```
PayOS -> PaymentController: POST /api/v1/payment/webhook { signature, data }
PaymentController -> PayOSService: VerifyWebhookSignatureAsync(signature, data)
PayOSService -> DB: SELECT Transactions WHERE OrderCode=? INCLUDE Payment, Appointment

[isSuccess = true]
PayOSService -> DB: UPDATE Transaction.Status = "Success", PaidAt = now
PayOSService -> DB: UPDATE Payment.Status = "Success"
PayOSService -> DB: UPDATE Appointment SET PaymentStatus="Paid", Status="Approved"
PayOSService -> DB: INSERT DoctorPayout (Status="Hold", Amount=payment.Amount)
PayOSService -> DB: INSERT ConsultationSessions (Status="Scheduled")
PayOSService -> HangFire: Schedule Reminder Job (T-15 phút)

[isSuccess = false]
PayOSService -> DB: UPDATE Transaction.Status = "Failed"
PayOSService -> DB: UPDATE Payment.Status = "Failed"
PayOSService -> DB: UPDATE Appointment SET Status="Cancelled", PaymentStatus="Cancelled"

PaymentController --> PayOS: 200 ACK
```

### Class Diagram

```
┌────────────────────────────────┐
│         PayOSService           │
│ +ProcessPaymentWebhookAsync()  │
│ +VerifyWebhookSignatureAsync() │
└──────────┬─────────────────────┘
           │
     ┌─────┼──────────────┐
     │     │              │
┌────▼──┐ ┌▼────────┐ ┌──▼──────────┐
│Transac│ │Payments │ │Appointments │
│Status │ │ Status  │ │PaymentStatus│
│PaidAt │ │         │ │Status       │
└───────┘ └─────────┘ └──────┬──────┘
                              │
                    ┌─────────┴─────────┐
                    │   DoctorPayout    │
                    │   Status="Hold"   │
                    └───────────────────┘
```

---

## Flow 4 – Bác Sĩ Cập Nhật Trạng Thái Lịch Hẹn

### Sequence Diagram

```
Doctor -> AppointmentsController: PUT /api/v1/appointments/{id}/status { status }
AppointmentService -> DB: SELECT Appointment + validate Doctor ownership

[Status = "Completed"]
AppointmentService -> DB: UPDATE DoctorPayout SET Status="ReadyToPay"

[Status = "Approved"]
AppointmentService -> HangFire: Schedule ReminderJob (T-15 phút)

AppointmentService -> DB: UPDATE Appointment.Status
AppointmentService -> DB: UPDATE ConsultationSession.Status
AppointmentService -> NotificationService: SendNotification(member)
AppointmentService -> SignalR: AppointmentStatusUpdated
AppointmentsController --> Doctor: 200 { AppointmentDto }
```

### Class Diagram

```
┌──────────────────────┐    ┌─────────────────────┐
│  AppointmentService  │    │  NotificationService │
│ +UpdateAppointment() │    │ +SendNotification()  │
└──────┬───────────────┘    └─────────────────────┘
       │
  ┌────┴────────┐    ┌──────────────────┐
  │Appointments │    │  DoctorPayout    │
  │  Status     │    │ Status=ReadyToPay│
  └─────────────┘    └──────────────────┘
```

---

## Flow 5 – Hủy Lịch & Hoàn Tiền (Refund)

### Sequence Diagram

```
User -> AppointmentsController: PUT /api/v1/appointments/{id}/cancel { cancelReason }
AppointmentService -> DB: UPDATE Appointment SET Status="Cancelled"
AppointmentService -> DB: UPDATE Appointment SET PaymentStatus="Refunded" (nếu đã Paid)
AppointmentService -> DB: UPDATE DoctorPayout SET Status="Cancelled"
AppointmentService -> NotificationService: Notify Doctor

Admin -> AppointmentsController: PUT /api/v1/appointments/{id}/complete-refund [Form: transferImage?]
AppointmentService -> UploadPhotoService: UploadPhotoAsync(transferImage) -> Cloudinary
AppointmentService -> DB: INSERT Payments (Type=Refund, Status="RefundCompleted")
AppointmentService -> DB: INSERT Transactions (
    Type=MoneySent "OUT",
    Status="Success",
    GatewayResponse=refundImageUrl,
    TransactionCode="REFUND-XXXXXXXX"
)
AppointmentService -> DB: UPDATE Appointment SET PaymentStatus="RefundCompleted"
AppointmentsController --> Admin: 200 { AppointmentDto }
```

### Class Diagram

```
┌──────────────────────────┐    ┌────────────────────────┐
│   AppointmentService     │    │   UploadPhotoService   │
│ + CancelAppointmentAsync │    │ + UploadPhotoAsync()   │
│ + CompleteRefundAsync()  │    │   -> Cloudinary        │
└──────┬───────────────────┘    └────────────────────────┘
       │
  ┌────┴─────────────────┐    ┌─────────────────────────┐
  │ Payments (Refund OUT)│    │ Transactions (OUT)       │
  │ Status=RefundComplete│    │ Type = MoneySent         │
  │ UserId=payerUserId   │    │ GatewayResponse=ImgUrl   │
  └──────────────────────┘    └─────────────────────────┘
```

---

## Flow 6 – Tham Gia Phiên Tư Vấn Trực Tuyến

### Sequence Diagram

```
[Trước khi khám - Tạo sẵn khi thanh toán]
PayOSService -> DB: INSERT ConsultationSessions (Status="Scheduled")

[Đến giờ khám]
User -> ConsultationSessionController: POST /api/v1/sessions/{sessionId}/join { role: "user" }
ConsultationService -> DB: UPDATE Session SET UserJoined=true
ConsultationService: Kiểm tra nếu cả 2 đã join → Status="InProgress"
ConsultationService -> SignalR: BroadcastSessionStarted

Doctor -> ConsultationSessionController: POST /api/v1/sessions/{sessionId}/join { role: "doctor" }
ConsultationService -> DB: UPDATE Session SET DoctorJoined=true, Status="InProgress"

[Bác sĩ trễ]
User -> ConsultationSessionController: POST /api/v1/sessions/{sessionId}/doctor-late { lateMinutes }
ConsultationService -> DB: UPDATE Session.Note = "Bác sĩ trễ X phút"

[No-show]
User -> ConsultationSessionController: PUT /api/v1/sessions/{sessionId}/cancel-no-show
ConsultationService -> DB: UPDATE Session.Status = "Cancelled"
ConsultationService -> DB: UPDATE Appointment.Status = "Cancelled"
ConsultationService -> DB: UPDATE Appointment.PaymentStatus = "Refunded"

[Kết thúc phiên]
Doctor -> ConsultationSessionController: POST /api/v1/sessions/{sessionId}/end
ConsultationService -> DB: UPDATE Session SET Status="Ended", EndedAt=now
ConsultationService -> DB: UPDATE Appointment.Status = "Completed"
ConsultationService -> DB: UPDATE DoctorPayout.Status = "ReadyToPay"

Doctor -> ConsultationSessionController: POST /api/v1/sessions/{sessionId}/attach-prescription
ConsultationService -> DB: Link PrescriptionId to Session

User -> ConsultationSessionController: GET /api/v1/sessions/{sessionId}/recording
AgoraRecordingService -> Cloudinary: GetRecordingUrl
ConsultationSessionController --> User: { recordingUrl }
```

### Class Diagram

```
┌───────────────────────────────┐
│      ConsultationService      │
│ + JoinSessionAsync()          │
│ + MarkDoctorLateAsync()       │
│ + CancelNoShowAsync()         │
│ + EndSessionAsync()           │
│ + AttachPrescriptionAsync()   │
└──────────┬────────────────────┘
           │
     ┌─────┴───────────────────┐
     │                         │
┌────▼──────────────────┐  ┌───▼──────────────────┐
│  ConsultationSessions │  │  AgoraRecordingService│
│  Status               │  │ + GetRecordingUrl()   │
│  UserJoined           │  │ + StartRecording()    │
│  DoctorJoined         │  └──────────────────────┘
│  RecordUrl            │
└──────────┬────────────┘
           │ 1:1
     ┌─────▼────────┐
     │ Appointments │
     │ Status       │
     └──────────────┘
```

---

## Flow 7 – Quản Lý Prescription & OCR

### Sequence Diagram

```
[Upload ảnh đơn thuốc - OCR trên App]
User -> UploadController: POST /api/v1/upload/image [Form: file]
UploadController -> UploadPhotoService -> Cloudinary: Upload image
UploadController --> User: { imageUrl }

User -> (Mobile App): OCR ảnh bằng AI trên thiết bị (hoặc Google Vision)
App --> User: { parsedPrescriptionData }

[Lưu đơn thuốc]
User -> PrescriptionController: POST /api/v1/prescriptions/member/{memberId}
    { imageUrl, medications[{ name, dosage, frequency }], notes }
PrescriptionService -> DB: Validate caller owns memberId
PrescriptionService -> DB: INSERT Prescriptions
PrescriptionService -> DB: INSERT PrescriptionItems[] (từng thuốc)
PrescriptionController --> User: 201 { PrescriptionResponse }

[Tạo đơn trống để bác sĩ điền]
User -> PrescriptionController: POST /api/v1/prescriptions/member/{memberId}/empty
PrescriptionService -> DB: INSERT Prescriptions (rỗng)

[Bác sĩ tạo đơn qua phiên tư vấn]
Doctor -> PrescriptionByDoctorController: POST /api/v1/prescriptions-by-doctor/session/{sessionId}
PrescriptionService -> DB: INSERT Prescriptions (linked to Session)
PrescriptionService -> DB: INSERT PrescriptionItems[]

[Xem đơn thuốc]
User -> PrescriptionController: GET /api/v1/prescriptions/member/{memberId}
PrescriptionController --> User: { list of PrescriptionResponse }
```

### Class Diagram

```
┌─────────────────────────┐    ┌─────────────────────────┐
│   PrescriptionService   │    │  UploadPhotoService     │
│ + CreatePrescription()  │    │ + UploadPhotoAsync()    │
│ + CreateEmpty()         │    │   -> Cloudinary         │
│ + GetByMember()         │    └─────────────────────────┘
└──────────┬──────────────┘
           │
     ┌─────┴──────────────────┐
     │                        │
┌────▼──────────┐  ┌──────────▼──────────┐
│ Prescriptions │  │  PrescriptionItems  │
│ MemberId      │  │  MedicationName     │
│ ImageUrl      │  │  Dosage             │
│ SessionId     │  │  Frequency          │
│ Notes         │  │  Duration           │
└───────────────┘  └─────────────────────┘
```

---

## Flow 8 – Quản Lý Lịch Uống Thuốc

### Sequence Diagram

```
[Tạo lịch uống thuốc từ đơn]
User -> MedicationScheduleController: POST /api/v1/members/{memberId}/schedules
    { prescriptionItemId, times[], startDate, endDate }
ScheduleService -> DB: Validate caller owns memberId (User hoặc Dependent)
ScheduleService -> DB: INSERT MedicationSchedules
ScheduleService -> HangFire: Schedule notification jobs theo giờ uống
MedicationScheduleController --> User: 201 { ScheduleResponse }

[Tạo nhiều lịch cùng lúc]
User -> MedicationScheduleController: POST /api/v1/members/{memberId}/schedules/bulk
ScheduleService -> DB: INSERT nhiều MedicationSchedules

[Xem lịch uống theo ngày]
User -> MedicationScheduleController: GET /api/v1/members/{memberId}/schedules?date=2024-01-15
ScheduleService -> DB: SELECT Schedules WHERE MemberId=? AND date=?
MedicationScheduleController --> User: { list ScheduleResponse }

[Ghi nhận đã uống]
Dependent/User -> MedicationLogController: POST /api/v1/medication-logs
    { scheduleId, takenAt, status: "Taken" | "Skipped" }
MedicationLogService -> DB: INSERT MedicationLogs
MedicationLogController --> User: 200 { MedicationLogResponse }
```

### Class Diagram

```
┌──────────────────────────────┐
│   MedicationSchedulesService │
│ + CreateScheduleAsync()      │
│ + CreateBulkSchedulesAsync() │
│ + GetByDateAsync()           │
└──────────┬───────────────────┘
           │
     ┌─────┴─────────────────────┐
     │                           │
┌────▼──────────────┐   ┌────────▼──────────┐
│ MedicationSchedules│   │  MedicationLogs   │
│  MemberId          │   │  ScheduleId       │
│  MedicationName    │   │  TakenAt          │
│  DosageAmount      │   │  Status           │
│  Times[]           │   │  (Taken/Skipped)  │
│  StartDate         │   └───────────────────┘
│  EndDate           │
└──────────┬─────────┘
           │ N:1
     ┌─────▼────────┐    ┌─────────────┐
     │  Members     │    │  HangFire   │
     │  FamilyId    │    │  Reminder   │
     └──────────────┘    │  Jobs       │
                         └─────────────┘
```

---

## Flow 9 – Mua Gói Gia Đình (Subscription)

### Sequence Diagram

```
User -> MembershipPackageController: GET /api/v1/membership-packages (xem danh sách gói)
MembershipPackageController --> User: { list packages: name, price, memberLimit, ocrLimit }

User -> PaymentController: POST /api/v1/payment/create
    { subscriptionId, returnUrl, cancelUrl }
PayOSService -> DB: SELECT FamilySubscription WHERE SubscriptionId=?
PayOSService -> PayOS API: POST /v2/payment-requests { orderCode, amount }
PayOS API --> PayOSService: { checkoutUrl }
PayOSService -> DB: INSERT Transactions (Status=Pending)
PaymentController --> User: { checkoutUrl }

User -> PayOS: Redirect thanh toán

PayOS -> PaymentController: POST /api/v1/payment/webhook { isSuccess=true }
PayOSService -> DB: UPDATE Transaction.Status = "Success"
PayOSService -> DB: UPDATE Payment.Status = "Success"
PayOSService -> DB: UPDATE FamilySubscription SET Status="Active",
    StartDate=today, EndDate=today+durationDays,
    RemainingOcrCount=package.OcrLimit
PayOSService -> DB: (Hủy gói cũ nếu có) UPDATE oldSubscriptions SET Status="Inactive"
```

### Class Diagram

```
┌──────────────────────────┐    ┌────────────────────────┐
│       PayOSService       │    │  MembershipPackage     │
│ + CreatePaymentLinkAsync │    │  PackageName           │
│ + ProcessWebhookAsync()  │    │  Price                 │
└──────────┬───────────────┘    │  DurationDays          │
           │                    │  MemberLimit           │
     ┌─────┴────────────────┐   │  OcrLimit              │
     │                      │   │  ConsultantLimit       │
┌────▼──────────┐  ┌────────▼─────────────────┐
│  Transactions │  │   FamilySubscriptions    │
│  OrderCode    │  │   FamilyId               │
│  Status       │  │   Status (Active/Inactive│
│               │  │   StartDate, EndDate     │
└───────────────┘  │   RemainingOcrCount      │
                   └──────────────────────────┘
```

---

## Flow 10 – Admin Thanh Toán Công Nợ Phòng Khám (Payout)

### Sequence Diagram

```
Admin -> PayoutController: GET /api/v1/payout/summary
PayoutService -> DB: SELECT DoctorPayouts GROUP BY ClinicId
    WHERE Status IN ("ReadyToPay", "Paid")
PayoutController --> Admin: { list ClinicSummary: totalPending, count, totalPaid }

Admin -> PayoutController: GET /api/v1/payout?clinicId=?&status=ReadyToPay&page=1
PayoutService -> DB: SELECT DoctorPayouts INCLUDE
    Appointment.Member, Appointment.Doctor,
    Appointment.Payments.User,
    ConsultationSession
PayoutService -> DB: SELECT UserBankAccounts WHERE UserId IN payerIds
PayoutController --> Admin: { list PayoutItemDto:
    appointmentDate, patientName, doctorName,
    paymentStatus, payerName, payerPhone,
    payerBankName, payerBankAccountNumber }

Admin -> PayoutController: POST /api/v1/payout/clinic/{clinicId}/process
    [Form: TransferImage?, ReportFile?, Note?]

PayoutService -> UploadPhotoService: UploadPhotoAsync(TransferImage) -> Cloudinary
PayoutService -> UploadPhotoService: UploadDocumentAsync(ReportFile) -> Cloudinary

loop foreach DoctorPayout WHERE Status="ReadyToPay"
    PayoutService -> DB: UPDATE DoctorPayout SET
        Status="Paid", PaidAt=now,
        TransferImageUrl=?, ReportFileUrl=?
end

PayoutService -> DB: SaveChanges()
PayoutService -> EmailService: SendEmailAsync(clinic.Admin.Email, htmlBody)
    (background Task.Run)
PayoutController --> Admin: 200 { count đã thanh toán }
```

### Class Diagram

```
┌──────────────────────────────────────────┐
│              PayoutService               │
│ + GetPayoutsAsync(filter)                │
│ + GetPayoutSummaryByClinicAsync()        │
│ + ProcessClinicPayoutAsync(clinicId, dto)│
└──────────┬───────────────────────────────┘
           │ depends on
     ┌─────┼───────────────┐
     │     │               │
┌────▼──┐ ┌▼────────────┐ ┌▼────────────────┐
│Upload │ │EmailService │ │  DoctorPayout   │
│PhotoSvc│ │+SendEmail() │ │  ClinicId       │
│+Upload │ └─────────────┘ │  Status         │
│+UpDoc  │                 │  TransferImgUrl │
└────────┘                 │  ReportFileUrl  │
                           └──────┬──────────┘
                                  │ N:1
                            ┌─────▼──────────┐
                            │    Clinics     │
                            │  Admin (User)  │
                            │    .Email      │
                            └────────────────┘

PayoutItemDto (Response)
├── AppointmentDate, AppointmentTime
├── PatientName (Member.FullName)
├── DoctorName (Doctor.FullName)
├── PaymentStatus (Appointment.PaymentStatus)
├── PayerName, PayerPhoneNumber (Payment.User)
├── PayerBankName, PayerBankAccountNumber, PayerBankAccountHolder
│   (UserBankAccount lookup by payerUserId)
├── Amount, Status (DoctorPayout)
├── TransferImageUrl, ReportFileUrl
└── PaidAt
```

---

## Bảng Trạng Thái Chuẩn Hóa

| Entity | Status values |
|--------|--------------|
| `Transaction.TransactionStatus` | `Pending` → `Success` / `Failed` |
| `Transaction.TransactionType` | `IN` (MoneyReceived) / `OUT` (MoneySent) |
| `Payment.Status` | `Pending` → `Success` / `Failed` / `RefundCompleted` |
| `Appointment.Status` | `Pending` → `Approved` / `Completed` / `Cancelled` |
| `Appointment.PaymentStatus` | `Pending` → `Paid` / `Refunded` / `RefundCompleted` / `Cancelled` |
| `DoctorPayout.Status` | `Hold` → `ReadyToPay` → `Paid` / `Cancelled` |
| `ConsultationSessions.Status` | `Scheduled` → `InProgress` → `Ended` / `Cancelled` |
| `FamilySubscriptions.Status` | `Pending` → `Active` / `Inactive` / `Failed` |
