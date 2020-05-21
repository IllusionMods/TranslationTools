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

        public delegate bool CellTransform(int rowIndex, int colIndex, string cellText, out string newCellText);
        public delegate bool CellVisitor(int rowIndex, int colIndex, string cellText);
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
            _rowSplitStrings = tmpList.ToArray();

            // col split strings
            tmpList.Clear();
            tmpList.AddRange(colSplitStrings?.ToArray() ?? new string[0]);
            tmpList.Sort(Comp);
            _colSplitStrings = tmpList.ToArray();

            // invalid col strings
            tmpList.Clear();
            tmpList.AddRange(RowSplitStrings);
            tmpList.AddRange(ColSplitStrings);
            tmpList.Sort(Comp);
            _invalidColStrings = tmpList.ToArray();

            Enabled = ColSplitStrings.Any() && RowSplitStrings.Any();
        }

        protected ManualLogSource Logger => _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(GetType().Name);

        private readonly string[] _rowSplitStrings;
        private readonly string[] _colSplitStrings;
        private readonly string[] _invalidColStrings;
        public bool Enabled { get; }
        public IEnumerable<string> RowSplitStrings => _rowSplitStrings;
        public IEnumerable<string> ColSplitStrings => _colSplitStrings;
        public IEnumerable<string> InvalidColStrings => _invalidColStrings;

        public List<int> HTextColumns { get; } = new List<int>();

        public Encoding TextAssetEncoding { get; }

        public virtual bool TryTranslateTextAsset(ref TextAsset textAsset, CellTransform translator, out string result)
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
            // possible table is only 1 row, but it needs to have at least 1 column break
            if (string.IsNullOrEmpty(table)) return false;

            var row = table.Split(_rowSplitStrings, StringSplitOptions.None).FirstOrDefault();

            return row != null && ColSplitStrings.Any(row.Contains);
        }

        public bool IsTableRow(string row)
        {
            return ColSplitStrings.Any(row.Contains);
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

            return table.Split(_rowSplitStrings, StringSplitOptions.None);
        }

        public IEnumerable<string> SplitRowToCells(string row)
        {
            Debug.Assert(IsTableRow(row), "row does not contain a table row");
            return row.Split(_colSplitStrings, StringSplitOptions.None);
        }

        public string[][] SplitTable(TextAsset textAsset)
        {
            return SplitTable(textAsset.text);
        }

        public string[][] SplitTable(string table)
        {
            if (!IsTable(table))
            {
                throw new ArgumentException("textAsset does not contain a table");
            }

            return table.Split(_rowSplitStrings, StringSplitOptions.None)
                .Select(r => r.Split(_colSplitStrings, StringSplitOptions.None)).ToArray();

        }

        public void ActOnCells(TextAsset textAsset, Action<string> cellAction, out TextAssetTableResult tableResult)
        {
            ActOnCells(textAsset, cell =>
            {
                cellAction(cell);
                return false;
            }, out tableResult);
        }

        public bool ActOnCells(TextAsset textAsset, CellVisitor cellVisitor, out TextAssetTableResult tableResult)
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
                    if (cellVisitor(i, j, col))
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

        public bool ActOnCells(TextAsset textAsset, Func<string, bool> cellVisitor, out TextAssetTableResult tableResult)
        {
            bool CellVisitorWrapper(int i, int j, string cellContents)
            {
                var _ = (i == j);
                return cellVisitor(cellContents);
            }

            return ActOnCells(textAsset, CellVisitorWrapper, out tableResult);
        }
        public string ProcessTable(TextAsset textAsset, CellTransform columnTransform, out TextAssetTableResult tableResult)
        {
            tableResult = new TextAssetTableResult();
            var colJoin = ColSplitStrings.First();
            var result = new StringBuilder(textAsset.text.Length * 2);
            var colBuilder = new StringBuilder();

            bool ColumnTransformWrapper(int rowIndex, int colIndex, string col, out string newCol)
            {
                if (!columnTransform(rowIndex, colIndex, col, out newCol)) return false;

                colBuilder.Length = 0;
                colBuilder.Append(newCol);
                colBuilder = InvalidColStrings.Aggregate(colBuilder,
                    (current, invalid) => current.Replace(invalid, " "));

                newCol = colBuilder.ToString();
                return true;
            }

            var table = SplitTable(textAsset);
            tableResult.Rows = table.Length;
            tableResult.Cols = 0;
            for(var r = 0; r < table.Length; r++)
            {
                var rowUpdated = false;
                tableResult.Cols = Math.Max(tableResult.Cols, table[r].Length);

                for (var c = 0; c < table[r].Length; c++)
                {
                    var col = table[r][c];
                    tableResult.Cols = Math.Max(tableResult.Cols, col.Length);
                    if (ColumnTransformWrapper(r, c, col, out var newCol))
                    {
                        tableResult.CellsUpdated++;
                        rowUpdated = true;
                        result.Append(newCol);
                    } else
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
            }

            // table complete
            // remove last newline
            result.Length -= Environment.NewLine.Length;

            return tableResult.Updated ? result.ToString() : textAsset.text;
        }

#endregion
    }
}
