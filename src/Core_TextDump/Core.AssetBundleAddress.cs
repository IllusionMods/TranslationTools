namespace IllusionMods
{
    public partial class AssetBundleAddress
    {
        public string Name;
        public string AssetBundle;
        public string Asset;
        public string Manifest;

        public AssetBundleAddress(string name, string assetBundle, string asset, string manifest=null)
        {
            Name = name;
            AssetBundle = assetBundle;
            Asset = asset;
            Manifest = manifest;
        }
    }
}
