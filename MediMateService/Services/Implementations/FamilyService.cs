using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using Share.Constants;
using static MediMateRepository.Model.Families;

namespace MediMateService.Services.Implementations
{
    public class FamilyService : IFamilyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _activityLogService;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly INotificationService _notificationService;

        public FamilyService(IUnitOfWork unitOfWork, IActivityLogService activityLogService, IUploadPhotoService uploadPhotoService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _activityLogService = activityLogService;
            _uploadPhotoService = uploadPhotoService;
            _notificationService = notificationService;
        }

        // --- LOGIC 1: CHẾ ĐỘ CÁ NHÂN ---
        public async Task<ApiResponse<FamilyResponse>> CreatePersonalFamilyAsync(Guid userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<FamilyResponse>.Fail("User not found", 404);
            }

            // 1. Tạo Family với Type = Personal
            var family = new Families
            {
                FamilyId = Guid.NewGuid(),
                FamilyName = $"Hồ sơ cá nhân của {user.FullName}", // Tên tự động
                CreateBy = userId,
                Type = FamilyType.Personal, // Đánh dấu là cá nhân
                JoinCode = GenerateJoinCode(),
                CreatedAt = DateTime.Now
            };

            // 2. Tạo Member (Copy info từ User)
            var member = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = family.FamilyId,
                UserId = userId,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth ?? DateTime.Now,
                Gender = user.Gender ?? "Other",
                AvatarUrl = user.AvatarUrl,
                Role = Roles.Owner,
                IsActive = true
            };

            await _unitOfWork.Repository<Families>().AddAsync(family);
            await _unitOfWork.Repository<Members>().AddAsync(member);

            // 3. Tự động gán gói Freemium (Price = 0)
            var freemiumPackage = (await _unitOfWork.Repository<MembershipPackages>()
                .FindAsync(p => p.Price == 0)).FirstOrDefault();

            if (freemiumPackage != null)
            {
                var subscription = new FamilySubscriptions
                {
                    SubscriptionId = Guid.NewGuid(),
                    FamilyId = family.FamilyId,
                    PackageId = freemiumPackage.PackageId,
                    UserId = userId,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(freemiumPackage.DurationDays)),
                    Status = "Active",
                    AutoRenew = false,
                    RemainingOcrCount = freemiumPackage.OcrLimit,
                };
                await _unitOfWork.Repository<FamilySubscriptions>().AddAsync(subscription);
            }

            await _unitOfWork.CompleteAsync();

            await _activityLogService.LogActivityAsync(
                familyId: family.FamilyId,
                memberId: member.MemberId,
                actionType: ActivityActionTypes.CREATE,
                entityName: ActivityEntityNames.FAMILY,
                entityId: family.FamilyId,
                description: "Đã khởi tạo hồ sơ cá nhân và kích hoạt gói Freemium."
            );

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, 1));
        }

        // --- LOGIC 2: CHẾ ĐỘ GIA ĐÌNH ---
        public async Task<ApiResponse<FamilyResponse>> CreateSharedFamilyAsync(Guid userId, CreateSharedFamilyRequest request)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<FamilyResponse>.Fail("User not found", 404);
            }

            // 1. Tạo Family với Type = Shared
            var family = new Families
            {
                FamilyId = Guid.NewGuid(),
                FamilyName = request.FamilyName, // Tên do user nhập
                CreateBy = userId,
                Type = FamilyType.Shared, // Đánh dấu là gia đình
                JoinCode = GenerateJoinCode(),
                CreatedAt = DateTime.Now
            };

            // 2. Tạo Member (Người tạo là Owner)
            var member = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = family.FamilyId,
                UserId = userId,
                FullName = user.FullName, // Mặc định lấy tên user, sau này có thể sửa biệt danh trong gia đình
                DateOfBirth = user.DateOfBirth ?? DateTime.Now,
                Gender = user.Gender ?? "Other",
                AvatarUrl = user.AvatarUrl,
                Role = Roles.Owner,
                IsActive = true
            };

            await _unitOfWork.Repository<Families>().AddAsync(family);
            await _unitOfWork.Repository<Members>().AddAsync(member);

            // 3. Tự động gán gói Freemium (Price = 0)
            var freemiumPackage = (await _unitOfWork.Repository<MembershipPackages>()
                .FindAsync(p => p.Price == 0)).FirstOrDefault();

            if (freemiumPackage != null)
            {
                var subscription = new FamilySubscriptions
                {
                    SubscriptionId = Guid.NewGuid(),
                    FamilyId = family.FamilyId,
                    PackageId = freemiumPackage.PackageId,
                    UserId = userId,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(freemiumPackage.DurationDays)),
                    Status = "Active",
                    AutoRenew = false,
                    RemainingOcrCount = freemiumPackage.OcrLimit,
                };
                await _unitOfWork.Repository<FamilySubscriptions>().AddAsync(subscription);
            }

            await _unitOfWork.CompleteAsync();

            await _activityLogService.LogActivityAsync(
                familyId: family.FamilyId,
                memberId: member.MemberId,
                actionType: ActivityActionTypes.CREATE,
                entityName: ActivityEntityNames.FAMILY,
                entityId: family.FamilyId,
                description: $"Đã tạo gia đình '{family.FamilyName}' và kích hoạt gói Freemium."
            );

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, 1));
        }

        public async Task<ApiResponse<IEnumerable<FamilyResponse>>> GetMyFamiliesAsync(Guid callerId)
        {
            IEnumerable<Members> membersList;

            // 1. Thử tìm theo UserId (Trường hợp là User/Bố Mẹ)
            // Một User có thể tham gia nhiều Family -> List Members
            membersList = await _unitOfWork.Repository<Members>().FindAsync(m => m.UserId == callerId);

            // 2. Nếu không thấy (hoặc list rỗng), thử tìm theo MemberId (Trường hợp là Dependent)
            if (!membersList.Any())
            {
                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(callerId);
                if (member != null)
                {
                    membersList = new List<Members> { member };
                }
            }

            // Nếu vẫn không thấy ai -> Trả về rỗng
            if (membersList == null || !membersList.Any())
            {
                return ApiResponse<IEnumerable<FamilyResponse>>.Ok(new List<FamilyResponse>(), "Không tìm thấy gia đình nào.");
            }

            var result = new List<FamilyResponse>();

            // 3. Duyệt danh sách để lấy thông tin Family
            foreach (var mem in membersList)
            {
                if (mem.FamilyId.HasValue) // Chỉ xử lý nếu đã join family
                {
                    var family = await _unitOfWork.Repository<Families>().GetByIdAsync(mem.FamilyId.Value);
                    if (family != null)
                    {
                        // Đếm số thành viên
                        var count = (await _unitOfWork.Repository<Members>()
                            .FindAsync(m => m.FamilyId == family.FamilyId)).Count();

                        // [SỬA LỖI Ở ĐÂY] 
                        // Dùng luôn hàm MapToResponse để tái sử dụng code, đảm bảo luôn trả về đầy đủ Avatar và các trường khác
                        result.Add(MapToResponse(family, count));
                    }
                }
            }

            return ApiResponse<IEnumerable<FamilyResponse>>.Ok(result.DistinctBy(f => f.FamilyId));
        }

        // Helper functions
        private FamilyResponse MapToResponse(Families family, int count)
        {
            return new FamilyResponse
            {
                FamilyId = family.FamilyId,
                FamilyName = family.FamilyName,
                Type = family.Type.ToString(), // Trả về "Personal" hoặc "Shared"
                JoinCode = family.JoinCode,
                IsOpenJoin = family.IsOpenJoin,
                FamilyAvatarUrl = family.FamilyAvatarUrl ?? null,
                MemberCount = count,
                CreatedAt = family.CreatedAt
            };
        }

        public async Task<ApiResponse<FamilyResponse>> GetFamilyByIdAsync(Guid familyId, Guid userId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null)
            {
                return ApiResponse<FamilyResponse>.Fail("Gia đình không tồn tại.", 404);
            }

            // Kiểm tra xem User có phải là thành viên của gia đình này không
            var isMember = (await _unitOfWork.Repository<Members>()
    .FindAsync(m => m.FamilyId == familyId && (m.UserId == userId || m.MemberId == userId))).Any();

            if (!isMember)
            {
                return ApiResponse<FamilyResponse>.Fail("Bạn không có quyền xem gia đình này.", 403);
            }

            // Đếm số thành viên
            var count = (await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId)).Count();

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, count));
        }

        public async Task<ApiResponse<FamilyResponse>> UpdateFamilyAsync(Guid familyId, Guid userId, UpdateFamilyRequest request)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);

            if (family == null)
            {
                return ApiResponse<FamilyResponse>.Fail("Gia đình không tồn tại.", 404);
            }

            // Kiểm tra quyền: Chỉ người tạo (CreateBy) hoặc Owner mới được sửa
            // (Nếu logic của bạn dùng bảng Members để phân quyền Owner thì query bảng Members ở đây)
            if (family.CreateBy != userId)
            {
                return ApiResponse<FamilyResponse>.Fail("Bạn không có quyền chỉnh sửa thông tin gia đình này.", 403);
            }
            var oldData = new { family.FamilyName, family.IsOpenJoin };
            bool hasChanges = false;

            // 1. Cập nhật Tên (nếu có gửi lên)
            if (!string.IsNullOrEmpty(request.FamilyName))
            {
                family.FamilyName = request.FamilyName;
                hasChanges = true;
            }

            // 2. Cập nhật Trạng thái Mở/Đóng Join Code (nếu có gửi lên)
            // .HasValue kiểm tra xem client có gửi trường này không
            if (request.IsOpenJoin.HasValue)
            {
                family.IsOpenJoin = request.IsOpenJoin.Value;
                hasChanges = true;
            }
            if (request.FamilyAvatar != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(request.FamilyAvatar);
                family.FamilyAvatarUrl = uploadResult.OriginalUrl;
                hasChanges = true;
            }

            _unitOfWork.Repository<Families>().Update(family);
            await _unitOfWork.CompleteAsync();

            if (hasChanges)
            {
                var newData = new { family.FamilyName, family.IsOpenJoin };

                // Lấy thông tin người thực hiện để lưu MemberId
                var doer = (await _unitOfWork.Repository<Members>()
    .FindAsync(m => m.FamilyId == familyId && (m.UserId == userId || m.MemberId == userId))).FirstOrDefault();

                if (doer != null)
                {
                    await _activityLogService.LogActivityAsync(
                        familyId: family.FamilyId,
                        memberId: doer.MemberId,
                        actionType: ActivityActionTypes.UPDATE,
                        entityName: ActivityEntityNames.FAMILY,
                        entityId: family.FamilyId,
                        description: "Đã cập nhật thông tin chung của gia đình.",
                        oldData: oldData,
                        newData: newData
                    );
                }
            }

            // Trả về thông tin mới nhất
            return await GetFamilyByIdAsync(familyId, userId);
        }

        public async Task<ApiResponse<bool>> DeleteFamilyAsync(Guid familyId, Guid userId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null)
            {
                return ApiResponse<bool>.Fail("Family not found", 404);
            }

            if (family.CreateBy != userId)
            {
                return ApiResponse<bool>.Fail("Chỉ chủ gia đình mới được xóa.", 403);
            }

            // ═══════════════════════════════════════════════════════════
            // [PHASE 4] RÀNG BUỘC: KIỂM TRA LỊCH HẸN ĐANG CHỜ TRƯỚC KHI XÓA
            // Nếu bất kỳ thành viên nào trong gia đình còn lịch Pending/Approved,
            // chặn thao tác xóa để tránh mất dữ liệu thanh toán.
            // ═══════════════════════════════════════════════════════════
            var memberIds = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId))
                .Select(m => m.MemberId)
                .ToList();

            if (memberIds.Any())
            {
                var hasActiveAppointments = (await _unitOfWork.Repository<Appointments>()
                    .FindAsync(a => memberIds.Contains(a.MemberId)
                                && (a.Status == "Pending" || a.Status == "Approved")))
                    .Any();

                if (hasActiveAppointments)
                {
                    return ApiResponse<bool>.Fail(
                        "Không thể giải tán gia đình khi có thành viên còn lịch khám đang chờ (Pending/Approved). " +
                        "Vui lòng hủy tất cả lịch hẹn trước.", 409);
                }
            }

            // Soft Delete: Xóa Family thì giải phóng tất cả thành viên (Set FamilyId = null)
            var members = await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId);
            foreach (var member in members)
            {
                member.FamilyId = null; // Mồ côi
                _unitOfWork.Repository<Members>().Update(member);
            }

            _unitOfWork.Repository<Families>().Remove(family); // Xóa Family
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã giải tán gia đình thành công.");
        }

        private string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[new Random().Next(s.Length)]).ToArray());
        }

        public async Task<ApiResponse<FamilySubscriptionResponse>> GetFamilySubscriptionAsync(Guid familyId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null)
            {
                return ApiResponse<FamilySubscriptionResponse>.Fail("Gia đình không tồn tại.", 404);
            }

            var subscription = (await _unitOfWork.Repository<FamilySubscriptions>()
                .FindAsync(fs => fs.FamilyId == familyId && fs.Status == "Active"))
                .OrderByDescending(fs => fs.EndDate)
                .FirstOrDefault();

            if (subscription == null)
            {
                return ApiResponse<FamilySubscriptionResponse>.Fail("Gia đình chưa có gói đăng ký nào hoặc gói đã hết hạn.", 404);
            }

            var package = await _unitOfWork.Repository<MembershipPackages>().GetByIdAsync(subscription.PackageId);
            if (package == null)
            {
                return ApiResponse<FamilySubscriptionResponse>.Fail("Không tìm thấy thông tin gói đăng ký.", 404);
            }

            var response = new FamilySubscriptionResponse
            {
                SubscriptionId = subscription.SubscriptionId,
                PackageName = package.PackageName,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status,
                RemainingOcrCount = subscription.RemainingOcrCount,
                OcrLimit = package.OcrLimit,

            };

            return ApiResponse<FamilySubscriptionResponse>.Ok(response, "Lấy thông tin gói đăng ký thành công.");
        }
        
        public async Task<ApiResponse<PagedResult<AdminFamilySubscriptionResponse>>> GetAllFamilySubscriptionsAsync(AdminFamilySubscriptionFilter filter)
        {
            var query = _unitOfWork.Repository<FamilySubscriptions>().GetQueryable();
            
            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(fs => fs.Status.ToLower() == filter.Status.ToLower());
            }

            if (filter.PackageId.HasValue)
            {
                query = query.Where(fs => fs.PackageId == filter.PackageId.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(fs => fs.Family.FamilyName.ToLower().Contains(search) 
                                       || fs.User.FullName.ToLower().Contains(search)
                                       || fs.User.Email.ToLower().Contains(search));
            }

            var totalCount = query.Count();

            var subscriptions = query
                .OrderByDescending(fs => fs.StartDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(fs => new AdminFamilySubscriptionResponse
                {
                    SubscriptionId = fs.SubscriptionId,
                    FamilyId = fs.FamilyId,
                    FamilyName = fs.Family.FamilyName ?? "N/A",
                    FamilyAvatarUrl = fs.Family.FamilyAvatarUrl,
                    PackageName = fs.Package.PackageName,
                    StartDate = fs.StartDate,
                    EndDate = fs.EndDate,
                    Status = fs.Status,
                    RemainingOcrCount = fs.RemainingOcrCount,
                    Price = fs.Package.Price,
                    UserName = fs.User.FullName ?? "N/A",
                    UserEmail = fs.User.Email
                })
                .ToList();

            var result = new PagedResult<AdminFamilySubscriptionResponse>
            {
                Items = subscriptions,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<AdminFamilySubscriptionResponse>>.Ok(result);
        }

        public async Task<ApiResponse<bool>> UpdateFamilySubscriptionStatusAsync(Guid subscriptionId, string status)
        {
            var subscription = await _unitOfWork.Repository<FamilySubscriptions>().GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy gói đăng ký.", 404);
            }

            var allowedStatuses = new[] { "Active", "Cancelled", "Expired" };
            if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return ApiResponse<bool>.Fail("Trạng thái không hợp lệ. Các trạng thái cho phép: Active, Cancelled, Expired.", 400);
            }

            subscription.Status = status;

            _unitOfWork.Repository<FamilySubscriptions>().Update(subscription);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, $"Đã cập nhật trạng thái gói thành {status}.");
        }

        // ─────────────────────────────────────────────────────────────────
        // HỦY GÓI ĐĂNG KÝ (Cancel Subscription)
        // Điều kiện: Gói phải còn Active, chỉ chủ hộ mới được hủy
        // Logic: Nếu đã dùng > 10% thời gian gói → KHÔNG được hoàn tiền
        //        Nếu dùng <= 10% → tạo Payment + Transaction hoàn tiền
        //        Sau khi hủy → kích hoạt lại gói Freemium (Price = 0)
        // ─────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<bool>> CancelSubscriptionAsync(Guid subscriptionId, Guid userId)
        {
            // 1. Lấy subscription và load Package
            var subscription = await _unitOfWork.Repository<FamilySubscriptions>().GetQueryable()
                .Include(s => s.Package)
                .Include(s => s.Family)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                return ApiResponse<bool>.Fail("Không tìm thấy gói đăng ký.", 404);

            // 2. Kiểm tra gói còn hiệu lực
            if (subscription.Status != "Active")
                return ApiResponse<bool>.Fail("Chỉ có thể hủy gói đang ở trạng thái Active.", 400);

            // 3. Gói Freemium (Price = 0) không cho phép hủy
            if (subscription.Package.Price == 0)
                return ApiResponse<bool>.Fail("Không thể hủy gói Freemium miễn phí.", 400);

            // 4. Kiểm tra quyền: chỉ chủ hộ (UserId khớp với người tạo subscription) mới được hủy
            if (subscription.UserId != userId)
            {
                // Kiểm tra xem có phải là Owner của Family không
                var isOwner = await _unitOfWork.Repository<Members>().GetQueryable()
                    .AnyAsync(m => m.FamilyId == subscription.FamilyId
                                   && m.UserId == userId
                                   && m.Role == Roles.Owner);

                if (!isOwner)
                    return ApiResponse<bool>.Fail("Chỉ chủ hộ gia đình mới có quyền hủy gói đăng ký.", 403);
            }

            // 5. Tính phần trăm thời gian đã sử dụng
            var today = DateOnly.FromDateTime(DateTime.Now);
            int totalDays = subscription.EndDate.DayNumber - subscription.StartDate.DayNumber;
            int usedDays = today.DayNumber - subscription.StartDate.DayNumber;
            
            // Đảm bảo usedDays không âm
            usedDays = Math.Max(0, usedDays);
            double timeUsagePercent = totalDays > 0 ? (double)usedDays / totalDays * 100 : 100;

            // 5b. Tính phần trăm OCR đã sử dụng
            // OcrLimit là tổng lượt, RemainingOcrCount là số còn lại
            int totalOcr = subscription.Package.OcrLimit;
            int usedOcr = totalOcr - subscription.RemainingOcrCount;
            usedOcr = Math.Max(0, usedOcr); // Đảm bảo không âm
            double ocrUsagePercent = totalOcr > 0 ? (double)usedOcr / totalOcr * 100 : 0;

            // Không đủ điều kiện hoàn tiền nếu:
            // - Thời gian sử dụng > 10% HOẶC
            // - OCR đã dùng > 10% tổng lượt
            bool isEligibleForRefund = timeUsagePercent <= 10.0 && ocrUsagePercent <= 10.0;

            // 6. Lấy Payment gốc của gói này để xác định số tiền và người trả
            var originalPayment = await _unitOfWork.Repository<Payments>().GetQueryable()
                .Where(p => p.SubscriptionId == subscriptionId && p.Status == "Success")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            // 7. Đánh dấu subscription là Cancelled
            subscription.Status = "Cancelled";
            subscription.EndDate = today; // Kết thúc sớm ngay hôm nay
            _unitOfWork.Repository<FamilySubscriptions>().Update(subscription);

            // 8. Nếu đủ điều kiện hoàn tiền → tạo Payment + Transaction
            decimal refundAmount = 0;
            if (isEligibleForRefund && originalPayment != null && originalPayment.Amount > 0)
            {
                refundAmount = originalPayment.Amount;

                var refundPayment = new Payments
                {
                    PaymentId = Guid.NewGuid(),
                    SubscriptionId = subscriptionId,
                    UserId = userId,
                    Amount = refundAmount,
                    PaymentContent = $"Hoàn tiền hủy gói {subscription.Package.PackageName} - #{subscriptionId.ToString()[..8].ToUpper()}",
                    Status = "Refunded",
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.Repository<Payments>().AddAsync(refundPayment);

                var refundTransaction = new Transactions
                {
                    TransactionId = Guid.NewGuid(),
                    TransactionCode = $"SUB-REFUND-{subscriptionId.ToString()[..8].ToUpper()}",
                    PaymentId = refundPayment.PaymentId,
                    GatewayName = "Manual",
                    TransactionStatus = "Pending", // Pending vì Admin cần duyệt chuyển khoản thực tế
                    AmountPaid = refundAmount,
                    TransactionType = Share.Constants.TransactionTypes.OutRefundSubscription,
                    GatewayResponse = null,
                    PaidAt = null // Chưa trả, Admin sẽ cập nhật sau
                };
                await _unitOfWork.Repository<Transactions>().AddAsync(refundTransaction);
            }

            // 9. Kích hoạt lại gói Freemium sẵn có (ưu tiên gói đã tồn tại trong DB)
            //    Tìm gói Free cũ (Price = 0) của gia đình đang ở trạng thái Inactive/Expired
            var freemiumPackage = await _unitOfWork.Repository<MembershipPackages>().GetQueryable()
                .FirstOrDefaultAsync(p => p.Price == 0 && p.IsActive);

            if (freemiumPackage != null)
            {
                var existingFreeSub = await _unitOfWork.Repository<FamilySubscriptions>().GetQueryable()
                    .Where(s => s.FamilyId == subscription.FamilyId
                                && s.PackageId == freemiumPackage.PackageId
                                && s.SubscriptionId != subscriptionId)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

                if (existingFreeSub != null)
                {
                    // Tái kích hoạt gói Free cũ đã tồn tại
                    existingFreeSub.Status = "Active";
                    existingFreeSub.EndDate = today.AddDays(freemiumPackage.DurationDays);
                    existingFreeSub.RemainingOcrCount = freemiumPackage.OcrLimit;
                    _unitOfWork.Repository<FamilySubscriptions>().Update(existingFreeSub);
                }
                else
                {
                    // Không tìm thấy gói Free cũ → tạo mới (fallback)
                    var newFreeSub = new FamilySubscriptions
                    {
                        SubscriptionId = Guid.NewGuid(),
                        FamilyId = subscription.FamilyId,
                        PackageId = freemiumPackage.PackageId,
                        UserId = userId,
                        StartDate = today,
                        EndDate = today.AddDays(freemiumPackage.DurationDays),
                        Status = "Active",
                        AutoRenew = false,
                        RemainingOcrCount = freemiumPackage.OcrLimit
                    };
                    await _unitOfWork.Repository<FamilySubscriptions>().AddAsync(newFreeSub);
                }
            }

            await _unitOfWork.CompleteAsync();

            // 10. Ghi Activity Log
            var doer = await _unitOfWork.Repository<Members>().GetQueryable()
                .FirstOrDefaultAsync(m => m.FamilyId == subscription.FamilyId && m.UserId == userId);

            if (doer != null)
            {
                await _activityLogService.LogActivityAsync(
                    familyId: subscription.FamilyId,
                    memberId: doer.MemberId,
                    actionType: ActivityActionTypes.UPDATE,
                    entityName: ActivityEntityNames.FAMILY,
                    entityId: subscription.SubscriptionId,
                    description: isEligibleForRefund
                        ? $"Đã hủy gói '{subscription.Package.PackageName}' (thời gian: {timeUsagePercent:F1}%, OCR: {ocrUsagePercent:F1}%). Yêu cầu hoàn tiền {refundAmount:N0} VND đang chờ xử lý."
                        : $"Đã hủy gói '{subscription.Package.PackageName}' (thời gian: {timeUsagePercent:F1}%, OCR: {ocrUsagePercent:F1}%). Không đủ điều kiện hoàn tiền. Đã chuyển về gói Freemium."
                );
            }

            // 11. Gửi thông báo cho chủ hộ
            string notifTitle = isEligibleForRefund
                ? " Hủy gói thành công - Đang xử lý hoàn tiền"
                : " Hủy gói thành công";

            string notifMessage = isEligibleForRefund
                ? $"Gói '{subscription.Package.PackageName}' đã bị hủy. Bạn đã sử dụng {timeUsagePercent:F1}% thời gian và {ocrUsagePercent:F1}% lượt OCR (đều ≤ 10%). " +
                  $"Yêu cầu hoàn tiền {refundAmount:N0} VND đang chờ Admin xử lý. Hệ thống đã kích hoạt lại gói Freemium cho gia đình bạn."
                : $"Gói '{subscription.Package.PackageName}' đã bị hủy. Bạn đã sử dụng {timeUsagePercent:F1}% thời gian và {ocrUsagePercent:F1}% lượt OCR — vượt ngưỡng 10%, không đủ điều kiện hoàn tiền. " +
                  "Hệ thống đã kích hoạt lại gói Freemium cho gia đình bạn.";

            await _notificationService.SendNotificationAsync(
                userId: userId,
                title: notifTitle,
                message: notifMessage,
                type: "SUBSCRIPTION_CANCELLED",
                referenceId: subscriptionId
            );

            // 12. Thông báo cho Admin nếu cần hoàn tiền
            if (isEligibleForRefund && refundAmount > 0)
            {
                await _notificationService.SendNotificationToRoleAsync(
                    Roles.Admin,
                    " Yêu cầu hoàn tiền hủy gói mới",
                    $"Gia đình '{subscription.Family?.FamilyName}' vừa hủy gói '{subscription.Package.PackageName}'. " +
                    $"Yêu cầu hoàn tiền {refundAmount:N0} VND đang chờ xử lý.",
                    "Warning"
                );
            }

            string successMessage = isEligibleForRefund
                ? $"Hủy gói thành công. Yêu cầu hoàn tiền {refundAmount:N0} VND đang chờ Admin xử lý."
                : $"Hủy gói thành công. Đã sử dụng {timeUsagePercent:F1}% thời gian / {ocrUsagePercent:F1}% OCR (> 10%) nên không được hoàn tiền.";

            return ApiResponse<bool>.Ok(true, successMessage);
        }
    }
}