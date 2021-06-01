using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using JetBrains.Annotations;

namespace IllusionMods
{
    
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class BaseHelperFactory<T> where T : IHelper
    {
        private const BindingFlags CtorFlags = BindingFlags.Instance | BindingFlags.CreateInstance |
                                               BindingFlags.NonPublic;

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
