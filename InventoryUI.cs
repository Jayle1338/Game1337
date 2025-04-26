using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    [SerializeField] private GameObject slotPrefab;
    public Sprite woodIcon;
    public Sprite ironIcon;
    public Sprite herbsIcon;
    public Sprite bottleIcon;
    public bool IsInventoryOpen { get; private set; }

    void Awake()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("InventoryPanel is not assigned in InventoryUI!");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("SlotPrefab is not assigned in InventoryUI!");
            return;
        }

        // Проверяем дочерние объекты и создаём недостающие слоты
        int childCount = inventoryPanel.transform.childCount;
        if (childCount < 20)
        {
            Debug.LogWarning($"InventoryUI expects 20 slots, but found {childCount}. Creating additional slots...");
            for (int i = childCount; i < 20; i++)
            {
                GameObject slot = Instantiate(slotPrefab, inventoryPanel.transform);
                slot.name = $"Slot {i}";

                // Проверяем, что у созданного слота есть все необходимые компоненты
                if (!slot.GetComponent<Image>())
                {
                    Debug.LogWarning($"Slot {i} created from SlotPrefab does not have an Image component!");
                    slot.AddComponent<Image>();
                }

                InventorySlot slotComponent = slot.GetComponent<InventorySlot>();
                if (!slotComponent)
                {
                    Debug.LogWarning($"Slot {i} created from SlotPrefab does not have an InventorySlot component!");
                    slotComponent = slot.AddComponent<InventorySlot>();
                }

                Transform amountTextTransform = slot.transform.Find("AmountText");
                if (!amountTextTransform || !amountTextTransform.GetComponent<TextMeshProUGUI>())
                {
                    Debug.LogWarning($"Slot {i} created from SlotPrefab does not have a child named 'AmountText' with TextMeshProUGUI!");
                    if (!amountTextTransform)
                    {
                        GameObject amountTextObj = new GameObject("AmountText");
                        amountTextObj.transform.SetParent(slot.transform, false);
                        amountTextTransform = amountTextObj.transform;
                    }
                    if (!amountTextTransform.GetComponent<TextMeshProUGUI>())
                    {
                        amountTextTransform.gameObject.AddComponent<TextMeshProUGUI>();
                    }
                }
            }
        }

        Inventory inventory = GetComponent<Inventory>() ?? GetComponentInParent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Inventory component not found for InventoryUI!");
            return;
        }

        // Инициализация всех слотов
        for (int i = 0; i < 20; i++)
        {
            Transform slotTransform = inventoryPanel.transform.GetChild(i);
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slot.Initialize(inventory, this, i);
            }
            else
            {
                Debug.LogWarning($"Inventory slot {i} does not have InventorySlot component!");
            }
        }

        inventoryPanel.SetActive(false);
        IsInventoryOpen = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        IsInventoryOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(IsInventoryOpen);

        if (IsInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Скрываем текст сообщения о бутылке при открытии инвентаря
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.HideBottleMessage();
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Inventory inventory = GetComponent<Inventory>() ?? GetComponentInParent<Inventory>();
        if (inventory != null)
        {
            inventory.RefreshUI();
        }
    }

    public void UpdateSlot(int slotIndex, string itemType, int amount)
    {
        if (slotIndex < 0 || slotIndex >= inventoryPanel.transform.childCount)
        {
            Debug.LogWarning($"Invalid slot index {slotIndex} in InventoryUI.UpdateSlot");
            return;
        }

        Transform slotTransform = inventoryPanel.transform.GetChild(slotIndex);
        Image itemImage = slotTransform.GetComponent<Image>();
        TextMeshProUGUI amountText = slotTransform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();

        if (itemImage == null)
        {
            Debug.LogWarning($"Image component not found on slot {slotIndex} in InventoryUI!");
            return;
        }

        if (amountText == null)
        {
            Debug.LogWarning($"AmountText (TextMeshProUGUI) not found on slot {slotIndex} in InventoryUI!");
        }

        switch (itemType)
        {
            case "Wood":
                itemImage.sprite = woodIcon;
                break;
            case "Iron":
                itemImage.sprite = ironIcon;
                break;
            case "Herbs":
                itemImage.sprite = herbsIcon;
                break;
            case "Bottle":
                itemImage.sprite = bottleIcon;
                break;
            default:
                Debug.LogWarning($"Unknown item type: {itemType} in slot {slotIndex}");
                itemImage.sprite = null;
                break;
        }

        if (amountText != null)
        {
            if (itemType == "Bottle")
            {
                amountText.text = "";
            }
            else
            {
                amountText.text = amount.ToString();
            }
        }
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventoryPanel.transform.childCount)
        {
            Debug.LogWarning($"Invalid slot index {slotIndex} in InventoryUI.ClearSlot");
            return;
        }

        Transform slotTransform = inventoryPanel.transform.GetChild(slotIndex);
        Image itemImage = slotTransform.GetComponent<Image>();
        TextMeshProUGUI amountText = slotTransform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();

        if (itemImage != null)
        {
            itemImage.sprite = null;
        }

        if (amountText != null)
        {
            amountText.text = "";
        }
    }
}