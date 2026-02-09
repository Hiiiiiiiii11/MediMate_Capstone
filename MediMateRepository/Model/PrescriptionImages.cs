using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public  class PrescriptionImages
    {
        public Guid ImageId { get; set; }
        public Guid PrescriptionId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string OcrRawData { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }
        public DateTime UploadedAt { get; set; }
        public virtual Prescriptions Prescription { get; set; }
    }
}
