using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace IllusionMods
{
    [PublicAPI]
    internal static class XuaHelper
    {
        public static IEnumerable<string> GetPathSegments(this GameObject obj)
        {
            var objects = new List<GameObject> {obj};
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                objects.Add(obj);
            }

            objects.Reverse();
            return objects.Select(o => o.name).ToList();
        }

        public static string GetXuaResizerPath(this GameObject obj)
        {
            var segments = GetPathSegments(obj);
            return string.Join("/", segments.ToArray());
        }

        public static string GetXuaResizerPath(this object ui)
        {
            return ui != null && ui is Component comp && comp.gameObject != null
                ? comp.gameObject.GetXuaResizerPath()
                : null;
        }
    }

    public class XuaResizerResult
    {
        public enum HorizontalOverflowValue
        {
            Wrap = 0,
            Overflow = 1
        }

        public enum VerticalOverflowValue
        {
            Truncate = 0,
            Overflow = 1
        }

        private decimal? _fontSize;

        private decimal? _lineSpacing;
        public HorizontalOverflowValue? HorizontalOverflow;
        public VerticalOverflowValue? VerticalOverflow;

        public bool? AutoResize { get; set; }

        public decimal? ChangeFontSize { get; private set; }

        public decimal? FontSize
        {
            get => _fontSize;
            set
            {
                if (value.HasValue)
                {
                    _fontSize = decimal.Round(value.Value, 4);
                }
                else
                {
                    _fontSize = null;
                }
            }
        }

        public decimal? LineSpacing
        {
            get => _lineSpacing;
            set
            {
                if (value.HasValue)
                {
                    _lineSpacing = decimal.Round(value.Value, 4);
                }
                else
                {
                    _lineSpacing = null;
                }
            }
        }


        public override bool Equals(object obj)
        {
            if (obj is XuaResizerResult other)
            {
                return FontSize.Equals(other.FontSize) &&
                       ChangeFontSize.Equals(other.ChangeFontSize) &&
                       AutoResize.Equals(other.AutoResize) &&
                       LineSpacing.Equals(other.LineSpacing) &&
                       HorizontalOverflow.Equals(other.HorizontalOverflow) &&
                       VerticalOverflow.Equals(other.VerticalOverflow);
            }

            return base.Equals(obj);
        }

        public XuaResizerResult Delta(XuaResizerResult original)
        {
            var result = new XuaResizerResult();
            if (Equals(original)) return result;


            if (AutoResize != original.AutoResize) result.AutoResize = AutoResize;
            if (LineSpacing != original.LineSpacing) result.LineSpacing = LineSpacing;
            if (HorizontalOverflow != original.HorizontalOverflow) result.HorizontalOverflow = HorizontalOverflow;
            if (VerticalOverflow != original.VerticalOverflow) result.VerticalOverflow = VerticalOverflow;

            if (FontSize == original.FontSize) return result;

            if (FontSize.HasValue)
            {
                if (decimal.Round(FontSize.Value, 0) == FontSize.Value)
                {
                    result.FontSize = FontSize;
                }
                else
                {
                    result.ChangeFontSize = FontSize / original.FontSize;
                }
            }
            else
            {
                result.FontSize = FontSize;
            }

            return result;
        }

        public IEnumerable<string> GetDirectives()
        {
            if (FontSize.HasValue) yield return $"ChangeFontSize({(int) decimal.Round(FontSize.Value)})";
            if (ChangeFontSize.HasValue) yield return $"ChangeFontSizeByPercentage({ChangeFontSize})";
            if (AutoResize.HasValue) yield return $"AutoResize({AutoResize.Value.ToString().ToLowerInvariant()})";
            if (LineSpacing.HasValue) yield return $"UGUI_ChangeLineSpacing({LineSpacing.Value})";
            if (HorizontalOverflow.HasValue)
            {
                yield return $"UGUI_HorizontalOverflow({HorizontalOverflow.Value.ToString().ToLowerInvariant()}";
            }

            if (VerticalOverflow.HasValue)
            {
                yield return $"UGUI_VerticalOverflow({VerticalOverflow.Value.ToString().ToLowerInvariant()}";
            }
        }

        public override int GetHashCode()
        {
            return $"{FontSize}/{ChangeFontSize}/{AutoResize}/{LineSpacing}/{HorizontalOverflow}/{VerticalOverflow}"
                .GetHashCode();
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            void AddFieldString<T>(string name, T field)
            {
                if (field == null) return;
                if (result[result.Length - 1] != '(') result.Append(", ");
                result.Append(name).Append("=").Append(field);
            }

            result.Append(GetType()).Append("(");
            AddFieldString(nameof(FontSize), FontSize);
            AddFieldString(nameof(ChangeFontSize), ChangeFontSize);
            AddFieldString(nameof(AutoResize), AutoResize);
            AddFieldString(nameof(LineSpacing), LineSpacing);
            AddFieldString(nameof(HorizontalOverflow), HorizontalOverflow);
            AddFieldString(nameof(VerticalOverflow), VerticalOverflow);
            result.Append(")");
            return result.ToString();
        }
    }
}
