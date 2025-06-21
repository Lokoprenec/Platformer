using UnityEngine;
using System.Collections.Generic;

public class ItemGenerator : MonoBehaviour
{
    public GameObject itemPool;
    public float spawnTime;
    [SerializeField] private float spawnTimer;
    public string itemType;
    public string itemName;
    [SerializeField] private List<GameObject> stack = new List<GameObject>();
    private GameObject chosenItem;
    private ObjectPooling objectPool;

    void Start()
    {
        spawnTimer = spawnTime;
    }

    void Update()
    {
        if ((spawnTimer -= Time.deltaTime) <= 0)
        {
            spawnTimer = spawnTime;
            GetItemFromPool();
        }
    }

    void GetItemFromPool()
    {
        Transform typeTransform = FindChildByName(itemPool.transform, itemType);
        if (typeTransform == null) return;

        Transform subtypeTransform = FindChildByName(typeTransform, itemName);
        if (subtypeTransform == null) return;

        objectPool = subtypeTransform.GetComponent<ObjectPooling>();
        if (objectPool == null || objectPool.stored == null) return;

        objectPool.stored.RemoveAll(item => item == null);
        if (objectPool.stored.Count == 0) return;

        chosenItem = objectPool.stored[0];
        stack.Add(chosenItem);
        TransferItemToStorage();
    }

    void TransferItemToStorage()
    {
        Invoke(nameof(CheckForTransferSuccess), 0.05f);
    }

    void CheckForTransferSuccess()
    {
        if (!stack.Contains(chosenItem))
        {
            objectPool?.stored.Remove(chosenItem);
        }

        stack.Remove(chosenItem);
        chosenItem = null;
        objectPool = null;
    }

    Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.name == name)
                return child;
        }

        return null;
    }
}
