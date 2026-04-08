using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;
using Share.Common;
using Share.Constants;
using Hangfire;

namespace MediMateService.Services.Implementations
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly IActivityLogService _activityLogService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public PrescriptionService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IUploadPhotoService uploadPhotoService, IActivityLogService activityLogService, IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _uploadPhotoService = uploadPhotoService;
            _activityLogService = activityLogService;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<ApiResponse<PrescriptionResponse>> CreatePrescriptionAsync(Guid memberId, Guid userId, CreatePrescriptionRequest request)
        {
            // 1. Check quyền truy cập Member
            if (!await _currentUserService.CheckAccess(memberId, userId))
            {
                return ApiResponse<PrescriptionResponse>.Fail("Không có quyền thêm đơn thuốc cho thành viên này.", 403);
            }

            // 2. Map dữ liệu Header (Bảng Prescriptions)
            var prescription = new Prescriptions
            {
                PrescriptionId = Guid.NewGuid(),
                MemberId = memberId,
                PrescriptionCode = request.PrescriptionCode,
                DoctorName = request.DoctorName,
                HospitalName = request.HospitalName,
                PrescriptionDate = request.PrescriptionDate,
                Notes = request.Notes,
                Status = "Active",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            // 3. Map dữ liệu Images (Bảng PrescriptionImages)
            //if (request.Images != null)
            //{
            //    foreach (var img in request.Images)
            //    {
            //        prescription.PrescriptionImages.Add(new PrescriptionImages
            //        {
            //            ImageId = Guid.NewGuid(),
            //            PrescriptionId = prescription.PrescriptionId,
            //            ImageUrl = img.ImageUrl,
            //            OcrRawData = img.OcrRawData ?? "",
            //            UploadedAt = DateTime.Now,
            //            IsProcessed = true // Vì UI đã xử lý rồi mới gửi xuống
            //        });
            //    }
            //}
            if (request.Images != null)
            {
                foreach (var img in request.Images)
                {
                    prescription.PrescriptionImages.Add(new PrescriptionImages
                    {
                        ImageId = Guid.NewGuid(),
                        PrescriptionId = prescription.PrescriptionId,
                        ImageUrl = img.ImageUrl,

                        // Lấy Thumbnail từ Request (FE gửi xuống)
                        // Nếu FE không gửi thì fallback bằng cách dùng ImageUrl
                        ThumbnailUrl = !string.IsNullOrEmpty(img.ThumbnailUrl) ? img.ThumbnailUrl : img.ImageUrl,

                        OcrRawData = img.OcrRawData ?? "",
                        UploadedAt = DateTime.Now,
                        IsProcessed = true
                    });
                }
            }

            // 4. Map dữ liệu Medicines (Bảng PrescriptionMedicines)
            if (request.Medicines != null)
            {
                foreach (var med in request.Medicines)
                {
                    prescription.PrescriptionMedicines.Add(new PrescriptionMedicines
                    {
                        PrescriptionMedicineId = Guid.NewGuid(),
                        PrescriptionId = prescription.PrescriptionId,
                        MedicineName = med.MedicineName,
                        Dosage = med.Dosage ?? "",
                        Unit = med.Unit ?? "",
                        Quantity = med.Quantity,
                        Instructions = med.Instructions ?? "",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            // 5. Lưu vào DB (EF Core tự xử lý Transaction lưu cả 3 bảng)
            await _unitOfWork.Repository<Prescriptions>().AddAsync(prescription);
            await _unitOfWork.CompleteAsync();

            // 6. Phân bổ thuốc vào các khung thời gian
            foreach (var med in prescription.PrescriptionMedicines)
            {
                await AutoDistributeMedicineAsync(memberId, med, prescription.PrescriptionDate);
            }

            var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (targetMember != null && targetMember.FamilyId.HasValue)
            {
                var doer = (await _unitOfWork.Repository<Members>()
.FindAsync(m => m.FamilyId == targetMember.FamilyId && (m.UserId == userId || m.MemberId == userId))).FirstOrDefault();

                if (doer != null)
                {
                    await _activityLogService.LogActivityAsync(
                        familyId: targetMember.FamilyId.Value,
                        memberId: doer.MemberId,
                        actionType: ActivityActionTypes.CREATE,
                        entityName: ActivityEntityNames.PRESCIPTION,
                        entityId: prescription.PrescriptionId,
                        description: $"Đã thêm đơn thuốc mới của Bác sĩ '{prescription.DoctorName}' cho '{targetMember.FullName}'."
                    );
                }
            }

            return ApiResponse<PrescriptionResponse>.Ok(MapToResponse(prescription), "Lưu đơn thuốc thành công.");
        }

        public async Task<ApiResponse<IEnumerable<PrescriptionResponse>>> GetPrescriptionsByMemberAsync(Guid memberId, Guid userId)
        {
            if (!await _currentUserService.CheckAccess(memberId, userId))
            {
                return ApiResponse<IEnumerable<PrescriptionResponse>>.Fail("Access Denied", 403);
            }

            // Include cả Images và Medications
            var list = await _unitOfWork.Repository<Prescriptions>()
                .FindAsync(p => p.MemberId == memberId, includeProperties: "PrescriptionImages,PrescriptionMedicines");

            var response = list.OrderByDescending(p => p.PrescriptionDate)
                               .Select(p => MapToResponse(p));

            return ApiResponse<IEnumerable<PrescriptionResponse>>.Ok(response);
        }

        public async Task<ApiResponse<PrescriptionResponse>> GetPrescriptionByIdAsync(Guid prescriptionId, Guid userId)
        {
            var p = (await _unitOfWork.Repository<Prescriptions>()
                .FindAsync(x => x.PrescriptionId == prescriptionId, includeProperties: "PrescriptionImages,PrescriptionMedicines"))
                .FirstOrDefault();

            if (p == null)
            {
                return ApiResponse<PrescriptionResponse>.Fail("Không tìm thấy đơn thuốc.", 404);
            }

            return !await _currentUserService.CheckAccess(p.MemberId, userId)
                ? ApiResponse<PrescriptionResponse>.Fail("Access Denied", 403)
                : ApiResponse<PrescriptionResponse>.Ok(MapToResponse(p));
        }

        public async Task<ApiResponse<PrescriptionResponse>> UpdatePrescriptionAsync(Guid prescriptionId, Guid userId, UpdatePrescriptionRequest request)
        {
            // 1. Lấy đơn thuốc kèm danh sách thuốc hiện tại
            var prescription = (await _unitOfWork.Repository<Prescriptions>()
                .FindAsync(p => p.PrescriptionId == prescriptionId, includeProperties: "PrescriptionMedicines,PrescriptionImages"))
                .FirstOrDefault();

            if (prescription == null)
            {
                return ApiResponse<PrescriptionResponse>.Fail("Đơn thuốc không tồn tại.", 404);
            }

            // 2. Kiểm tra quyền truy cập (Dùng service bạn đã có)
            if (!await _currentUserService.CheckAccess(prescription.MemberId, userId))
            {
                return ApiResponse<PrescriptionResponse>.Fail("Bạn không có quyền chỉnh sửa đơn thuốc này.", 403);
            }

            var oldData = new { prescription.DoctorName, prescription.HospitalName, prescription.Notes, prescription.Status };
            bool hasChanges = false;

            // 3. Cập nhật thông tin cơ bản
            if (!string.IsNullOrEmpty(request.DoctorName) && request.DoctorName != prescription.DoctorName)
            {
                prescription.DoctorName = request.DoctorName;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.HospitalName) && request.HospitalName != prescription.HospitalName)
            {
                prescription.HospitalName = request.HospitalName;
                hasChanges = true;
            }
            if (request.PrescriptionDate.HasValue && request.PrescriptionDate != prescription.PrescriptionDate)
            {
                prescription.PrescriptionDate = request.PrescriptionDate.Value;
                hasChanges = true;
            }
            if (request.Notes != prescription.Notes)
            {
                prescription.Notes = request.Notes;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.Status) && request.Status != prescription.Status)
            {
                prescription.Status = request.Status;
                hasChanges = true;
            }

            prescription.UpdateAt = DateTime.Now;

            // 4. XỬ LÝ DANH SÁCH THUỐC (Để tránh lỗi 0 rows affected)
            if (request.Medicines != null)
            {
                // Bước A: Lấy danh sách ID thuốc từ Request gửi lên (những thuốc cũ được giữ lại)
                var incomingMedicineIds = request.Medicines
                    .Where(m => m.PrescriptionMedicineId.HasValue)
                    .Select(m => m.PrescriptionMedicineId!.Value)
                    .ToList();

                // Bước B: Tìm và xóa những thuốc KHÔNG còn trong đơn thuốc mới
                var medicinesToRemove = prescription.PrescriptionMedicines
                    .Where(m => !incomingMedicineIds.Contains(m.PrescriptionMedicineId))
                    .ToList();

                foreach (var medToRemove in medicinesToRemove)
                {
                    // Xóa chi tiết lịch (Details) liên quan đến thuốc này trước
                    var oldDetails = await _unitOfWork.Repository<MedicationScheduleDetails>()
                        .FindAsync(d => d.PrescriptionMedicineId == medToRemove.PrescriptionMedicineId);

                    if (oldDetails.Any())
                    {
                        _unitOfWork.Repository<MedicationScheduleDetails>().RemoveRange(oldDetails);
                    }

                    // Sau đó mới xóa thuốc
                    _unitOfWork.Repository<PrescriptionMedicines>().Remove(medToRemove);
                }

                // Bước C: Cập nhật thuốc cũ hoặc Thêm thuốc mới
                foreach (var medDto in request.Medicines)
                {
                    if (medDto.PrescriptionMedicineId.HasValue)
                    {
                        // CẬP NHẬT: Tìm thuốc cũ trong DB để ghi đè thông tin mới
                        var existingMed = prescription.PrescriptionMedicines
                            .FirstOrDefault(m => m.PrescriptionMedicineId == medDto.PrescriptionMedicineId.Value);

                        if (existingMed != null)
                        {
                            existingMed.MedicineName = medDto.MedicineName;
                            existingMed.Dosage = medDto.Dosage ?? "";
                            existingMed.Unit = medDto.Unit ?? "";
                            existingMed.Quantity = medDto.Quantity;
                            existingMed.Instructions = medDto.Instructions ?? "";
                            existingMed.UpdatedAt = DateTime.Now;
                        }
                    }
                    else
                    {
                        // THÊM MỚI: Dành cho thuốc mới thêm tay hoặc từ OCR (ID là null)
                        prescription.PrescriptionMedicines.Add(new PrescriptionMedicines
                        {
                            PrescriptionMedicineId = Guid.NewGuid(),
                            PrescriptionId = prescriptionId,
                            MedicineName = medDto.MedicineName,
                            Dosage = medDto.Dosage ?? "",
                            Unit = medDto.Unit ?? "",
                            Quantity = medDto.Quantity,
                            Instructions = medDto.Instructions ?? "",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }
            }

            // 5. Lưu toàn bộ thay đổi vào Database (Chỉ gọi 1 lần để tránh lỗi Concurrency)
            _unitOfWork.Repository<Prescriptions>().Update(prescription);
            await _unitOfWork.CompleteAsync();

            // 6. CẬP NHẬT LẠI LỊCH (SCHEDULE) CHO TỪNG LOẠI THUỐC
            if (request.Medicines != null)
            {
                foreach (var med in prescription.PrescriptionMedicines)
                {
                    // Xóa các chi tiết lịch cũ của thuốc này trong các khung giờ để tránh trùng lặp
                    var currentDetails = await _unitOfWork.Repository<MedicationScheduleDetails>()
                        .FindAsync(d => d.PrescriptionMedicineId == med.PrescriptionMedicineId);

                    if (currentDetails.Any())
                    {
                        _unitOfWork.Repository<MedicationScheduleDetails>().RemoveRange(currentDetails);
                        await _unitOfWork.CompleteAsync(); // Lưu để dọn sạch trước khi phân bổ lại
                    }

                    // Gọi hàm phân bổ lịch tự động dựa trên lời dặn (Instructions)
                    await AutoDistributeMedicineAsync(prescription.MemberId, med, prescription.PrescriptionDate);
                }
            }

            // 7. Ghi nhật ký hoạt động (Activity Log)
            if (hasChanges)
            {
                var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(prescription.MemberId);
                if (targetMember?.FamilyId.HasValue == true)
                {
                    var doer = (await _unitOfWork.Repository<Members>()
                        .FindAsync(m => m.FamilyId == targetMember.FamilyId && (m.UserId == userId || m.MemberId == userId)))
                        .FirstOrDefault();

                    if (doer != null)
                    {
                        await _activityLogService.LogActivityAsync(
                            familyId: targetMember.FamilyId.Value,
                            memberId: doer.MemberId,
                            actionType: ActivityActionTypes.UPDATE,
                            entityName: ActivityEntityNames.PRESCIPTION,
                            entityId: prescription.PrescriptionId,
                            description: $"Đã cập nhật đơn thuốc của '{targetMember.FullName}'.",
                            oldData: oldData,
                            newData: new { prescription.DoctorName, prescription.HospitalName, prescription.Notes, prescription.Status }
                        );
                    }
                }
            }

            return ApiResponse<PrescriptionResponse>.Ok(MapToResponse(prescription), "Cập nhật đơn thuốc và lịch uống thuốc thành công.");
        }

        // --- HÀM 2: XÓA ĐƠN THUỐC ---
        public async Task<ApiResponse<bool>> DeletePrescriptionAsync(Guid prescriptionId, Guid userId)
        {
            var prescription = await _unitOfWork.Repository<Prescriptions>().GetByIdAsync(prescriptionId);
            if (prescription == null)
            {
                return ApiResponse<bool>.Fail("Đơn thuốc không tồn tại.", 404);
            }

            if (!await _currentUserService.CheckAccess(prescription.MemberId, userId))
            {
                return ApiResponse<bool>.Fail("Không có quyền xóa.", 403);
            }
            string doctorName = prescription.DoctorName ?? "";
            Guid memberId = prescription.MemberId;

            // Xóa cứng (Hard Delete) - EF Core sẽ tự Cascade xóa thuốc và ảnh liên quan nhờ cấu hình FluentAPI
            _unitOfWork.Repository<Prescriptions>().Remove(prescription);
            await _unitOfWork.CompleteAsync();

            var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (targetMember != null && targetMember.FamilyId.HasValue)
            {
                var doer = (await _unitOfWork.Repository<Members>()
    .FindAsync(m => m.FamilyId == targetMember.FamilyId && (m.UserId == userId || m.MemberId == userId))).FirstOrDefault();

                if (doer != null)
                {
                    await _activityLogService.LogActivityAsync(
                        familyId: targetMember.FamilyId.Value,
                        memberId: doer.MemberId,
                        actionType: ActivityActionTypes.DELETE,
                       entityName: ActivityEntityNames.PRESCIPTION,
                        entityId: prescriptionId, // ID vẫn lưu lại vết
                        description: $"Đã xóa đơn thuốc của bác sĩ '{doctorName}' cấp cho '{targetMember.FullName}'."
                    );
                }
            }

            return ApiResponse<bool>.Ok(true, "Đã xóa đơn thuốc.");
        }


        public async Task<ApiResponse<PrescriptionMedicineResponse>> AddMedicineAsync(Guid prescriptionId, Guid userId, AddMedicineRequest request)
        {
            var prescription = await _unitOfWork.Repository<Prescriptions>().GetByIdAsync(prescriptionId);
            if (prescription == null) return ApiResponse<PrescriptionMedicineResponse>.Fail("Không tìm thấy đơn thuốc.", 404);

            if (!await _currentUserService.CheckAccess(prescription.MemberId, userId))
                return ApiResponse<PrescriptionMedicineResponse>.Fail("Không có quyền thực hiện.", 403);

            var newMedicine = new PrescriptionMedicines
            {
                PrescriptionMedicineId = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                MedicineName = request.MedicineName,
                Dosage = request.Dosage ?? "",
                Unit = request.Unit ?? "",
                Quantity = request.Quantity,
                Instructions = request.Instructions ?? "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<PrescriptionMedicines>().AddAsync(newMedicine);
            await _unitOfWork.CompleteAsync();

            await AutoDistributeMedicineAsync(prescription.MemberId, newMedicine, prescription.PrescriptionDate);

            var responseDto = new PrescriptionMedicineResponse
            {
                PrescriptionMedicineId = newMedicine.PrescriptionMedicineId,
                MedicineName = newMedicine.MedicineName,
                Dosage = newMedicine.Dosage,
                Unit = newMedicine.Unit,
                Quantity = newMedicine.Quantity,
                Instructions = newMedicine.Instructions
            };

            return ApiResponse<PrescriptionMedicineResponse>.Ok(responseDto, "Thêm thuốc thành công.");
        }

        // =======================================================
        // 2. CẬP NHẬT 1 LOẠI THUỐC
        // =======================================================
        public async Task<ApiResponse<PrescriptionMedicineResponse>> UpdateMedicineAsync(Guid medicineId, Guid userId, UpdateMedicineRequest request)
        {
            var medicine = (await _unitOfWork.Repository<PrescriptionMedicines>()
                .FindAsync(m => m.PrescriptionMedicineId == medicineId, "Prescription")).FirstOrDefault();

            if (medicine == null)
                return ApiResponse<PrescriptionMedicineResponse>.Fail("Không tìm thấy loại thuốc này.", 404);

            if (!await _currentUserService.CheckAccess(medicine.Prescription.MemberId, userId))
                return ApiResponse<PrescriptionMedicineResponse>.Fail("Không có quyền thực hiện.", 403);

            bool hasChanges = false;

            // --- BẮT ĐẦU MAPPING: CHỈ UPDATE NẾU CÓ TRUYỀN DATA VÀ DATA KHÁC DB ---

            if (!string.IsNullOrWhiteSpace(request.MedicineName) && medicine.MedicineName != request.MedicineName)
            {
                medicine.MedicineName = request.MedicineName;
                hasChanges = true;
            }

            // Với Dosage, Unit, Instructions: Có thể FE muốn truyền chuỗi rỗng "" để xóa trắng dữ liệu
            // Nên ta chỉ check khác null
            if (request.Dosage != null && medicine.Dosage != request.Dosage)
            {
                medicine.Dosage = request.Dosage;
                hasChanges = true;
            }

            if (request.Unit != null && medicine.Unit != request.Unit)
            {
                medicine.Unit = request.Unit;
                hasChanges = true;
            }

            if (request.Quantity.HasValue && medicine.Quantity != request.Quantity.Value)
            {
                medicine.Quantity = request.Quantity.Value;
                hasChanges = true;
            }

            if (request.Instructions != null && medicine.Instructions != request.Instructions)
            {
                medicine.Instructions = request.Instructions;
                hasChanges = true;
            }

            // --- LƯU VÀO DB NẾU CÓ SỰ THAY ĐỔI ---
            if (hasChanges)
            {
                medicine.UpdatedAt = DateTime.Now;
                _unitOfWork.Repository<PrescriptionMedicines>().Update(medicine);

                // Dọn dẹp detail cũ nếu hướng dẫn hoặc số lượng đổi
                var oldDetails = await _unitOfWork.Repository<MedicationScheduleDetails>()
                    .FindAsync(d => d.PrescriptionMedicineId == medicineId);
                foreach (var d in oldDetails)
                {
                    _unitOfWork.Repository<MedicationScheduleDetails>().Remove(d);
                }

                await _unitOfWork.CompleteAsync();

                // Phân bổ lại
                await AutoDistributeMedicineAsync(medicine.Prescription.MemberId, medicine, medicine.Prescription.PrescriptionDate);
            }

            // Map lại ra DTO để trả về cho Frontend
            var responseDto = new PrescriptionMedicineResponse
            {
                PrescriptionMedicineId = medicine.PrescriptionMedicineId,
                MedicineName = medicine.MedicineName,
                Dosage = medicine.Dosage,
                Unit = medicine.Unit,
                Quantity = medicine.Quantity,
                Instructions = medicine.Instructions
            };

            return ApiResponse<PrescriptionMedicineResponse>.Ok(responseDto, "Cập nhật thuốc thành công.");
        }

        // =======================================================
        // 3. XÓA 1 LOẠI THUỐC
        // =======================================================
        public async Task<ApiResponse<bool>> DeleteMedicineAsync(Guid medicineId, Guid userId)
        {
            var medicine = (await _unitOfWork.Repository<PrescriptionMedicines>()
                .FindAsync(m => m.PrescriptionMedicineId == medicineId, "Prescription")).FirstOrDefault();

            if (medicine == null) return ApiResponse<bool>.Fail("Không tìm thấy loại thuốc này.", 404);

            if (!await _currentUserService.CheckAccess(medicine.Prescription.MemberId, userId))
                return ApiResponse<bool>.Fail("Không có quyền thực hiện.", 403);

            _unitOfWork.Repository<PrescriptionMedicines>().Remove(medicine);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa thuốc khỏi đơn.");
        }

        // --- HÀM 3: UPLOAD ẢNH CHO ĐƠN THUỐC ĐÃ CÓ ---
        //cần check lại
        public async Task<ApiResponse<string>> AddImageToPrescriptionAsync(Guid prescriptionId, Guid userId, IFormFile file)
        {

            var prescription = await _unitOfWork.Repository<Prescriptions>().GetByIdAsync(prescriptionId);
            if (prescription == null)
            {
                return ApiResponse<string>.Fail("Đơn thuốc không tồn tại.", 404);
            }

            if (!await _currentUserService.CheckAccess(prescription.MemberId, userId))
            {
                return ApiResponse<string>.Fail("Không có quyền truy cập.", 403);
            }

            try
            {
                // Gọi Service Upload chung
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(file);

                // Lưu vào DB
                var newImage = new PrescriptionImages
                {
                    ImageId = Guid.NewGuid(),
                    PrescriptionId = prescriptionId,
                    ImageUrl = uploadResult.OriginalUrl,
                    ThumbnailUrl = uploadResult.ThumbnailUrl, // Lưu thumbnail
                    IsProcessed = false,
                    UploadedAt = DateTime.Now
                };

                await _unitOfWork.Repository<PrescriptionImages>().AddAsync(newImage);
                await _unitOfWork.CompleteAsync();

                return ApiResponse<string>.Ok(uploadResult.OriginalUrl, "Thêm ảnh thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail(ex.Message, 500);
            }
        }

        // --- Helpers ---

        private PrescriptionResponse MapToResponse(Prescriptions p)
        {
            return new PrescriptionResponse
            {
                PrescriptionId = p.PrescriptionId,
                MemberId = p.MemberId,
                DoctorName = p.DoctorName,
                HospitalName = p.HospitalName,
                PrescriptionDate = p.PrescriptionDate,
                Status = p.Status,
                Notes = p.Notes,
                Images = p.PrescriptionImages.Select(i => new PrescriptionImageDto { ImageUrl = i.ImageUrl, OcrRawData = i.OcrRawData }).ToList(),
                Medicines = p.PrescriptionMedicines.Select(m => new PrescriptionMedicineResponse
                {
                    PrescriptionMedicineId = m.PrescriptionMedicineId,
                    MedicineName = m.MedicineName,
                    Dosage = m.Dosage,
                    Unit = m.Unit,
                    Quantity = m.Quantity,
                    Instructions = m.Instructions
                }).ToList()
            };
        }

        private async Task AutoDistributeMedicineAsync(Guid memberId, PrescriptionMedicines med, DateTime prescriptionDate)
        {
            var now = DateTime.Now.Date; // Lấy ngày thực tế lúc tạo/sửa trên app thay vì ngày ghi trên đơn thuốc

            var lowerInst = (med.Instructions ?? "").ToLower();
            int totalSessions = 0;
            bool hasMorning = lowerInst.Contains("sáng");
            bool hasNoon = lowerInst.Contains("trưa");
            bool hasAfternoon = lowerInst.Contains("chiều");
            bool hasEvening = lowerInst.Contains("tối");

            if (!hasMorning && !hasNoon && !hasAfternoon && !hasEvening)
            {
                hasMorning = true;
                if (med.Quantity > 1) hasEvening = true;
            }

            if (hasMorning) totalSessions++;
            if (hasNoon) totalSessions++;
            if (hasAfternoon) totalSessions++;
            if (hasEvening) totalSessions++;

            double days = totalSessions > 0 ? (double)med.Quantity / totalSessions : 1;
            if (days < 1) days = 1;

            DateTime endDate = now.AddDays(Math.Ceiling(days) - 1);
            if (endDate < now) endDate = now;

            var existingSchedules = (await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.MemberId == memberId)).ToList();

            var modifiedScheduleIds = new List<Guid>();

            if (hasMorning)
            {
                var schedule = existingSchedules.FirstOrDefault(s => (s.ScheduleName ?? "").ToLower().Contains("sáng"))
                    ?? await CreateDefaultSchedule(memberId, "Buổi sáng", new TimeSpan(8, 0, 0));
                
                string specificDosage = ExtractDosageForSession(med.Instructions, "sáng", med.Dosage);
                await AddDetailAsync(schedule.ScheduleId, med, now, endDate, specificDosage);
                modifiedScheduleIds.Add(schedule.ScheduleId);
                if (!existingSchedules.Any(s => s.ScheduleId == schedule.ScheduleId)) existingSchedules.Add(schedule);
            }

            if (hasNoon)
            {
                var schedule = existingSchedules.FirstOrDefault(s => (s.ScheduleName ?? "").ToLower().Contains("trưa"))
                    ?? await CreateDefaultSchedule(memberId, "Buổi trưa", new TimeSpan(12, 0, 0));
                
                string specificDosage = ExtractDosageForSession(med.Instructions, "trưa", med.Dosage);
                await AddDetailAsync(schedule.ScheduleId, med, now, endDate, specificDosage);
                modifiedScheduleIds.Add(schedule.ScheduleId);
                if (!existingSchedules.Any(s => s.ScheduleId == schedule.ScheduleId)) existingSchedules.Add(schedule);
            }

            if (hasAfternoon)
            {
                var schedule = existingSchedules.FirstOrDefault(s => (s.ScheduleName ?? "").ToLower().Contains("chiều"))
                    ?? await CreateDefaultSchedule(memberId, "Buổi chiều", new TimeSpan(14, 0, 0));
                
                string specificDosage = ExtractDosageForSession(med.Instructions, "chiều", med.Dosage);
                await AddDetailAsync(schedule.ScheduleId, med, now, endDate, specificDosage);
                modifiedScheduleIds.Add(schedule.ScheduleId);
                if (!existingSchedules.Any(s => s.ScheduleId == schedule.ScheduleId)) existingSchedules.Add(schedule);
            }

            if (hasEvening)
            {
                var schedule = existingSchedules.FirstOrDefault(s => (s.ScheduleName ?? "").ToLower().Contains("tối"))
                    ?? await CreateDefaultSchedule(memberId, "Buổi tối", new TimeSpan(20, 0, 0));
                
                string specificDosage = ExtractDosageForSession(med.Instructions, "tối", med.Dosage);
                await AddDetailAsync(schedule.ScheduleId, med, now, endDate, specificDosage);
                modifiedScheduleIds.Add(schedule.ScheduleId);
                if (!existingSchedules.Any(s => s.ScheduleId == schedule.ScheduleId)) existingSchedules.Add(schedule);
            }

            await _unitOfWork.CompleteAsync();

            // ─── XÓA Reminders Pending cũ của các schedule bị ảnh hưởng ───
            // Để tránh trùng lặp sau khi đổi số lượng/hướng dẫn thuốc
            var staleReminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => modifiedScheduleIds.Contains(r.ScheduleId)
                                && r.Status == "Pending"
                                && r.ReminderDate >= now);
            if (staleReminders.Any())
            {
                _unitOfWork.Repository<MedicationReminders>().RemoveRange(staleReminders);
                await _unitOfWork.CompleteAsync();
            }

            // ─── TẠO reminders mới theo lịch mới ───
            var existingReminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => modifiedScheduleIds.Contains(r.ScheduleId) && r.ReminderDate >= now && r.ReminderDate <= endDate);

            var newReminders = new List<MedicationReminders>();

            foreach (var sId in modifiedScheduleIds)
            {
                var schedule = await _unitOfWork.Repository<MedicationSchedules>().GetByIdAsync(sId);
                
                for (var date = now.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var reminderTime = date.Add(schedule.TimeOfDay);
                    var endTime = reminderTime.AddHours(2);

                    if (endTime > DateTime.Now)
                    {
                        if (!existingReminders.Any(r => r.ScheduleId == sId && r.ReminderDate == date))
                        {
                            var reminder = new MedicationReminders
                            {
                                ReminderId = Guid.NewGuid(),
                                ScheduleId = sId,
                                ReminderDate = date,
                                ReminderTime = reminderTime,
                                EndTime = endTime,
                                Status = "Pending"
                            };
                            await _unitOfWork.Repository<MedicationReminders>().AddAsync(reminder);
                            newReminders.Add(reminder);
                        }
                    }
                }
            }

            await _unitOfWork.CompleteAsync();

            foreach (var reminder in newReminders)
            {
                _backgroundJobClient.Schedule<IReminderJobService>(
                    job => job.CheckAndNotifyOverdueReminder(reminder.ReminderId),
                    new DateTimeOffset(reminder.EndTime)
                );
            }
        }

        private async Task<MedicationSchedules> CreateDefaultSchedule(Guid memberId, string name, TimeSpan time)
        {
            var schedule = new MedicationSchedules
            {
                ScheduleId = Guid.NewGuid(),
                MemberId = memberId,
                ScheduleName = name,
                TimeOfDay = time,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Repository<MedicationSchedules>().AddAsync(schedule);
            // Save immediately to ensure it can be found by subsequent calls if needed
            await _unitOfWork.CompleteAsync();
            return schedule;
        }

        private async Task AddDetailAsync(Guid scheduleId, PrescriptionMedicines med, DateTime start, DateTime end, string specificDosage)
        {
            var detail = new MedicationScheduleDetails
            {
                ScheduleDetailId = Guid.NewGuid(),
                ScheduleId = scheduleId,
                PrescriptionMedicineId = med.PrescriptionMedicineId,
                Dosage = specificDosage,
                StartDate = start,
                EndDate = end
            };
            await _unitOfWork.Repository<MedicationScheduleDetails>().AddAsync(detail);
        }

        private string ExtractDosageForSession(string instructions, string sessionName, string defaultDosage)
        {
            if (string.IsNullOrWhiteSpace(instructions)) return defaultDosage ?? "1 Viên";

            string lowerInst = instructions.ToLower();
            string lowerSession = sessionName.ToLower();

            int idx = lowerInst.IndexOf(lowerSession);
            if (idx == -1) return defaultDosage ?? "1 Viên";

            string sub = lowerInst.Substring(idx);

            int endIdx = sub.IndexOf(',');
            if (endIdx == -1) endIdx = sub.IndexOf('-');
            if (endIdx == -1) endIdx = sub.IndexOf('.');

            string sessionPart = endIdx != -1 ? sub.Substring(0, endIdx) : sub;

            string finalDosage = sessionPart.Replace(lowerSession, "").Trim();

            return string.IsNullOrWhiteSpace(finalDosage) ? (defaultDosage ?? "1 Viên") : finalDosage;
        }
        private async Task ClearMedicationSchedulesAsync(Guid medicineId)
        {
            // Chúng ta phải tìm trong bảng MedicationScheduleDetails 
            // vì đây mới là nơi chứa ID của thuốc (PrescriptionMedicineId)
            var oldDetails = await _unitOfWork.Repository<MedicationScheduleDetails>()
                .FindAsync(d => d.PrescriptionMedicineId == medicineId);

            if (oldDetails.Any())
            {
                // 1. Xóa các chi tiết lịch của thuốc này
                _unitOfWork.Repository<MedicationScheduleDetails>().RemoveRange(oldDetails);

                // 2. (Tùy chọn) Kiểm tra nếu MedicationSchedules (khung giờ) đó 
                // không còn thuốc nào khác thì có thể xóa luôn khung giờ đó để sạch DB
                // Nhưng thông thường chỉ cần xóa Detail là đủ.
            }
        }
    }
}
