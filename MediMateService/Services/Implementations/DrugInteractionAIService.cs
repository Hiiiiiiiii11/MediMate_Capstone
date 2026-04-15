using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediMateService.Services.Implementations
{
    public class DrugInteractionAIService : IDrugInteractionAIService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DrugInteractionAIService> _logger;

        // ─── REGEX SHIELD: chặn AI đưa lời khuyên liều lượng / thay thuốc ───
        private static readonly Regex[] _dangerousPatterns = new[]
        {
            new Regex(@"(uống|dùng|tiêm|bôi)\s+\d+\s*(mg|mcg|ml|viên|gói|ống|đơn vị)", RegexOptions.IgnoreCase),
            new Regex(@"(tăng|giảm|điều chỉnh|thay đổi)\s+(liều|lượng|liều lượng)", RegexOptions.IgnoreCase),
            new Regex(@"(thay|đổi|ngừng|dừng)\s+(thuốc|dùng thuốc|sử dụng)", RegexOptions.IgnoreCase),
            new Regex(@"(có thể|nên|hãy|cần)\s+(uống|dùng|tiêm|thay thế)", RegexOptions.IgnoreCase),
            new Regex(@"liều\s+(an toàn|khuyến cáo|tối đa|tối thiểu)", RegexOptions.IgnoreCase),
        };


        public DrugInteractionAIService(
            IUnitOfWork unitOfWork,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DrugInteractionAIService> logger)
        {
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> ExplainInteractionAsync(DrugInteractionExplainRequest request)
        {
            // ─── Bước 1: Lấy thêm context từ DB ───
            string diagnosisContext = string.Empty;
            string currentMedsContext = string.Empty;

            if (Guid.TryParse(request.PrescriptionId, out var prescriptionId))
            {
                var prescription = (await _unitOfWork.Repository<Prescriptions>()
                    .FindAsync(p => p.PrescriptionId == prescriptionId, "PrescriptionMedicines"))
                    .FirstOrDefault();

                if (prescription != null)
                {
                    if (!string.IsNullOrWhiteSpace(prescription.Diagnosis))
                        diagnosisContext = $"Chẩn đoán bệnh nhân: {prescription.Diagnosis}";

                    var medNames = prescription.PrescriptionMedicines
                        .Select(m => $"- {m.MedicineName} ({m.Dosage})");
                    currentMedsContext = "Thuốc hiện đang dùng trong đơn:\n" + string.Join("\n", medNames);
                }
            }

            // ─── Bước 2: Xây dựng RAG Context chi tiết từ DB interactions ───
            var interactionDetails = request.Conflicts
                .Select(c => $"• {c.NewDrugName} ↔ {c.ConflictingDrugName}: {c.Description}")
                .ToList();

            var ragContext = string.Join("\n", interactionDetails);

            // ─── Bước 3: Xây prompt ───
            var systemPrompt =
                "Bạn là trợ lý y tế của ứng dụng MediMate. Nhiệm vụ của bạn là GIẢI THÍCH tương tác thuốc " +
                "một cách dễ hiểu cho người dùng phổ thông bằng tiếng Việt. " +
                "TUYỆT ĐỐI KHÔNG đưa ra lời khuyên thay đổi liều lượng, thay thuốc, hoặc ngừng dùng thuốc. " +
                "Chỉ giải thích TẠI SAO có tương tác và HẬU QUẢ có thể xảy ra.";

            var userPrompt = $@"
{diagnosisContext}
{currentMedsContext}

Thuốc mới cần thêm: {request.NewDrugName}

Dữ liệu tương tác từ cơ sở dữ liệu DrugBank:
{ragContext}

Hãy giải thích ngắn gọn (3-5 câu) cho bệnh nhân hiểu tại sao {request.NewDrugName} có thể gây nguy hiểm khi dùng cùng các thuốc hiện tại, dựa trên chẩn đoán bệnh và thông tin tương tác ở trên.
Không đưa lời khuyên thay đổi thuốc hay liều lượng.";

            // ─── Bước 4: Gọi Groq API ───
            var aiResponse = await CallGroqAsync(systemPrompt, userPrompt);

            // ─── Bước 5: Regex Shield - kiểm tra AI có vi phạm không ───
            foreach (var pattern in _dangerousPatterns)
            {
                if (pattern.IsMatch(aiResponse))
                {
                    _logger.LogWarning("[AI Shield] AI cố đưa lời khuyên nguy hiểm. Pattern: {Pattern}", pattern);
                    aiResponse = "Đây là tương tác thuốc cần được theo dõi cẩn thận. " +
                                 "Vui lòng liên hệ bác sĩ hoặc dược sĩ của bạn để được tư vấn chi tiết và an toàn.";
                    break;
                }
            }

            return aiResponse;
        }

        private async Task<string> CallGroqAsync(string systemPrompt, string userPrompt)
        {
            var apiKey = _configuration["GroqSettings:ApiKey"]
                         ?? _configuration["GroqSettings__ApiKey"]
                         ?? throw new InvalidOperationException("Thiếu GroqSettings:ApiKey.");

            var model = _configuration["GroqSettings:Model"] ?? "llama-3.3-70b-versatile";

            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt }
                },
                temperature = 0.3,
                max_tokens = 512
            };

            var client = _httpClientFactory.CreateClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[DrugInteractionAI] Groq lỗi {Status}: {Body}", response.StatusCode, body);
                return "Không thể lấy thông tin giải thích lúc này. Vui lòng thử lại sau.";
            }

            var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
    }
}
