using UnityEngine;
using System.Collections.Generic;

public class NodeComparer : IComparer<Node>
{
    public int Compare(Node a, Node b)
    {
        if (a == null || b == null) return 0;

        int fCompare = a.F.CompareTo(b.F);
        if (fCompare != 0) return fCompare;

        int hCompare = a.H.CompareTo(b.H);
        if (hCompare != 0) return hCompare;

        // Use position as a final tie-breaker to avoid equal nodes in SortedSet
        return a.Position.sqrMagnitude.CompareTo(b.Position.sqrMagnitude);
    }
}
