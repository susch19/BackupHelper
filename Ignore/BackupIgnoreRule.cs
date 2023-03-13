namespace Ignore
{
    using Humanizer.Bytes;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using TimeSpanParserUtil;

    public class BackupDateIgnoreRule : IIgnoreRule
    {
        public bool Negate { get; }
        private TimeSpan span;
        private bool newerThan = false;

        public static string[] AcceptsPattern = new[] { IIgnoreRule.OlderThanLine, IIgnoreRule.NewerThanLine };
        public BackupDateIgnoreRule(string pattern)
        {
            if (pattern.StartsWith(IIgnoreRule.OlderThanLine, System.StringComparison.InvariantCultureIgnoreCase))
            {
                var olderThan = pattern[IIgnoreRule.OlderThanLine.Length..];
                if (TimeSpan.TryParse(olderThan, out var ts))
                    span = ts;
                else if (TimeSpanParser.TryParse(olderThan, out ts))
                    span = ts;
            }
            else if (pattern.StartsWith(IIgnoreRule.NewerThanLine, System.StringComparison.InvariantCultureIgnoreCase))
            {
                var olderThan = pattern[IIgnoreRule.NewerThanLine.Length..];
                if (TimeSpan.TryParse(olderThan, out var ts))
                    span = ts;
                else if (TimeSpanParser.TryParse(olderThan, out ts))
                    span = ts;
                newerThan = true;
            }

        }

        public bool ShouldIgnore<T>(T value)
        {
            if (value is not FileInfo fi)
                return Negate;

            return (newerThan && fi.LastWriteTimeUtc.Add(span) < DateTime.UtcNow)
                || (!newerThan && fi.LastWriteTimeUtc.Add(span) > DateTime.UtcNow);

        }
    }

    public class BackupSizeIgnoreRule : IIgnoreRule
    {
        public bool Negate { get; }

        public long Size { get; }
        private bool smaller = false;

        public static string[] AcceptsPattern = new[] { IIgnoreRule.LargerThanLine, IIgnoreRule.SmallerThanLine };

        public BackupSizeIgnoreRule(string pattern)
        {
            if (pattern.StartsWith(IIgnoreRule.LargerThanLine, System.StringComparison.InvariantCultureIgnoreCase))
            {
                var largerThan = pattern[IIgnoreRule.LargerThanLine.Length..];

                if (long.TryParse(largerThan, out var bytes))
                    Size = bytes;
                else if (ByteSize.TryParse(largerThan, out var byteSize))
                    Size = byteSize.Bits / 8;
            }
            else if (pattern.StartsWith(IIgnoreRule.SmallerThanLine, System.StringComparison.InvariantCultureIgnoreCase))
            {
                var smallerThan = pattern[IIgnoreRule.SmallerThanLine.Length..];

                if (long.TryParse(smallerThan, out var bytes))
                    Size = bytes;
                else if (ByteSize.TryParse(smallerThan, out var byteSize))
                    Size = byteSize.Bits / 8;
                smaller = true;
            }

        }

        public bool ShouldIgnore<T>(T value)
        {
            if (value is not FileInfo fi)
                return Negate;
            return (smaller && fi.Length > Size) || (!smaller && fi.Length < Size);

        }
    }

    public class BackupIgnoreRule : IIgnoreRule
    {
        private static readonly List<Replacer> Replacers = new List<Replacer>
        {
            ReplacerStash.TrailingSpaces,
            ReplacerStash.EscapedSpaces,
            ReplacerStash.LiteralDot,
            ReplacerStash.LiteralPlus,

            // probably not needed
            // ReplacerStash.Metacharacters,
            ReplacerStash.QuestionMark,
            ReplacerStash.NonLeadingSingleStar,
            ReplacerStash.LeadingSingleStar,
            ReplacerStash.LeadingDoubleStar,

            // probably not needed
            // ReplacerStash.MetacharacterSlashAfterLeadingSlash,
            ReplacerStash.MiddleDoubleStar,
            ReplacerStash.LeadingSlash,
            ReplacerStash.TrailingDoubleStar,
            ReplacerStash.OtherDoubleStar,
            ReplacerStash.MiddleSlash,
            ReplacerStash.TrailingSlash,
            ReplacerStash.NoSlash,
            ReplacerStash.NoTrailingSlash,
            ReplacerStash.Ending
        };

        public Regex ParsedRegex { get; }
#if DEBUG
        public string RegexPattern { get; }
#endif
        static string AltDirString = $"{System.IO.Path.AltDirectorySeparatorChar}";
        static string ReplacedDirString = $"[\\{System.IO.Path.DirectorySeparatorChar}|\\{System.IO.Path.AltDirectorySeparatorChar}]";


        public static string[] AcceptsPattern = new[] { IIgnoreRule.RegexLine };

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupIgnoreRule"/> class.
        /// Parses the given pattern as per .gitignore spec.
        /// https://git-scm.com/docs/gitignore#_pattern_format.
        /// </summary>
        /// <param name="pattern">Pattern to parse.</param>
        public BackupIgnoreRule(string pattern)
        {
            // A blank line matches no files, so it can serve as a separator for readability.
            if (string.IsNullOrEmpty(pattern.Trim()))
            {
                return;
            }

            // A line starting with # serves as a comment. Put a backslash ("\") in front of the first hash for patterns that begin with a hash.
            if (pattern.StartsWith("#"))
            {
                if (pattern.StartsWith(IIgnoreRule.RegexLine))
                {
                    pattern = pattern[IIgnoreRule.RegexLine.Length..];
                }
                else
                {
                    return;
                }

            }
            else
            {
                // Account for escaped # and !, remove the leading backslash.
                // Also either a pattern will start with \ or with !
                if (pattern.StartsWith("\\!") || pattern.StartsWith("\\#"))
                {
                    pattern = pattern[1..];
                }
                else if (pattern.StartsWith("!"))
                {
                    Negate = true;
                    pattern = pattern[1..];
                }

                foreach (var replacer in Replacers)
                {
                    pattern = replacer.Invoke(pattern);
                }
            }
            pattern = ReplaceDirChars(pattern);
#if DEBUG
            RegexPattern = pattern;
#endif
            ParsedRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        }
        private static string ReplaceDirChars(string pattern)
        {
            pattern = pattern.Replace(AltDirString, ReplacedDirString);
            return pattern;
        }

        public bool Negate { get; }

        public bool ShouldIgnore<T>(T input)
        {
            if (input is not string inp)
                return Negate;
            return (ParsedRegex != null && ParsedRegex.IsMatch(inp));
        }

    }
}
