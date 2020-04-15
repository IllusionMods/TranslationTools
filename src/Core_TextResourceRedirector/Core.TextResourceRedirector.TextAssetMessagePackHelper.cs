#if !HS
using ADV.Commands.Base;
using BepInEx.Logging;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;

namespace IllusionMods
{
    public static class TextAssetMessagePackHelper
    {
        private static ManualLogSource Logger => TextResourceRedirector.Logger;
        private static readonly List<IHandler> handlers = new List<IHandler>();
        internal static TextAssetMessagePackHandler _textAssetMessagePackHandler = null;

        public delegate bool CanHandleAssetDelegate(TextAsset textAsset);
        public delegate bool CanHandleTypeDelegate(Type type);
        public delegate T LoadDelegate<T>(TextAsset textAsset) where T : class;
        public delegate TextAndEncoding StoreDelegate<T>(T obj) where T : class;
        public delegate bool TranslateDelegate<T>(ref T obj, SimpleTextTranslationCache cache, string calculatedModificationPath) where T : class;
        public static int HandlerCount => handlers.Count;

        public static bool Enabled { get; private set; } = false;

        public static CanHandleAssetDelegate MakeStandardCanHandleAsset(string mark, int searchLength = -1, Encoding markEncoding = null)
        {
            markEncoding = markEncoding ?? Encoding.UTF8;
            var searchMark = markEncoding.GetBytes(mark);
            var _searchLength = searchLength != -1 ? searchLength : searchMark.Length * 3;
            bool stdCanHandleAsset(TextAsset textAsset)
            {
                return textAsset.bytes != null && TextResourceHelper.ArrayContains<byte>(textAsset.bytes.Take(_searchLength), searchMark);
            }
            return stdCanHandleAsset;
        }
        public static CanHandleTypeDelegate MakeStandardCanHandleType<T>() where T : class
        {
            var searchType = typeof(T);
            bool stdCanHandleType(Type type)
            {
                return type == searchType;
            }
            return stdCanHandleType;
        }

        public static LoadDelegate<T> MakeStandardLoad<T>() where T : class
        {
            T stdLoad(TextAsset textAsset)
            {
                return MessagePackSerializer.Deserialize<T>(textAsset.bytes);
            }
            return stdLoad;
        }

        public static StoreDelegate<T> MakeStandardStore<T>() where T : class
        {
            TextAndEncoding stdStore(T obj)
            {
                byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    MessagePackSerializer.Serialize<T>(stream, obj);
                    bytes = stream.ToArray();
                }
                return new TextAndEncoding(bytes, Encoding.UTF8);
            }
            return stdStore;
        }

        public static void RegisterHandler<T>(TranslateDelegate<T> translate, CanHandleAssetDelegate canHandleAsset, CanHandleTypeDelegate canHandleType = null, LoadDelegate<T> load = null, StoreDelegate<T> store = null) where T : class
        {
            handlers.Add(new Handler<T>(
                translate,
                canHandleAsset,
                canHandleType ?? MakeStandardCanHandleType<T>(),
                load ?? MakeStandardLoad<T>(),
                store ?? MakeStandardStore<T>()));

            Enabled = HandlerCount > 0;
            if (Enabled && _textAssetMessagePackHandler is null)
            {
                // don't create until needed to improve performance
                _textAssetMessagePackHandler = new TextAssetMessagePackHandler();
            }
        }
        public static void RegisterHandler<T>(TranslateDelegate<T> translate, string mark, int searchLength = -1, Encoding markEncoding = null, CanHandleTypeDelegate canHandleType = null, LoadDelegate<T> load = null, StoreDelegate<T> store = null) where T: class
        {
            RegisterHandler(
                translate,
                MakeStandardCanHandleAsset(mark, searchLength, markEncoding),
                canHandleType,
                load,
                store);
        }

        public static bool RemoveHandler(IHandler handler)
        {
            var result = handlers.Remove(handler);
            Enabled = HandlerCount > 0;
            return result;
        }

        public static IHandler GetHandler(TextAsset textAsset) => textAsset.bytes is null ? null : handlers.Find((h) => h.CanHandleAsset(textAsset));
                
        public static IHandler GetHandler(Type type) => handlers.Find((h) => h.CanHandleType(type));
        public static Handler<T> GetHandler<T>() where T : class => handlers.Find((h) => h is Handler<T> th && th.CanHandleType<T>()) as Handler<T>;

        public static Handler<T> GetHandler<T>(TextAsset textAsset) where T : class => textAsset.bytes is null ? null : handlers.Find((h) => h is Handler<T> th && th.CanHandleAsset(textAsset)) as Handler<T>;
 

        public static bool CanHandleAsset<T>(TextAsset textAsset, out Handler<T> handler) where T : class
        {
            handler = null;
            if (textAsset.bytes != null)
            {
                handler = GetHandler<T>(textAsset);
            }
            return handler != null;
        }

        public static bool CanHandleAsset(TextAsset textAsset, out IHandler handler)
        {
            handler = null;
            if (textAsset.bytes != null)
            {
                handler = GetHandler(textAsset);
            }
            return handler != null;
        }

        public static bool CanHandleAsset(TextAsset textAsset) => CanHandleAsset(textAsset, out var _);
        public static bool CanHandleAsset<T>(TextAsset textAsset) where T : class => CanHandleAsset<T>(textAsset, out var _);

        public static bool CanHandleType(Type type, out IHandler handler)
        {
            handler = GetHandler(type);
            return handler != null;
        }

        public static bool CanHandleType<TObj>(out IHandler handler) => CanHandleType(typeof(TObj), out handler);
        public static bool CanHandleType<TObj>() => CanHandleType<TObj>(out var _);
        public static bool CanHandleType(Type type) => CanHandleType(type, out var _);

        public static T Load<T>(TextAsset textAsset) where T : class
        {
            if (!CanHandleAsset<T>(textAsset, out var handler))
            {
                throw new NotSupportedException($"No registered handler supports {textAsset.name}");
            }
            return handler.Load(textAsset);
        }

        public static bool Translate<T>(ref T obj, SimpleTextTranslationCache cache, string calculatedModificationPath) where T : class
        {
            var handler = GetHandler<T>();
            if (handler is null)
            {
                throw new NotSupportedException($"No registered handler supports {typeof(T).Name}");
            }
            return handler.Translate(ref obj, cache, calculatedModificationPath);
        }

        public static TextAndEncoding Store<T>(T obj) where T : class
        {
            var handler = GetHandler<T>();
            if (handler is null)
            {
                throw new NotSupportedException($"No registered handler supports {typeof(T).Name}");
            }
            return handler.Store(obj);
        }

        public interface IHandler
        {
            bool CanHandleAsset(TextAsset textAsset);
            bool CanHandleType(Type type);
            object Load(TextAsset textAsset);
            TextAndEncoding Store(object obj);
            bool Translate(ref object obj, SimpleTextTranslationCache cache, string calculatedModificationPath);
        }

        public class Handler<T> : IHandler where T: class
        {
            private readonly CanHandleAssetDelegate _canHandleAsset;
            private readonly CanHandleTypeDelegate _canHandlType;
            private readonly LoadDelegate<T> _load;
            private readonly StoreDelegate<T> _store;
            private readonly TranslateDelegate<T> _translate;
            
            internal Handler(TranslateDelegate<T> translate, CanHandleAssetDelegate canHandleAsset, CanHandleTypeDelegate canHandleType, LoadDelegate<T> load, StoreDelegate<T> store)
            {
                _canHandleAsset = canHandleAsset;
                _canHandlType = canHandleType;
                _load = load;
                _store = store;
                _translate = translate;
            }

            public bool CanHandleAsset(TextAsset textAsset) => textAsset.bytes != null && _canHandleAsset(textAsset);
            public bool CanHandleType(Type type) => _canHandlType(type);
            public bool CanHandleType<Tobj>() => CanHandleType(typeof(Tobj));
            public T Load(TextAsset textAsset) => textAsset.bytes is null ? null : _load(textAsset);

            public TextAndEncoding Store(T obj) => _store(obj);
            public bool Translate(ref T obj, SimpleTextTranslationCache cache, string calculatedModificationPath) => _translate(ref obj, cache, calculatedModificationPath);

            object IHandler.Load(TextAsset textAsset) => Load(textAsset);
            public bool Translate(ref object obj, SimpleTextTranslationCache cache, string calculatedModificationPath)
            {
                T typedObj = obj as T;
                return Translate(ref typedObj, cache, calculatedModificationPath);
            }

            public TextAndEncoding Store(object obj) => Store((T)obj);
        }
    }
}
#endif
