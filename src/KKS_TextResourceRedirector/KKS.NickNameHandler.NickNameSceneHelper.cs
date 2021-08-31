using ADV;
using SaveData;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public partial class NickNameHandler
    {
        internal static class NickNameSceneHelper
        {
            private const string NickNameButtonDefaultText = "ああああああ";
            private static bool _enabled;
            private static SaveData.CharaData _charaData;

            private static SaveData.CharaData CharaData
            {
                get
                {
                    if (_charaData == null)
                    {
                        Object.FindObjectOfType<NickNameSettingScene>().SafeProc(
                            s => _charaData = s.charaData);
                    }

                    return _charaData;
                }
            }

            internal static bool Enabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value) return;
                    _charaData = null;
                    _enabled = value;
                    if (_enabled)
                    {
                        AutoTranslator.Default.RegisterOnTranslatingCallback(NickNameSettingTranslatingCallback);
                    }
                    else
                    {
                        AutoTranslator.Default.UnregisterOnTranslatingCallback(NickNameSettingTranslatingCallback);
                    }
                }
            }

            private static bool ShouldHandleTextMesh(TextMeshProUGUI textMesh)
            {

                var result = false;

                textMesh.SafeProc(tm =>
                {
                    if (tm.name != "text") return;
                    tm.gameObject.SafeProc(gm => gm.transform.SafeProc(trans => trans.parent.SafeProc(parent =>
                    {
                        result = (parent.name == "playerCall" || parent.name.StartsWith("NickNameNode"));
                    })));
                });
                return result;
            }

            private static void NickNameSettingTranslatingCallback(ComponentTranslationContext context)
            {
                if (!(context.Component is TextMeshProUGUI textMesh) || !ShouldHandleTextMesh(textMesh)) return;
                context.IgnoreComponent();
                // starting coroutine on textMesh so it stops if UI element goes away
                textMesh.StartCoroutine(HandleNickNameButton(textMesh));

                // handle translation now if possible, otherwise coroutine above will handle
                if (TryNickNameSettingLookup(context.OriginalText, out var translatedText) ||
                    context.OriginalText != textMesh.text &&
                    TryNickNameSettingLookup(textMesh.text, out translatedText))
                {
                    context.OverrideTranslatedText(translatedText);
                }
            }

            private static IEnumerator HandleNickNameButton(TextMeshProUGUI textMesh)
            {
                var orig = NickNameButtonDefaultText;
                textMesh.SafeProc(tm =>
                {
                    if (!string.IsNullOrEmpty(tm.text)) orig = tm.text;
                });

                // wait for CharaData to be available
                while (true)
                {
                    // always yielding once is intentional to allow OverrideTranslatedText to complete
                    yield return null;
                    // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
                    if (textMesh == null) yield break;
                    if (orig == NickNameButtonDefaultText && textMesh.text != orig)
                    {
                        // save the first non-default value in textMesh.text
                        orig = textMesh.text;
                    }

                    if (CharaData != null) break;
                }

                // try current value first if they differ for performance
                if (orig != textMesh.text && TryNickNameSettingLookup(textMesh.text, out var translatedText) ||
                    TryNickNameSettingLookup(orig, out translatedText))
                {
                    textMesh.text = translatedText;
                }

                // wait a frame before restoring XUA behavior for component
                yield return null;
                AutoTranslator.Default.UnignoreTextComponent(textMesh);
            }

            private static bool TryNickNameSettingLookup(string text, out string translatedText)
            {
                translatedText = null;

                if (Instance == null || text == NickNameButtonDefaultText) return false;
                if (Instance._matched.Contains(text))
                {
                    translatedText = text;
                    return true;
                }

                if (CharaData == null ||
                    !TryGetReplacementsByPersonality(CharaData.personality, out var replacements))
                {
                    return false;
}

                var nickParam = WorldData.GetCallNameList(CharaData).FirstOrDefault(n => n.Name == text);
                if (nickParam == null) return false;
                var key = Instance.TextResourceHelper.GetSpecializedKey(nickParam, nickParam.Name);
                if (!replacements.TryGetValue(key, out translatedText)) return false;
                Instance._matched.Add(translatedText);
                return true;
            }
        }
    }
}
