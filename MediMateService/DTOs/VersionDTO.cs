using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class VersionDto
    {
        public Guid VersionId { get; set; }
        public string VersionNumber { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        public bool IsForceUpdate { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateVersionDto
    {
        public string VersionNumber { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        public bool IsForceUpdate { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class UpdateVersionDto
    {
        public string? VersionNumber { get; set; }
        public string? Platform { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        public bool? IsForceUpdate { get; set; }
        public string? Status { get; set; }
    }
}
