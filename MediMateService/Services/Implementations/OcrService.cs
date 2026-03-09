using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MediMateService.Services.Implementations
{
    public class OcrService : IOcrService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OcrService> _logger;

        public OcrService(
            Cloudinary cloudinary,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<OcrService> logger)
        {
            _cloudinary = cloudinary;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<OcrScanResponse> ScanPrescriptionAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File ảnh không hợp lệ hoặc rỗng.");

           
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            _logger.LogInformation("[OCR] Bắt đầu chạy song song: Cloudinary Upload + Google Vision");

           
            var cloudinaryTask = UploadToCloudinaryAsync(imageBytes, file.FileName);
            var visionTask = CallGoogleVisionAsync(imageBytes);

            await Task.WhenAll(cloudinaryTask, visionTask);

            var uploadResult = await cloudinaryTask;
            var rawText = await visionTask;

            _logger.LogInformation("[OCR] Cả 2 luồng hoàn thành. Raw text length: {Len}", rawText.Length);

      
            // ===== BƯỚC 3: Dùng Groq LLM bóc tách raw text → JSON =====
            var extractedData = await CallGroqExtractAsync(rawText);

            return new OcrScanResponse
            {
                ImageUrl = uploadResult.OriginalUrl,
                ThumbnailUrl = uploadResult.ThumbnailUrl,
                RawText = rawText,
                ExtractedData = extractedData
            };
        }

      
        private async Task<FileUploadResult> UploadToCloudinaryAsync(byte[] imageBytes, string fileName)
        {
            _logger.LogInformation("[OCR][Luồng 1] Bắt đầu upload Cloudinary...");

            using var stream = new MemoryStream(imageBytes);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "prescriptions",
                Transformation = new Transformation().Width(2000).Crop("limit")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary lỗi: {result.Error.Message}");

            var thumbnailUrl = _cloudinary.Api.UrlImgUp
                .Transform(new Transformation()
                    .Width(300).Height(300).Crop("fill").Gravity("auto")
                    .Quality("auto").FetchFormat("auto"))
                .BuildUrl(result.PublicId);

            _logger.LogInformation("[OCR][Luồng 1] Cloudinary upload xong: {Url}", result.SecureUrl);

            return new FileUploadResult
            {
                OriginalUrl = result.SecureUrl.AbsoluteUri,
                ThumbnailUrl = thumbnailUrl
            };
        }

      
        private async Task<string> CallGoogleVisionAsync(byte[] imageBytes)
        {
            _logger.LogInformation("[OCR][Luồng 2] Bắt đầu gọi Google Vision API...");

            var apiKey = _configuration["GoogleVision:ApiKey"]
                         ?? _configuration["GoogleVision__ApiKey"]
                         ?? throw new InvalidOperationException("Thiếu GoogleVision:ApiKey trong config.");

            var base64Image = Convert.ToBase64String(imageBytes);

           
            var requestBody = new
            {
                requests = new[]
                {
                    new
                    {
                        image = new { content = base64Image },
                        features = new[]
                        {
                            new { type = "DOCUMENT_TEXT_DETECTION" }
                        },
                        imageContext = new
                        {
                            languageHints = new[] { "vi", "en" }  
                        }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();
            var url = $"https://vision.googleapis.com/v1/images:annotate?key={apiKey}";

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(url, jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Google Vision lỗi {response.StatusCode}: {responseBody}");

            
            var doc = JsonDocument.Parse(responseBody);
            var rawText = doc.RootElement
                .GetProperty("responses")[0]
                .TryGetProperty("fullTextAnnotation", out var annotation)
                    ? annotation.GetProperty("text").GetString() ?? string.Empty
                    : string.Empty;

            _logger.LogInformation("[OCR][Luồng 2] Vision xong. Đọc được {Chars} ký tự.", rawText.Length);
            return rawText;
        }

      
        private async Task<ExtractedPrescriptionData> CallGroqExtractAsync(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("[OCR][Groq] Raw text rỗng, bỏ qua bước AI bóc tách.");
                return new ExtractedPrescriptionData();
            }

            _logger.LogInformation("[OCR][Groq] Gọi Groq để bóc tách đơn thuốc...");

            var apiKey = _configuration["GroqSettings:ApiKey"]
                         ?? _configuration["GroqSettings__ApiKey"]
                         ?? throw new InvalidOperationException("Thiếu GroqSettings:ApiKey trong config.");

            // llama-3.3-70b-versatile: hiểu tiếng Việt tốt, miễn phí, cực nhanh trên Groq
            var model = _configuration["GroqSettings:Model"] ?? "llama-3.3-70b-versatile";

            var prompt = $$"""
                Bạn là AI bóc tách đơn thuốc Việt Nam. Đọc đoạn text sau và trả về JSON với cấu trúc:
                {
                  "DoctorName": "...",
                  "HospitalName": "...",
                  "PrescriptionCode": "...",
                  "PrescriptionDate": "DD/MM/YYYY",
                  "Notes": "...",
                  "Medicines": [
                    {
                      "MedicineName": "...",
                      "Dosage": "...",
                      "Unit": "...",
                      "Quantity": 0,
                      "Instructions": "..."
                    }
                  ]
                }
                Quy tắc:
                - Nếu không tìm thấy thông tin → để null (string) hoặc 0 (số) hoặc [] (mảng).
                - Dosage: liều lượng vd "500mg". Unit: đơn vị vd "Viên", "Gói". Instructions: hướng dẫn uống vd "Sáng 1 viên, Tối 1 viên".
                - Quantity: số lượng tổng cộng (số nguyên).
                - CHỈ trả về JSON thuần, KHÔNG giải thích, KHÔNG markdown code block.

                TEXT:
                {{rawText}}
                """;

            // Groq dùng OpenAI-compatible Chat Completions API
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 2048
            };

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[OCR][Groq] Groq lỗi {Status}: {Body}", response.StatusCode, responseBody);
                return new ExtractedPrescriptionData();
            }

            // Parse response theo OpenAI format: choices[0].message.content
            var groqDoc = JsonDocument.Parse(responseBody);
            var groqText = groqDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            groqText = groqText.Trim();

            // Strip markdown code block nếu model vẫn bọc ```json ... ```
            if (groqText.StartsWith("```"))
            {
                var firstNewline = groqText.IndexOf('\n');
                var lastBacktick = groqText.LastIndexOf("```");
                if (firstNewline >= 0 && lastBacktick > firstNewline)
                    groqText = groqText[(firstNewline + 1)..lastBacktick].Trim();
            }

            _logger.LogInformation("[OCR][Groq] Groq trả về JSON: {Json}",
                groqText[..Math.Min(200, groqText.Length)]);

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var extracted = JsonSerializer.Deserialize<ExtractedPrescriptionData>(groqText, options);
                return extracted ?? new ExtractedPrescriptionData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OCR][Groq] Parse JSON thất bại. Raw output: {Raw}", groqText);
                return new ExtractedPrescriptionData();
            }
        }
    }
}
