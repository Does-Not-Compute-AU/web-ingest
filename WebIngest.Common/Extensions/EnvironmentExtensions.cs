using System;
using System.IO;

namespace WebIngest.Common.Extensions
{
    public static class EnvironmentExtensions
    {
        private static bool _envLoaded;
        public static void LoadEnv()
        {
            if (_envLoaded) return;
            
            _envLoaded = true;
            var root = Directory.GetCurrentDirectory();
            
            // look first for project level env
            var dotenv = Path.Combine(root, ".env");
            
            // look next for solution level env
            var parentPath = Directory.GetParent(root)?.FullName;
            if (parentPath != null)
                dotenv = File.Exists(dotenv) ? dotenv : Path.Combine(parentPath, ".env");

            // escape if no env file found
            if (!File.Exists(dotenv))
                return;

            foreach (var line in File.ReadAllLines(dotenv))
            {
                var parts = line.Split(
                    '=',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}