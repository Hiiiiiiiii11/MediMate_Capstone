namespace MediMateService.DTOs
{
    public class AgoraRecordingResultDto
    {
        public bool Success { get; set; }
        public string? RecordingUrl { get; set; }   // URL trên Cloudinary
        public int DurationSeconds { get; set; }    // Thời lượng ghi hình
        public string? ErrorMessage { get; set; }
    }

    // Agora Cloud Recording REST API response shapes
    public class AgoraAcquireResponse
    {
        public string? ResourceId { get; set; }
    }

    public class AgoraStartRecordingResponse
    {
        public string? ResourceId { get; set; }
        public string? Sid { get; set; }
    }

    public class AgoraStopRecordingResponse
    {
        public string? ResourceId { get; set; }
        public string? Sid { get; set; }
        public AgoraStopPayload? ServerResponse { get; set; }
    }

    public class AgoraStopPayload
    {
        public List<AgoraFileList>? FileList { get; set; }
        public int? UploadingStatus { get; set; }
    }

    public class AgoraFileList
    {
        public string? FileName { get; set; }
        public bool? IsPlayable { get; set; }
        public long? SliceStartTime { get; set; }
    }
}
