using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IllusionMods
{
    public partial class TextDump
    {
        private static Version _gameVersion;
        /// <summary>
        /// Get current version of the game.
        /// </summary>
        public static Version GetGameVersion()
        {
            if (_gameVersion == null)
            {
                _gameVersion = new Version();
                var versionFile = Path.Combine(DefaultData.Path, "system\\version.dat");
                if (File.Exists(versionFile))
                {
                    var version = File.ReadAllText(versionFile);
                    if (!string.IsNullOrWhiteSpace(version))
                        _gameVersion = new Version(version);
                }
            }
            return _gameVersion;
        }
    }
}
