using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IPayoutService
    {
        /// <summary>Admin xem danh sách tất cả các khoản công nợ (có thể filter theo clinic/status).</summary>
        Task<PagedResult<PayoutItemDto>> GetPayoutsAsync(PayoutFilterDto filter);

        /// <summary>Tổng hợp công nợ ReadyToPay theo từng phòng khám — dùng cho trang Dashboard Admin.</summary>
        Task<IReadOnlyList<PayoutSummaryDto>> GetPayoutSummaryByClinicAsync();

        /// <summary>Admin xác nhận đã chuyển tiền cho phòng khám — batch update các Payout ReadyToPay của clinic đó sang Paid.</summary>
        Task<int> ProcessClinicPayoutAsync(Guid clinicId, ProcessPayoutDto dto);
    }
}
