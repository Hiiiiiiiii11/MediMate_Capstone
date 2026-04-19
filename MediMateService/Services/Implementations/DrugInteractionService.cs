using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;

namespace MediMateService.Services.Implementations
{
    public class DrugInteractionService : IDrugInteractionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DrugInteractionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DrugInteractionCheckResult> CheckInteractionAsync(Guid memberId, IEnumerable<string> newDrugNames)
        {
            var result = new DrugInteractionCheckResult();
            var newDrugList = newDrugNames
    .Where(n => !string.IsNullOrWhiteSpace(n))
    .Select(n => n.Trim()) // Thêm bước Trim() ở đây
    .ToList();

            if (!newDrugList.Any())
                return result;

            // ─── Bước 1: Lấy danh sách tên thuốc từ đơn thuốc Active của member ───
            // Không dùng EndDate (dễ bị expired với test data)
            // Thay vào đó lấy thẳng từ PrescriptionMedicines qua Prescriptions Active
            var activePrescriptions = await _unitOfWork.Repository<Prescriptions>()
                .FindAsync(
                    p => p.MemberId == memberId && p.Status == "Active",
                    includeProperties: "PrescriptionMedicines"
                );

            var currentDrugNames = activePrescriptions
                .SelectMany(p => p.PrescriptionMedicines)
                .Select(m => m.MedicineName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Đưa thêm các thuốc mới vào mảng để check tương tác chéo (chính các thuốc mới mâu thuẫn với nhau)
            currentDrugNames.AddRange(newDrugList);
            currentDrugNames = currentDrugNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (!currentDrugNames.Any())
                return result;

            // ─── Bước 2: Với mỗi thuốc mới, tìm Drug record và tra tương tác ───
            foreach (var newDrug in newDrugList)
            {
                var newDrugLower = newDrug.ToLower();

                // Tìm trong bảng Drugs theo tên (EF Core dịch ra LIKE)
                var matchedDrugs = await _unitOfWork.Repository<Drug>()
                    .FindAsync(d => d.Name.ToLower().Contains(newDrugLower));

                if (!matchedDrugs.Any()) continue;

                var matchedDrugIds = matchedDrugs.Select(d => d.DrugId).ToList();

                // Lấy toàn bộ interactions của các thuốc match được
                var interactions = await _unitOfWork.Repository<DrugInteraction>()
                    .FindAsync(i => matchedDrugIds.Contains(i.DrugId));

                // ─── Bước 3: So sánh InteractingDrugName với danh sách thuốc đang có (client-side) ───
                foreach (var interaction in interactions)
                {
                    if (string.IsNullOrWhiteSpace(interaction.InteractingDrugName)) continue;

                    var interactingLower = interaction.InteractingDrugName.ToLower();

                    var conflict = currentDrugNames.FirstOrDefault(current =>
                    {
                        var currentLower = current.ToLower();
                        // Loại bỏ thuốc chính nó khỏi danh sách check
                        if (currentLower.Contains(newDrugLower) || newDrugLower.Contains(currentLower))
                            return false;
                        return currentLower.Contains(interactingLower) || interactingLower.Contains(currentLower);
                    });

                    if (conflict != null)
                    {
                        result.HasInteraction = true;
                        result.Conflicts.Add(new DrugInteractionConflict
                        {
                            NewDrugName = newDrug,
                            ConflictingDrugName = conflict,
                            Description = interaction.Description
                        });
                    }
                }
            }

            // ─── Bước 4: Dedup conflicts (DrugBank có nhiều entry trùng tên) ───
            result.Conflicts = result.Conflicts
                .GroupBy(c => new { c.NewDrugName, c.ConflictingDrugName })
                .Select(g => g.First())
                .ToList();

            return result;
        }
    }
}
