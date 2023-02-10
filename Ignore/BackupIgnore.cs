namespace Ignore
{
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;




    public class BackupIgnore
    {
        private readonly List<IIgnoreRule> rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupIgnore"/> class.
        /// </summary>
        public BackupIgnore()
        {
            rules = new List<IIgnoreRule>();
            OriginalRules = new List<string>();
        }

        /// <summary>
        /// Gets the list of the original rules passed in to the class ctor.
        /// </summary>
        public List<string> OriginalRules { get; }

        /// <summary>
        /// Adds the given pattern to this <see cref="BackupIgnore"/> instance.
        /// </summary>
        /// <param name="rule">Gitignore style pattern string.</param>
        /// <returns>Current instance of <see cref="BackupIgnore"/>.</returns>
        public BackupIgnore Add(string rule)
        {
            OriginalRules.Add(rule);
            var backupIgnore = new BackupIgnoreRule(rule);
            if (backupIgnore.ParsedRegex is not null)
                rules.Add(backupIgnore);
            if (BackupSizeIgnoreRule.AcceptsPattern.Any(x => rule.StartsWith(x)))
                rules.Add(new BackupSizeIgnoreRule(rule));            
            if (BackupDateIgnoreRule.AcceptsPattern.Any(x => rule.StartsWith(x)))
                rules.Add(new BackupDateIgnoreRule(rule));
            return this;
        }

        /// <summary>
        /// Adds the given pattern list to this <see cref="BackupIgnore"/> instance.
        /// </summary>
        /// <param name="patterns">List of gitignore style pattern strings.</param>
        /// <returns>Current instance of <see cref="BackupIgnore"/>.</returns>
        public BackupIgnore Add(IEnumerable<string> patterns)
        {
            var patternList = patterns.ToList();
            OriginalRules.AddRange(patternList);
            patternList.ForEach(pattern =>
            {
                var backupIgnore = new BackupIgnoreRule(pattern);
                if (backupIgnore.ParsedRegex is not null)
                    rules.Add(backupIgnore);
                if (BackupSizeIgnoreRule.AcceptsPattern.Any(x => pattern.StartsWith(x, System.StringComparison.InvariantCultureIgnoreCase)))
                    rules.Add(new BackupSizeIgnoreRule(pattern));
                if (BackupDateIgnoreRule.AcceptsPattern.Any(x => pattern.StartsWith(x, System.StringComparison.InvariantCultureIgnoreCase)))
                    rules.Add(new BackupDateIgnoreRule(pattern));
            });
            return this;
        }

        /// <summary>
        /// Test whether the input path is ignored as per the rules
        /// specified in the class ctor.
        /// </summary>
        /// <param name="path">File path to consider.</param>
        /// <returns>A boolean indicating if the path is ignored.</returns>
        public bool IsIgnored<T>(T path)
        {
            var ignore = IsPathIgnored(path);

            return ignore;
        }


        /// <summary>
        /// Filters the input paths as per the rules specified in the
        /// class ctor.
        /// </summary>
        /// <param name="paths">List of input file paths.</param>
        /// <returns>List of filtered paths (the paths that are not ignored).</returns>
        public IEnumerable<string> Filter(IEnumerable<string> paths)
        {
            var filteredPaths = new List<string>();
            foreach (var path in paths)
            {
                var ignore = IsPathIgnored(path);
                if (ignore == false)
                {
                    filteredPaths.Add(path);
                }
            }

            return filteredPaths;
        }

        private bool IsPathIgnored<T>(T path)
        {
            var ignore = false;
            foreach (var rule in rules)
            {
                if (rule.Negate)
                {
                    if (ignore && rule.IsMatch(path))
                    {
                        ignore = false;
                    }
                }
                else if (!ignore && rule.IsMatch(path))
                {
                    ignore = true;
                }
            }

            return ignore;
        }
    }
}
