# if !HS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using MessagePack;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public abstract class TextAssetMessagePackHandlerBase<T> : RedirectorTextAssetLoadedHandlerBase, IPathListBoundHandler
        where T : class
    {
        protected TextAssetMessagePackHandlerBase(TextResourceRedirector plugin, string mark,
            bool allowTranslationRegistration = false) : base(plugin,
            $"with {typeof(T).Name} packed inside", allowTranslationRegistration)
        {
            SetObjectMark(mark);
        }

        public IEnumerable<byte> ObjectMark { get; private set; }
        public virtual Encoding Encoding => Encoding.UTF8;
        public int SearchLength { get; protected set; } = -1;

        public virtual bool CanHandleAsset(TextAsset textAsset, IAssetOrResourceLoadedContext context)
        {
            var pth = context.GetUniqueFileSystemAssetPath(textAsset).Replace(".unity3d", string.Empty);
            if (ObjectMark == null || !this.IsPathAllowed(pth, true)) return false;
            var searchLength = SearchLength != -1 ? SearchLength : ObjectMark.Count() * 3;
            IEnumerable<byte> haystack = null;
            textAsset.SafeProc(a => a.bytes.SafeProc(b => haystack = b.Take(searchLength)));
            return haystack != null && TextResourceHelper.Helpers.ArrayContains(haystack, ObjectMark);
        }

        public virtual T LoadFromAsset(TextAsset textAsset)
        {
            return MessagePackSerializer.Deserialize<T>(textAsset.bytes);
        }

        public virtual TextAndEncoding StoreAsset(T obj)
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                MessagePackSerializer.Serialize(stream, obj);
                bytes = stream.ToArray();
            }

            return new TextAndEncoding(bytes, Encoding);
        }

        public abstract bool TranslateObject(ref T obj, SimpleTextTranslationCache cache,
            string calculatedModificationPath);

        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {

            Logger.DebugLogDebug($"{GetType()} attempt to handle {calculatedModificationPath}");
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            if (cache.IsEmpty)
            {
                Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath} (no cache)");
                return null;
            }

            var obj = LoadFromAsset(asset);

            if (obj != null && TranslateObject(ref obj, cache, calculatedModificationPath))
            {
                Logger.DebugLogDebug($"{GetType()} handled {calculatedModificationPath}");
                return StoreAsset(obj);
            }

            Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath}");
            return null;
        }



        protected void SetObjectMark(string mark)
        {
            ObjectMark = Encoding.GetBytes(mark);
        }
        
        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            return false; //throw new NotImplementedException();
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}])?");
            var result = base.ShouldHandleAsset(asset, context) && CanHandleAsset(asset, context);
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }


        #region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();

        #endregion IPathListBoundHandler;
    }
}

#endif
