using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;
using Share.Common;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly IActivityLogService _activityLogService;

        public PrescriptionService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IUploadPhotoService uploadPhotoService, IActivityLogService activityLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _uploadPhotoService = uploadPhotoService;
            _activityLogService = activityLogService;
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
            // 1. Lấy đơn thuốc (kèm thuốc để xử lý update list)
            var prescription = (await _unitOfWork.Repository<Prescriptions>()
                .FindAsync(p => p.PrescriptionId == prescriptionId, includeProperties: "PrescriptionMedicines,PrescriptionImages"))
                .FirstOrDefault();

            if (prescription == null)
            {
                return ApiResponse<PrescriptionResponse>.Fail("Đơn thuốc không tồn tại.", 404);
            }

            // 2. Check quyền
            if (!await _currentUserService.CheckAccess(prescription.MemberId, userId))
            {
                return ApiResponse<PrescriptionResponse>.Fail("Không có quyền chỉnh sửa.", 403);
            }
            var oldData = new { prescription.DoctorName, prescription.HospitalName, prescription.Notes, prescription.Status };
            bool hasChanges = false;

            // 3. Update từng trường (Giữ giá trị cũ nếu request null)
            if (!string.IsNullOrEmpty(request.PrescriptionCode) && request.PrescriptionCode != prescription.PrescriptionCode)
            {
                prescription.PrescriptionCode = request.PrescriptionCode;
                hasChanges = true;
            }

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

            if (!string.IsNullOrEmpty(request.Notes) && request.Notes != prescription.Notes)
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

            // 4. Xử lý danh sách thuốc (Nếu có gửi list mới)
            if (request.Medicines != null)
            {
                // Xóa hết thuốc cũ
                var oldMedicines = prescription.PrescriptionMedicines.ToList();
                foreach (var oldMed in oldMedicines)
                {
                    _unitOfWork.Repository<PrescriptionMedicines>().Remove(oldMed);
                }

                // Thêm thuốc mới (đã chỉnh sửa từ UI)
                foreach (var med in request.Medicines)
                {
                    prescription.PrescriptionMedicines.Add(new PrescriptionMedicines
                    {
                        PrescriptionMedicineId = Guid.NewGuid(),
                        PrescriptionId = prescriptionId,
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

            _unitOfWork.Repository<Prescriptions>().Update(prescription);
            await _unitOfWork.CompleteAsync();

            if (hasChanges)
            {
                var newData = new { prescription.DoctorName, prescription.HospitalName, prescription.Notes, prescription.Status };
                var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(prescription.MemberId);

                if (targetMember != null && targetMember.FamilyId.HasValue)
                {
                    var doer = (await _unitOfWork.Repository<Members>()
    .FindAsync(m => m.FamilyId == targetMember.FamilyId && (m.UserId == userId || m.MemberId == userId))).FirstOrDefault();

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
                            newData: newData
                        );
                    }
                }
            }

            return ApiResponse<PrescriptionResponse>.Ok(MapToResponse(prescription), "Cập nhật đơn thuốc thành công.");
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
                Medicines = p.PrescriptionMedicines.Select(m => new PrescriptionMedicineDto
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


    }
}
