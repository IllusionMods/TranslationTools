using System.Text.RegularExpressions;

namespace TranslationStyleCheck
{
    public class TranslationCheck : BaseCheck
    {
        private readonly char[] _transSplitter = {'='};

        public TranslationCheck(Regex testRegex, string name, Severity severity, string message,
            CheckFailMode failMode = CheckFailMode.Match, bool checkKey = false) :
            base(testRegex, name, severity, message, failMode)
        {
            CheckKey = checkKey;
        }

        public TranslationCheck(string testRegexString, string name, Severity severity, string message,
            CheckFailMode failMode = CheckFailMode.Match, RegexOptions regexOptions = CheckDefaultRegexOptions,
            bool checkKey = false) :
            this(new Regex(testRegexString, regexOptions), name, severity, message, failMode, checkKey) { }

        public bool CheckKey { get; protected set; }

        public override bool CheckLine(string input, out CheckResult result)
        {
            result = null;
            var parts = input.Split(_transSplitter, 2);
            if (parts.Length < 2) return true;
            var check = base.CheckLine(CheckKey ? parts[0] : parts[1], out result);
            if (result != null) result.Line = input;
            return check;
        }
    }
}
