namespace Ignore;

public interface IIgnoreRule
{
    public const string RegexLine = "#Regex ";
    public const string SmallerThanLine = "#SmallerThan ";
    public const string LargerThanLine = "#LargerThan ";
    public const string OlderThanLine = "#OlderThan ";
    public const string NewerThanLine = "#NewerThan ";

    bool Negate { get; }
    public bool IsMatch<T>(T value);

}