using System;
using System.Globalization;
using System.IO;

namespace AccesClientWPF.Helpers
{
    public static class AppVersion
    {
        private static readonly Lazy<string> _currentString = new(() =>
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, "version.txt");
                if (!File.Exists(path)) return "0.0.0";
                return File.ReadAllText(path).Trim();
            }
            catch
            {
                return "0.0.0";
            }
        });

        public static string CurrentString => _currentString.Value;

        public static Version Current
        {
            get
            {
                // Support "1.5.2"
                if (Version.TryParse(Normalize(CurrentString), out var v))
                    return v;

                return new Version(0, 0, 0);
            }
        }

        public static Version ParseSafe(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Version(0, 0, 0);
            return Version.TryParse(Normalize(s.Trim()), out var v) ? v : new Version(0, 0, 0);
        }

        // Version.Parse accepte 1.5.2 -> OK, mais on normalise au cas où
        private static string Normalize(string input)
        {
            // garde uniquement chiffres + points, ex: "v1.5.2" -> "1.5.2"
            var cleaned = "";
            foreach (var ch in input)
            {
                if (char.IsDigit(ch) || ch == '.') cleaned += ch;
            }
            // évite "1.5" vs "1.5.0" : Version gère, pas besoin d'ajouter
            return string.IsNullOrWhiteSpace(cleaned) ? "0.0.0" : cleaned;
        }
    }
}