using UnityEngine;
using System;

public class NodeWithPriority : IComparable<NodeWithPriority>
{
    public Node Node { get; set; }
    public float Priority { get; set; }

    public NodeWithPriority(Node node, float priority)
    {
        Node = node;
        Priority = priority;
    }

    // Compare nodes based on priority (F score)
    public int CompareTo(NodeWithPriority other)
    {
        return Priority.CompareTo(other.Priority);
    }
}
