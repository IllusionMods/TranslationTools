using System.IO;
using BepInEx.Logging;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TextAssetRawBytesHandler : TextAssetLoadedHandlerBase, IPathListBoundHandler
    {
        private readonly TextResourceHelper _textResourceHelper;

        public TextAssetRawBytesHandler(TextResourceHelper textResourceHelper)
        {
            CheckDirectory = true;
            _textResourceHelper = textResourceHelper;
            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        public static bool Enabled { get; set; } = false;

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            if (asset?.bytes == null) return false;

            var defaultFile = Path.Combine(calculatedModificationPath, "translation.bytes");

            File.WriteAllBytes(defaultFile, asset.bytes);
            return true;
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}])?");
            var result = Enabled && !context.HasReferenceBeenRedirectedBefore(asset) &&
                         this.IsPathAllowed(asset, context) && asset.bytes != null;
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }


        protected byte[] ReadBytes(Stream stream)
        {
#if AI
            using (var collector = new MemoryStream())
            {
                stream.CopyTo(collector);
                return collector.ToArray();
            }
#else
            const int bufferSize = 4096;
            using (var collector = new MemoryStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var buffer = new byte[bufferSize];
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        collector.Write(buffer, 0, count);
                    }

                    return collector.ToArray();
                }
            }
#endif
            }


        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()} attempt to handle {calculatedModificationPath}");
            if (!Enabled || asset.bytes == null)
            {
                Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath}");
                return null;
            }
            byte[] bytes = null;
            var defaultFile = Path.Combine(calculatedModificationPath, "translation.bytes");
            if (File.Exists(defaultFile))
            {
                bytes = File.ReadAllBytes(defaultFile);
            }

            if (bytes == null || bytes.Length == 0)
            {
                foreach (var entry in RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".bytes"))
                {
                    using (var stream = entry.OpenStream())
                    {
                        bytes = ReadBytes(stream);
                    }

                    if (bytes.Length > 0) break;
                }
            }

            if (bytes == null || bytes.Length == 0)
            {
                Logger.DebugLogDebug($"{GetType()}  unable to handle {calculatedModificationPath}: no .bytes files");
                return null;
            }
            Logger.DebugLogDebug($"{GetType()} handled {calculatedModificationPath}");
            return new TextAndEncoding(bytes, _textResourceHelper.TableHelper.TextAssetEncoding);
        }

#region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();

#endregion IPathListBoundHandler
    }
}
