using System;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AbimToolsMine
{
    public static class LicenseChecker
    {
        private static string GetCacheFilePath()
        {
            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AbimTools");

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            return Path.Combine(cacheDir, "license_cache.json");
        }

        public static bool IsLicenseValid(string org, string code)
        {
            if (string.IsNullOrWhiteSpace(org) || string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("❌ Организация или код не указаны");
                return false;
            }

            string url = $"http://176.113.83.160:3001/check?org={Uri.EscapeDataString(org)}&code={Uri.EscapeDataString(code)}";
            string cacheFile = GetCacheFilePath();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = client.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode();

                    var json = response.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<LicenseResponse>(json);

                    if (result != null && result.valid)
                    {
                        SaveLicenseTimestamp(cacheFile);
                        Console.WriteLine("✅ Лицензия действительна (онлайн)");
                        return true;
                    }

                    Console.WriteLine("❌ Лицензия отклонена сервером");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка связи с сервером: {ex.Message}");
                Console.WriteLine("Пробуем офлайн-проверку...");

                if (IsLicenseValidOffline(cacheFile))
                {
                    Console.WriteLine("✅ Офлайн лицензия разрешена (в пределах 24 часов)");
                    return true;
                }

                Console.WriteLine("❌ Офлайн лицензия истекла или отсутствует");
                return false;
            }
        }

        private static void SaveLicenseTimestamp(string path)
        {
            try
            {
                var cache = new LicenseCache { LastSuccess = DateTime.UtcNow };
                var json = JsonConvert.SerializeObject(cache);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка записи кеша: {ex.Message}");
            }
        }

        private static bool IsLicenseValidOffline(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                var json = File.ReadAllText(path);
                var cache = JsonConvert.DeserializeObject<LicenseCache>(json);

                if (cache == null) return false;

                var elapsed = DateTime.UtcNow - cache.LastSuccess;
                return elapsed < TimeSpan.FromHours(24);
            }
            catch
            {
                return false;
            }
        }

        private class LicenseResponse
        {
            public bool valid { get; set; }
        }

        private class LicenseCache
        {
            public DateTime LastSuccess { get; set; }
        }
    }
}