using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Hotbar : MonoBehaviour
{
    private Inventory inventory;
    private InventoryUI inventoryUI;
    private HotbarSlots[] slots;
    private int selectedSlot = 0;
    private InventoryItem[] hotbarItems;

    void Awake()
    {
        slots = GetComponentsInChildren<HotbarSlots>();
        hotbarItems = new InventoryItem[slots.Length];

        for (int i = 0; i < hotbarItems.Length; i++)
        {
            hotbarItems[i] = new InventoryItem { type = "", amount = 0 };
        }
    }

    void Start()
    {
        inventory = GetComponentInParent<Inventory>();
        inventoryUI = GetComponent<InventoryUI>();
        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInParent<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogError("InventoryUI component not found for Hotbar!");
            }
        }
        UpdateVisual();
        Debug.Log("Hotbar initialized. Initial items:");
        for (int i = 0; i < hotbarItems.Length; i++)
        {
            Debug.Log($"Slot {i}: {hotbarItems[i].type}, amount: {hotbarItems[i].amount}");
        }
    }

    void Update()
    {
        // Выбор слота через клавиши 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);

        // Выброс предметов из выбранного слота на G
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Проверяем, открыт ли инвентарь
            if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            {
                // Проверяем, наведён ли курсор на слот инвентаря или хотбара
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<InventorySlot>() != null || result.gameObject.GetComponent<HotbarSlots>() != null)
                    {
                        // Курсор наведён на слот, пропускаем выброс из выбранного слота хотбара
                        return;
                    }
                }
            }

            // Выбрасываем из выбранного слота хотбара, если инвентарь закрыт или курсор не на слоте
            InventoryItem item = hotbarItems[selectedSlot];
            if (item.type == "" || item.amount <= 0) return;

            // Выбрасываем весь стак, если зажат Shift, иначе 1 единицу
            int amountToDrop = Input.GetKey(KeyCode.LeftShift) ? item.amount : 1;

            DropItem(item.type, amountToDrop);

            int newAmount = item.amount - amountToDrop;
            if (newAmount <= 0)
            {
                hotbarItems[selectedSlot] = new InventoryItem { type = "", amount = 0 };
                Player player = FindObjectOfType<Player>();
                if (player != null && player.IsBottleEquipped() && player.GetEquippedBottleSlot() == selectedSlot)
                {
                    player.UnequipBottle();
                }
            }
            else
            {
                hotbarItems[selectedSlot] = new InventoryItem { type = item.type, amount = newAmount };
            }

            UpdateVisual();
        }
    }

    private void DropItem(string itemType, int amount)
    {
        Player player = FindObjectOfType<Player>();
        if (player == null || player.playerCamera == null)
        {
            Debug.LogWarning("Cannot drop item: Player or playerCamera is null!");
            return;
        }

        Vector3 spawnPosition = player.playerCamera.transform.position + player.playerCamera.transform.forward * 2f;
        spawnPosition.y = player.transform.position.y + 0.5f;

        GameObject resourceObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        resourceObj.name = $"Dropped {itemType}";
        resourceObj.transform.position = spawnPosition;

        Resource resource = resourceObj.AddComponent<Resource>();
        resource.Initialize(itemType, amount, true);

        Debug.Log($"Dropped {amount} of {itemType} at {spawnPosition}");
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            if (player.IsBottleEquipped())
            {
                player.UnequipBottle();
            }

            InventoryItem item = hotbarItems[index];
            if (item.type == "Bottle")
            {
                player.EquipBottle(index, true);
            }
        }

        selectedSlot = index;
        UpdateVisual();
    }

    public InventoryItem GetItemAt(int index)
    {
        if (hotbarItems == null || index < 0 || index >= hotbarItems.Length)
        {
            Debug.LogWarning($"Hotbar: GetItemAt called with invalid index {index} or uninitialized hotbarItems.");
            return new InventoryItem { type = "", amount = 0 };
        }
        Debug.Log($"Hotbar GetItemAt({index}): {hotbarItems[index].type}, amount: {hotbarItems[index].amount}");
        return hotbarItems[index];
    }

    public void SetItemAt(int index, InventoryItem item)
    {
        if (index >= 0 && index < hotbarItems.Length)
        {
            Debug.Log($"Hotbar SetItemAt({index}): {item.type}, amount: {item.amount}");
            hotbarItems[index] = item;
            UpdateVisual();
        }
    }

    public void UpdateVisual()
    {
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.UpdateSlotVisual();
            }
        }
    }

    public int GetSelectedSlot()
    {
        return selectedSlot;
    }
}