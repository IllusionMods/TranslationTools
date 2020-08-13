using System;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class BenchmarkResult
    {
        private readonly TranslationResult _result;

        public BenchmarkResult(string originalText, TranslationResult result, TimeSpan elapsed)
        {
            OriginalText = originalText;
            _result = result;
            Elapsed = elapsed;
        }

        public string OriginalText { get; }
        public TimeSpan Elapsed { get; }

        public string TranslatedText => _result.TranslatedText;
        public bool TranslationSucceeded => _result.Succeeded;

        public bool Unchanged => OriginalText.Equals(TranslatedText, StringComparison.OrdinalIgnoreCase);

        public static string GetCSVHeaderLine()
        {
            return string.Join(",", new[]
            {
                "Elapsed",
                "Succeeded",
                "Unchanged",
                "Original",
                "Translated"
            });
        }

        public string GetCSVLine()
        {
            return string.Join(",", new[]
            {
                $"{Elapsed}",
                $"{TranslationSucceeded}",
                $"{Unchanged}",
                $"\"{OriginalText}\"",
                $"\"{TranslatedText}\""
            });
        }
    }
}
