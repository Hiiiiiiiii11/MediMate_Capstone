# Backend Audit & Fix Tasks

## Nhóm 1 – AppointmentDto (List Screen)
- [x] Có: AppointmentId, DoctorId, ClinicId, MemberId, MemberName, Status, PaymentStatus, CancelReason, CreatedAt
- [x] Đã thêm: DoctorName, ClinicName, Amount (fee), ConsultationSessionId

## Nhóm 2 – AppointmentDetailDto (Detail Screen)
- [x] Có: DoctorName, DoctorAvatar, Specialty, MemberName, Status, Dates
- [x] Đã thêm: PaymentStatus, Amount, ClinicName, ConsultationSessionId, ConsultationSessionStatus

## Nhóm 3 – ConsultationSessionDto
- [x] Có: Status, UserJoined, DoctorJoined, StartedAt, EndedAt, DoctorNote, MemberName, DoctorName
- [x] Đã thêm: RecordingUrl, PrescriptionId (để redirect sang màn đơn thuốc)

## Nhóm 4 – Routes
- [x] Sửa /refunds → /refundable
- [x] Thêm GET /members/{memberId}
- [x] GET /detail/{id} đã thỏa mãn GET by ID đơn

## Nhóm 5 – PayoutController
- [x] Đã có GET /payouts?filter và GET /payouts/summary 

## Nhóm 6 – UserBankAccountController
- [x] Đã có các routes CRUD tại /api/v1/user/bank-account
