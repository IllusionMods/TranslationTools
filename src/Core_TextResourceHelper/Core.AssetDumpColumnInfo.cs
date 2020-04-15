using BepInEx.Logging;
using System.Collections.Generic;

namespace IllusionMods
{
    public class AssetDumpColumnInfo
    {
        protected static ManualLogSource Logger = null;
        public bool MissingMappingOk { get; }
        public bool CombineWithParentBundle { get; set; } = false;
        public IEnumerable<KeyValuePair<int, int>> NumericMappings { get; } = null;

        public IEnumerable<KeyValuePair<string, string>> NameMappings { get; } = null;

        public IEnumerable<string> ItemLookupColumns { get; } = null;

        public AssetDumpColumnInfo(IEnumerable<KeyValuePair<int, int>> numericMappings = null, IEnumerable<KeyValuePair<string, string>> nameMappings = null, bool missingMappingOk = true, IEnumerable<string> itemLookupColumns = null)
        {
            AssetDumpColumnInfo.Logger = AssetDumpColumnInfo.Logger ?? BepInEx.Logging.Logger.CreateLogSource(GetType().FullName);
            this.NumericMappings = numericMappings ?? new KeyValuePair<int, int>[0];
            this.NameMappings = nameMappings ?? new KeyValuePair<string, string>[0];
            this.ItemLookupColumns = itemLookupColumns ?? new string[0];
            MissingMappingOk = missingMappingOk;
        }

        public AssetDumpColumnInfo() : this(new KeyValuePair<int, int>[0], new KeyValuePair<string, string>[0], true, new string[0]) { }

        public AssetDumpColumnInfo(IEnumerable<KeyValuePair<string, string>> nameMappings = null, bool missingMappingOk = true, IEnumerable<string> itemLookupColumns = null) :
            this(null, nameMappings, missingMappingOk, itemLookupColumns)
        { }
        public AssetDumpColumnInfo(KeyValuePair<int, int> numericMapping, bool missingMappingOk = true) :
            this(new KeyValuePair<int, int>[] { numericMapping }, null, missingMappingOk)
        { }

        public AssetDumpColumnInfo(int srcCol, int destCol = -1, bool missingMappingOk = true) :
            this(new KeyValuePair<int, int>(srcCol, destCol), missingMappingOk)
        { }

        public AssetDumpColumnInfo(KeyValuePair<string, string> nameMapping, bool missingMappingOk = true) :
            this(null, new KeyValuePair<string, string>[] { nameMapping }, missingMappingOk)
        { }

        public AssetDumpColumnInfo(string srcCol, string destCol = null, bool missingMappingOk = true) :
            this(new KeyValuePair<string, string>(srcCol, destCol ?? string.Empty), missingMappingOk)
        { }
    }
}
