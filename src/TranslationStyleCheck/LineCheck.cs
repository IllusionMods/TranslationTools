using System.Text.RegularExpressions;

namespace TranslationStyleCheck
{
    public class LineCheck : BaseCheck
    {
        public LineCheck(Regex testRegex, string name, Severity severity, string message, CheckFailMode failMode = CheckFailMode.Match) :
            base(testRegex, name, severity, message, failMode) { }

        public LineCheck(string testRegexString, string name, Severity severity, string message,
            CheckFailMode failMode = CheckFailMode.Match, RegexOptions regexOptions = CheckDefaultRegexOptions) :
            base(testRegexString, name, severity, message, failMode, regexOptions) { }
    }
}
