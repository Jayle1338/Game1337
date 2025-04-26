using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Inventory inventory;
    private InventoryUI inventoryUI;
    private Hotbar hotbar;
    public int slotIndex;
    private Image slotImage;
    private TextMeshProUGUI amountText;
    private Color normalColor = new Color(1f, 1f, 1f, 0.3f);
    private Color hoverColor = new Color(1f, 1f, 0f, 0.5f);

    private GameObject draggedItem;
    private CanvasGroup canvasGroup;
    private bool dragStarted;
    private InventoryItem? draggedItemData;
    private const float holdThreshold = 0.3f;
    private float clickTime;
    private bool isHolding;
    private const int MaxStackSize = 64;
    private bool isSplitting;

    public int SlotIndex => slotIndex;

    public void Initialize(Inventory inv, InventoryUI ui, int index)
    {
        inventory = inv;
        inventoryUI = ui;
        hotbar = ui.GetComponent<Hotbar>();
        slotIndex = index;
        slotImage = GetComponent<Image>();
        amountText = transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
        if (amountText == null)
        {
            Debug.LogWarning($"AmountText (TextMeshProUGUI) not found on Inventory slot {slotIndex}!");
        }
        UpdateSlotVisual();
    }

    public void UpdateSlotVisual()
    {
        InventoryItem? item = inventory.GetItemAt(slotIndex);
        if (item.HasValue && item.Value.type != "")
        {
            Sprite sprite = null;
            switch (item.Value.type)
            {
                case "Wood":
                    sprite = inventoryUI.woodIcon;
                    break;
                case "Iron":
                    sprite = inventoryUI.ironIcon;
                    break;
                case "Herbs":
                    sprite = inventoryUI.herbsIcon;
                    break;
                case "Bottle":
                    sprite = inventoryUI.bottleIcon;
                    break;
            }

            if (sprite != null)
            {
                slotImage.sprite = sprite;
                slotImage.color = normalColor;
                if (amountText != null)
                {
                    if (item.Value.type == "Bottle")
                    {
                        amountText.text = "";
                    }
                    else
                    {
                        amountText.text = item.Value.amount.ToString();
                    }
                }
            }
        }
        else
        {
            slotImage.sprite = null;
            slotImage.color = normalColor;
            if (amountText != null)
            {
                amountText.text = "";
            }
        }
    }

    void Update()
    {
        if (dragStarted && draggedItem != null)
        {
            draggedItem.transform.position = Input.mousePosition;

            // Механика: раздача предметов по 1 при удержании ПКМ и нажатии ЛКМ
            // или при удержании ЛКМ и нажатии ПКМ
            if ((Input.GetMouseButton(1) && Input.GetMouseButtonDown(0)) || (Input.GetMouseButton(0) && Input.GetMouseButtonDown(1)))
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                InventorySlot targetInventorySlot = null;
                HotbarSlots targetHotbarSlot = null;

                foreach (var result in results)
                {
                    targetInventorySlot = result.gameObject.GetComponent<InventorySlot>();
                    targetHotbarSlot = result.gameObject.GetComponent<HotbarSlots>();
                    if (targetInventorySlot != null || targetHotbarSlot != null) break;
                }

                if (targetInventorySlot != null && draggedItemData.HasValue && draggedItemData.Value.type != "Bottle")
                {
                    InventoryItem? targetItem = inventory.GetItemAt(targetInventorySlot.slotIndex);
                    bool canAddItem = false;
                    int currentAmountInSlot = 0;

                    if (!targetItem.HasValue || targetItem.Value.type == "")
                    {
                        canAddItem = true; // Слот пустой
                    }
                    else if (targetItem.Value.type == draggedItemData.Value.type && targetItem.Value.amount < MaxStackSize)
                    {
                        canAddItem = true; // Слот содержит тот же тип предмета, и стак не полный
                        currentAmountInSlot = targetItem.Value.amount;
                    }

                    if (canAddItem)
                    {
                        // Добавляем 1 единицу в слот
                        inventory.AddItemAt(targetInventorySlot.slotIndex, draggedItemData.Value.type, currentAmountInSlot + 1);
                        targetInventorySlot.UpdateSlotVisual();

                        // Уменьшаем количество переносимых предметов
                        int newAmount = draggedItemData.Value.amount - 1;
                        draggedItemData = new InventoryItem { type = draggedItemData.Value.type, amount = newAmount };

                        // Обновляем текст на перетаскиваемом объекте
                        TextMeshProUGUI draggedAmountText = draggedItem.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
                        if (draggedAmountText != null)
                        {
                            draggedAmountText.text = newAmount.ToString();
                        }

                        // Если предметов больше не осталось, завершаем перетаскивание
                        if (newAmount <= 0)
                        {
                            EndDrag();
                        }
                    }
                }
                else if (targetHotbarSlot != null && draggedItemData.HasValue && draggedItemData.Value.type != "Bottle")
                {
                    InventoryItem targetItem = hotbar.GetItemAt(targetHotbarSlot.transform.GetSiblingIndex());
                    bool canAddItem = false;
                    int currentAmountInSlot = 0;

                    if (targetItem.type == "")
                    {
                        canAddItem = true; // Слот пустой
                    }
                    else if (targetItem.type == draggedItemData.Value.type && targetItem.amount < MaxStackSize)
                    {
                        canAddItem = true; // Слот содержит тот же тип предмета, и стак не полный
                        currentAmountInSlot = targetItem.amount;
                    }

                    if (canAddItem)
                    {
                        // Добавляем 1 единицу в слот
                        hotbar.SetItemAt(targetHotbarSlot.transform.GetSiblingIndex(), new InventoryItem { type = draggedItemData.Value.type, amount = currentAmountInSlot + 1 });
                        targetHotbarSlot.UpdateSlotVisual();

                        // Уменьшаем количество переносимых предметов
                        int newAmount = draggedItemData.Value.amount - 1;
                        draggedItemData = new InventoryItem { type = draggedItemData.Value.type, amount = newAmount };

                        // Обновляем текст на перетаскиваемом объекте
                        TextMeshProUGUI draggedAmountText = draggedItem.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
                        if (draggedAmountText != null)
                        {
                            draggedAmountText.text = newAmount.ToString();
                        }

                        // Если предметов больше не осталось, завершаем перетаскивание
                        if (newAmount <= 0)
                        {
                            EndDrag();
                        }
                    }
                }
            }
        }

        // Выброс предметов на клавишу G
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Проверяем, наведён ли курсор на этот слот
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            bool isHovered = false;
            foreach (var result in results)
            {
                if (result.gameObject == gameObject)
                {
                    isHovered = true;
                    break;
                }
            }

            if (isHovered)
            {
                InventoryItem? item = inventory.GetItemAt(slotIndex);
                if (!item.HasValue || item.Value.type == "" || item.Value.amount <= 0) return;

                // Выбрасываем весь стак, если зажат Shift, иначе 1 единицу
                int amountToDrop = Input.GetKey(KeyCode.LeftShift) ? item.Value.amount : 1;

                DropItem(item.Value.type, amountToDrop);

                int newAmount = item.Value.amount - amountToDrop;
                if (newAmount <= 0)
                {
                    inventory.ClearSlot(slotIndex);
                }
                else
                {
                    inventory.AddItemAt(slotIndex, item.Value.type, newAmount);
                }

                inventory.RefreshUI();
                UpdateSlotVisual();
            }
        }
    }

    private void EndDrag()
    {
        if (draggedItem == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        InventorySlot targetSlot = null;
        HotbarSlots hotbarSlot = null;

        foreach (var result in results)
        {
            targetSlot = result.gameObject.GetComponent<InventorySlot>();
            hotbarSlot = result.gameObject.GetComponent<HotbarSlots>();
            if (targetSlot != null || hotbarSlot != null) break;
        }

        if (targetSlot == null && hotbarSlot == null)
        {
            Debug.Log($"Drag cancelled for slot {slotIndex}, restoring item");
            if (draggedItemData.HasValue)
            {
                InventoryItem? currentItem = inventory.GetItemAt(slotIndex);
                if (currentItem.HasValue && currentItem.Value.type == draggedItemData.Value.type && currentItem.Value.type != "Bottle")
                {
                    int totalAmount = currentItem.Value.amount + draggedItemData.Value.amount;
                    if (totalAmount <= MaxStackSize)
                    {
                        inventory.AddItemAt(slotIndex, currentItem.Value.type, totalAmount);
                    }
                    else
                    {
                        inventory.AddItemAt(slotIndex, currentItem.Value.type, MaxStackSize);
                        int remainingAmount = totalAmount - MaxStackSize;
                        for (int i = 0; i < 20; i++)
                        {
                            if (i != slotIndex)
                            {
                                InventoryItem? otherSlot = inventory.GetItemAt(i);
                                if (!otherSlot.HasValue || otherSlot.Value.type == "")
                                {
                                    inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    inventory.AddItemAt(slotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                }
            }
        }
        else if (targetSlot == this || hotbarSlot == this) // Возврат в тот же слот
        {
            if (draggedItemData.HasValue)
            {
                if (targetSlot == this) // Перетаскивание начато из инвентаря
                {
                    InventoryItem? currentItem = inventory.GetItemAt(slotIndex);
                    if (currentItem.HasValue && currentItem.Value.type == draggedItemData.Value.type && currentItem.Value.type != "Bottle")
                    {
                        int totalAmount = currentItem.Value.amount + draggedItemData.Value.amount;
                        if (totalAmount <= MaxStackSize)
                        {
                            inventory.AddItemAt(slotIndex, currentItem.Value.type, totalAmount);
                        }
                        else
                        {
                            inventory.AddItemAt(slotIndex, currentItem.Value.type, MaxStackSize);
                            int remainingAmount = totalAmount - MaxStackSize;
                            for (int i = 0; i < 20; i++)
                            {
                                if (i != slotIndex)
                                {
                                    InventoryItem? otherSlot = inventory.GetItemAt(i);
                                    if (!otherSlot.HasValue || otherSlot.Value.type == "")
                                    {
                                        inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        inventory.AddItemAt(slotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                    }
                }
                else if (hotbarSlot == this) // Перетаскивание начато из хотбара
                {
                    InventoryItem currentItem = hotbar.GetItemAt(slotIndex);
                    if (currentItem.type == draggedItemData.Value.type && currentItem.type != "Bottle")
                    {
                        int totalAmount = currentItem.amount + draggedItemData.Value.amount;
                        if (totalAmount <= MaxStackSize)
                        {
                            hotbar.SetItemAt(slotIndex, new InventoryItem { type = currentItem.type, amount = totalAmount });
                        }
                        else
                        {
                            hotbar.SetItemAt(slotIndex, new InventoryItem { type = currentItem.type, amount = MaxStackSize });
                            int remainingAmount = totalAmount - MaxStackSize;
                            for (int i = 0; i < 20; i++)
                            {
                                if (i != slotIndex)
                                {
                                    InventoryItem? otherSlot = inventory.GetItemAt(i);
                                    if (!otherSlot.HasValue || otherSlot.Value.type == "")
                                    {
                                        inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        hotbar.SetItemAt(slotIndex, draggedItemData.Value);
                    }
                }
            }
        }
        else if (targetSlot != null && targetSlot != this)
        {
            InventoryItem? targetItem = inventory.GetItemAt(targetSlot.slotIndex);

            if (targetItem.HasValue && targetItem.Value.type == draggedItemData.Value.type && targetItem.Value.type != "Bottle")
            {
                int totalAmount = targetItem.Value.amount + draggedItemData.Value.amount;
                if (totalAmount <= MaxStackSize)
                {
                    inventory.AddItemAt(targetSlot.slotIndex, targetItem.Value.type, totalAmount);
                    if (!isSplitting)
                    {
                        inventory.ClearSlot(slotIndex);
                    }
                }
                else
                {
                    inventory.AddItemAt(targetSlot.slotIndex, targetItem.Value.type, MaxStackSize);
                    inventory.AddItemAt(slotIndex, draggedItemData.Value.type, totalAmount - MaxStackSize);
                }
            }
            else if (!targetItem.HasValue || targetItem.Value.type == "")
            {
                inventory.AddItemAt(targetSlot.slotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                if (!isSplitting)
                {
                    inventory.ClearSlot(slotIndex);
                }
            }
            else if (isSplitting && targetItem.HasValue && targetItem.Value.type != draggedItemData.Value.type)
            {
                // Если это разделение стака и в целевом слоте другой тип предмета, возвращаем предметы в исходный слот
                InventoryItem? currentItem = inventory.GetItemAt(slotIndex);
                if (currentItem.HasValue && currentItem.Value.type == draggedItemData.Value.type && currentItem.Value.type != "Bottle")
                {
                    int totalAmount = currentItem.Value.amount + draggedItemData.Value.amount;
                    if (totalAmount <= MaxStackSize)
                    {
                        inventory.AddItemAt(slotIndex, currentItem.Value.type, totalAmount);
                    }
                    else
                    {
                        inventory.AddItemAt(slotIndex, currentItem.Value.type, MaxStackSize);
                        int remainingAmount = totalAmount - MaxStackSize;
                        for (int i = 0; i < 20; i++)
                        {
                            if (i != slotIndex)
                            {
                                InventoryItem? otherSlot = inventory.GetItemAt(i);
                                if (!otherSlot.HasValue || otherSlot.Value.type == "")
                                {
                                    inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    inventory.AddItemAt(slotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                }
            }
            else
            {
                inventory.ClearSlot(targetSlot.slotIndex);
                if (targetItem.HasValue)
                {
                    inventory.AddItemAt(slotIndex, targetItem.Value.type, targetItem.Value.amount);
                }
                else if (!isSplitting)
                {
                    inventory.ClearSlot(slotIndex);
                }
                inventory.AddItemAt(targetSlot.slotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
            }
        }
        else if (hotbarSlot != null && hotbarSlot != this)
        {
            InventoryItem targetItem = hotbar.GetItemAt(hotbarSlot.transform.GetSiblingIndex());

            if (targetItem.type != "" && targetItem.type == draggedItemData.Value.type && targetItem.type != "Bottle")
            {
                int totalAmount = targetItem.amount + draggedItemData.Value.amount;
                if (totalAmount <= MaxStackSize)
                {
                    hotbar.SetItemAt(hotbarSlot.transform.GetSiblingIndex(), new InventoryItem { type = targetItem.type, amount = totalAmount });
                    if (!isSplitting)
                    {
                        inventory.ClearSlot(slotIndex);
                    }
                }
                else
                {
                    hotbar.SetItemAt(hotbarSlot.transform.GetSiblingIndex(), new InventoryItem { type = targetItem.type, amount = MaxStackSize });
                    inventory.AddItemAt(slotIndex, draggedItemData.Value.type, totalAmount - MaxStackSize);
                }
            }
            else if (targetItem.type == "")
            {
                hotbar.SetItemAt(hotbarSlot.transform.GetSiblingIndex(), new InventoryItem { type = draggedItemData.Value.type, amount = draggedItemData.Value.amount });
                if (!isSplitting)
                {
                    inventory.ClearSlot(slotIndex);
                }
            }
            else if (isSplitting && targetItem.type != draggedItemData.Value.type)
            {
                // Если это разделение стака и в целевом слоте другой тип предмета, возвращаем предметы в исходный слот
                InventoryItem? currentItem = inventory.GetItemAt(slotIndex);
                if (currentItem.HasValue && currentItem.Value.type == draggedItemData.Value.type && currentItem.Value.type != "Bottle")
                {
                    int totalAmount = currentItem.Value.amount + draggedItemData.Value.amount;
                    if (totalAmount <= MaxStackSize)
                    {
                        inventory.AddItemAt(slotIndex, currentItem.Value.type, totalAmount);
                    }
                    else
                    {
                        inventory.AddItemAt(slotIndex, currentItem.Value.type, MaxStackSize);
                        int remainingAmount = totalAmount - MaxStackSize;
                        for (int i = 0; i < 20; i++)
                        {
                            if (i != slotIndex)
                            {
                                InventoryItem? otherSlot = inventory.GetItemAt(i);
                                if (!otherSlot.HasValue || otherSlot.Value.type == "")
                                {
                                    inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    inventory.AddItemAt(slotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                }
            }
            else
            {
                hotbar.SetItemAt(hotbarSlot.transform.GetSiblingIndex(), new InventoryItem { type = draggedItemData.Value.type, amount = draggedItemData.Value.amount });
                if (targetItem.type != "")
                {
                    inventory.AddItemAt(slotIndex, targetItem.type, targetItem.amount);
                }
                else if (!isSplitting)
                {
                    inventory.ClearSlot(slotIndex);
                }
            }
        }

        Destroy(draggedItem);
        draggedItem = null;
        canvasGroup = null;
        draggedItemData = null;
        dragStarted = false;
        isSplitting = false;

        inventory.RefreshUI();
        UpdateSlotVisual();
        if (targetSlot != null)
        {
            targetSlot.UpdateSlotVisual();
        }
        if (hotbarSlot != null)
        {
            hotbarSlot.UpdateSlotVisual();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage != null)
        {
            slotImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotImage != null)
        {
            slotImage.color = normalColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            clickTime = Time.time;
            isHolding = true;
            dragStarted = false;

            // Проверяем Shift + ЛКМ
            if (Input.GetKey(KeyCode.LeftShift))
            {
                CombineStacks();
                return;
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!Input.GetMouseButton(0)) // Игнорируем ПКМ, если ЛКМ уже нажата
            {
                SplitStack();
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isHolding = false;
            if (dragStarted && draggedItem != null)
            {
                // Проверяем, наведён ли курсор на этот слот
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                bool isOverSameSlot = false;
                foreach (var result in results)
                {
                    if (result.gameObject == gameObject)
                    {
                        isOverSameSlot = true;
                        break;
                    }
                }

                if (isOverSameSlot)
                {
                    EndDrag();
                }
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (dragStarted && draggedItem != null)
            {
                // Проверяем, наведён ли курсор на этот слот
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                bool isOverSameSlot = false;
                foreach (var result in results)
                {
                    if (result.gameObject == gameObject)
                    {
                        isOverSameSlot = true;
                        break;
                    }
                }

                if (isOverSameSlot)
                {
                    EndDrag();
                }
            }
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

    private void SplitStack()
    {
        if (draggedItem != null)
        {
            Destroy(draggedItem);
            draggedItem = null;
            canvasGroup = null;
            draggedItemData = null;
            dragStarted = false;
            isSplitting = false;
        }

        InventoryItem? item = inventory.GetItemAt(slotIndex);
        if (!item.HasValue || item.Value.type == "" || item.Value.amount <= 1 || item.Value.type == "Bottle") return;

        int amountToTake = Mathf.CeilToInt(item.Value.amount / 2f);
        int amountToLeave = item.Value.amount - amountToTake;

        inventory.AddItemAt(slotIndex, item.Value.type, amountToLeave);

        draggedItemData = new InventoryItem { type = item.Value.type, amount = amountToTake };
        draggedItem = new GameObject("DraggedItem");
        draggedItem.transform.SetParent(transform.root, false);

        // Добавляем изображение предмета
        Image draggedImage = draggedItem.AddComponent<Image>();
        draggedImage.sprite = slotImage.sprite;
        draggedImage.rectTransform.sizeDelta = new Vector2(50, 50);

        // Добавляем текст для отображения количества
        GameObject amountTextObj = new GameObject("AmountText");
        amountTextObj.transform.SetParent(draggedItem.transform, false);
        TextMeshProUGUI draggedAmountText = amountTextObj.AddComponent<TextMeshProUGUI>();
        draggedAmountText.text = item.Value.type == "Bottle" ? "" : amountToTake.ToString();
        draggedAmountText.fontSize = 14;
        draggedAmountText.color = Color.white;
        draggedAmountText.alignment = TextAlignmentOptions.BottomLeft; // Изменяем выравнивание на слева
        RectTransform textRect = amountTextObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(50, 50);
        textRect.anchoredPosition = new Vector2(5, 5); // Сдвигаем текст влево

        canvasGroup = draggedItem.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
        draggedItem.transform.position = Input.mousePosition;
        dragStarted = true;
        isSplitting = true;

        inventory.RefreshUI();
        UpdateSlotVisual();
    }

    private void CombineStacks()
    {
        InventoryItem? currentItem = inventory.GetItemAt(slotIndex);
        if (!currentItem.HasValue || currentItem.Value.type == "" || currentItem.Value.type == "Bottle") return;

        string itemType = currentItem.Value.type;
        int amountToTransfer = currentItem.Value.amount;

        // Ищем в инвентаре другие слоты с таким же типом предметов
        for (int i = 0; i < 20; i++)
        {
            if (i == slotIndex) continue; // Пропускаем текущий слот

            InventoryItem? targetItem = inventory.GetItemAt(i);
            if (targetItem.HasValue && targetItem.Value.type == itemType && targetItem.Value.amount < MaxStackSize)
            {
                int spaceLeft = MaxStackSize - targetItem.Value.amount;
                int amountToAdd = Mathf.Min(amountToTransfer, spaceLeft);

                // Добавляем предметы в целевой слот
                inventory.AddItemAt(i, itemType, targetItem.Value.amount + amountToAdd);
                amountToTransfer -= amountToAdd;

                if (amountToTransfer <= 0) break;
            }
        }

        // Ищем в хотбаре слоты с таким же типом предметов
        if (amountToTransfer > 0 && hotbar != null)
        {
            for (int i = 0; i < 4; i++)
            {
                InventoryItem targetItem = hotbar.GetItemAt(i);
                if (targetItem.type == itemType && targetItem.amount < MaxStackSize)
                {
                    int spaceLeft = MaxStackSize - targetItem.amount;
                    int amountToAdd = Mathf.Min(amountToTransfer, spaceLeft);

                    // Добавляем предметы в целевой слот хотбара
                    hotbar.SetItemAt(i, new InventoryItem { type = itemType, amount = targetItem.amount + amountToAdd });
                    amountToTransfer -= amountToAdd;

                    if (amountToTransfer <= 0) break;
                }
            }
        }

        // Обновляем текущий слот
        if (amountToTransfer > 0)
        {
            inventory.AddItemAt(slotIndex, itemType, amountToTransfer);
        }
        else
        {
            inventory.ClearSlot(slotIndex);
        }

        inventory.RefreshUI();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || isSplitting) return;

        InventoryItem? item = inventory.GetItemAt(slotIndex);
        if (item.HasValue && item.Value.type != "")
        {
            if (draggedItem != null)
            {
                Destroy(draggedItem);
            }

            draggedItemData = item;
            draggedItem = new GameObject("DraggedItem");
            draggedItem.transform.SetParent(transform.root, false);

            // Добавляем изображение предмета
            Image draggedImage = draggedItem.AddComponent<Image>();
            draggedImage.sprite = slotImage.sprite;
            draggedImage.rectTransform.sizeDelta = new Vector2(50, 50);

            // Добавляем текст для отображения количества
            GameObject amountTextObj = new GameObject("AmountText");
            amountTextObj.transform.SetParent(draggedItem.transform, false);
            TextMeshProUGUI draggedAmountText = amountTextObj.AddComponent<TextMeshProUGUI>();
            draggedAmountText.text = item.Value.type == "Bottle" ? "" : item.Value.amount.ToString();
            draggedAmountText.fontSize = 14;
            draggedAmountText.color = Color.white;
            draggedAmountText.alignment = TextAlignmentOptions.BottomLeft; // Изменяем выравнивание на слева
            RectTransform textRect = amountTextObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(50, 50);
            textRect.anchoredPosition = new Vector2(5, 5); // Сдвигаем текст влево

            canvasGroup = draggedItem.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;
            draggedItem.transform.position = eventData.position;
            dragStarted = true;
            isSplitting = false;

            // Очищаем исходный слот при начале перетаскивания
            inventory.ClearSlot(slotIndex);

            slotImage.sprite = null;
            slotImage.color = normalColor;
            if (amountText != null)
            {
                amountText.text = "";
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedItem != null)
        {
            draggedItem.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        HotbarSlots sourceSlot = eventData.pointerDrag?.GetComponent<HotbarSlots>();
        if (sourceSlot != null)
        {
            UpdateSlotVisual();
        }
    }
}