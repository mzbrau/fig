using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.WebHost
{
    public static class ConfigureHttps
    {
        public static void ConfigureHttpsListener(this ConfigureWebHostBuilder builder)
        {
            var certPath = Environment.GetEnvironmentVariable("SSL_CERT_PATH");
            var keyPath = Environment.GetEnvironmentVariable("SSL_KEY_PATH");
            if (certPath != null && keyPath != null && int.TryParse(Environment.GetEnvironmentVariable("FIG_API_SSL_PORT"), out int sslPort))
            {
                builder.ConfigureKestrel((context, serverOptions) =>
                {
                    var exportableCert = new X509Certificate2(X509Certificate2.CreateFromPemFile(certPath, keyPath).Export(X509ContentType.Pfx, ""), "", X509KeyStorageFlags.Exportable);
                    serverOptions.Configure()
                    .AnyIPEndpoint(sslPort, listenOptions =>
                    {
                        listenOptions.UseHttps(exportableCert);
                    });
                });
            }
        }
    }
}
