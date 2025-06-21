using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NodeCreator : MonoBehaviour
{
    public float maxNeighborDistance;
    public LayerMask obstacles;
    public float nodeDensity = 1f; // Density to control spacing between nodes
    public LayerMask groundLayer;
    [SerializeField] private List<GameObject> groundObjects = new List<GameObject>();
    [SerializeField] private List<Vector2> nodes = new List<Vector2>();
    [SerializeField] private List<Transform> allTransforms = new List<Transform>();
    public List<Node> allNodes = new List<Node>();

    void Start()
    {
        nodes.Clear();
        allNodes.Clear();
        groundObjects.Clear();

        // Populate allTransforms with child transforms
        GetAllChildTransforms(transform, allTransforms);

        // Filter ground objects based on the layer mask
        foreach (Transform child in allTransforms)
        {
            if (((1 << child.gameObject.layer) & groundLayer) != 0)
            {
                groundObjects.Add(child.gameObject);
            }
        }

        // Create nodes for each ground object
        foreach (GameObject ground in groundObjects)
        {
            Collider2D col = ground.GetComponent<Collider2D>();
            if (col != null)
            {
                NodeCreation(col);
            }
        }

        // Convert all Vector2 nodes into Node objects
        foreach (Vector2 nodePosition in nodes)
        {
            allNodes.Add(new Node(nodePosition));
        }

        BakeNeighbors();
        CleanUpNullNodes();
    }

    void GetAllChildTransforms(Transform root, List<Transform> list)
    {
        Stack<Transform> stack = new Stack<Transform>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            list.Add(current);

            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }
    }

    void NodeCreation(Collider2D col)
    {
        if (col == null) return;

        // Define initial left and right nodes based on the collider bounds
        Vector2 leftNode = new Vector2(col.bounds.min.x + 0.5f, col.bounds.max.y + 1f);
        Vector2 rightNode = new Vector2(col.bounds.max.x - 0.5f, col.bounds.max.y + 1f);

        nodes.Add(leftNode);
        nodes.Add(rightNode);

        // Generate nodes between the left and right bounds
        GenerateNodes(leftNode, rightNode);
    }

    void GenerateNodes(Vector2 left, Vector2 right)
    {
        float distance = Vector2.Distance(left, right);

        if (distance > nodeDensity)
        {
            // Calculate the middle point
            Vector2 middleNode = (left + right) / 2;

            // Add the middle node
            nodes.Add(middleNode);

            // Recursively generate nodes
            GenerateNodes(left, middleNode);
            GenerateNodes(middleNode, right);
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize all nodes in the Unity Editor
        Gizmos.color = Color.red;

        foreach (Vector2 node in nodes)
        {
            Gizmos.DrawLine(node, new Vector2(node.x, node.y - 1f));
        }
    }

    public void BakeNeighbors()
    {
        foreach (Node node in allNodes)
        {
            node.neighbors = allNodes.Where(other =>
                other != node &&
                Vector2.Distance(node.Position, other.Position) <= maxNeighborDistance &&
                !Physics2D.Linecast(node.Position, other.Position, obstacles)
            ).ToList();
        }
    }

    public void CleanUpNullNodes()
    {
        allNodes = allNodes.Where(node => node != null).ToList();
    }
}