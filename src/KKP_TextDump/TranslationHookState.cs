using System.Collections.Generic;
using SimpleJson.Reflection;

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
