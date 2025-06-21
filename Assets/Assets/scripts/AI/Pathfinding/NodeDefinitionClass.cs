using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Node
{
    public List<Node> neighbors;
    public Vector2 Position;
    public float G; // Cost from start
    public float H; // Heuristic to target
    public Node Connection; // Parent node (for path reconstruction)

    // Constructor
    public Node(Vector2 position)
    {
        Position = position;
    }

    // F = G + H (used to order nodes in the priority queue)
    public float F => G + H;

    // Implements IComparable to compare nodes by their F value
    public int CompareTo(Node other)
    {
        if (F < other.F) return -1;
        if (F > other.F) return 1;
        return 0;
    }

    public float GetDistance(Node other)
    {
        return Vector2.Distance(Position, other.Position);
    }

    public void SetG(float g)
    {
        G = g;
    }

    public void SetH(float h)
    {
        H = h;
    }

    public void SetConnection(Node connection)
    {
        Connection = connection;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is Node other)
        {
            return Position == other.Position;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }

    public Transform GetPlatformUnderNode(Node node, LayerMask platform)
    {
        RaycastHit2D hit = Physics2D.Raycast(node.Position, Vector2.down, 1.1f, platform); // Ensure the layer mask targets platforms only

        if (hit.collider != null)
        {
            return hit.collider.transform;
        }
        else
        {
            return null;
        }
    }
}
