# Kế hoạch Tối ưu và Minh bạch hóa Flow Hiện tại (Booking & Pay Per Session)

> **Mục tiêu:** Chuyển đổi mô hình thành Đặt lịch Khám trả phí theo từng lượt (Pay-per-booking). Quản lý bác sĩ thông qua hệ thống Phòng khám (Clinic) để đảm bảo uy tín và dòng tiền minh bạch.

---

## 1. Luồng Thanh toán & Trả tiền (Pay-per-booking & Clinic Payout)


**Đề xuất cải tiến:**
- **Thanh toán trước khi đặt lịch:** Khi user chọn giờ khám của bác sĩ, hệ thống sinh ra mã QR PayOS với số tiền tương ứng với Bác sĩ đó (ví dụ: Bác sĩ xương khớp A giá 150k). User phải quét mã thanh toán thành công thì `Appointment` mới được tạo.
- **Xử lý Hoàn tiền (Refund):**
  - Nếu User huỷ trước giờ hẹn (theo quy định), hệ thống tạo Yêu cầu hoàn tiền (Refund Request).
  - Nếu Bác sĩ từ chối khám, hệ thống tạo Yêu cầu hoàn tiền (Refund Request).
- **State Machine cho Payout (Công nợ):**
  1. **Tạm giữ:** Tiền thanh toán của User ban đầu nằm trong tài khoản trung gian của Admin (Hệ thống).
  2. **Ghi nhận Công nợ:** Khi đến giờ hẹn, bác sĩ và bệnh nhân vào phòng tư vấn (`ConsultationSession` thành công -> `Ended`). Hệ thống tạo 1 bản ghi `DoctorPayout` với trạng thái `ReadyToPay`. Khoản tiền này được tính là **Công nợ của hệ thống đối với Phòng khám** mà bác sĩ trực thuộc.
  3. **Thanh toán cho Phòng khám:** Cuối tháng (hoặc định kỳ), Admin tổng hợp công nợ và chuyển khoản một lần cho Phòng khám (`Paid`). Phòng khám tự chịu trách nhiệm chia lương lại cho bác sĩ.

## 2. Minh bạch Gói dịch vụ thành viên (Chỉ áp dụng OCR & Premium)

**Vấn đề hiện tại:** Do lượt khám đã chuyển sang thanh toán lẻ từng lượt, gói thành viên (FamilySubscriptions) giờ đây chỉ nên dùng để quản lý các tính năng đặc quyền khác (như lượt quét OCR đơn thuốc, mở khoá tính năng nhắc nhở, hoặc giảm giá khi đặt lịch).

**Đề xuất cải tiến:**
- **Tạo bảng `SubscriptionUsageLogs` (Nhật ký sử dụng OCR):**
  - Cấu trúc: `Id, SubscriptionId, UsageType (OCR), Amount, ReferenceId, CreatedAt, Description`.
  - Mỗi khi hệ thống trừ lượt quét OCR, bắt buộc phải insert 1 dòng log.
- **Minh bạch với User:** Hiển thị trang "Lịch sử sử dụng gói" trên Mobile App, ví dụ: 
  - *Ngày 12/10: Trừ 1 lượt OCR cho ảnh đơn thuốc X.*
- **Huỷ gói và Hoàn tiền (Refund/Cancel Package):** Cung cấp tính năng cho phép User huỷ gói dịch vụ đã mua nếu họ **chưa sử dụng bất kỳ tính năng nào** (ví dụ: `OcrLimit` vẫn còn nguyên). Khi thực hiện:
  - Hệ thống kiểm tra trong `SubscriptionUsageLogs`. Nếu chưa có lịch sử trừ lượt nào, cho phép huỷ.
  - Chuyển trạng thái `FamilySubscription` sang `Cancelled`.
  - Tự động tạo yêu cầu hoàn tiền (Refund Request) cho Admin duyệt.

## 3. Lưu trữ Video Tư vấn (Consultation Recording)

**Vấn đề hiện tại:** Khám qua video nhưng không có bằng chứng lưu lại nếu có sự cố y khoa hoặc chẩn đoán sai.

**Đề xuất cải tiến:**
- **Sử dụng Agora Cloud Recording:**
  - Tích hợp API của Agora để ghi lại tự động khi cả 2 bên (Bác sĩ & Bệnh nhân) join vào channel.
  - Video được tự động push về **Cloudinary** hoặc **AWS S3** sau khi cuộc gọi kết thúc.
- **Thêm trường dữ liệu:** Thêm cột `RecordingUrl` và `RecordingDuration` vào bảng `ConsultationSessions`.
- **Quyền riêng tư:** Chỉ Bác sĩ khám và User (Chủ hộ) mới được quyền xem lại video này. Phục vụ cho việc User xem lại lời dặn của bác sĩ mà không cần ghi nhớ.

## 4. Lịch làm việc của Bác sĩ (Doctor Availability)

**Vấn đề hiện tại:** Double booking (2 người đặt cùng 1 slot) hoặc bác sĩ quên lịch.

**Đề xuất cải tiến:**
- **Lock Slot (Giữ chỗ 15 phút):** Khi user bắt đầu chọn giờ, slot đó bị lock tạm thời (Trạng thái Pending). Nếu user không xác nhận book, slot tự nhả ra.
- **Xử lý Đụng lịch (Concurrency):** Sử dụng Optimistic Concurrency Control (EF Core `[Timestamp]`) hoặc Transaction Level Serializable khi insert Appointment để đảm bảo 1 slot thời gian không bị ghi đè.
- **Exceptions (Nghỉ phép):** Đã có bảng `DoctorAvailabilityExceptions` (Rất tốt!). Cần đảm bảo API lấy slot trống phải query cẩn thận: `Lịch cố định - Lịch Exception - Lịch đã bị Book`.
- **Hệ thống nhắc nhở đa kênh:**
  - Gửi Push Noti (FCM) trước 15 phút.

## 5. Cải thiện luồng Đặt lịch khám (Booking Flow)

- **Quy trình Thanh toán - Đặt lịch:** User chọn chuyên khoa -> Chọn bác sĩ thuộc phòng khám -> Xem giá khám (VD: 150k) -> Chọn giờ -> **Thanh toán PayOS** -> Trả về kết quả thành công -> Xác nhận tạo lịch hẹn thành công.
- **Bác sĩ duyệt (Tùy chọn Auto-Confirm):** Cung cấp cấu hình cho Bác sĩ: 
  - `Auto-Confirm`: Đặt là nhận luôn (khuyên dùng để tăng trải nghiệm user).
  - `Manual-Confirm`: Bác sĩ phải ấn chấp nhận trong vòng 2 tiếng, nếu không hệ thống tự động huỷ lịch và báo PayOS hoàn tiền lại cho User.
- **Check-in:** Thêm nút "Check-in" hoặc tự động check-in khi join phòng Agora. Trạng thái Appointment sẽ là `InProgress`. Căn cứ vào thời gian check-in để xác định ai đến trễ.

## 6. Đánh giá Bác sĩ (Rating & Review)

**Vấn đề hiện tại:** Tránh spam đánh giá ảo hoặc đánh giá tặc.

**Đề xuất cải tiến:**
- **Validate nghiêm ngặt:** Chỉ cho phép gọi API tạo Rating khi:
  1. `Session` thuộc về chính `UserId` đang gọi.
  2. `Session` phải có trạng thái `Ended` / `Completed`.
  3. Thời lượng cuộc gọi (dựa trên Agora hoặc thời gian join-end) phải > 1 phút (chống việc vừa vào đã thoát rồi đánh giá 1 sao).
- **Hệ thống Khiếu nại (Report):** Thêm tính năng "Báo cáo phiên khám". Nếu user đánh giá < 3 sao, hệ thống bật popup hỏi lý do chi tiết và gửi Report về cho Admin xử lý (có thể Admin sẽ xem lại Video Recording để đối chứng và quyết định hoàn lượt khám cho user hay không).

## 7. Logic Quản lý Phiên khám & Nhắn tin (Session & Chat)

**Vấn đề hiện tại:** Không có cơ chế đóng băng phòng chat sau khi kết thúc, khó kiểm soát nội dung và thiếu minh bạch về thời gian thực tế của phiên.

**Đề xuất cải tiến:**
-Giữ logic session cũ
- **Minh bạch Lịch sử Chat:** Toàn bộ tin nhắn (kể cả ảnh đính kèm) được coi như một phần của **Hồ sơ bệnh án (Medical Record)**. Không cho phép xoá (Delete) tin nhắn, chỉ cho phép "Thu hồi" (Revoke) trong vòng 5 phút sau khi gửi để sửa lỗi gõ sai. Mọi hình ảnh gửi lên phải qua hệ thống Cloudinary và gắn tag `ChatAttachment` để chống mất mát dữ liệu.
- **Ghi nhận giờ thực tế nghiêm ngặt:** Cập nhật chính xác `UserJoinedAt` và `DoctorJoinedAt` khi họ thực sự vào phòng (connect thành công Agora hoặc Webhook Socket). Thời gian tính tiền/trừ lượt chỉ bắt đầu đếm khi cả 2 đã join.

## 8. Logic Tạo Đơn thuốc của Bác sĩ (Doctor Prescription)


- **Gửi đơn thuốc & Tự động nhắn tin vào Chat:** Khi bác sĩ ấn "Hoàn tất / Gửi cho bệnh nhân", đơn thuốc sẽ được lưu và bị **Khoá cứng (Read-only)** để làm bằng chứng y khoa. Đồng thời, hệ thống sẽ **tự động sinh ra một tin nhắn hệ thống (System Message)** và gửi thẳng vào khung chat (Session Chat) của chính phiên khám đó.
- **Nội dung tin nhắn Đơn thuốc:** Tin nhắn này sẽ chứa toàn bộ thông tin chi tiết dưới dạng văn bản (Text), bao gồm:
  - Thông tin bệnh nhân: Tên user, ngày sinh/tuổi, chẩn đoán bệnh.
  - Chi tiết danh sách thuốc: Tên thuốc, hàm lượng, tổng số lượng.
  - Liều dùng: Chi tiết liều dùng và cách dùng của từng loại thuốc.
  - Lời dặn dò thêm của bác sĩ.
- **Tiện lợi cho User:** Bệnh nhân chỉ cần mở khung chat là có ngay văn bản đơn thuốc đầy đủ thông tin để mang ra hiệu thuốc mua hoặc theo dõi mà không phải tải file hay mở nhiều màn hình phức tạp.

## 9. Cải thiện các Flow và Service Khác (Bảo mật & Trải nghiệm)

**9.1. Flow Xác thực (Auth Service)**
- **Vấn đề:** Hiện tại chỉ cấp Access Token (JWT), nếu token hết hạn thì user phải login lại từ đầu (nhập email, pass), gây trải nghiệm rất kém. Gửi OTP qua email liên tục dễ bị spam gây tốn tài nguyên.
- **Đề xuất:** 
  - Cấu hình **Refresh Token**: Lưu Refresh Token vào Database hoặc HttpOnly Cookie với thời hạn dài (Ví dụ: 30 ngày). Khi Access Token hết hạn, tự động dùng Refresh Token để lấy token mới (Silent Login) mà không phiền user.
  - **Rate Limiting cho OTP:** Giới hạn 1 email/SĐT chỉ được gửi tối đa 3 mã OTP trong vòng 5 phút để chống spam.

**9.2. Quản lý Gia đình & Thành viên (Family & Member Service)**
  - **Ràng buộc Xoá Family:** Trước khi cho phép xoá (Delete) Family, hệ thống bắt buộc phải kiểm tra xem có bất kỳ thành viên nào trong gia đình đó đang có `Appointments` ở trạng thái `Pending` hoặc `Confirmed` (chưa khám) hay không. Nếu có, chặn thao tác xoá và thông báo yêu cầu người dùng phải huỷ lịch hẹn trước khi xoá gia đình.

**9.3. Flow Thanh toán & Webhook (Payment Service)**
- **Vấn đề:** Cổng thanh toán (PayOS) có thể gửi cùng một Webhook báo thành công 2-3 lần do lỗi mạng (Retry), dẫn đến việc hệ thống có thể cộng nhầm 2 lần gói dịch vụ cho khách.
- **Đề xuất:**
  - Áp dụng **Idempotency Key (Khóa bất biến)** khi xử lý Webhook. Check trong database xem `OrderCode` hoặc `TransactionId` đã được xử lý thành công (Status = Success) hay chưa. Nếu đã xử lý rồi thì return 200 OK ngay lập tức, tuyệt đối bỏ qua logic cộng gói phía sau.

**9.4. Hồ sơ bệnh án & Tương tác nhiều người dùng**
- **Vấn đề:** Gia đình chung có nhiều thành viên cùng quản lý sức khoẻ cho 1 người phụ thuộc (Ví dụ: Bố và Mẹ cùng chăm sóc Con). Nếu Mẹ sửa bệnh án hoặc cho con uống thuốc, Bố không biết.
- **Đề xuất:**
  - Thêm **Audit Log (Lịch sử thao tác)** cơ bản cho `HealthProfile` và `MedicationLogs`. Ghi lại rõ ràng: Ai (UserId nào) đã cập nhật hồ sơ bệnh án hoặc ai đã đánh dấu "Đã cho con uống thuốc" vào lúc mấy giờ, để các thành viên khác trong Family cùng được đồng bộ thông tin.

## 10. Mở rộng Hệ thống: Mô hình Phòng khám / Tổ chức Y tế (Clinic / Hospital)

**Vấn đề:** Hiện tại bác sĩ hoạt động theo dạng độc lập (Freelance), khó kiểm soát chứng chỉ hành nghề thực tế.

**Đề xuất cải tiến:**
- **Thực thể `Clinics` (Phòng khám):** Tạo bảng `Clinics` (Id, Name, Address, License, AdminId). Bác sĩ phải được liên kết vào 1 Clinic thông qua `ClinicDoctors`.
- **Cấu hình Giá khám (Dynamic Pricing):** Mỗi Phòng khám sẽ có bảng giá riêng cho từng chuyên khoa hoặc từng bác sĩ của mình. Khi user tìm kiếm, hệ thống sẽ hiển thị giá này để họ thanh toán trực tiếp trước khi đặt lịch.
- **Minh bạch Nguồn gốc:** Khi user xem profile bác sĩ hoặc đặt lịch, UI sẽ hiển thị rõ: *"Bác sĩ Nguyễn Văn A - Trực thuộc Phòng khám Đa khoa Quốc tế X"*. Điều này giúp bệnh nhân hoàn toàn yên tâm.
- **Xử lý Dòng tiền (Payout):** Toàn bộ doanh thu từ các ca khám thành công sẽ được ghi nhận là Công nợ của hệ thống đối với **Phòng khám**. Đến kỳ thanh toán, Admin sẽ chuyển tổng tiền cho Phòng khám thay vì rải rác chuyển cho từng cá nhân bác sĩ.
- Admin quản lý phòng khám và bác sĩ thông qua hợp đồng, nên cần có thêm bảng `ClinicContracts` để lưu trữ thông tin hợp đồng giữa Admin và phòng khám.
- Admin KHÔNG quản lý bác sĩ tự do (Freelance), Admin chỉ quản lý phòng khám và bác sĩ phòng khám đưa vào hệ thống thông qua hợp đồng

---

## Tóm tắt các bảng cần chỉnh sửa / thêm mới

1. **`DoctorPayout`**: Thêm field `Status` (Hold, ReadyToPay, Cancelled, Paid), `ClinicId`.
2. **`SubscriptionUsageLogs` (Mới)**: Ghi log trừ/cộng lượt OCR.
3. **`ConsultationSessions`**: Thêm `RecordingUrl` (string), `DurationInSeconds` (int).
4. **`Appointments`**: Đảm bảo có `CheckInTime`, `CheckOutTime` để đo thời gian thực tế.
5. **`Ratings`**: Ràng buộc thêm điều kiện tạo rating dựa trên thời lượng call.
6. **`Clinics` & `ClinicDoctors` (Mới)**: Quản lý tổ chức y tế và danh sách bác sĩ trực thuộc.
