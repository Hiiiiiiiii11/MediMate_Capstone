using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Share.Common;
using Share.Constants;

namespace MediMateService.Services.Implementations;

public class PayOSService : IPayOSService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayOSService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    private readonly IActivityLogService _activityLogService;

    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;
    private readonly string _baseUrl;
    private readonly string _defaultReturnUrl;
    private readonly string _defaultCancelUrl;

    public PayOSService(HttpClient httpClient, IConfiguration configuration, ILogger<PayOSService> logger, IUnitOfWork unitOfWork, IActivityLogService activityLogService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;

        _clientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID") ?? _configuration["PayOS:ClientId"] ?? throw new InvalidOperationException("PayOS ClientId not configured");
        _apiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY") ?? _configuration["PayOS:ApiKey"] ?? throw new InvalidOperationException("PayOS ApiKey not configured");
        _checksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY") ?? _configuration["PayOS:ChecksumKey"] ?? throw new InvalidOperationException("PayOS ChecksumKey not configured");
        _baseUrl = Environment.GetEnvironmentVariable("PAYOS_BASE_URL") ?? _configuration["PayOS:BaseUrl"] ?? throw new InvalidOperationException("PayOS BaseUrl not configured");
        _defaultReturnUrl = Environment.GetEnvironmentVariable("PAYOS_RETURN_URL") ?? _configuration["PayOS:ReturnUrl"] ?? throw new InvalidOperationException("PayOS ReturnUrl not configured");
        _defaultCancelUrl = Environment.GetEnvironmentVariable("PAYOS_CANCEL_URL") ?? _configuration["PayOS:CancelUrl"] ?? throw new InvalidOperationException("PayOS CancelUrl not configured");

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _activityLogService = activityLogService;
    }

    public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var package = await _unitOfWork.Repository<MembershipPackages>().GetByIdAsync(request.PackageId);
            if (package == null) throw new InvalidOperationException("Package not found");

            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(request.FamilyId);
            if (family == null) throw new InvalidOperationException("Family not found");

            // Check member limit vs current family members count
            var memberCount = (await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == request.FamilyId && m.IsActive)).Count();
            if (memberCount > package.MemberLimit)
            {
                throw new InvalidOperationException($"Gia đình hiện có {memberCount} thành viên, vượt quá giới hạn {package.MemberLimit} của gói này. Vui lòng chọn gói cao hơn.");
            }
            int orderCode = (int)(DateTime.Now.Ticks % int.MaxValue);
            var subscription = new FamilySubscriptions
            {
                SubscriptionId = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                PackageId = request.PackageId,
                UserId = userId,
                // Dates will be finalized when payment succeeds
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                EndDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                AutoRenew = false
            };
            await _unitOfWork.Repository<FamilySubscriptions>().AddAsync(subscription);

            // Create Pending Payment
            var payment = new Payments
            {
                PaymentId = Guid.NewGuid(),
                SubscriptionId = subscription.SubscriptionId,
                UserId = userId,
                Amount = package.Price,
                PaymentContent = $"Mua goi {package.PackageName}",
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Repository<Payments>().AddAsync(payment);

            // Create Transaction to store OrderCode
            var transaction = new Transactions
            {
                TransactionId = Guid.NewGuid(),
                TransactionCode = $"PKG{orderCode}",
                PaymentId = payment.PaymentId,
                GatewayName = "PayOS",
                GatewayTransactionId = orderCode.ToString(),
                TransactionStatus = "Pending",
                TransactionType = TransactionTypes.MoneyReceived,
                AmountPaid = 0 // Will update on success
            };
            await _unitOfWork.Repository<Transactions>().AddAsync(transaction);

            await _unitOfWork.CompleteAsync();

            var payload = new
            {
                orderCode = orderCode,
                amount = (int)package.Price,
                description = $"Thanh toan #{orderCode}",
                buyerName = request.BuyerName,
                buyerEmail = request.BuyerEmail,
                buyerPhone = request.BuyerPhone,
                items = new[] {
                    new {
                        name = package.PackageName,
                        quantity = 1,
                        price = (int)package.Price,
                        unit = "VND"
                    }
                },
                cancelUrl = request.CancelUrl ?? _defaultCancelUrl,
                returnUrl = request.ReturnUrl ?? _defaultReturnUrl,
                expiredAt = (int?)DateTime.Now.AddMinutes(15).Subtract(DateTime.UnixEpoch).TotalSeconds
            };

            var signature = CreateSignature(payload);
            var requestBody = new
            {
                orderCode = payload.orderCode,
                amount = payload.amount,
                description = payload.description,
                buyerName = payload.buyerName,
                buyerEmail = payload.buyerEmail,
                buyerPhone = payload.buyerPhone,
                items = payload.items,
                cancelUrl = payload.cancelUrl,
                returnUrl = payload.returnUrl,
                expiredAt = payload.expiredAt,
                signature = signature
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating PayOS payment link for OrderCode {OrderCode}", orderCode);

            var response = await _httpClient.PostAsync("/v2/payment-requests", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PayOSApiResponse<PaymentLinkData>>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                if (result?.Data != null)
                {
                    return new PaymentLinkResponse
                    {
                        PaymentUrl = result.Data.CheckoutUrl,
                        OrderCode = orderCode,
                        QrCode = result.Data.QrCode,
                        Message = "Payment link created successfully"
                    };
                }
            }

            _logger.LogError("PayOS Error: {Response}", responseContent);
            throw new Exception($"Failed to create payment link: {responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
            throw;
        }
    }

    public async Task<PaymentLinkResponse> CreateAppointmentPaymentLinkAsync(Guid userId, Payments paymentRecord, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            int orderCode = (int)(DateTime.Now.Ticks % int.MaxValue);

            // Create Transaction to store OrderCode
            var transaction = new Transactions
            {
                TransactionId = Guid.NewGuid(),
                TransactionCode = $"APP{orderCode}",
                PaymentId = paymentRecord.PaymentId,
                GatewayName = "PayOS",
                GatewayTransactionId = orderCode.ToString(),
                TransactionStatus = "Pending",
                TransactionType = TransactionTypes.MoneyReceived,
                AmountPaid = 0 // Will update on success
            };
            await _unitOfWork.Repository<Transactions>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            var payload = new
            {
                orderCode = orderCode,
                amount = (int)paymentRecord.Amount,
                description = $"Thanh toan #{orderCode}",
                buyerName = request.BuyerName,
                buyerEmail = request.BuyerEmail,
                buyerPhone = request.BuyerPhone,
                items = new[] {
                    new {
                        name = "Thanh toán tiền khám bệnh",
                        quantity = 1,
                        price = (int)paymentRecord.Amount,
                        unit = "VND"
                    }
                },
                cancelUrl = request.CancelUrl ?? _defaultCancelUrl,
                returnUrl = request.ReturnUrl ?? _defaultReturnUrl,
                expiredAt = (int?)DateTime.Now.AddMinutes(15).Subtract(DateTime.UnixEpoch).TotalSeconds
            };

            var signature = CreateSignature(payload);
            var requestBody = new
            {
                orderCode = payload.orderCode,
                amount = payload.amount,
                description = payload.description,
                buyerName = payload.buyerName,
                buyerEmail = payload.buyerEmail,
                buyerPhone = payload.buyerPhone,
                items = payload.items,
                cancelUrl = payload.cancelUrl,
                returnUrl = payload.returnUrl,
                expiredAt = payload.expiredAt,
                signature = signature
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating PayOS appointment payment link for OrderCode {OrderCode}", orderCode);

            var response = await _httpClient.PostAsync("/v2/payment-requests", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PayOSApiResponse<PaymentLinkData>>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                if (result?.Data != null)
                {
                    return new PaymentLinkResponse
                    {
                        PaymentUrl = result.Data.CheckoutUrl,
                        OrderCode = orderCode,
                        QrCode = result.Data.QrCode,
                        Message = "Payment link created successfully"
                    };
                }
            }

            _logger.LogError("PayOS Error: {Response}", responseContent);
            throw new Exception($"Failed to create appointment payment link: {responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment payment link");
            throw;
        }
    }

    public async Task<PaymentStatusResponse?> GetPaymentInfoAsync(int orderCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/payment-requests/{orderCode}", cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PayOSApiResponse<PaymentInfoData>>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                if (result?.Data != null)
                {
                    return new PaymentStatusResponse
                    {
                        OrderCode = result.Data.OrderCode,
                        Amount = result.Data.Amount,
                        Description = result.Data.Description,
                        Status = result.Data.Status,
                        CreatedAt = result.Data.CreatedAt,
                        PaidAt = result.Data.PaidAt,
                        TransactionId = result.Data.TransactionId
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment info");
            throw;
        }
    }

    public async Task<bool> ProcessPaymentWebhookAsync(int orderCode, bool isSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing webhook for order {OrderCode}. IsSuccess: {IsSuccess}", orderCode, isSuccess);

            // Tìm giao dịch liên quan (Eager loading các bảng cần thiết)
            var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                .ThenInclude(p => p.Subscription)
                .ThenInclude(s => s.Package)
                .Include(t => t.Payment)
                .ThenInclude(p => p.Appointment)
                .FirstOrDefaultAsync(t => t.GatewayName == "PayOS" && t.GatewayTransactionId == orderCode.ToString(), cancellationToken);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for OrderCode {OrderCode}", orderCode);
                return false;
            }

            // Nếu giao dịch đã được ghi nhận là Thành công trước đó thì bỏ qua (tránh Webhook gọi trùng lặp)
            if (transaction.TransactionStatus == "Success" || transaction.Payment.Status == "Success")
            {
                return true;
            }

            if (isSuccess)
            {
                // ==========================================
                // TRƯỜNG HỢP 1: THANH TOÁN THÀNH CÔNG
                // ==========================================

                // 1. Cập nhật Transaction
                transaction.TransactionStatus = "Success";
                transaction.AmountPaid = transaction.Payment.Amount;
                transaction.PaidAt = DateTime.Now;

                // 2. Cập nhật Payment
                transaction.Payment.Status = "Success";

                // XỬ LÝ THEO LOẠI THANH TOÁN (Gói dịch vụ HOẶC Đặt lịch)
                if (transaction.Payment.SubscriptionId != null && transaction.Payment.Subscription != null)
                {
                    // ==========================================
                    // 3A. Cập nhật Subscription
                    // ==========================================
                    var sub = transaction.Payment.Subscription;

                    // Hủy các gói đang kích hoạt cũ của Gia đình này
                    var oldActiveSubscriptions = await _unitOfWork.Repository<FamilySubscriptions>().GetQueryable()
                        .Where(s => s.FamilyId == sub.FamilyId && s.Status == "Active" && s.SubscriptionId != sub.SubscriptionId)
                        .ToListAsync(cancellationToken);

                    foreach (var oldSub in oldActiveSubscriptions)
                    {
                        oldSub.Status = "Inactive";
                    }

                    // Kích hoạt gói mới và thiết lập ngày tháng, số lượt
                    sub.Status = "Active";
                    sub.StartDate = DateOnly.FromDateTime(DateTime.Now);
                    sub.EndDate = sub.StartDate.AddDays(sub.Package.DurationDays);
                    sub.RemainingOcrCount = sub.Package.OcrLimit;

                    _unitOfWork.Repository<FamilySubscriptions>().UpdateRange(oldActiveSubscriptions);

                    var member = await _unitOfWork.Repository<Members>().GetQueryable()
                        .FirstOrDefaultAsync(m => m.UserId == transaction.Payment.UserId && m.FamilyId == sub.FamilyId, cancellationToken);

                    await _activityLogService.LogActivityAsync(
                        familyId: sub.FamilyId,
                        memberId: member?.MemberId ?? Guid.Empty,
                        actionType: ActivityActionTypes.UPDATE,
                        entityName: ActivityEntityNames.FAMILY,
                        entityId: sub.FamilyId,
                        description: $"Gia đình đã nâng cấp thành công lên gói '{sub.Package.PackageName}'."
                    );
                }
                else if (transaction.Payment.AppointmentId != null && transaction.Payment.Appointment != null)
                {
                    // ==========================================
                    // 3B. Cập nhật Appointment
                    // ==========================================
                    var appointment = transaction.Payment.Appointment;
                    appointment.PaymentStatus = "Paid";
                    // Chuyển sang trạng thái Approved ngay sau khi thanh toán thành công
                    appointment.Status = AppointmentConstants.APPROVED;
                    
                    _unitOfWork.Repository<Appointments>().Update(appointment);

                    // Khởi tạo dòng tiền chờ thanh toán cho Phòng khám
                    var doctorPayout = new DoctorPayout
                    {
                        PayoutId = Guid.NewGuid(),
                        AppointmentId = appointment.AppointmentId,
                        ClinicId = appointment.ClinicId ?? Guid.Empty,
                        Amount = transaction.Payment.Amount,
                        Status = "Hold", // Tạm giữ chờ đến khi khám xong
                        CalculatedAt = DateTime.Now
                    };
                    await _unitOfWork.Repository<DoctorPayout>().AddAsync(doctorPayout);

                    var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
                    if (member?.FamilyId != null)
                    {
                        await _activityLogService.LogActivityAsync(
                            familyId: member.FamilyId.Value,
                            memberId: member.MemberId,
                            actionType: ActivityActionTypes.UPDATE,
                            entityName: "Appointment",
                            entityId: appointment.AppointmentId,
                            description: $"Đã thanh toán thành công tiền khám bệnh."
                        );
                    }
                }
            }
            else
            {
                // ==========================================
                // TRƯỜNG HỢP 2: THANH TOÁN THẤT BẠI / HỦY BỎ
                // ==========================================

                transaction.TransactionStatus = "Failed";
                transaction.PaidAt = DateTime.Now; // Ghi nhận thời điểm báo lỗi

                transaction.Payment.Status = "Failed";

                // Hủy bỏ gói Subscription hoặc Lịch khám (chưa thanh toán)
                if (transaction.Payment.SubscriptionId != null && transaction.Payment.Subscription != null)
                {
                    transaction.Payment.Subscription.Status = "Failed";
                }
                else if (transaction.Payment.AppointmentId != null && transaction.Payment.Appointment != null)
                {
                    transaction.Payment.Appointment.Status = AppointmentConstants.CANCELLED;
                    transaction.Payment.Appointment.PaymentStatus = "Cancelled";
                }
            }

            // Lưu toàn bộ thay đổi xuống DB
            _unitOfWork.Repository<Transactions>().Update(transaction);
            await _unitOfWork.CompleteAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook for order {OrderCode}", orderCode);
            return false;
        }
    }

    public Task<bool> VerifyWebhookSignatureAsync(string signature, string data, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookBody = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data);
            if (webhookBody == null || !webhookBody.ContainsKey("data")) return Task.FromResult(false);

            var dataObject = webhookBody["data"];
            var expectedSignature = CreateWebhookSignatureFromData(dataObject);
            return Task.FromResult(signature == expectedSignature);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string CreateSignature(object payload)
    {
        var properties = payload.GetType().GetProperties();
        var signatureData = new Dictionary<string, string>();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(payload);
            if (value != null)
            {
                var key = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
                if (key == "amount" || key == "cancelUrl" || key == "description" || key == "orderCode" || key == "returnUrl")
                {
                    string valueStr = value switch
                    {
                        DateTime dt => ((long)dt.Subtract(DateTime.UnixEpoch).TotalSeconds).ToString(),
                        double d => ((long)d).ToString(),
                        _ => value.ToString() ?? ""
                    };
                    signatureData[key] = valueStr;
                }
            }
        }

        var sortedData = signatureData.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        var queryString = string.Join("&", sortedData.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return Convert.ToHexString(hash).ToLower();
    }

    private string CreateWebhookSignatureFromData(JsonElement dataElement)
    {
        try
        {
            var sortedData = new SortedDictionary<string, string>();
            foreach (var property in dataElement.EnumerateObject())
            {
                string valueStr = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? "",
                    JsonValueKind.Number => property.Value.GetInt64().ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "",
                    _ => property.Value.GetRawText()
                };
                sortedData[property.Name] = valueStr;
            }

            var queryString = string.Join("&", sortedData.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            return Convert.ToHexString(hash).ToLower();
        }
        catch
        {
            return "";
        }
    }

    public async Task<ApiResponse<PagedResult<PaymentItemDto>>> GetAllPaymentsAsync(PaymentFilterDto filter)
    {
        // Đã bổ sung Include Transactions ở đây
        IQueryable<Payments> query = _unitOfWork.Repository<Payments>().GetQueryable()
            .Include(p => p.User)
            .Include(p => p.Transactions);

        query = ApplyFiltersAndSorting(query, filter);

        var totalCount = await query.CountAsync();

        var payments = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = payments.Select(p => new PaymentItemDto
        {
            PaymentId = p.PaymentId,
            UserId = p.UserId,
            UserName = p.User?.FullName ?? "Unknown",

            // Đã bổ sung map OrderCode ở đây
            OrderCode = p.Transactions != null && p.Transactions.Any()
                ? long.Parse(p.Transactions.First().GatewayTransactionId ?? "0")
                : 0,

            Amount = p.Amount,
            PaymentContent = p.PaymentContent ?? "",
            Status = p.Status,
            CreatedAt = p.CreatedAt
        }).ToList();

        var result = new PagedResult<PaymentItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return ApiResponse<PagedResult<PaymentItemDto>>.Ok(result, "Lấy danh sách thanh toán thành công.");
    }

    // ==========================================
    // 2. LẤY PAYMENTS CỦA 1 USER (CHO APP)
    // ==========================================
    public async Task<ApiResponse<PagedResult<PaymentItemDto>>> GetPaymentsByUserIdAsync(Guid userId, PaymentFilterDto filter)
    {
        IQueryable<Payments> query = _unitOfWork.Repository<Payments>().GetQueryable()
            .Include(p => p.User)
            .Include(p => p.Transactions)
            .Include(p => p.Subscription).ThenInclude(s => s!.Family)
            .Include(p => p.Subscription).ThenInclude(s => s!.Package)
            .Where(p => p.UserId == userId);

        query = ApplyFiltersAndSorting(query, filter);
        var totalCount = await query.CountAsync();
        var payments = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

        var items = payments.Select(p => new PaymentItemDto
        {
            PaymentId = p.PaymentId,
            UserId = p.UserId,
            UserName = p.User?.FullName ?? "Unknown",
            OrderCode = p.Transactions != null && p.Transactions.Any()
                        ? long.Parse(p.Transactions.First().GatewayTransactionId ?? "0") : 0,
            Amount = p.Amount,
            PaymentContent = p.PaymentContent ?? "",
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            // --- DATA BỔ SUNG ---
            PackageName = p.Subscription?.Package?.PackageName ?? "N/A",
            FamilyName = p.Subscription?.Family?.FamilyName ?? "N/A",
            FamilyId = p.Subscription?.FamilyId ?? Guid.Empty
        }).ToList();

        return ApiResponse<PagedResult<PaymentItemDto>>.Ok(new PagedResult<PaymentItemDto> { Items = items, TotalCount = totalCount, PageNumber = filter.PageNumber, PageSize = filter.PageSize });
    }

    // ==========================================
    // 3. HÀM HELPER: XỬ LÝ LỌC & SẮP XẾP CHUNG
    // ==========================================
    private IQueryable<Payments> ApplyFiltersAndSorting(IQueryable<Payments> query, PaymentFilterDto filter)
    {
        // Lọc theo từ khóa (Tìm trong Nội dung thanh toán hoặc Tên người dùng)
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(p =>
                (p.PaymentContent != null && p.PaymentContent.ToLower().Contains(term)) ||
                (p.User != null && p.User.FullName.ToLower().Contains(term))
            );
        }

        // Lọc theo Trạng thái (Pending, Success...)
        if (!string.IsNullOrEmpty(filter.Status))
        {
            var status = filter.Status.ToLower();
            query = query.Where(p => p.Status.ToLower() == status);
        }

        // Sắp xếp
        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            var sortBy = filter.SortBy.ToLower();
            switch (sortBy)
            {
                case "amount":
                    query = filter.IsDescending
                        ? query.OrderByDescending(p => p.Amount)
                        : query.OrderBy(p => p.Amount);
                    break;
                case "createdat":
                default:
                    query = filter.IsDescending
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt);
                    break;
            }
        }
        else
        {
            // Mặc định luôn sắp xếp mới nhất lên đầu
            query = query.OrderByDescending(p => p.CreatedAt);
        }

        return query;
    }

    // ==========================================
    // CẬP NHẬT TRẠNG THÁI TỪ FRONTEND (THÀNH CÔNG / THẤT BẠI / HỦY)
    // ==========================================
    public async Task<ApiResponse<bool>> UpdatePaymentStatusAsync(int orderCode, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manually updating status for OrderCode {OrderCode} to {Status}", orderCode, status);

            var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                .ThenInclude(p => p.Subscription)
                .FirstOrDefaultAsync(t => t.GatewayName == "PayOS" && t.GatewayTransactionId == orderCode.ToString(), cancellationToken);

            if (transaction == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy giao dịch hợp lệ.", 404);
            }

            // Chặn: Không cho phép cập nhật lùi từ Success về Failed/Cancelled
            if (transaction.TransactionStatus == "Success" || transaction.Payment.Status == "Success")
            {
                return ApiResponse<bool>.Ok(true, "Giao dịch này đã được xử lý thành công trước đó.");
            }

            // XỬ LÝ THEO TRẠNG THÁI CHUẨN ĐÃ ĐƯỢC CONTROLLER VALIDATE
            if (status == "SUCCESS")
            {
                bool isProcessed = await ProcessPaymentWebhookAsync(orderCode, true, cancellationToken);
                return isProcessed
                    ? ApiResponse<bool>.Ok(true, "Đã cập nhật trạng thái Thành công.")
                    : ApiResponse<bool>.Fail("Có lỗi khi xử lý dữ liệu kích hoạt gói.", 500);
            }
            else
            {
                // Quy chuẩn hóa format lưu vào DB: "CANCELED" / "CANCELLED" -> "Cancelled", "FAILED" -> "Failed"
                var finalStatus = (status == "CANCELED" || status == "CANCELLED") ? "Cancelled" : "Failed";

                transaction.TransactionStatus = finalStatus;
                transaction.PaidAt = DateTime.Now;
                transaction.Payment.Status = finalStatus;

                if (transaction.Payment.Subscription != null)
                {
                    transaction.Payment.Subscription.Status = finalStatus;
                }

                _unitOfWork.Repository<Transactions>().Update(transaction);
                await _unitOfWork.CompleteAsync();

                return ApiResponse<bool>.Ok(true, $"Đã cập nhật trạng thái thành {finalStatus}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually updating payment status for order {OrderCode}", orderCode);
            return ApiResponse<bool>.Fail($"Lỗi hệ thống: {ex.Message}", 500);
        }
    }
}

