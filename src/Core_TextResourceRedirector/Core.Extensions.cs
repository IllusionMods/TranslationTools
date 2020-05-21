using System.Collections.Generic;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public static class Extensions
    {
        public static Dictionary<string, string> BuildReverseDictionary(this SimpleTextTranslationCache cache)
        {
            var translations = Traverse.Create(cache).Field<Dictionary<string, string>>("_translations").Value;
            var reverse = new Dictionary<string, string>();
            foreach (var entry in translations)
            {
                reverse[entry.Value] = entry.Key;
            }
            return reverse;
        }

        public static bool TryGetReverseTranslation(this SimpleTextTranslationCache cache, string translatedText,
            out string result)
        {
            return BuildReverseDictionary(cache).TryGetValue(translatedText, out result);
        }
    }
}
