using System.Collections.Generic;

namespace IllusionMods
{
    internal class TranslationHookState
    {
        public string Path { get; }
        public List<object> Context { get; } = new List<object>();
        internal TranslationHookState(string path)
        {
            Path = path;
        }
    }
}
