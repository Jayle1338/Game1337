using UnityEngine;

public class Inventory : MonoBehaviour
{
    private InventoryItem?[] items = new InventoryItem?[20];
    private InventoryUI inventoryUI;

    void Start()
    {
        inventoryUI = GetComponent<InventoryUI>();
        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInChildren<InventoryUI>();
        }

        if (inventoryUI == null)
        {
            Debug.LogWarning("InventoryUI not found for Inventory component!");
        }

        for (int i = 0; i < items.Length; i++)
        {
            items[i] = null;
        }
    }

    public bool AddItem(string itemType, int amount)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].HasValue && items[i].Value.type == itemType && itemType != "Bottle")
            {
                int spaceLeft = 64 - items[i].Value.amount;
                if (spaceLeft >= amount)
                {
                    items[i] = new InventoryItem { type = itemType, amount = items[i].Value.amount + amount };
                    RefreshUI();
                    return true;
                }
                else
                {
                    items[i] = new InventoryItem { type = itemType, amount = 64 };
                    amount -= spaceLeft;
                }
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (!items[i].HasValue || items[i].Value.type == "")
            {
                items[i] = new InventoryItem { type = itemType, amount = amount };
                RefreshUI();
                return true;
            }
        }

        return false;
    }

    public void AddItemAt(int slotIndex, string itemType, int amount)
    {
        if (slotIndex < 0 || slotIndex >= items.Length)
        {
            Debug.LogWarning($"Invalid slot index {slotIndex} in Inventory.AddItemAt");
            return;
        }

        items[slotIndex] = new InventoryItem { type = itemType, amount = amount };
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Length)
        {
            Debug.LogWarning($"Invalid slot index {slotIndex} in Inventory.ClearSlot");
            return;
        }

        items[slotIndex] = null;
    }

    public InventoryItem? GetItemAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Length)
        {
            Debug.LogWarning($"Invalid slot index {slotIndex} in Inventory.GetItemAt");
            return null;
        }

        return items[slotIndex];
    }

    public void RefreshUI()
    {
        if (inventoryUI == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].HasValue && items[i].Value.type != "")
            {
                inventoryUI.UpdateSlot(i, items[i].Value.type, items[i].Value.amount);
            }
            else
            {
                inventoryUI.ClearSlot(i);
            }
        }
    }
}

// Определение структуры InventoryItem
[System.Serializable]
public struct InventoryItem
{
    public string type;
    public int amount;
}