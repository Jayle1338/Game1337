using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public string itemType;
    public int itemAmount;

    void Start()
    {
        if (string.IsNullOrEmpty(itemType))
        {
            Debug.LogWarning($"DroppedItem on {gameObject.name} has no Item Type assigned!");
        }
        if (itemAmount <= 0)
        {
            Debug.LogWarning($"DroppedItem on {gameObject.name} has invalid Item Amount: {itemAmount}");
        }
        Debug.Log($"DroppedItem initialized on {gameObject.name}: {itemType} x{itemAmount}");
    }

    public void Initialize(string type, int amount)
    {
        itemType = type;
        itemAmount = amount;
        Debug.Log($"DroppedItem {gameObject.name} initialized with {itemType} x{itemAmount}");
    }

    public string GetItemType()
    {
        return itemType;
    }

    public int GetItemAmount()
    {
        return itemAmount;
    }
}