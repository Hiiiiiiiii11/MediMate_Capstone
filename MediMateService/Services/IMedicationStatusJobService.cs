namespace MediMateService.Services
{
    public interface IMedicationStatusJobService
    {
        /// <summary>
        /// Chạy hàng ngày lúc nửa đêm:
        /// 1. Tắt MedicationSchedule nếu tất cả thuốc bên trong đã hết hạn (EndDate < hôm nay)
        /// 2. Chuyển Prescription sang "Completed" nếu tất cả thuốc trong đơn đã hết lịch
        /// </summary>
        Task CheckAndUpdateExpiredStatusAsync();
    }
}
