using UnityEngine;
using System.Collections.Generic;

public class SurroundingsDatabase : MonoBehaviour
{
    public LayerMask detectableLayer;
    public float horizontalRadius;
    public float verticalRadius;
    public List<Transform> Unimportant;
    public List<Transform> MildlyImportant;
    public List<Transform> Important;
    public List<Transform> VeryImportant;
    public List<Transform> ExtremelyImportant;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Unimportant.Clear();
        MildlyImportant.Clear();
        Important.Clear();
        VeryImportant.Clear();
        ExtremelyImportant.Clear();
    }

    private void FixedUpdate()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.localPosition, new Vector2(horizontalRadius, verticalRadius), 0f, detectableLayer);

        foreach (var collider in colliders)
        {
            Transform transform = collider.transform;

            if (!Unimportant.Contains(transform) && transform != this.transform)
            {
                Unimportant.Add(transform);
            }
        }
    }

    public Transform GetOptimalTransform()
    {
        return null;
    }
}
