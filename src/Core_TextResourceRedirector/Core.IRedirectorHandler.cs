using System;
using System.Collections.Generic;
using System.Text;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public interface IRedirectorHandler
    {
        bool Enabled { get; }
        TextResourceRedirector Plugin { get; } 
        string ConfigSectionName { get; }
    }

    public interface IRedirectorHandler<T> : IRedirectorHandler where T : UnityEngine.Object { }
}
