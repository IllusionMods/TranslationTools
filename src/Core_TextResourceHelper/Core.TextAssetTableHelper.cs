using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace IllusionMods
{
    public class TextAssetTableHelper
    {
        private ManualLogSource _logger;

        public TextAssetTableHelper(IEnumerable<string> rowSplitStrings = null,
            IEnumerable<string> colSplitStrings = null, Encoding encoding = null)
        {
            TextAssetEncoding = encoding ?? Encoding.UTF8;

            int Comp(string a, string b)
            {
                return b.Length.CompareTo(a.Length);
            }

            var tmpList = new List<string>();

            // row split strings
            tmpList.AddRange(rowSplitStrings?.ToArray() ?? new string[0]);
            tmpList.Sort(Comp);
            RowSplitStrings = tmpList.ToArray();

            // col split strings
            tmpList.Clear();
            tmpList.AddRange(colSplitStrings?.ToArray() ?? new string[0]);
            tmpList.Sort(Comp);
            ColSplitStrings = tmpList.ToArray();

            // invalid col strings
            tmpList.Clear();
            tmpList.AddRange(RowSplitStrings);
            tmpList.AddRange(ColSplitStrings);
            tmpList.Sort(Comp);
            InvalidColStrings = tmpList.ToArray();

            Enabled = ColSplitStrings.Any() && RowSplitStrings.Any();
        }

        protected ManualLogSource Logger => _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(GetType().Name);

        public bool Enabled { get; }
        public IEnumerable<string> RowSplitStrings { get; }
        public IEnumerable<string> ColSplitStrings { get; }
        public IEnumerable<string> InvalidColStrings { get; }

        public Encoding TextAssetEncoding { get; }

        public virtual bool TryTranslateTextAsset(ref TextAsset textAsset, Func<string, string> translator,
            out string result)
        {
            if (IsTable(textAsset))
            {
                result = ProcessTable(textAsset, translator, out var tableResult);
                if (tableResult.RowsUpdated > 0)
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        #region table processing

        public bool IsTable(TextAsset textAsset)
        {
            return textAsset.text != null && IsTable(textAsset.text);
        }

        public bool IsTable(string table)
        {
            // possible table is only 1 row, so only check for column split strings
            return !string.IsNullOrEmpty(table) && ColSplitStrings.Any(table.Contains);
        }

        public bool IsTableRow(string row)
        {
            foreach (var rowSplit in ColSplitStrings)
            {
                if (row.Contains(rowSplit))
                {
                    return false;
                }
            }

            return true;
        }

        public virtual bool ShouldHandleAsset(TextAsset asset)
        {
            return Enabled && IsTable(asset);
        }

        public IEnumerable<string> SplitTableToRows(TextAsset textAsset)
        {
            return SplitTableToRows(textAsset.text);
        }

        public IEnumerable<string> SplitTableToRows(string table)
        {
            if (!IsTable(table))
            {
                throw new ArgumentException("textAsset does not contain a table");
            }

            return table.Split(RowSplitStrings.ToArray(), StringSplitOptions.None);
        }

        public IEnumerable<string> SplitRowToCells(string row)
        {
            Debug.Assert(IsTableRow(row), "row does not contain a table row");
            return row.Split(ColSplitStrings.ToArray(), StringSplitOptions.None);
        }

        public void ActOnCells(TextAsset textAsset, Action<string> cellAction, out TextAssetTableResult tableResult)
        {
            ActOnCells(textAsset, cell =>
            {
                cellAction(cell);
                return false;
            }, out tableResult);
        }

        public bool ActOnCells(TextAsset textAsset, Func<int, int, string, bool> cellAction,
            out TextAssetTableResult tableResult)
        {
            tableResult = new TextAssetTableResult();
            var i = 0;
            foreach (var row in SplitTableToRows(textAsset))
            {
                tableResult.Rows++;
                var colCount = 0;

                var j = 0;
                foreach (var col in SplitRowToCells(row))
                {
                    colCount++;
                    if (cellAction(i, j, col))
                    {
                        tableResult.CellsActedOn++;
                    }

                    j++;
                }

                tableResult.Cols = Math.Max(tableResult.Cols, colCount);
                i++;
            }

            return tableResult.CellsActedOn > 0;
        }

        public bool ActOnCells(TextAsset textAsset, Func<string, bool> cellAction, out TextAssetTableResult tableResult)
        {
            bool ActOnCellsWrapper(int i, int j, string cellContents)
            {
                var _ = i;
                _ = j;
                return cellAction(cellContents);
            }

            return ActOnCells(textAsset, ActOnCellsWrapper, out tableResult);
        }

        public string ProcessTable(TextAsset textAsset, Func<string, string> columnTransform,
            out TextAssetTableResult tableResult)
        {
            tableResult = new TextAssetTableResult();
            var colJoin = ColSplitStrings.First();
            var result = new StringBuilder(textAsset.text.Length * 2);
            //foreach (string row in EnumerateRows(textAsset))
            foreach (var row in SplitTableToRows(textAsset))
            {
                tableResult.Rows++;
                var colCount = 0;

                var rowUpdated = false;
                //foreach (string col in EnumerateCols(row))
                foreach (var col in SplitRowToCells(row))
                {
                    colCount++;
                    var newCol = columnTransform(col);
                    if (newCol != null && col != newCol)
                    {
                        tableResult.CellsUpdated++;
                        rowUpdated = true;
                        foreach (var invalid in InvalidColStrings)
                        {
                            newCol = newCol.Replace(invalid, " ");
                        }

                        result.Append(newCol);
                    }
                    else
                    {
                        result.Append(col);
                    }

                    result.Append(colJoin);
                }

                // row complete
                // remove trailing colSplit
                result.Length -= colJoin.Length;
                result.Append(Environment.NewLine);
                if (rowUpdated)
                {
                    tableResult.RowsUpdated++;
                }

                tableResult.Cols = Math.Max(tableResult.Cols, colCount);
            }

            // table complete
            // remove last newline
            result.Length -= Environment.NewLine.Length;

            if (!tableResult.Updated)
            {
                return textAsset.text;
            }

            return result.ToString();
        }

        #endregion
    }
}
