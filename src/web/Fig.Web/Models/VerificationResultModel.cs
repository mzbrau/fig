namespace Fig.Web.Models
{
    public class VerificationResultModel
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public List<string> Logs { get; set; } = new();
    }
}
