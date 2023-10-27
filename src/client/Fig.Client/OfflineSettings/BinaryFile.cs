using System;
using System.IO;

namespace Fig.Client.OfflineSettings
{
    internal class BinaryFile : IBinaryFile
    {
        public void Write(string clientName, string value)
        {
            CreateFigFolder();
            Delete(clientName);
            
            using var fileStream = new FileStream(GetFilePath(clientName), FileMode.Create);
            using var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(value);
            binaryWriter.Close();
        }

        public string? Read(string clientName)
        {
            var path = GetFilePath(clientName);
            if (!File.Exists(path))
                return null;
            
            using var fileStream = new FileStream(GetFilePath(clientName), FileMode.Open);
            using var binaryReader = new BinaryReader(fileStream);
            var value = binaryReader.ReadString();
            fileStream.Close();

            return value;
        }

        public void Delete(string clientName)
        {
            var path = GetFilePath(clientName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string GetFilePath(string clientName)
        {
            return Path.Combine(GetFigFolder(), $"{clientName.Replace(" ", "")}.dat");
        }

        private string GetFigFolder()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "Fig");
        }

        private void CreateFigFolder()
        {
            var folder = GetFigFolder();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }
}