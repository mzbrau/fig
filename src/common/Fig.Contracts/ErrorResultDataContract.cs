namespace Fig.Contracts
{
    public class ErrorResultDataContract

    {
        public string ErrorType { get; set; }

        public string Message { get; set; }

        public string Detail { get; set; }

        public string Reference { get; set; }

        public override string ToString()
        {
            return $"{ErrorType}: {Message}.{Detail}. Reference:{Reference}";
        }
    }
}