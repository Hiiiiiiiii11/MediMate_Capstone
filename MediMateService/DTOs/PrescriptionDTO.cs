namespace MediMateService.DTOs
{
    // Request tạo đơn thuốc (Dữ liệu đã được UI/AI xử lý)
    public class CreatePrescriptionRequest
    {
        public string? PrescriptionCode { get; set; }
        public string? DoctorName { get; set; }
        public string? HospitalName { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string? Notes { get; set; }
        public string? Diagnosis { get; set; } // Chẩn đoán bệnh

        public List<PrescriptionImageDto> Images { get; set; } = new List<PrescriptionImageDto>();
        public List<PrescriptionMedicineResponse> Medicines { get; set; } = new List<PrescriptionMedicineResponse>();
    }

    public class PrescriptionImageDto
    {
        public string ImageUrl { get; set; }     // Link gốc (nhận từ API upload trên)
        public string? ThumbnailUrl { get; set; } // Link thumbnail (nhận từ API upload trên)
        public string? OcrRawData { get; set; }
    }
    public class FileUploadResult
    {
        public string OriginalUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class PrescriptionMedicineResponse
    {
        public Guid? PrescriptionMedicineId { get; set; }
        public string MedicineName { get; set; }
        public string? Dosage { get; set; } // Liều lượng (vd: 500mg)
        public string? Unit { get; set; }   // Đơn vị (vd: Viên)
        public int Quantity { get; set; }
        public string? Instructions { get; set; } // HDSD (Sáng 1, Chiều 1)
    }
    public class UpdatePrescriptionRequest
    {
        public string? PrescriptionCode { get; set; }
        public string? DoctorName { get; set; }
        public string? HospitalName { get; set; }
        public DateTime? PrescriptionDate { get; set; }
        public string? Notes { get; set; }
        public string? Diagnosis { get; set; }
        public string? Status { get; set; }

        public List<PrescriptionMedicineResponse>? Medicines { get; set; }
    }

    // Response trả về cho UI
    public class PrescriptionResponse
    {
        public Guid PrescriptionId { get; set; }
        public string PrescriptionCode { get; set; }
        public Guid MemberId { get; set; }
        public string DoctorName { get; set; }
        public string HospitalName { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string? Diagnosis { get; set; }

        public List<PrescriptionImageDto> Images { get; set; }
        public List<PrescriptionMedicineResponse> Medicines { get; set; }
    }

    
    public class OcrScanResponse
    {
        public string ImageUrl { get; set; } = string.Empty;       
        public string ThumbnailUrl { get; set; } = string.Empty;   
        public string RawText { get; set; } = string.Empty;        
        public ExtractedPrescriptionData ExtractedData { get; set; } = new();
    }


    public class ExtractedPrescriptionData
    {
        public string? DoctorName { get; set; }
        public string? HospitalName { get; set; }
        public string? PrescriptionCode { get; set; }
        public string? PrescriptionDate { get; set; }
        public string? Notes { get; set; }
        public string? Diagnosis { get; set; } // Chẩn đoán bệnh (từ OCR)
        public List<PrescriptionMedicineResponse> Medicines { get; set; } = new();
    }
    public class CreateEmptyPrescriptionRequest
    {
        public string? HospitalName { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? PrescriptionDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? Notes { get; set; }
    }
}