using System;

namespace IllusionMods
{
    public static class HelperFactory
    {
        public static T CreateHelper<T>() where T : IHelper, new()
        {
            return CreateHelper(() => new T());
        }

        public static T CreateHelper<T>(Func<T> initializer)
            where T : IHelper
        {
            var obj = initializer();
            obj.InitializeHelper();
            return obj;
        }
    }
}
