namespace FypTracker.Models
{
    public class StatusPageViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-exclamation-triangle";
        public string? RequestId { get; set; }
    }
}