using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    /// <summary>
    ///     <c>NickName</c> assets can not be modified in place without error, but they can be modified after they're all
    ///     loaded.
    ///     <para>
    ///         Caches translations where replacement would normally occur, then hook
    ///         <c>SaveData.LoadNickNames</c> to apply them.
    ///     </para>
    /// </summary>
    /// <seealso cref="XUnity.AutoTranslator.Plugin.Core.AssetRedirection.AssetLoadedHandlerBaseV2{T}" />
    /// <seealso cref="NickName" />
    public class NickNameHandler : AssetLoadedHandlerBaseV2<NickName>
    {
        internal static NickNameHandler Instance;

        private static bool _hooksInitialized;
        private static readonly object HookLock = new object();

        private readonly Dictionary<string, Dictionary<string, string>> _replacements =
            new Dictionary<string, Dictionary<string, string>>();

        private readonly TextResourceHelper _textResourceHelper;

        public NickNameHandler(TextResourceHelper helper)
        {
            Instance = this;
            CheckDirectory = true;
            _textResourceHelper = helper;
            // replacement fires INSIDE the function we need to postfix, so init hooks up front
            InitHooks();
            Logger.LogDebug($"{GetType()} enabled");
        }

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        /// <inheritdoc />
        /// <remarks>Always returns <c>true</c> to signal nothing else should handle these.</remarks>
        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref NickName asset,
            IAssetOrResourceLoadedContext context)
        {
            // updating the NickName assets directly causes issues, but after SaveData.LoadNickNameParam() they
            // are safe to manipulate
            InitHooks();

            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            if (cache.IsEmpty) return true;

            var replacementKey = calculatedModificationPath
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .LastOrDefault();
            // don't touch "player" entry
            if (string.IsNullOrEmpty(replacementKey) ||
                replacementKey.Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!_replacements.TryGetValue(replacementKey, out var replacements))
            {
                _replacements[replacementKey] = replacements = new Dictionary<string, string>();
            }

            foreach (var entry in asset.param)
            {
                if (!entry.isSpecial) continue;
                var key = _textResourceHelper.GetSpecializedKey(entry, entry.Name);
                if (string.IsNullOrEmpty(key)) continue;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    replacements[key] = translated;
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, entry.Name);
                }
            }

            return true;
        }

        protected override bool DumpAsset(string calculatedModificationPath, NickName asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            foreach (var entry in asset.param)
            {
                if (!entry.isSpecial) continue;
                var key = _textResourceHelper.GetSpecializedKey(entry, entry.Name);
                if (!string.IsNullOrEmpty(key) && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, entry.Name);
                }
            }

            return true;
        }

        protected override string CalculateModificationFilePath(NickName asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool ShouldHandleAsset(NickName asset, IAssetOrResourceLoadedContext context)
        {
            return !context.HasReferenceBeenRedirectedBefore(asset);
        }

        private void InitHooks()
        {
            if (_hooksInitialized) return;
            lock (HookLock)
            {
                if (_hooksInitialized) return;
                HarmonyWrapper.PatchAll(typeof(Hooks));
                _hooksInitialized = true;
            }
        }

        internal static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveData), "LoadNickNameParam")]
            internal static void SaveDataLoadNickNameParamPostfix(ref Dictionary<string, List<NickName.Param>> __result)
            {
                if (Instance == null) return;
                foreach (var nicks in __result)
                {
                    if (!Instance._replacements.TryGetValue(nicks.Key, out var replacements)) continue;
                    foreach (var entry in nicks.Value)
                    {
                        if (!entry.isSpecial) continue;
                        var key = Instance._textResourceHelper.GetSpecializedKey(entry, entry.Name);
                        if (!replacements.TryGetValue(key, out var translated)) continue;
                        entry.Name = translated;
                    }
                }
            }
        }
    }
}
