using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace IllusionMods.TranslationTools.Tools.MergeIntoDump
{
    internal class MergeJob
    {
        private FileInfo _sourceFileInfo = null;
        private string _destFilePath = null;
        private FileInfo _destFileInfo = null;
        public string SourceFilePath { get; }

        public FileInfo SourceFileInfo => _sourceFileInfo ?? (_sourceFileInfo = new FileInfo(SourceFilePath));

        public string DestFilePath =>
            _destFilePath ?? (_destFilePath = Path.Combine(
                Program.TranslationRootInfo.FullName,
                SourceFilePath.Substring(Program.CleanDumpRootInfo.FullName.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

        public FileInfo DestFileInfo => _destFileInfo = _destFileInfo ?? new FileInfo(DestFilePath);

        public MergeJob(string sourceFilePath)
        {
            SourceFilePath = sourceFilePath;
        }

        public MergeJob(FileInfo sourceFileInfo)
        {
            _sourceFileInfo = sourceFileInfo;
            SourceFilePath = sourceFileInfo?.FullName;
        }
    }
}
