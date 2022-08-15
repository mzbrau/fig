namespace Fig.Contracts.Status
{
    public class ApiVersionDataContract
    {
        public ApiVersionDataContract(string apiVersion)
        {
            ApiVersion = apiVersion;
        }

        public string ApiVersion { get; }
    }
}