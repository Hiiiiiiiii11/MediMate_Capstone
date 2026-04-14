using System;
using System.Linq;
using System.Threading.Tasks;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MediMateService.Services.Implementations
{
    public class MedicationStatusJobService : IMedicationStatusJobService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MedicationStatusJobService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CheckAndUpdateExpiredStatusAsync()
        {
            var today = DateTime.Now.Date;

            // ─── BƯỚC 1: Tắt MedicationSchedule đã hết thuốc ───
            // Lấy tất cả Schedule đang active kèm ScheduleDetails
            var activeSchedules = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.IsActive, includeProperties: "ScheduleDetails");

            int schedulesDeactivated = 0;
            foreach (var schedule in activeSchedules)
            {
                // Schedule hết hạn khi KHÔNG còn detail nào có EndDate >= hôm nay
                bool hasActiveDetail = schedule.ScheduleDetails.Any(d => d.EndDate >= today);
                if (!hasActiveDetail && schedule.ScheduleDetails.Any())
                {
                    schedule.IsActive = false;
                    _unitOfWork.Repository<MedicationSchedules>().Update(schedule);
                    schedulesDeactivated++;
                }
            }

            if (schedulesDeactivated > 0)
                await _unitOfWork.CompleteAsync();

            // ─── BƯỚC 2: Chuyển Prescription → "Completed" nếu tất cả thuốc hết lịch ───
            var activePrescriptions = await _unitOfWork.Repository<Prescriptions>()
                .FindAsync(
                    p => p.Status == "Active",
                    includeProperties: "PrescriptionMedicines"
                );

            int prescriptionsCompleted = 0;
            foreach (var prescription in activePrescriptions)
            {
                if (!prescription.PrescriptionMedicines.Any()) continue;

                // Lấy toàn bộ ScheduleDetails của đơn thuốc này
                var medicineIds = prescription.PrescriptionMedicines
                    .Select(m => m.PrescriptionMedicineId)
                    .ToList();

                var details = await _unitOfWork.Repository<MedicationScheduleDetails>()
                    .FindAsync(d => medicineIds.Contains(d.PrescriptionMedicineId));

                // Nếu không có detail nào còn active → đơn thuốc hoàn thành
                bool allExpired = !details.Any(d => d.EndDate >= today);

                if (allExpired && details.Any())
                {
                    prescription.Status = "Completed";
                    prescription.UpdateAt = DateTime.Now;
                    _unitOfWork.Repository<Prescriptions>().Update(prescription);
                    prescriptionsCompleted++;
                }
            }

            if (prescriptionsCompleted > 0)
                await _unitOfWork.CompleteAsync();

            Console.WriteLine($"[MedicationStatusJob] {DateTime.Now:HH:mm:ss} | " +
                              $"Schedules deactivated: {schedulesDeactivated} | " +
                              $"Prescriptions completed: {prescriptionsCompleted}");
        }
    }
}
