using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Pathfinder : MonoBehaviour
{
    [Header("Target setup")]
    public bool reset;
    public Transform target;
    [SerializeField] private Vector3 savedTargetPosition;
    [SerializeField] private Vector3 savedCurrentPosition;
    [SerializeField] private bool positionSaved;
    [SerializeField] private bool pathCalculated;

    [Header("Movement variables")]
    public float speed;
    [SerializeField] private int currentPathIndex = 0;
    [SerializeField] private bool isMoving = false;
    private Collider2D col;
    private Rigidbody2D rb;
    public float jumpForce;
    public LayerMask ground;
    [SerializeField] private float changePathTimer;
    public bool isJumping;

    [Header("Node management")]
    public NodeCreator creator;
    public float searchRadius;
    [SerializeField] private List<Node> path;
    public LayerMask obstacles;
    private float lastPathCalcTime = 0f;

    private Coroutine moveCoroutine;

    // Start is called before the first execution of Update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        creator = GetComponentInParent<NodeCreator>();
        positionSaved = false;
    }

    // Update is called once per frame
    void Update()
    {
        float targetMoveThreshold = 1.5f; // The threshold distance for the target
        float recalcCooldown = 1f; // Cooldown time before recalculating path
        bool shouldRecalculate = false;

        if (target != null)
        {
            // Check if path is not calculated yet or the target has moved significantly
            shouldRecalculate = !pathCalculated || Vector3.Distance(target.position, savedTargetPosition) > targetMoveThreshold;
        }

        // Only recalculate if enough time has passed since the last calculation
        if (target != null && shouldRecalculate && Time.time - lastPathCalcTime > recalcCooldown)
        {
            reset = false;

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine); // Stop the previous movement
                moveCoroutine = null;
            }

            isMoving = false;

            savedTargetPosition = target.position;  // Save the target's position
            savedCurrentPosition = transform.position;  // Save the current position
            pathCalculated = false;  // Reset path calculation
            path = null;

            path = CalculatePath(savedCurrentPosition, savedTargetPosition);

            lastPathCalcTime = Time.time;  // Update the last calculation time
        }

        if (path != null && path.Count > 0 && !isMoving && !isJumping)
        {
            // Start moving along the path only if not already moving
            if (moveCoroutine == null)
            {
                currentPathIndex = 0;
                moveCoroutine = StartCoroutine(MoveAlongPath());
            }
        }

        if (path != null && path.Count > 0)
        {
            pathCalculated = true;

            #if UNITY_EDITOR
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].Position, path[i + 1].Position, Color.cyan);
            }
            #endif

            if (!isMoving && !isJumping)
            {
                currentPathIndex = 0;
                moveCoroutine = StartCoroutine(MoveAlongPath());
            }
        }
        else if (path == null || path.Count == 0)
        {
            positionSaved = false;
            pathCalculated = false;
        }

        if (path == null || path.Count == 0)
        {
            pathCalculated = false; // Force recalculation if path is invalid
        }

        if (IsGrounded())
        {
            isJumping = false;
        }
    }

    public List<Node> CalculatePath(Vector2 start, Vector2 target)
    {
        Node startNode = FindClosestNode(start);
        Node targetNode = FindClosestNode(target);

        if (startNode == null || targetNode == null)
            return null;

        if (Vector2.Distance(startNode.Position, start) > searchRadius ||
            Vector2.Distance(targetNode.Position, target) > searchRadius)
            return null;

        var openSet = new PriorityQueue();
        var closedSet = new HashSet<Node>();

        // Reset G/H/Connection for all nodes before starting (optional but safer)
        foreach (var node in creator.allNodes)
        {
            node.SetG(0);
            node.SetH(0);
            node.SetConnection(null);
        }

        startNode.SetG(0);
        startNode.SetH(startNode.GetDistance(targetNode));
        openSet.Enqueue(new NodeWithPriority(startNode, startNode.F));

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue().Node;
            closedSet.Add(current);

            if (current == targetNode)
            {
                List<Node> path = new List<Node>();
                while (current != null)
                {
                    path.Add(current);
                    current = current.Connection;
                }
                path.Reverse();
                return path;
            }

            List<Node> neighbors = GetNeighborList(current);
            foreach (var neighbor in neighbors)
            {
                if (closedSet.Contains(neighbor)) continue;

                Vector2 direction = neighbor.Position - current.Position;
                float distance = direction.magnitude;
                RaycastHit2D hit = Physics2D.Raycast(current.Position, direction.normalized, distance, obstacles);

                if (hit.collider != null) continue;

                float tentativeG = current.G + current.GetDistance(neighbor);
                if (!openSet.Contains(neighbor))
                {
                    neighbor.SetG(tentativeG);
                    neighbor.SetH(neighbor.GetDistance(targetNode));
                    neighbor.SetConnection(current);
                    openSet.Enqueue(new NodeWithPriority(neighbor, neighbor.F));
                }
                else if (tentativeG < neighbor.G)
                {
                    neighbor.SetG(tentativeG);
                    neighbor.SetConnection(current);
                    openSet.UpdatePriority(new NodeWithPriority(neighbor, neighbor.F));
                }
            }
        }

        return null;
    }

    public List<Node> GetNeighborList(Node currentNode)
    {
        List<Node> neighbors = new List<Node>();

        Transform plat2 = currentNode.GetPlatformUnderNode(currentNode, obstacles);

        foreach (Node node in currentNode.neighbors)
        {
            if (node == null) continue;

            Transform plat1 = node.GetPlatformUnderNode(node, obstacles);

            if (plat1 == null || plat2 == null)
            {
                continue; // Skip if either platform is null
            }

            if (!plat1.Equals(plat2))
            {
                List<Vector2> possibleVelocities = CalculateJumpVelocity(currentNode.Position, node.Position);

                if (possibleVelocities.Count > 0)
                {
                    neighbors.Add(node);
                }
            }
            else
            {
                neighbors.Add(node);
            }
        }

        return neighbors;
    }

    private IEnumerator MoveAlongPath()
    {
        isMoving = true;

        while (path != null && currentPathIndex < path.Count)
        {
            changePathTimer = 1.5f;
            Vector2 targetPosition = path[currentPathIndex].Position;

            // Move towards the target node
            float nodeTimer = 2f;  // Added fail-safe for node timing
            while (Vector2.Distance(transform.position, targetPosition) > 0.75f && nodeTimer > 0)
            {
                if (IsAllowedToMove(targetPosition))
                {
                    Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                    rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);
                }

                // Decrease timer each frame
                nodeTimer -= Time.deltaTime;

                if ((changePathTimer -= Time.deltaTime) < 0 || path == null)
                {
                    rb.linearVelocityY = 1f;
                    StopMovement();
                    yield break;
                }

                yield return null;
            }

            // If node timer ran out, skip to next node
            if (nodeTimer <= 0)
            {
                Debug.LogWarning("Failed to reach node, skipping...");
                currentPathIndex++;
                continue;
            }

            currentPathIndex++;

            if (path != null && currentPathIndex < path.Count)
            {
                Node currentNode = path[currentPathIndex - 1];
                Node nextNode = path[currentPathIndex];

                yield return new WaitUntil(IsGrounded);

                if (ShouldJumpBetween(currentNode, nextNode))
                {
                    Jump(currentNode.Position, nextNode.Position);
                    yield return null;
                }
            }
        }

        FinishPath();
    }

    private bool IsAllowedToMove(Vector2 targetPosition)
    {
        // Adjusting tolerances to avoid getting stuck at corners or when near ledges
        return IsGrounded() || Mathf.Abs(transform.position.x - targetPosition.x) >= 0.2f || transform.position.y >= targetPosition.y - 0.1f;
    }

    private bool ShouldJumpBetween(Node currentNode, Node nextNode)
    {
        Transform currentPlatform = currentNode.GetPlatformUnderNode(currentNode, ground);
        Transform nextPlatform = nextNode.GetPlatformUnderNode(nextNode, ground);

        if (currentPlatform == null || nextPlatform == null || currentPlatform.Equals(nextPlatform))
            return false;

        float xDistance = Mathf.Abs(currentNode.Position.x - nextNode.Position.x);
        float yDistance = Mathf.Abs(currentNode.Position.y - nextNode.Position.y);
        float gravity = rb.gravityScale * 9.81f;
        float fallTime = Mathf.Sqrt(2 * yDistance / gravity);

        return (xDistance / speed) > fallTime || nextNode.Position.y > currentNode.Position.y;
    }

    private void StopMovement()
    {
        reset = true;
        isMoving = false;
        path = null;
        pathCalculated = false;
    }

    private void FinishPath()
    {
        reset = true;
        rb.linearVelocity = Vector2.zero;
        isMoving = false;
        ClearPath();
    }

    private void Jump(Vector2 from, Vector2 to)
    {
        isJumping = true;
        List<Vector2> velocities = CalculateJumpVelocity(from, to);

        if (velocities.Count > 0)
        {
            Vector2 chosenVelocity = velocities[Random.Range(0, velocities.Count)];
            rb.linearVelocity = chosenVelocity;
        }
    }

    public List<Vector2> CalculateJumpVelocity(Vector2 from, Vector2 to)
    {
        List<Vector2> velocities = new List<Vector2>();
        float gravity = rb.gravityScale * 9.81f;
        float maxSpeed = speed; // Assuming speed is the maximum horizontal speed
        float speedStep = maxSpeed / 4f; // Divide speed into 4 steps for sampling

        for (float s = maxSpeed; s > 0; s -= speedStep)
        {
            float horizontalDistance = Mathf.Abs(to.x - from.x);
            float overallSpeed = Mathf.Sqrt((s * s) - (2 * horizontalDistance));

            float jumpTime = horizontalDistance / overallSpeed; // Time to reach the target x-coordinate

            float minJumpVelocity = (to.y - from.y + (0.5f * gravity * jumpTime * jumpTime)) / jumpTime;

            if (minJumpVelocity > 0 && minJumpVelocity <= jumpForce)
            {
                float jumpAddition = (jumpTime / horizontalDistance) * 15f;
                float jumpStep = (jumpForce - minJumpVelocity + jumpAddition) / 5f; // Divide jump force range into 5 steps

                for (float i = minJumpVelocity + jumpAddition; i <= jumpForce; i += jumpStep)
                {
                    bool collision = false;
                    Vector2 previousPosition = from;

                    for (float t = 0; t <= jumpTime; t += jumpTime / 5f)
                    {
                        float displacementX = from.x + (overallSpeed * t);
                        float displacementY = from.y + (i * t) - (0.5f * gravity * t * t);

                        Vector2 position = new Vector2(displacementX, displacementY);

                        // Cast a ray from the previous position to the current position
                        Vector2 direction = position - previousPosition;
                        float distance = direction.magnitude; // Ensure the ray length matches the displacement
                        RaycastHit2D hit = Physics2D.Raycast(previousPosition, direction.normalized, distance, obstacles);

                        if (hit.collider != null)
                        {
                            collision = true;
                            break;
                        }

                        previousPosition = position; // Update for the next step
                    }

                    if (!collision)
                    {
                        velocities.Add(new Vector2(s, i));
                    }
                }
            }
        }

        return velocities;
    }

    private bool IsGrounded()
    {
        // Raycast down from a little above the AI's current position to check for ground
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(col.bounds.center.x, col.bounds.min.y), Vector2.down, 0.1f, ground);
        return hit.collider != null; // Ensure the ray hits something, meaning the AI is grounded
    }

    private void ClearPath()
    {
        currentPathIndex = 0;
        path = null;
        target = null;
    }

    public Node FindClosestNode(Vector2 position)
    {
        Node closestNode = null;
        float shortestDistance = float.MaxValue;

        foreach (Node node in creator.allNodes)
        {
            if (node == null) continue;

            float distance = Vector2.Distance(node.Position, position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestNode = node;
            }
        }

        return closestNode;
    }
}