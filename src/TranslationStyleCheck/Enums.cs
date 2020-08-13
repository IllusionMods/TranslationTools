namespace TranslationStyleCheck
{

    public enum CheckFailMode
    {
        Match,
        Mismatch
    }
    public enum Severity
    {
        Skip,
        PotentialIssue,
        Suggestion,
        Style,
        Fatal
    }
}
