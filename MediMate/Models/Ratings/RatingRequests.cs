namespace MediMate.Models.Ratings
{
    public class CreateRatingRequest
    {
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }
    }
}
