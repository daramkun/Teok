namespace Hazelnut.Teok;

public enum DifferentKind
{
    None,
    Added,
    Removed,
}

// This code based on https://www.codeproject.com/Articles/39184/An-LCS-based-diff-ing-library-in-C
public static class Different
{
    public static IEnumerable<(DifferentKind, T)> Determine<T>(IReadOnlyList<T> x, IReadOnlyList<T> y, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= new DefaultEqualityComparer<T>();

        var preSkip = CalculatePreSkip(x, y, comparer);
        var postSkip = CalculatePostSkip(x, y, comparer, preSkip);
        var matrix = CreateLongestCommonSubsequenceMatrix(x, y, comparer, preSkip, postSkip);
        if (matrix == null)
            return x.Select(item => (DifferentKind.None, item));

        var preSkipped = preSkip > 0
            ? x.Take(preSkip).Select(item => (DifferentKind.None, item))
            : Enumerable.Empty<(DifferentKind, T)>();

        var totalSkip = preSkip + postSkip;
        var results = InternalDetermine(x, y, comparer, matrix, preSkip, x.Count - totalSkip, y.Count - totalSkip);

        var postSkipped = postSkip > 0
            ? x.Take(^postSkip..).Select(item => (DifferentKind.None, item))
            : Enumerable.Empty<(DifferentKind, T)>();

        return preSkipped.Concat(results).Concat(postSkipped);
    }
    
    private static int CalculatePreSkip<T>(IReadOnlyList<T> x, IReadOnlyList<T> y, IEqualityComparer<T> comparer)
    {
        var leftLen = x.Count;
        var rightLen = y.Count;

        var preSkip = 0;
        while (preSkip < leftLen && preSkip < rightLen &&
               comparer.Equals(x[preSkip], y[preSkip]))
            preSkip++;

        return preSkip;
    }

    private static int CalculatePostSkip<T>(IReadOnlyList<T> x, IReadOnlyList<T> y, IEqualityComparer<T> comparer, int preSkip)
    {
        var leftLen = x.Count;
        var rightLen = y.Count;

        var postSkip = 0;
        while (postSkip < leftLen && postSkip < rightLen &&
               postSkip < leftLen - preSkip &&
               comparer.Equals(x[leftLen - postSkip - 1], y[rightLen - postSkip - 1]))
            postSkip++;

        return postSkip;
    }
    
    private static int[,]? CreateLongestCommonSubsequenceMatrix<T>(IReadOnlyList<T> x, IReadOnlyList<T> y, IEqualityComparer<T> comparer, int preSkip, int postSkip)
    {
        var totalSkip = preSkip + postSkip;
        if (totalSkip >= x.Count || totalSkip >= y.Count)
            return null;

        var matrix = new int[x.Count - totalSkip + 1, y.Count - totalSkip + 1];

        for (var i = 1; i <= x.Count - totalSkip; ++i)
        {
            var leftIndex = preSkip + i - 1;

            for (int j = 1, rightIndex = preSkip + 1; j <= y.Count - totalSkip; ++j, ++rightIndex)
            {
                matrix[i, j] = comparer.Equals(x[leftIndex], y[rightIndex - 1])
                    ? matrix[i - 1, j - 1] + 1
                    : Math.Max(matrix[i, j - 1], matrix[i - 1, j]);
            }
        }

        return matrix;
    }

    private static IEnumerable<(DifferentKind, T)> InternalDetermine<T>(IReadOnlyList<T> x, IReadOnlyList<T> y,
        IEqualityComparer<T> comparer,
        int[,] matrix, int preSkip,
        int leftIndex, int rightIndex)
    {
        if (matrix.GetLength(0) == 0 || matrix.GetLength(1) == 0)
            return Enumerable.Empty<(DifferentKind, T)>();

        if (leftIndex > 0 && rightIndex > 0 &&
            comparer.Equals(x[preSkip + leftIndex - 1], y[preSkip + rightIndex - 1]))
        {
            return InternalDetermine(x, y, comparer, matrix, preSkip, leftIndex - 1, rightIndex - 1)
                .Append((DifferentKind.None, x[preSkip + leftIndex - 1]));
        }

        if (rightIndex > 0 &&
            (leftIndex == 0 || matrix[leftIndex, rightIndex - 1] >= matrix[leftIndex - 1, rightIndex]))
        {
            return InternalDetermine(x, y, comparer, matrix, preSkip, leftIndex, rightIndex - 1)
                .Append((DifferentKind.Added, y[preSkip + rightIndex - 1]));
        }

        if (leftIndex > 0 &&
            (rightIndex == 0 || matrix[leftIndex, rightIndex - 1] < matrix[leftIndex - 1, rightIndex]))
        {
            return InternalDetermine(x, y, comparer, matrix, preSkip, leftIndex - 1, rightIndex)
                .Append((DifferentKind.Removed, x[preSkip + leftIndex - 1]));
        }

        return Enumerable.Empty<(DifferentKind, T)>();
    }

    private class DefaultEqualityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y)
        {
            if (x == null && y == null)
                return true;

            if (x != null && y != null)
                return x.Equals(y) || y.Equals(x);

            return false;
        }

        public int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;
    }
}