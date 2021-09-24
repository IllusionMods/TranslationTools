using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Logging;
using IllusionMods.Shared;
using JetBrains.Annotations;

namespace IllusionMods
{
    public class BaseHelperFactory
    {
        private static readonly Dictionary<Type, ManualLogSource> Loggers =
            new Dictionary<Type, ManualLogSource>();

        protected static ManualLogSource GetLogger<T>() where T : IHelper
        {
            var key = typeof(T);
            return Loggers.GetOrInit(key, () => Logger.CreateLogSource(key.Name));
        }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class BaseHelperFactory<T> : BaseHelperFactory where T : IHelper
    {
        private const BindingFlags CtorFlags = BindingFlags.Instance | BindingFlags.CreateInstance |
                                               BindingFlags.NonPublic;

        internal ManualLogSource Logger => GetLogger<T>();

        public static TSub Create<TSub>() where TSub : T, IHelper
        {
            var helper = (TSub) Activator.CreateInstance(typeof(TSub), true);
            helper.InitializeHelper();
            return helper;
        }

        [UsedImplicitly]
        public static TSub Create<TSub>(params object[] args) where TSub : T, IHelper
        {
            var helper = (TSub) Activator.CreateInstance(typeof(TSub), CtorFlags, null,
                args, CultureInfo.CurrentCulture);
            helper.InitializeHelper();
            return helper;
        }
    }
}
