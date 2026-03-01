using ProductCatalog.API.Interfaces;

namespace ProductCatalog.API.Utilities
{
    /// <summary>
    /// Represents a searchable field with its name, value accessor, and relative weight.
    /// </summary>
    public record SearchField<T>(string FieldName, Func<T, string?> ValueAccessor, double Weight);

    /// <summary>
    /// A scored match result returned by the search engine.
    /// </summary>
    public record SearchResult<T>(T Item, double Score, string MatchedOn);

    /// <summary>
    /// Generic in-memory search engine using only .NET BCL — no external NuGet packages.
    /// </summary>
    public class ProductSearchEngine<T> : IProductSearchEngine<T>
    {
        private readonly List<SearchField<T>> _fields = new();

        // Controls how many character edits are tolerated for fuzzy matches
        private const int FuzzyThresholdBase = 2;

        public IProductSearchEngine<T> AddField(string fieldName, Func<T, string?> accessor, double weight = 1.0)
        {
            _fields.Add(new SearchField<T>(fieldName, accessor, weight));
            return this; // fluent API
        }

        /// <summary>
        /// Execute a search over the provided candidates.
        /// Returns only results with a positive score, sorted by score descending.
        /// </summary>
        public IEnumerable<SearchResult<T>> Search(IEnumerable<T> candidates, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return candidates.Select(c => new SearchResult<T>(c, 1.0, "all"));

            var normalised = query.Trim().ToLowerInvariant();
            var results = new List<SearchResult<T>>();

            foreach (var candidate in candidates)
            {
                double totalScore = 0;
                string? bestField = null;

                foreach (var field in _fields)
                {
                    var fieldValue = field.ValueAccessor(candidate);
                    if (fieldValue is null) continue;

                    var normValue = fieldValue.ToLowerInvariant();
                    double fieldScore = ScoreField(normValue, normalised) * field.Weight;

                    if (fieldScore > 0 && fieldScore > totalScore)
                    {
                        bestField = field.FieldName;
                    }
                    totalScore += fieldScore;
                }

                if (totalScore > 0)
                    results.Add(new SearchResult<T>(candidate, totalScore, bestField ?? "unknown"));
            }

            // Sort descending by score (uses IComparable on double)
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            return results;
        }

        // ---------------- Private scoring helpers --------------------------------------

        private static double ScoreField(string fieldValue, string query)
        {
            // Exact match
            if (fieldValue == query) return 1.0;

            // Prefix match (e.g. "lapt" matches "laptop")
            if (fieldValue.StartsWith(query)) return 0.8;

            // Substring match (e.g. "top" matches "laptop")
            if (fieldValue.Contains(query)) return 0.6;

            // Token-level matching: split field into words and check each
            var tokens = fieldValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token == query) return 0.75;
                if (token.StartsWith(query)) return 0.65;
            }

            // Fuzzy matching — threshold scales with query length for better precision
            int threshold = query.Length <= 3 ? 1 : FuzzyThresholdBase;
            int distance = LevenshteinDistance(fieldValue, query);
            if (distance <= threshold)
            {
                double normalised = 1.0 - (double)distance / Math.Max(fieldValue.Length, query.Length);
                return normalised * 0.5;
            }

            // Check fuzzy against individual tokens too
            foreach (var token in tokens)
            {
                int tokenDist = LevenshteinDistance(token, query);
                if (tokenDist <= threshold)
                {
                    double normalised = 1.0 - (double)tokenDist / Math.Max(token.Length, query.Length);
                    return normalised * 0.45;
                }
            }

            return 0;
        }

        /// <summary>
        /// Classic iterative Levenshtein distance — O(m×n) time, O(min(m,n)) space.
        /// Uses two rows instead of a full matrix to minimise allocations.
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            if (s.Length == 0) return t.Length;
            if (t.Length == 0) return s.Length;

            // Ensuring s is the shorter string to minimise memory
            if (s.Length > t.Length) (s, t) = (t, s);

            int[] previousRow = new int[s.Length + 1];
            int[] currentRow = new int[s.Length + 1];

            for (int i = 0; i <= s.Length; i++) previousRow[i] = i;

            for (int j = 1; j <= t.Length; j++)
            {
                currentRow[0] = j;
                for (int i = 1; i <= s.Length; i++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    currentRow[i] = Math.Min(
                        Math.Min(currentRow[i - 1] + 1,     // insertion
                                 previousRow[i] + 1),        // deletion
                        previousRow[i - 1] + cost);           // substitution
                }
                Array.Copy(currentRow, previousRow, currentRow.Length);
            }

            return previousRow[s.Length];
        }
    }

}
