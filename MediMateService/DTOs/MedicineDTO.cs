using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class AddMedicineRequest
    {
        public string MedicineName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Unit { get; set; }
        public int Quantity { get; set; }
        public string? Instructions { get; set; }
    }

    public class UpdateMedicineRequest
    {
        public string? MedicineName { get; set; }
        public string? Dosage { get; set; }
        public string? Unit { get; set; }
        public int? Quantity { get; set; } // Đổi thành int?
        public string? Instructions { get; set; }
    }
}
