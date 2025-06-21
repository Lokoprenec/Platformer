using UnityEngine;
using System.Collections.Generic;

public class ObjectPooling : MonoBehaviour
{
    public List<GameObject> stored;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Transform t in transform)
        {
            if (t.gameObject.activeSelf == false)
            {
                stored.Add(t.gameObject);
            }
        }
    }
}
