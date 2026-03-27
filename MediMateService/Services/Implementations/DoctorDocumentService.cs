using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using Share.Constants;
using System.Globalization;
using MediMateService.Shared;

namespace MediMateService.Services.Implementations
{
    public class DoctorDocumentService : IDoctorDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public DoctorDocumentService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<ApiResponse<DoctorDocumentDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorDocumentRequest request)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(doctorId);
            if (doctor == null)
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy thông tin bác sĩ.", 404);
            }

            if (doctor.UserId != currentUserId)
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Bạn không có quyền thêm tài liệu cho bác sĩ này.", 403);
            }

            var document = new DoctorDocument
            {
                DocumentId = Guid.NewGuid(),
                DoctorId = doctorId,
                FileUrl = request.FileUrl,
                Type = request.Type,
                Status = "Pending", // Mặc định luôn là chờ duyệt
                Note = string.Empty,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<DoctorDocument>().AddAsync(document);

            if (doctor.Status != DoctorStatuses.Inactive && doctor.Status != DoctorStatuses.Pending)
            {
                doctor.Status = DoctorStatuses.Pending;
                _unitOfWork.Repository<Doctors>().Update(doctor);
            }

            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document), "Nộp tài liệu thành công. Vui lòng chờ phê duyệt.");
        }

        public async Task<ApiResponse<IEnumerable<DoctorDocumentDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            var documents = await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DoctorId == doctorId, "Doctor");

            var response = documents.OrderByDescending(d => d.CreatedAt).Select(MapToDto);
            return ApiResponse<IEnumerable<DoctorDocumentDto>>.Ok(response);
        }

        public async Task<ApiResponse<PagedResult<DoctorDocumentDto>>> GetAllAsync(DoctorDocumentFilter filter)
        {
            filter ??= new DoctorDocumentFilter();

            if (filter.PageNumber <= 0)
            {
                filter.PageNumber = 1;
            }

            if (filter.PageSize <= 0)
            {
                filter.PageSize = 10;
            }

            IQueryable<DoctorDocument> query = _unitOfWork.Repository<DoctorDocument>()
                .GetQueryable()
                .Include(d => d.Doctor);

            if (filter.DoctorId.HasValue)
            {
                query = query.Where(d => d.DoctorId == filter.DoctorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(d => d.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                query = query.Where(d => d.Type.Contains(filter.Type));
            }

            var totalCount = query.Count();

            query = (filter.SortBy ?? string.Empty).ToLower() switch
            {
                "status" => filter.IsDescending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
                "type" => filter.IsDescending ? query.OrderByDescending(d => d.Type) : query.OrderBy(d => d.Type),
                _ => filter.IsDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
            };

            var items = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PagedResult<DoctorDocumentDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = items.Select(MapToDto).ToList()
            };

            return ApiResponse<PagedResult<DoctorDocumentDto>>.Ok(result);
        }

        public async Task<ApiResponse<DoctorDocumentDto>> GetByIdAsync(Guid documentId)
        {
            var document = (await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DocumentId == documentId, "Doctor")).FirstOrDefault();
            return document == null
                ? ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy tài liệu.", 404)
                : ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document));
        }

        public async Task<ApiResponse<DoctorDocumentDto>> UpdateAsync(Guid documentId, Guid currentUserId, UpdateDoctorDocumentRequest request)
        {
            var document = (await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DocumentId == documentId, "Doctor")).FirstOrDefault();

            if (document == null)
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);
            }

            if (document.Doctor.UserId != currentUserId)
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Bạn không có quyền sửa tài liệu này.", 403);
            }

            // Bác sĩ cập nhật lại tài liệu -> Trạng thái phải quay về Pending để duyệt lại
            document.FileUrl = request.FileUrl;
            document.Type = request.Type;
            document.Status = "Pending";
            document.ReviewBy = string.Empty;
            document.ReviewAt = string.Empty;
            document.Note = "Tài liệu vừa được cập nhật, chờ duyệt lại.";

            _unitOfWork.Repository<DoctorDocument>().Update(document);

            // Chốt hạ Direction 2: Hễ sửa bằng cấp là quay về Pending
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(document.DoctorId);
            if (doctor != null && doctor.Status != DoctorStatuses.Inactive && doctor.Status != DoctorStatuses.Pending)
            {
                doctor.Status = DoctorStatuses.Pending;
                _unitOfWork.Repository<Doctors>().Update(doctor);
            }

            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document), "Cập nhật tài liệu thành công. Vui lòng chờ phê duyệt lại.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid documentId, Guid currentUserId)
        {
            var document = (await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DocumentId == documentId, "Doctor")).FirstOrDefault();

            if (document == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy tài liệu.", 404);
            }

            if (document.Doctor.UserId != currentUserId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa tài liệu này.", 403);
            }

            _unitOfWork.Repository<DoctorDocument>().Remove(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tài liệu thành công.");
        }

        // Dành riêng cho Admin / Quản lý
        public async Task<ApiResponse<DoctorDocumentDto>> ReviewDocumentAsync(Guid documentId, string reviewerName, ReviewDoctorDocumentRequest request)
        {
            var document = await _unitOfWork.Repository<DoctorDocument>().GetByIdAsync(documentId);
            if (document == null)
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);
            }

            var normalizedStatus = NormalizeReviewStatus(request.Status);
            if (string.IsNullOrEmpty(normalizedStatus))
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Trạng thái duyệt không hợp lệ.", 400);
            }

            if (normalizedStatus == "Rejected" && string.IsNullOrWhiteSpace(request.Note))
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Vui lòng nhập lý do từ chối (Note) để bác sĩ biết và khắc phục.", 400);
            }

            document.Status = normalizedStatus;
            document.Note = request.Note ?? string.Empty;
            document.ReviewBy = reviewerName;
            document.ReviewAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            _unitOfWork.Repository<DoctorDocument>().Update(document);

            // Tự động chuyển trạng thái bác sĩ sang Verified sau khi Approve HẾT tài liệu
            if (normalizedStatus == "Approved")
            {
                var allDocs = await _unitOfWork.Repository<DoctorDocument>()
                    .FindAsync(d => d.DoctorId == document.DoctorId);

                // Nếu tất cả tài liệu hiện có đều đã được Approved (bao gồm cả tài liệu vừa update)
                if (allDocs.All(d => d.Status == "Approved"))
                {
                    var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(document.DoctorId);
                    if (doctor != null && doctor.Status == DoctorStatuses.Pending)
                    {
                        doctor.Status = DoctorStatuses.Verified;
                        _unitOfWork.Repository<Doctors>().Update(doctor);

                        var userRepo = _unitOfWork.Repository<User>();
                        var user = await userRepo.GetByIdAsync(doctor.UserId);
                        if (user != null)
                        {
                            var otp = new Random().Next(100000, 999999);
                            user.VerifyCode = otp;
                            user.ExpiriedAt = DateTime.Now.AddMinutes(30);
                            userRepo.Update(user);

                            if (!string.IsNullOrEmpty(user.Email))
                            {
                                string subject = "Tài khoản Bác sĩ của bạn đã được duyệt";
                                string body = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <meta http-equiv=""X-UA-Compatible"" content=""ie=edge"" />
  <title>OTP Verification</title>
  <link href=""https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600&display=swap"" rel=""stylesheet"" />
</head>
<body style=""margin: 0; font-family: 'Poppins', sans-serif; background: #ffffff; font-size: 14px;"">
  <div style=""max-width: 680px; margin: 0 auto; padding: 45px 30px 60px; background: #f4f7ff; background-image: url(https://archisketch-resources.s3.ap-northeast-2.amazonaws.com/vrstyler/1661497957196_595865/email-template-background-banner); background-repeat: no-repeat; background-size: 800px 452px; background-position: top center; color: #434343;"">
    <main style=""margin: 0; margin-top: 70px; padding: 92px 30px 115px; background: #ffffff; border-radius: 30px; text-align: center;"">
      <div style=""width: 100%; max-width: 489px; margin: 0 auto;"">
        <h1 style=""margin: 0; font-size: 24px; font-weight: 500; color: #1f1f1f;"">Your OTP</h1>
        <p style=""margin: 0; margin-top: 17px; font-size: 16px; font-weight: 500;"">Hey {user.FullName},</p>
        <p style=""margin: 0; margin-top: 17px; font-weight: 500; letter-spacing: 0.56px;"">
          Thank you for choosing MediMate+. Use the following OTP to complete the activation procedure for your doctor account. OTP is valid for <strong>30 minutes</strong>. Do not share this code with others.
        </p>
        <p style=""margin: 0; margin-top: 60px; font-size: 40px; font-weight: 600; letter-spacing: 12px; color: #ba3d4f;"">
          {otp}
        </p>
      </div>
    </main>
  </div>
</body>
</html>";

                                await _emailService.SendEmailAsync(user.Email, subject, body);
                            }
                        }
                    }
                }
            }

            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document), "Phê duyệt tài liệu thành công.");
        }

        private DoctorDocumentDto MapToDto(DoctorDocument d)
        {
            var normalizedStatus = NormalizeDocumentStatus(d.Status);
            var normalizedType = NormalizeDocumentType(d.Type);
            var reviewAt = ParseDateTime(d.ReviewAt);
            var extension = GetFileExtension(d.FileUrl);

            return new DoctorDocumentDto
            {
                DocumentId = d.DocumentId,
                DoctorId = d.DoctorId,
                DoctorName = d.Doctor?.FullName,
                DoctorSpecialty = d.Doctor?.Specialty,
                FileUrl = d.FileUrl,
                DocumentName = d.Type,
                DocumentType = normalizedType,
                Type = d.Type,
                Status = normalizedStatus,
                RejectReason = normalizedStatus == "REJECTED" ? d.Note : null,
                SubmittedAt = d.CreatedAt,
                ReviewedByName = d.ReviewBy,
                ReviewedAt = reviewAt,
                FileMimeType = GetMimeTypeByExtension(extension),
                FileExtension = extension,
                UpdatedAt = d.UpdatedAt,
                ReviewBy = d.ReviewBy,
                ReviewAt = d.ReviewAt,
                Note = d.Note,
                CreatedAt = d.CreatedAt
            };
        }

        private static string NormalizeDocumentStatus(string? rawStatus)
        {
            var value = (rawStatus ?? string.Empty).Trim().ToUpperInvariant();
            return value switch
            {
                "PENDING" => "PENDING",
                "APPROVED" => "APPROVED",
                "REJECTED" => "REJECTED",
                _ => value
            };
        }

        private static string? NormalizeReviewStatus(string? input)
        {
            var value = (input ?? string.Empty).Trim().ToUpperInvariant();
            return value switch
            {
                "PENDING" => "Pending",
                "APPROVED" => "Approved",
                "REJECTED" => "Rejected",
                _ => null
            };
        }

        private static string NormalizeDocumentType(string? rawType)
        {
            var value = (rawType ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value))
            {
                return DoctorDocumentTypes.Other;
            }

            var upper = value.ToUpperInvariant();
            if (DoctorDocumentTypes.All.Contains(upper))
            {
                return upper;
            }

            if (upper.Contains("PRACTICE") || upper.Contains("LICENSE") || upper.Contains("HANH_NGHE"))
            {
                return DoctorDocumentTypes.PracticeLicense;
            }

            if (upper.Contains("SPECIALIST") || upper.Contains("CHUYEN_KHOA"))
            {
                return DoctorDocumentTypes.SpecialistCertificate;
            }

            return upper.Contains("CME") ? DoctorDocumentTypes.Cme : DoctorDocumentTypes.Other;
        }

        private static DateTime? ParseDateTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
        }

        private static string? GetFileExtension(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return null;
            }

            var rawPath = fileUrl;
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                rawPath = uri.AbsolutePath;
            }

            var ext = Path.GetExtension(rawPath);
            return string.IsNullOrWhiteSpace(ext) ? null : ext.TrimStart('.').ToLowerInvariant();
        }

        private static string? GetMimeTypeByExtension(string? extension)
        {
            return extension switch
            {
                "pdf" => "application/pdf",
                "jpg" => "image/jpeg",
                "jpeg" => "image/jpeg",
                "png" => "image/png",
                "webp" => "image/webp",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => null
            };
        }
    }
}