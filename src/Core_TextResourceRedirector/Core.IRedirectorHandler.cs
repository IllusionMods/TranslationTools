using BepInEx.Logging;
using XUnity.ResourceRedirector;
using UnityObject = UnityEngine.Object;

namespace IllusionMods
{
    public interface IRedirectorHandler
    {
        bool Enabled { get; }
        TextResourceRedirector Plugin { get; }
        string ConfigSectionName { get; }
        ManualLogSource GetLogger();
        bool EnableFallbackMapping { get; }
    }

    public interface IRedirectorHandler<T> : IRedirectorHandler where T : UnityObject
    {
        bool AllowTranslationRegistration { get; }



        bool ShouldHandleAssetForContext(T asset, IAssetOrResourceLoadedContext context);

        void ExcludePathFromTranslationRegistration(string path);

        bool IsTranslationRegistrationAllowed(string path);
    }
}
