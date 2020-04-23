namespace IllusionMods
{
    public class TextAssetTableResult
    {
        public int CellsActedOn;
        public int CellsUpdated;
        public int Cols;
        public int Rows;
        public int RowsUpdated;

        public TextAssetTableResult()
        {
            Rows = Cols = RowsUpdated = CellsUpdated = CellsActedOn = 0;
        }

        public bool Updated => RowsUpdated > 0;
    }
}
