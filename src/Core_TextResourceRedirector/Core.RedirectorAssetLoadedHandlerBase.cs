using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public abstract class RedirectorAssetLoadedHandlerBase<T> : AssetLoadedHandlerBaseV2<T>, IRedirectorHandler<T>
        where T : Object
    {
        protected RedirectorAssetLoadedHandlerBase(TextResourceRedirector plugin)
        {
            CheckDirectory = true;
            Plugin = plugin;
            ConfigSectionName = GetType().Name;

            EnableHandler = this.ConfigEntryBind("Enabled", true, $"Handle {typeof(T).Name} assets");

            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        public ConfigEntry<bool> EnableHandler { get; }

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => EnableHandler.Value;
        public TextResourceRedirector Plugin { get; }

        protected TextResourceHelper TextResourceHelper => Plugin.TextResourceHelper;

        public string ConfigSectionName { get; }

        protected override string CalculateModificationFilePath(T asset, IAssetOrResourceLoadedContext context)
        {
            return asset.DefaultCalculateModificationFilePath(context);
        }

        protected override bool ShouldHandleAsset(T asset, IAssetOrResourceLoadedContext context)
        {
            return this.DefaultShouldHandleAsset(asset, context);
        }
    }
}
