using System;
using System.IO;
using System.Text.Json;
using AccesClientWPF.Models;

namespace AccesClientWPF.Services
{
    public static class UpdateApplyService
    {
        private static string BaseFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AccesClient");

        private static string PendingPath => Path.Combine(BaseFolder, "update.pending.json");

        public static bool CanWriteToDirectory(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                var testFile = Path.Combine(dir, $"._write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch { return false; }
        }

        public static void WritePending(UpdatePendingInfo info)
        {
            Directory.CreateDirectory(BaseFolder);
            var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PendingPath, json);
        }
    }
}