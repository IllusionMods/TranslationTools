using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public abstract class RedirectorTextAssetLoadedHandlerBase : TextAssetLoadedHandlerBase, IRedirectorHandler<TextAsset>
    {
        protected RedirectorTextAssetLoadedHandlerBase(TextResourceRedirector plugin)
        {
            CheckDirectory = true;
            Plugin = plugin;
            ConfigSectionName = GetType().Name;

            EnableHandler = this.ConfigEntryBind("Enabled", true,
                $"Handle {nameof(TextAsset)} assets with tables");

            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");

        }

        public ConfigEntry<bool> EnableHandler { get; }

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => EnableHandler.Value;
        public TextResourceRedirector Plugin { get; }

        protected TextResourceHelper TextResourceHelper => Plugin.TextResourceHelper;

        public string ConfigSectionName { get; }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return asset.DefaultCalculateModificationFilePath(context);
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return this.DefaultShouldHandleAsset(asset, context);
        }
    }
}
