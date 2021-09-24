using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IllusionMods
{
    public class MakerCustomDataHandler : ExcelDataHandler
    {
        public MakerCustomDataHandler(TextResourceRedirector plugin) : base(plugin, true)
        {
            WhiteListPaths.Add("abdata/custom");

            var baseHandler = plugin.ExcelDataHandler;
            SupportedColumnNames.AddRange(baseHandler.SupportedColumnNames.ToList());
            foreach (var pth in WhiteListPaths)
            {
               baseHandler.BlackListPaths.Add(pth);
            }


        }


    }
}
