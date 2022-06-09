namespace CSharpExporter.AspNetCore.ServiceCommunciation.Models
{
    public class ActionResultError
    {
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public int? Status { get; set; }
        public string? Title { get; set; }
        public string? TraceId { get; set; }
        public string? Type { get; set; }
    }
}