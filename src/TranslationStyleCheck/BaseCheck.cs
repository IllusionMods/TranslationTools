using System;
using System.Text.RegularExpressions;

namespace TranslationStyleCheck
{
    public class BaseCheck
    {
        internal const RegexOptions CheckDefaultRegexOptions = RegexOptions.Compiled;
        public BaseCheck(Regex testRegex, string name, Severity severity, string message, CheckFailMode failMode = CheckFailMode.Match)
        {
            TestRegex = testRegex;
            Severity = severity;
            Name = name;
            Message = message;
            FailMode = failMode;
        }

        public BaseCheck(string testRegexString, string name, Severity severity, string message,
            CheckFailMode failMode = CheckFailMode.Match, RegexOptions regexOptions = CheckDefaultRegexOptions) :
            this(new Regex(testRegexString, regexOptions), name, severity, message, failMode) { }

        public Severity Severity { get; }

        protected CheckFailMode FailMode { get; }
        public string Name { get; }
        public string Message { get; }
        protected Regex TestRegex { get; }


        public virtual bool CheckLine(string input, out CheckResult result)
        {
            result = null;
            var match = TestRegex.IsMatch(input);

            if ((match && FailMode == CheckFailMode.Mismatch) || (!match && FailMode == CheckFailMode.Match))
            {
                return true;
            }

            result = new CheckResult
            {
                Line = input,
                Message = Message,
                Severity = Severity
            };
            return false;
        }

        public bool Check(string input)
        {
            return CheckLine(input, out _);
        }

        public class CheckResult
        {
            public Severity Severity { get; set; }
            public string Message { get; set; }
            public string Line { get; set; }

        }
    }
}
