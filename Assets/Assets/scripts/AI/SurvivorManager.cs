using System.Collections.Generic;
using UnityEngine;

public class SurvivorManager : MonoBehaviour
{
    [Header("Task management")]
    public MonoBehaviour ResourceGathering;
    public List<MonoBehaviour> states;
    [SerializeField] private MonoBehaviour currentState;
    public List<string> objectives;
    [SerializeField] private string currentObjective;

    [Header("Biology management")]
    public float maxHunger;
    public float hunger;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hunger = maxHunger;
    }

    // Update is called once per frame
    void Update()
    {
        hunger -= Time.deltaTime / 10f;
    }
}
