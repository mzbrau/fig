namespace Fig.Api.Utils
{
    public class TempFile : IDisposable
    {
        private string? _filePath;

        public TempFile(byte[] data, string fileName)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var tempPath = Path.GetTempPath();
            _filePath = Path.Combine(tempPath, fileName);

            File.WriteAllBytes(_filePath, data);
        }

        public string FilePath
        {
            get
            {
                if (_filePath == null)
                    throw new ObjectDisposedException(GetType().Name);

                return _filePath;
            }
        }

        ~TempFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            if (_filePath != null)
            {
                try
                {
                    File.Delete(_filePath);
                }
                catch
                {
                    // best effort
                }

                _filePath = null;
            }
        }
    }
}
