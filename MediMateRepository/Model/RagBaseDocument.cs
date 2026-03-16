using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class RagBaseDocument
    {
        public Guid RagDocumentId { get; set; }
        public string DocName { get; set; }
        public string FilePath { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public int FileSize { get; set; }
        public string CheckSum { get; set; }
        public DateTime CreateAt { get; set; }
        public Guid CollectionId { get; set; }
        public virtual RagBaseCollection RagBaseCollection { get; set; }
    }
}
