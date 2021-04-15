using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIProject;
using HarmonyLib;
using Localize.Translate;
using UnityEngine;
using UnityEngine.UI;
using UnityEx;
using static IllusionMods.TextResourceHelper.Helpers;
using Resources = Manager.Resources;

namespace IllusionMods
{
    public partial class AI_INT_LocalizationDumpHelper
    {

        protected IEnumerable<KeyValuePair<GameObject, Text>> EnumerateTexts(GameObject gameObject,
            MonoBehaviour component, HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            var _ = binders;
            if (handled is null)
            {
                handled = new HashSet<object>();
            }

            if (!handled.Contains(component))
            {
                handled.Add(component);
                if (component is Text text)
                {
                    //Logger.LogInfo($"EnumerateTexts: {gameObject} yield {text}");
                    yield return new KeyValuePair<GameObject, Text>(gameObject, text);
                }
                else
                {
                    var trav = Traverse.Create(component);
                    foreach (var fieldName in trav.Fields())
                    {
                        var field = trav.Field(fieldName);
                        var fieldType = field.GetValueType();
                        if (fieldType == typeof(Text))
                        {
                            var fieldValue = field.GetValue<Text>();
                            if (fieldValue != null && !handled.Contains(fieldValue))
                            {
                                //Logger.LogInfo($"EnumerateTexts: {gameObject} field {fieldName} text {fieldValue}");
                                yield return new KeyValuePair<GameObject, Text>(gameObject, fieldValue);
                            }
                        }
                        else if (typeof(MonoBehaviour).IsAssignableFrom(fieldType))
                        {
                            var subBehaviour = field.GetValue<MonoBehaviour>();
                            if (subBehaviour != null && !handled.Contains(subBehaviour))
                            {
                                foreach (var subValue in EnumerateTexts(gameObject, subBehaviour, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }

                        /*
                        else if (typeof(GameObject).IsAssignableFrom(fieldType))
                        {
                            var subObject = field.GetValue<GameObject>();
                            if (subObject != null && !handled.Contains(subObject))
                            {
                                handled.Add(subObject);
                                Logger.LogInfo($"EnumerateTexts: {gameObject} field {fieldName} GameObject {subObject}");
                                foreach (var subValue in EnumerateTexts(subObject, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }
                        */
                    }
                }
            }
        }

        protected IEnumerable<KeyValuePair<GameObject, Text>> EnumerateTexts(GameObject gameObject,
            HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            handled = handled ?? new HashSet<object>();

            if (handled.Contains(gameObject)) yield break;
            handled.Add(gameObject);

            if (binders != null)
            {
                foreach (var binder in gameObject.GetComponents<UIBinder>())
                {
                    if (!binders.Contains(binder))
                    {
                        binders.Add(binder);
                    }
                }
            }

            foreach (var text in gameObject.GetComponents<Text>())
            {
                //Logger.LogInfo($"EnumerateTexts: {gameObject} GetComponents (text) {text}");
                yield return new KeyValuePair<GameObject, Text>(gameObject, text);
            }

            foreach (var component in gameObject.GetComponents<MonoBehaviour>())
            {
                foreach (var result in EnumerateTexts(gameObject, component, handled, binders))
                {
                    yield return result;
                }
            }

            foreach (var childText in GetChildrenFromGameObject(gameObject)
                .SelectMany(child => EnumerateTexts(child, handled, binders)))
            {
                yield return childText;
            }
        }


        protected override IEnumerable<ITranslationDumper> GetBindLocalizers(string assetPath)
        {
            foreach (var generator in base.GetBindLocalizers(assetPath))
            {
                yield return generator;
            }

            var handled = new HashSet<object>();
            foreach (var entry in GetAssetBundleInfos(assetPath))
            {
                var path = CombinePaths(
                    Path.GetDirectoryName(entry.assetbundle),
                    Path.GetFileNameWithoutExtension(entry.asset));
                foreach (var gameObject in LoadGameObjects(entry))
                {
                    var outputName = $"Bind/{path}/{gameObject.name}";

                    Dictionary<string, string> Localizer()
                    {
                        var binders = new List<UIBinder>();
                        var textList = EnumerateTexts(gameObject, handled, binders).Select(t => t.Value).ToArray();
                        var before = textList.Select(t => t.text).ToArray();

                        foreach (var binder in binders)
                        {
                            var binderLoad = Traverse.Create(binder).Method("Load");
                            if (binderLoad?.MethodExists() == true)
                            {
                                binderLoad.GetValue();
                            }
                        }

                        var results = new Dictionary<string, string>();
                        var after = textList.Select(t => t.text).ToArray();
                        for (var i = 1; i < before.Length; i++)
                        {
                            AddLocalizationToResults(results, before[i], after[i]);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(outputName, Localizer);
                }
            }
        }
    }
}
