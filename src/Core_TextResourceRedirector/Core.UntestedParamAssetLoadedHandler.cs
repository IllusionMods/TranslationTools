using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using IllusionMods.Shared;
using UnityEngine;

namespace IllusionMods
{
    public abstract class UntestedParamAssetLoadedHandler<T, TParam> : ParamAssetLoadedHandler<T, TParam>
        where T : Object
    {
        private string _lastWarningPath;

        protected UntestedParamAssetLoadedHandler(TextResourceRedirector plugin,
            bool allowTranslationRegistration = false, bool enableSafeModeByDefault=true) :
            base(plugin, allowTranslationRegistration)
        {
            EnableSafeMode = this.ConfigEntryBind("Enable Safe Mode", enableSafeModeByDefault,
                new ConfigDescription($@"
Handle {typeof(T).Name} assets indirectly, enable this if you suspect this handler 
is causing game to misbehave.".ToSingleLineString(),
                    null, "Advanced"));
        }

        protected ConfigEntry<bool> EnableSafeMode { get; }

        protected void WarnIfUnsafe(string calculatedModificationPath)
        {
            if (EnableSafeMode.Value || calculatedModificationPath == _lastWarningPath) return;
            Logger.LogWarning(
                $"{GetType().Name}: performing full replacement for {calculatedModificationPath}. If you experience issues you suspect are related to this try setting 'Enable Safe Mode' for {ConfigSectionName}");
            _lastWarningPath = calculatedModificationPath;
        }

        protected override void DefaultUpdateParamValue(string calculatedModificationPath, Traverse<string> field,
            string translated)
        {
            if (!EnableSafeMode.Value)
            {
                WarnIfUnsafe(calculatedModificationPath);
                base.DefaultUpdateParamValue(calculatedModificationPath, field, translated);
            }
            
        }
    }
}
