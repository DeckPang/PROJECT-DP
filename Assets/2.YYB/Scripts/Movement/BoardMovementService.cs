using System.Collections.Generic;
using UnityEngine;

public static class BoardMovementService
{
    public static int NormalizeIndex(int index, int nodeCount)
    {
        if (nodeCount <= 0)
            return 0;

        int normalized = index % nodeCount;
        if (normalized < 0)
            normalized += nodeCount;

        return normalized;
    }

    public static int GetDestinationIndex(int currentIndex, int steps, int nodeCount)
    {
        return NormalizeIndex(currentIndex + steps, nodeCount);
    }

    public static List<int> GetPathIndices(int currentIndex, int steps, int nodeCount)
    {
        List<int> path = new List<int>();

        if (nodeCount <= 0 || steps == 0)
            return path;

        int direction = steps > 0 ? 1 : -1;
        int moveCount = Mathf.Abs(steps);

        for (int i = 0; i < moveCount; i++)
        {
            currentIndex = NormalizeIndex(currentIndex + direction, nodeCount);
            path.Add(currentIndex);
        }

        return path;
    }
}