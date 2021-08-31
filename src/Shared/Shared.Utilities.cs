using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IllusionMods.Shared
{
    public static class Utilities
    {
        public static string GetCurrentExecutableName()

        {
            var process = Process.GetCurrentProcess();

            if (process.MainModule == null) return string.Empty;
            try
            {
                return Path.GetFileNameWithoutExtension(process.MainModule.FileName);
            }
            catch { }

            try
            {
                return process.ProcessName;
            }
            catch { }

            return string.Empty;
        }
    }
}
