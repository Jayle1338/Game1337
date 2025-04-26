using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Player : MonoBehaviour
{
    public Inventory inventory;
    public Hotbar hotbar;
    public float pickupRange = 3f;
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public Camera playerCamera;
    public InventoryUI inventoryUI;
    public GameObject bottleModel;
    public TextMeshProUGUI pickupPromptText;
    public TextMeshProUGUI bottleMessageText;
    private const int MaxStackSize = 64;

    private int equippedBottleSlot = -1;
    private bool isBottleFromHotbar = false;
    private int bottleWaterLevel = 0;
    private const int maxBottleWaterLevel = 3;
    private const float thirstPerBottleDrink = 10f;
    private int selectedHotbarSlot = -1;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioSource component not found on Player! Please add one.");
            }
        }

        if (pickupSound == null)
        {
            Debug.LogWarning("Pickup sound not assigned in Player!");
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("Player camera not found! Please assign a camera.");
            }
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogWarning("InventoryUI not found! Please assign it in the inspector.");
            }
        }

        if (hotbar == null)
        {
            hotbar = FindObjectOfType<Hotbar>();
            if (hotbar == null)
            {
                Debug.LogWarning("Hotbar not found! Please assign a Hotbar in the inspector.");
            }
        }

        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("Inventory not found! Please assign an Inventory in the inspector.");
            }
        }

        if (bottleModel == null)
        {
            Debug.LogWarning("Bottle model not assigned! Please assign a bottle model in the inspector.");
        }
        else
        {
            bottleModel.SetActive(false);
            Debug.Log($"BottleModel initial state: {bottleModel.activeSelf}");
        }

        if (pickupPromptText == null)
        {
            Debug.LogWarning("PickupPromptText not assigned in Player! Please assign a TextMeshProUGUI component.");
        }
        else
        {
            pickupPromptText.gameObject.SetActive(false);
        }

        if (bottleMessageText == null)
        {
            Debug.LogWarning("BottleMessageText not assigned in Player! Please assign a TextMeshProUGUI component.");
        }
        else
        {
            bottleMessageText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        UpdatePickupPrompt();

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbarSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbarSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbarSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectHotbarSlot(3);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            {
                Debug.Log("Cannot pick up items while inventory is open");
                return;
            }

            PickupItem();
        }

        // Набор воды в бутылку на Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log($"Q pressed, equippedBottleSlot: {equippedBottleSlot}");
            if (equippedBottleSlot != -1)
            {
                WaterSource waterSource = FindObjectOfType<WaterSource>();
                if (waterSource == null)
                {
                    Debug.LogWarning("WaterSource not found in the scene!");
                    return;
                }

                float distance = Vector3.Distance(transform.position, waterSource.transform.position);
                if (distance <= waterSource.interactRange)
                {
                    Debug.Log($"Player is within range of WaterSource ({distance} <= {waterSource.interactRange})");
                    FillBottle();
                }
                else
                {
                    Debug.Log($"Player is too far from WaterSource ({distance} > {waterSource.interactRange})");
                }
            }
            else
            {
                Debug.Log("No bottle equipped to fill!");
            }
        }

        // Питьё на F
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            {
                Debug.Log("Cannot drink while inventory is open");
                return;
            }

            if (equippedBottleSlot != -1)
            {
                Debug.Log("Drinking from bottle since bottle is equipped");
                DrinkFromBottle();
            }
            else
            {
                WaterSource waterSource = FindObjectOfType<WaterSource>();
                if (waterSource != null)
                {
                    float distance = Vector3.Distance(transform.position, waterSource.transform.position);
                    if (distance <= waterSource.interactRange)
                    {
                        SurvivalStats survivalStats = GetComponent<SurvivalStats>();
                        if (survivalStats != null)
                        {
                            survivalStats.RestoreThirst(waterSource.thirstRestoreAmount);
                            Debug.Log($"Drank directly from water source, thirst restored by {waterSource.thirstRestoreAmount}");
                        }
                    }
                }
            }
        }

        // Использование предмета из выбранного слота хотбара на R
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            {
                Debug.Log("Cannot use item while inventory is open");
                return;
            }

            UseItemFromHotbar();
        }
    }

    void UseItemFromHotbar()
    {
        if (hotbar == null)
        {
            Debug.LogWarning("Hotbar is not assigned!");
            return;
        }

        int selectedSlot = hotbar.GetSelectedSlot();
        InventoryItem item = hotbar.GetItemAt(selectedSlot);

        if (item.type == "" || item.amount <= 0)
        {
            Debug.Log("No item in selected hotbar slot to use!");
            return;
        }

        SurvivalStats survivalStats = GetComponent<SurvivalStats>();
        if (survivalStats == null)
        {
            Debug.LogWarning("SurvivalStats component not found on Player!");
            return;
        }

        // Проверяем, можно ли использовать предмет
        if (item.type == "Herbs" || item.type == "Food")
        {
            survivalStats.ConsumeItem(item.type);

            // Уменьшаем количество предметов в слоте
            int newAmount = item.amount - 1;
            if (newAmount <= 0)
            {
                hotbar.SetItemAt(selectedSlot, new InventoryItem { type = "", amount = 0 });
            }
            else
            {
                hotbar.SetItemAt(selectedSlot, new InventoryItem { type = item.type, amount = newAmount });
            }

            Debug.Log($"Used {item.type} from hotbar slot {selectedSlot}, remaining amount: {newAmount}");
        }
        else
        {
            Debug.Log($"Cannot use item of type {item.type}!");
        }
    }

    void SelectHotbarSlot(int slotIndex)
    {
        if (hotbar == null)
        {
            Debug.LogWarning("Hotbar is not assigned!");
            return;
        }

        selectedHotbarSlot = slotIndex;
        InventoryItem item = hotbar.GetItemAt(slotIndex);
        Debug.Log($"Selecting slot {slotIndex}, item: {item.type}, amount: {item.amount}");

        // Проверяем, была ли бутылка экипирована ранее
        if (equippedBottleSlot != -1 && equippedBottleSlot != slotIndex)
        {
            UnequipBottle();
        }

        // Экипируем бутылку, если текущий слот содержит бутылку
        if (item.type == "Bottle")
        {
            EquipBottle(slotIndex, true);
        }

        // Обновляем визуальное выделение слотов
        for (int i = 0; i < 4; i++)
        {
            HotbarSlots slot = hotbar.transform.GetChild(i).GetComponent<HotbarSlots>();
            if (slot != null)
            {
                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null)
                {
                    slotImage.color = (i == slotIndex) ? new Color(1f, 1f, 0f, 1f) : new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
    }

    void UpdatePickupPrompt()
    {
        if (playerCamera == null || pickupPromptText == null)
        {
            Debug.LogWarning("Cannot update pickup prompt: playerCamera or pickupPromptText is null!");
            return;
        }

        bool isInventoryOpen = inventoryUI != null && inventoryUI.IsInventoryOpen;
        if (isInventoryOpen)
        {
            pickupPromptText.gameObject.SetActive(false);
            return;
        }

        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, pickupRange, layerMask))
        {
            Resource targetResource = hit.collider.GetComponent<Resource>();
            if (targetResource != null)
            {
                pickupPromptText.gameObject.SetActive(true);
                return;
            }
        }

        pickupPromptText.gameObject.SetActive(false);
    }

    void PickupItem()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("Cannot pick up item: Player camera is not assigned!");
            return;
        }

        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, pickupRange, layerMask))
        {
            Resource targetResource = hit.collider.GetComponent<Resource>();
            if (targetResource != null)
            {
                if (targetResource.IsDroppedItem())
                {
                    string itemType = targetResource.GetResourceType();
                    int itemAmount = targetResource.GetAmount();

                    int remainingAmount = itemAmount;

                    if (hotbar != null)
                    {
                        if (itemType == "Bottle")
                        {
                            for (int i = 0; i < 4 && remainingAmount > 0; i++)
                            {
                                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                                if (hotbarItem.type == "")
                                {
                                    hotbarItem.type = itemType;
                                    hotbarItem.amount = 1;
                                    hotbar.SetItemAt(i, hotbarItem);
                                    remainingAmount--;
                                    Debug.Log($"Added 1 Bottle to new hotbar slot {i}, remaining: {remainingAmount}");

                                    // Если бутылка добавлена в текущий выбранный слот, экипируем её
                                    if (i == selectedHotbarSlot)
                                    {
                                        Debug.Log($"Bottle added to selected slot {i}, equipping bottle automatically.");
                                        EquipBottle(i, true);
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 4 && remainingAmount > 0; i++)
                            {
                                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                                if (hotbarItem.type == itemType && hotbarItem.amount > 0)
                                {
                                    int spaceLeft = MaxStackSize - hotbarItem.amount;
                                    if (spaceLeft > 0)
                                    {
                                        int amountToAdd = Mathf.Min(remainingAmount, spaceLeft);
                                        hotbarItem.amount += amountToAdd;
                                        hotbar.SetItemAt(i, hotbarItem);
                                        remainingAmount -= amountToAdd;
                                        Debug.Log($"Added {amountToAdd} of {itemType} to hotbar slot {i}, remaining: {remainingAmount}");
                                    }
                                }
                            }

                            for (int i = 0; i < 4 && remainingAmount > 0; i++)
                            {
                                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                                if (hotbarItem.type == "")
                                {
                                    int amountToAdd = Mathf.Min(remainingAmount, MaxStackSize);
                                    hotbarItem.type = itemType;
                                    hotbarItem.amount = amountToAdd;
                                    hotbar.SetItemAt(i, hotbarItem);
                                    remainingAmount -= amountToAdd;
                                    Debug.Log($"Added {amountToAdd} of {itemType} to new hotbar slot {i}, remaining: {remainingAmount}");
                                }
                            }
                        }
                    }

                    if (remainingAmount > 0 && inventory != null)
                    {
                        if (inventory.AddItem(itemType, remainingAmount))
                        {
                            Debug.Log($"Added {remainingAmount} of {itemType} to inventory");
                        }
                        else
                        {
                            Debug.Log($"Failed to add {remainingAmount} of {itemType} to inventory");
                            return;
                        }
                    }

                    Destroy(targetResource.gameObject);

                    if (audioSource != null && pickupSound != null)
                    {
                        audioSource.PlayOneShot(pickupSound);
                    }
                }
                else
                {
                    string itemType = targetResource.GetResourceType();
                    int itemAmount = targetResource.GetAmount();

                    int remainingAmount = itemAmount;

                    if (hotbar != null)
                    {
                        if (itemType == "Bottle")
                        {
                            for (int i = 0; i < 4 && remainingAmount > 0; i++)
                            {
                                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                                if (hotbarItem.type == "")
                                {
                                    hotbarItem.type = itemType;
                                    hotbarItem.amount = 1;
                                    hotbar.SetItemAt(i, hotbarItem);
                                    remainingAmount--;
                                    Debug.Log($"Added 1 Bottle to new hotbar slot {i}, remaining: {remainingAmount}");

                                    // Если бутылка добавлена в текущий выбранный слот, экипируем её
                                    if (i == selectedHotbarSlot)
                                    {
                                        Debug.Log($"Bottle added to selected slot {i}, equipping bottle automatically.");
                                        EquipBottle(i, true);
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 4 && remainingAmount > 0; i++)
                            {
                                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                                if (hotbarItem.type == itemType && hotbarItem.amount > 0)
                                {
                                    int spaceLeft = MaxStackSize - hotbarItem.amount;
                                    if (spaceLeft > 0)
                                    {
                                        int amountToAdd = Mathf.Min(remainingAmount, spaceLeft);
                                        hotbarItem.amount += amountToAdd;
                                        hotbar.SetItemAt(i, hotbarItem);
                                        remainingAmount -= amountToAdd;
                                        Debug.Log($"Added {amountToAdd} of {itemType} to hotbar slot {i}, remaining: {remainingAmount}");
                                    }
                                }
                            }

                            for (int i = 0; i < 4 && remainingAmount > 0; i++)
                            {
                                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                                if (hotbarItem.type == "")
                                {
                                    int amountToAdd = Mathf.Min(remainingAmount, MaxStackSize);
                                    hotbarItem.type = itemType;
                                    hotbarItem.amount = amountToAdd;
                                    hotbar.SetItemAt(i, hotbarItem);
                                    remainingAmount -= amountToAdd;
                                    Debug.Log($"Added {amountToAdd} of {itemType} to new hotbar slot {i}, remaining: {remainingAmount}");
                                }
                            }
                        }
                    }

                    if (remainingAmount > 0 && inventory != null)
                    {
                        if (inventory.AddItem(itemType, remainingAmount))
                        {
                            Debug.Log($"Added {remainingAmount} of {itemType} to inventory");
                        }
                        else
                        {
                            Debug.Log($"Failed to add {remainingAmount} of {itemType} to inventory");
                            return;
                        }
                    }

                    targetResource.Harvest();
                }
            }
        }
    }

    public bool IsBottleEquipped()
    {
        return equippedBottleSlot != -1;
    }

    public int GetEquippedBottleSlot()
    {
        return equippedBottleSlot;
    }

    public void EquipBottle(int slotIndex, bool fromHotbar = false)
    {
        InventoryItem item;
        if (fromHotbar)
        {
            if (hotbar == null)
            {
                Debug.LogWarning("Hotbar is null in EquipBottle!");
                return;
            }
            item = hotbar.GetItemAt(slotIndex);
        }
        else
        {
            if (inventory == null)
            {
                Debug.LogWarning("Inventory is null in EquipBottle!");
                return;
            }
            InventoryItem? nullableItem = inventory.GetItemAt(slotIndex);
            if (!nullableItem.HasValue)
            {
                Debug.LogWarning($"No item found at inventory slot {slotIndex}");
                return;
            }
            item = nullableItem.Value;
        }

        if (item.type == "Bottle")
        {
            Debug.Log($"Equipping bottle from slot {slotIndex}, fromHotbar: {fromHotbar}");
            equippedBottleSlot = slotIndex;
            isBottleFromHotbar = fromHotbar;
            if (bottleModel != null)
            {
                Debug.Log("Activating bottle model");
                bottleModel.SetActive(true);
                Debug.Log($"BottleModel active state after activation: {bottleModel.activeSelf}");
            }
            else
            {
                Debug.LogWarning("Cannot activate bottle model: bottleModel is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Item at slot {slotIndex} is not a bottle! Found: {item.type}");
        }
    }

    public void UnequipBottle()
    {
        Debug.Log("Unequipping bottle");
        equippedBottleSlot = -1;
        isBottleFromHotbar = false;
        bottleWaterLevel = 0;
        if (bottleModel != null)
        {
            Debug.Log("Deactivating bottle model");
            bottleModel.SetActive(false);
            Debug.Log($"BottleModel active state after deactivation: {bottleModel.activeSelf}");
        }
    }

    public void FillBottle()
    {
        if (inventoryUI != null && inventoryUI.IsInventoryOpen)
        {
            Debug.Log("Cannot fill bottle while inventory is open");
            return;
        }

        if (equippedBottleSlot != -1)
        {
            Debug.Log($"Attempting to fill bottle. Current water level: {bottleWaterLevel}/{maxBottleWaterLevel}");
            if (bottleWaterLevel < maxBottleWaterLevel)
            {
                bottleWaterLevel++;
                Debug.Log($"Bottle filled! Water level: {bottleWaterLevel}/{maxBottleWaterLevel}");
            }
            else
            {
                Debug.Log("Bottle is already full, showing message");
                ShowBottleMessage("Бутылка полная");
            }
        }
        else
        {
            Debug.LogWarning("Cannot fill bottle: No bottle equipped!");
        }
    }

    public void DrinkFromBottle()
    {
        if (inventoryUI != null && inventoryUI.IsInventoryOpen)
        {
            Debug.Log("Cannot drink from bottle while inventory is open");
            return;
        }

        Debug.Log($"Attempting to drink from bottle. Current water level: {bottleWaterLevel}");
        if (bottleWaterLevel > 0)
        {
            bottleWaterLevel--;
            SurvivalStats survivalStats = GetComponent<SurvivalStats>();
            if (survivalStats != null)
            {
                survivalStats.RestoreThirst(thirstPerBottleDrink);
            }

            if (bottleWaterLevel == 0)
            {
                Debug.Log("Bottle is now empty, showing message");
                ShowBottleMessage("Бутылка пустая");
            }
        }
        else
        {
            Debug.Log("Bottle is empty, showing message");
            ShowBottleMessage("Бутылка пустая");
        }
    }

    private void ShowBottleMessage(string message)
    {
        if (inventoryUI != null && inventoryUI.IsInventoryOpen)
        {
            Debug.Log("Cannot show bottle message while inventory is open");
            return;
        }

        if (bottleMessageText != null)
        {
            Debug.Log($"Showing bottle message: {message}");
            StopAllCoroutines();
            bottleMessageText.text = message;
            bottleMessageText.gameObject.SetActive(true);
            StartCoroutine(HideBottleMessageAfterDelay(2f));
        }
        else
        {
            Debug.LogWarning("Cannot show bottle message: bottleMessageText is null!");
        }
    }

    private IEnumerator HideBottleMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bottleMessageText != null)
        {
            Debug.Log("Hiding bottle message");
            bottleMessageText.gameObject.SetActive(false);
        }
    }

    public void HideBottleMessage()
    {
        StopAllCoroutines();
        if (bottleMessageText != null)
        {
            Debug.Log("Hiding bottle message immediately");
            bottleMessageText.gameObject.SetActive(false);
        }
    }
}