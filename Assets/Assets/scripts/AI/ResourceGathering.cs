using System.Collections.Generic;
using UnityEngine;

public class ResourceGathering : MonoBehaviour
{
    public float Timer;
    public Pathfinder pathfinder;
    public SurroundingsDatabase sD;
    public bool foundResource;

    private void Start()
    {
        Timer = Random.Range(1, 30);
    }

    private void Update()
    {
        Timer -= Time.deltaTime;

        if (Timer < 0)
        {
            if (sD.Unimportant.Count > 0)
            {
                Transform t = sD.Unimportant[Random.Range(0, sD.Unimportant.Count)];
                pathfinder.target = t;
                Timer = Random.Range(1, 30);
            }
        }
    }

    public void Gather()
    {
        sD.GetOptimalTransform();
    }
}
