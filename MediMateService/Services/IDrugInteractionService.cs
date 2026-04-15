using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IDrugInteractionService
    {
        /// <summary>
        /// Kiểm tra xem danh sách thuốc mới có tương tác với thuốc member đang uống không.
        /// </summary>
        Task<DrugInteractionCheckResult> CheckInteractionAsync(Guid memberId, IEnumerable<string> newDrugNames);
    }
}
