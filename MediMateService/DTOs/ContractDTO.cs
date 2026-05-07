using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class DoctorContractResponse
    {
        public Guid ContractId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public string? Note { get; set; }
    }

    public class CreateContractRequest
    {
        public IFormFile File { get; set; } // Bắt buộc khi tạo mới
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateContractRequest
    {
        public IFormFile? File { get; set; } // Cho phép null/empty để chỉ cập nhật metadata
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
    }
}
