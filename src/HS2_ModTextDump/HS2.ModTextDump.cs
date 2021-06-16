using System;
using System.IO;
using BepInEx;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ModTextDump
    {
        public const string PluginNameInternal = "HS2_ModTextDump";

        private static Version _gameVersion;

        public ModTextDump()
        {
            SetTextResourceHelper(CreateHelper<HS2_TextResourceHelper>());
        }


        protected override Version GetGameVersion()
        {
            if (_gameVersion == null)
            {
                _gameVersion = new Version();
                var versionFile = Path.Combine(DefaultData.Path, "system\\version.dat");
                if (File.Exists(versionFile))
                {
                    var version = File.ReadAllText(versionFile);
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        _gameVersion = new Version(version);
                    }
                }
            }

            return _gameVersion;
        }
    }
}
