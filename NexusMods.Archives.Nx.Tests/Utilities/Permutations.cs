namespace NexusMods.Archives.Nx.Tests.Utilities;

public static class Permutations
{
    /// <summary>
    ///     Retrieves all permutations of a given collection.
    /// </summary>
    public static IEnumerable<T[]> GetPermutations<T>(this IEnumerable<T> elements)
    {
        var elementList = elements.ToList();
        var indexList = Enumerable.Range(0, elementList.Count).ToArray();

        yield return elementList.ToArray();
        while (true)
        {
            var i = elementList.Count - 1;
            while (i > 0 && indexList[i - 1] >= indexList[i])
                i--;

            if (i <= 0)
                break;

            var j = elementList.Count - 1;
            while (indexList[j] <= indexList[i - 1])
                j--;

            Swap(indexList, i - 1, j);
            j = elementList.Count - 1;
            while (i < j)
            {
                Swap(indexList, i, j);
                i++;
                j--;
            }

            yield return indexList.Select(x => elementList[x]).ToArray();
        }
    }

    private static void Swap<T>(T[] array, int i, int j)
    {
        (array[i], array[j]) = (array[j], array[i]);
    }
}
