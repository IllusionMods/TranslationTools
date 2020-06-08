#if false
// WIP: not read for prime time
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace IllusionMods
{
    public partial class UIDumpHelper : BaseDumpHelper
    {
        private static readonly List<string> SupportedEnumerationTypeNames = new List<string>
        {
            "UnityEngine.UI.Text",
            "UnityEngine.TextMesh",
            "TMPro.TMP_Text",
            "TMPro.TextMeshProUGUI",
            "TMPro.TextMeshPro",
            "UILabel",
            "FairyGUI.TextField"
        };

        private ReadOnlyDictionary<string, Type> _supportedEnumerationTypeMap = null;

        private IReadOnlyCollection<Type> _supportedEnumerationTypes = null;
        protected IReadOnlyCollection<Type> SupportedEnumerationTypes
        {
            get
            {
                if (_supportedEnumerationTypes != null) return _supportedEnumerationTypes;
                _supportedEnumerationTypes =
                    SupportedEnumerationTypeMap.Values.Where(o => o != null).ToList().AsReadOnly();
                return _supportedEnumerationTypes;
            }
        }

        private IDictionary<string, Type> SupportedEnumerationTypeMap
        {
            get
            {
                if (_supportedEnumerationTypeMap != null) return _supportedEnumerationTypeMap;
                var typeMap = new Dictionary<string, Type>();
                foreach (var typeName in SupportedEnumerationTypeNames)
                {
                    Type type = null;
                    try
                    {
                        type = AccessTools.TypeByName(typeName);
                    }
                    catch (Exception)
                    {
                        type = TextDump.Helpers.FindType(typeName);
                    }

                    if (type == null)
                    {
                        TextDump.Logger.LogDebug(
                            $"SupportedEnumerationTypes: Unable to find type {typeName} {type}, skipping.");
                    }

                    typeMap[typeName] = type;
                }
                _supportedEnumerationTypeMap = new ReadOnlyDictionary<string, Type>(typeMap);
                return _supportedEnumerationTypeMap;
            }
        }

      
        protected UIDumpHelper(TextDump plugin) : base(plugin) { }


        private bool IsSupportedForEnumeration(Type type)
        {
            return type != null &&
                   SupportedEnumerationTypes.Any(supported => type == supported || type.IsSubclassOf(supported));
        }

        private bool IsSupportedForEnumeration(Component component)
        {
            return IsSupportedForEnumeration(component.GetType());
        }

        private static string GetTextFromSupportedComponent(Component component)
        {
            if (component == null) return string.Empty;

            var componentType = component.GetType();
            var fieldInfo = AccessTools.Property(componentType, "text");
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(component, new object[0]) as string;
            }

            var propInfo = AccessTools.Property(componentType, "text");
            if (propInfo != null)
            {
                return propInfo.GetValue(component, new object[0]) as string;
            }

            TextDump.Logger.LogWarning($"Unable to access 'text' property for {component}");
            return string.Empty;
        }

        protected static IEnumerable<KeyValuePair<GameObject, Component>> EnumerateTextComponents(GameObject gameObject,
            Component component, HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            var _ = binders;
            if (handled is null)
            {
                handled = new HashSet<object>();
            }

            if (!handled.Contains(component))
            {
                handled.Add(component);
                if (IsSupportedForEnumeration(component))
                {
                    //Logger.LogInfo($"EnumerateTextComponents: {gameObject} yield {text}");
                    yield return new KeyValuePair<GameObject, Component>(gameObject, component);
                }
                else
                {
                    var trav = Traverse.Create(component);
                    foreach (var fieldName in trav.Fields())
                    {
                        var field = trav.Field(fieldName);
                        var fieldType = field.GetValueType();
                        if (IsSupportedForEnumeration(fieldType))
                        {
                            var fieldValue = field.GetValue<Component>();
                            if (fieldValue != null && !handled.Contains(fieldValue))
                            {
                                //Logger.LogInfo($"EnumerateTextComponents: {gameObject} field {fieldName} text {fieldValue}");
                                yield return new KeyValuePair<GameObject, Component>(gameObject, fieldValue);
                            }
                        }
                        else if (typeof(Component).IsAssignableFrom(fieldType))
                        {
                            var subComponent = field.GetValue<Component>();
                            if (subComponent != null && !handled.Contains(subComponent))
                            {
                                foreach (var subValue in EnumerateTextComponents(gameObject, subComponent, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<GameObject, Component>> EnumerateTextComponents(GameObject gameObject,
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

            foreach (var component in gameObject.GetComponents<Component>())
            {
                //Logger.LogInfo($"EnumerateTextComponents: {gameObject} GetComponents (text) {text}");
                if (IsSupportedForEnumeration(component))
                {
                    yield return new KeyValuePair<GameObject, Component>(gameObject, component);
                }

                foreach (var result in EnumerateTextComponents(gameObject, component, handled, binders))
                {
                    yield return result;
                }
            }

            foreach (var childText in GetChildrenFromGameObject(gameObject)
                .SelectMany(child => EnumerateTextComponents(child, handled, binders)))
            {
                yield return childText;
            }
        }


    }
}
#endif
