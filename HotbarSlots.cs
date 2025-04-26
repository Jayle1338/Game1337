using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class HotbarSlots : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Hotbar hotbar;
    private Inventory inventory;
    private int slotIndex;
    private Image slotImage;
    private TextMeshProUGUI amountText;
    private Color normalColor = new Color(1f, 1f, 1f, 0.3f);
    private Color hoverColor = new Color(1f, 1f, 0f, 0.5f);
    private Color selectedColor = new Color(0f, 1f, 0f, 0.5f);

    private GameObject draggedItem;
    private CanvasGroup canvasGroup;
    private bool dragStarted;
    private InventoryItem? draggedItemData;
    private const float holdThreshold = 0.3f;
    private float clickTime;
    private bool isHolding;
    private const int MaxStackSize = 64;
    private bool isSplitting;

    void Awake()
    {
        slotImage = GetComponent<Image>();
        amountText = transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
        if (amountText == null)
        {
            Debug.LogWarning($"AmountText (TextMeshProUGUI) not found on Hotbar slot {slotIndex}!");
        }
    }

    void Start()
    {
        hotbar = GetComponentInParent<Hotbar>();
        inventory = hotbar.GetComponentInParent<Inventory>();
        slotIndex = transform.GetSiblingIndex();
        if (hotbar != null)
        {
            UpdateSlotVisual();
        }
    }

    public void UpdateSlotVisual()
    {
        if (hotbar == null)
        {
            Debug.LogWarning($"Hotbar is null for slot {slotIndex}. Cannot update visual.");
            return;
        }

        InventoryItem item = hotbar.GetItemAt(slotIndex);
        bool isSelected = slotIndex == hotbar.GetSelectedSlot();

        if (item.type != "")
        {
            InventoryUI inventoryUI = hotbar.GetComponent<InventoryUI>();
            if (inventoryUI != null)
            {
                Sprite sprite = null;
                switch (item.type)
                {
                    case "Bottle":
                        sprite = inventoryUI.bottleIcon;
                        break;
                    case "Wood":
                        sprite = inventoryUI.woodIcon;
                        break;
                    case "Iron":
                        sprite = inventoryUI.ironIcon;
                        break;
                    case "Herbs":
                        sprite = inventoryUI.herbsIcon;
                        break;
                }

                if (sprite != null)
                {
                    slotImage.sprite = sprite;
                    slotImage.color = isSelected ? selectedColor : normalColor;
                    if (amountText != null)
                    {
                        if (item.type == "Bottle")
                        {
                            amountText.text = "";
                        }
                        else
                        {
                            amountText.text = item.amount.ToString();
                        }
                    }
                }
            }
        }
        else
        {
            slotImage.sprite = null;
            slotImage.color = isSelected ? selectedColor : normalColor;
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

        // Выброс предметов на клавишу G (только при наведении на слот)
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
                InventoryItem item = hotbar.GetItemAt(slotIndex);
                if (item.type == "" || item.amount <= 0) return;

                // Выбрасываем весь стак, если зажат Shift, иначе 1 единицу
                int amountToDrop = Input.GetKey(KeyCode.LeftShift) ? item.amount : 1;

                DropItem(item.type, amountToDrop);

                int newAmount = item.amount - amountToDrop;
                if (newAmount <= 0)
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });

                    // Если выбрасываем бутылку, снимаем её с экипировки
                    if (item.type == "Bottle")
                    {
                        Player player = FindObjectOfType<Player>();
                        if (player != null && player.IsBottleEquipped() && player.GetEquippedBottleSlot() == slotIndex)
                        {
                            player.UnequipBottle();
                        }
                    }
                }
                else
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = item.type, amount = newAmount });
                }

                UpdateSlotVisual();
                hotbar.UpdateVisual();
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

        Player player = FindObjectOfType<Player>();
        bool wasBottle = draggedItemData.HasValue && draggedItemData.Value.type == "Bottle";

        if (targetSlot == null && hotbarSlot == null)
        {
            Debug.Log($"Drag cancelled for hotbar slot {slotIndex}, restoring item");
            if (draggedItemData.HasValue)
            {
                InventoryItem currentItem = hotbar.GetItemAt(slotIndex);
                if (currentItem.type != "" && currentItem.type == draggedItemData.Value.type && currentItem.type != "Bottle")
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
                            InventoryItem? otherSlot = inventory.GetItemAt(i);
                            if (!otherSlot.HasValue || otherSlot.Value.type == "")
                            {
                                inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                break;
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
        else if (hotbarSlot == this || targetSlot == this) // Возврат в тот же слот
        {
            if (draggedItemData.HasValue)
            {
                if (hotbarSlot == this) // Перетаскивание начато из хотбара
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
                                InventoryItem? otherSlot = inventory.GetItemAt(i);
                                if (!otherSlot.HasValue || otherSlot.Value.type == "")
                                {
                                    inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        hotbar.SetItemAt(slotIndex, draggedItemData.Value);
                    }
                }
                else if (targetSlot == this) // Перетаскивание начато из инвентаря
                {
                    InventoryItem? currentItem = inventory.GetItemAt(targetSlot.SlotIndex);
                    if (currentItem.HasValue && currentItem.Value.type == draggedItemData.Value.type && currentItem.Value.type != "Bottle")
                    {
                        int totalAmount = currentItem.Value.amount + draggedItemData.Value.amount;
                        if (totalAmount <= MaxStackSize)
                        {
                            inventory.AddItemAt(targetSlot.SlotIndex, currentItem.Value.type, totalAmount);
                        }
                        else
                        {
                            inventory.AddItemAt(targetSlot.SlotIndex, currentItem.Value.type, MaxStackSize);
                            int remainingAmount = totalAmount - MaxStackSize;
                            for (int i = 0; i < 20; i++)
                            {
                                if (i != targetSlot.SlotIndex)
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
                        inventory.AddItemAt(targetSlot.SlotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                    }
                }
            }
        }
        else if (hotbarSlot != null && hotbarSlot != this)
        {
            InventoryItem targetItem = hotbar.GetItemAt(hotbarSlot.slotIndex);

            if (targetItem.type != "" && targetItem.type == draggedItemData.Value.type && targetItem.type != "Bottle")
            {
                int totalAmount = targetItem.amount + draggedItemData.Value.amount;
                if (totalAmount <= MaxStackSize)
                {
                    hotbar.SetItemAt(hotbarSlot.slotIndex, new InventoryItem { type = targetItem.type, amount = totalAmount });
                    if (!isSplitting)
                    {
                        hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });
                    }
                }
                else
                {
                    hotbar.SetItemAt(hotbarSlot.slotIndex, new InventoryItem { type = targetItem.type, amount = MaxStackSize });
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = draggedItemData.Value.type, amount = totalAmount - MaxStackSize });
                }
            }
            else if (targetItem.type == "")
            {
                hotbar.SetItemAt(hotbarSlot.slotIndex, new InventoryItem { type = draggedItemData.Value.type, amount = draggedItemData.Value.amount });
                if (!isSplitting)
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });
                }
            }
            else if (isSplitting && targetItem.type != draggedItemData.Value.type)
            {
                // Если это разделение стака и в целевом слоте другой тип предмета, возвращаем предметы в исходный слот
                InventoryItem currentItem = hotbar.GetItemAt(slotIndex);
                if (currentItem.type != "" && currentItem.type == draggedItemData.Value.type && currentItem.type != "Bottle")
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
                            InventoryItem? otherSlot = inventory.GetItemAt(i);
                            if (!otherSlot.HasValue || otherSlot.Value.type == "")
                            {
                                inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = draggedItemData.Value.type, amount = draggedItemData.Value.amount });
                }
            }
            else
            {
                hotbar.SetItemAt(hotbarSlot.slotIndex, new InventoryItem { type = draggedItemData.Value.type, amount = draggedItemData.Value.amount });
                if (!isSplitting)
                {
                    hotbar.SetItemAt(slotIndex, targetItem);
                }
            }
        }
        else if (targetSlot != null)
        {
            InventoryItem? targetItem = inventory.GetItemAt(targetSlot.SlotIndex);

            if (targetItem.HasValue && targetItem.Value.type == draggedItemData.Value.type && targetItem.Value.type != "Bottle")
            {
                int totalAmount = targetItem.Value.amount + draggedItemData.Value.amount;
                if (totalAmount <= MaxStackSize)
                {
                    inventory.AddItemAt(targetSlot.SlotIndex, targetItem.Value.type, totalAmount);
                    if (!isSplitting)
                    {
                        hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });
                    }
                }
                else
                {
                    inventory.AddItemAt(targetSlot.SlotIndex, targetItem.Value.type, MaxStackSize);
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = draggedItemData.Value.type, amount = totalAmount - MaxStackSize });
                }
            }
            else if (!targetItem.HasValue || targetItem.Value.type == "")
            {
                inventory.AddItemAt(targetSlot.SlotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                if (!isSplitting)
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });
                }
            }
            else if (isSplitting && targetItem.HasValue && targetItem.Value.type != draggedItemData.Value.type)
            {
                // Если это разделение стака и в целевом слоте другой тип предмета, возвращаем предметы в исходный слот
                InventoryItem currentItem = hotbar.GetItemAt(slotIndex);
                if (currentItem.type != "" && currentItem.type == draggedItemData.Value.type && currentItem.type != "Bottle")
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
                            InventoryItem? otherSlot = inventory.GetItemAt(i);
                            if (!otherSlot.HasValue || otherSlot.Value.type == "")
                            {
                                inventory.AddItemAt(i, draggedItemData.Value.type, remainingAmount);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = draggedItemData.Value.type, amount = draggedItemData.Value.amount });
                }
            }
            else
            {
                inventory.ClearSlot(targetSlot.SlotIndex);
                inventory.AddItemAt(targetSlot.SlotIndex, draggedItemData.Value.type, draggedItemData.Value.amount);
                if (targetItem.HasValue)
                {
                    hotbar.SetItemAt(slotIndex, targetItem.Value);
                }
                else if (!isSplitting)
                {
                    hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });
                }
                if (wasBottle && player != null && player.IsBottleEquipped() && player.GetEquippedBottleSlot() == slotIndex)
                {
                    player.UnequipBottle();
                }
            }
            inventory.RefreshUI();
        }

        Destroy(draggedItem);
        draggedItem = null;
        canvasGroup = null;
        draggedItemData = null;
        dragStarted = false;
        isSplitting = false;

        UpdateSlotVisual();
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
            bool isSelected = slotIndex == hotbar.GetSelectedSlot();
            slotImage.color = isSelected ? selectedColor : normalColor;
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

        InventoryItem item = hotbar.GetItemAt(slotIndex);
        if (item.type == "" || item.amount <= 1 || item.type == "Bottle") return;

        int amountToTake = Mathf.CeilToInt(item.amount / 2f);
        int amountToLeave = item.amount - amountToTake;

        hotbar.SetItemAt(slotIndex, new InventoryItem { type = item.type, amount = amountToLeave });

        draggedItemData = new InventoryItem { type = item.type, amount = amountToTake };
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
        draggedAmountText.text = item.type == "Bottle" ? "" : amountToTake.ToString();
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

        UpdateSlotVisual();
    }

    private void CombineStacks()
    {
        InventoryItem currentItem = hotbar.GetItemAt(slotIndex);
        if (currentItem.type == "" || currentItem.type == "Bottle") return;

        string itemType = currentItem.type;
        int amountToTransfer = currentItem.amount;

        // Ищем в хотбаре другие слоты с таким же типом предметов
        for (int i = 0; i < 4; i++)
        {
            if (i == slotIndex) continue; // Пропускаем текущий слот

            InventoryItem targetItem = hotbar.GetItemAt(i);
            if (targetItem.type == itemType && targetItem.amount < MaxStackSize)
            {
                int spaceLeft = MaxStackSize - targetItem.amount;
                int amountToAdd = Mathf.Min(amountToTransfer, spaceLeft);

                // Добавляем предметы в целевой слот
                hotbar.SetItemAt(i, new InventoryItem { type = itemType, amount = targetItem.amount + amountToAdd });
                amountToTransfer -= amountToAdd;

                if (amountToTransfer <= 0) break;
            }
        }

        // Ищем в инвентаре слоты с таким же типом предметов
        if (amountToTransfer > 0 && inventory != null)
        {
            for (int i = 0; i < 20; i++)
            {
                InventoryItem? targetItem = inventory.GetItemAt(i);
                if (targetItem.HasValue && targetItem.Value.type == itemType && targetItem.Value.amount < MaxStackSize)
                {
                    int spaceLeft = MaxStackSize - targetItem.Value.amount;
                    int amountToAdd = Mathf.Min(amountToTransfer, spaceLeft);

                    // Добавляем предметы в целевой слот инвентаря
                    inventory.AddItemAt(i, itemType, targetItem.Value.amount + amountToAdd);
                    amountToTransfer -= amountToAdd;

                    if (amountToTransfer <= 0) break;
                }
            }
        }

        // Обновляем текущий слот
        if (amountToTransfer > 0)
        {
            hotbar.SetItemAt(slotIndex, new InventoryItem { type = itemType, amount = amountToTransfer });
        }
        else
        {
            hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });
        }

        hotbar.UpdateVisual();
        if (inventory != null) inventory.RefreshUI();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || isSplitting) return;

        InventoryItem item = hotbar.GetItemAt(slotIndex);
        if (item.type != "")
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
            draggedAmountText.text = item.type == "Bottle" ? "" : item.amount.ToString();
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
            hotbar.SetItemAt(slotIndex, new InventoryItem { type = "", amount = 0 });

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
        InventorySlot sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (sourceSlot != null)
        {
            UpdateSlotVisual();
        }
    }
}