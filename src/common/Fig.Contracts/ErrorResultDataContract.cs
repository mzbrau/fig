namespace Fig.Contracts
{
    public class ErrorResultDataContract

    {
        public ErrorResultDataContract(string errorType, string message, string? detail, string? reference)
        {
            ErrorType = errorType;
            Message = message;
            Detail = detail;
            Reference = reference;
        }

        public string ErrorType { get; }

        public string Message { get; }

        public string? Detail { get; }

        public string? Reference { get; }

        public override string ToString()
        {
            return $"{ErrorType}: {Message}.{Detail}. Reference:{Reference}";
        }
    }
}