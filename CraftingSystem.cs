using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    private Inventory inventory; // Ссылка на инвентарь игрока
    private Hotbar hotbar; // Ссылка на хотбар игрока
    private InventoryUI inventoryUI; // Ссылка на UI инвентаря
    private bool isCraftingWindowOpen = false; // Флаг открытия окна крафта

    // Структура для рецепта
    [System.Serializable]
    public struct Recipe
    {
        public string category; // Категория рецепта (например, "Food/Water")
        public string resultItem; // Что получится (например, "Axe")
        public int resultAmount; // Количество получаемого предмета
        public List<Ingredient> ingredients; // Список необходимых ресурсов
    }

    // Структура для ингредиента рецепта
    [System.Serializable]
    public struct Ingredient
    {
        public string itemType; // Тип ресурса (например, "Wood")
        public int amount; // Количество ресурса
    }

    // Список доступных рецептов
    public List<Recipe> recipes = new List<Recipe>();

    void Start()
    {
        // Ищем необходимые компоненты
        inventory = FindObjectOfType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Inventory not found for CraftingSystem!");
            return;
        }

        hotbar = FindObjectOfType<Hotbar>();
        if (hotbar == null)
        {
            Debug.LogError("Hotbar not found for CraftingSystem!");
            return;
        }

        inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI not found for CraftingSystem!");
            return;
        }

        // Инициализируем рецепты
        InitializeRecipes();
    }

    void Update()
    {
        // Открытие/закрытие окна крафта по клавише C
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Проверяем, открыт ли инвентарь
            if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            {
                Debug.Log("Cannot open crafting window while inventory is open!");
                return;
            }

            ToggleCraftingWindow();
        }
    }

    // Метод для переключения окна крафта
    public void ToggleCraftingWindow()
    {
        isCraftingWindowOpen = !isCraftingWindowOpen;
        Debug.Log($"Crafting window {(isCraftingWindowOpen ? "opened" : "closed")}.");
    }

    // Проверка, открыто ли окно крафта
    public bool IsCraftingWindowOpen()
    {
        return isCraftingWindowOpen;
    }

    // Получение списка категорий
    public List<string> GetCategories()
    {
        List<string> categories = new List<string>();
        foreach (var recipe in recipes)
        {
            if (!categories.Contains(recipe.category))
            {
                categories.Add(recipe.category);
            }
        }
        return categories;
    }

    // Получение рецептов по категории
    public List<Recipe> GetRecipesByCategory(string category)
    {
        return recipes.FindAll(recipe => recipe.category == category);
    }

    // Проверка, можно ли скрафтить рецепт (для UI)
    public bool CanCraftRecipe(Recipe recipe)
    {
        return CanCraft(recipe);
    }

    // Инициализация рецептов
    private void InitializeRecipes()
    {
        // Рецепт 1: Топор (2 Wood + 1 Iron = 1 Axe) в категории "Tools"
        Recipe axeRecipe = new Recipe
        {
            category = "Tools",
            resultItem = "Axe",
            resultAmount = 1,
            ingredients = new List<Ingredient>
            {
                new Ingredient { itemType = "Wood", amount = 2 },
                new Ingredient { itemType = "Iron", amount = 1 }
            }
        };
        recipes.Add(axeRecipe);

        // Рецепт 2: Еда (2 Herbs = 1 Food) в категории "Food/Water"
        Recipe foodRecipe = new Recipe
        {
            category = "Food/Water",
            resultItem = "Food",
            resultAmount = 1,
            ingredients = new List<Ingredient>
            {
                new Ingredient { itemType = "Herbs", amount = 2 }
            }
        };
        recipes.Add(foodRecipe);

        Debug.Log($"CraftingSystem initialized with {recipes.Count} recipes in {GetCategories().Count} categories.");
    }

    // Метод для крафта
    public bool Craft(Recipe recipe)
    {
        // Шаг 1: Проверяем наличие всех ингредиентов
        if (!CanCraft(recipe))
        {
            Debug.Log($"Cannot craft {recipe.resultItem}: Not enough resources!");
            return false;
        }

        // Шаг 2: Удаляем использованные ресурсы
        foreach (var ingredient in recipe.ingredients)
        {
            RemoveResources(ingredient.itemType, ingredient.amount);
        }

        // Шаг 3: Добавляем результат крафта в инвентарь или хотбар
        AddCraftedItem(recipe.resultItem, recipe.resultAmount);

        Debug.Log($"Successfully crafted {recipe.resultAmount} {recipe.resultItem}!");
        return true;
    }

    // Проверка, можно ли скрафтить предмет
    private bool CanCraft(Recipe recipe)
    {
        // Проверяем наличие каждого ингредиента
        foreach (var ingredient in recipe.ingredients)
        {
            int requiredAmount = ingredient.amount;
            int availableAmount = 0;

            // Считаем количество в хотбаре
            for (int i = 0; i < 4; i++)
            {
                InventoryItem hotbarItem = hotbar.GetItemAt(i);
                if (hotbarItem.type == ingredient.itemType)
                {
                    availableAmount += hotbarItem.amount;
                }
            }

            // Считаем количество в инвентаре
            for (int i = 0; i < 20; i++)
            {
                InventoryItem? inventoryItem = inventory.GetItemAt(i);
                if (inventoryItem.HasValue && inventoryItem.Value.type == ingredient.itemType)
                {
                    availableAmount += inventoryItem.Value.amount;
                }
            }

            if (availableAmount < requiredAmount)
            {
                Debug.Log($"Not enough {ingredient.itemType}: Need {requiredAmount}, but only {availableAmount} available.");
                return false;
            }
        }

        return true;
    }

    // Удаление ресурсов после крафта
    private void RemoveResources(string itemType, int amount)
    {
        int remainingAmount = amount;

        // Сначала удаляем из хотбара
        for (int i = 0; i < 4 && remainingAmount > 0; i++)
        {
            InventoryItem hotbarItem = hotbar.GetItemAt(i);
            if (hotbarItem.type == itemType && hotbarItem.amount > 0)
            {
                int amountToRemove = Mathf.Min(remainingAmount, hotbarItem.amount);
                hotbarItem.amount -= amountToRemove;
                remainingAmount -= amountToRemove;

                if (hotbarItem.amount <= 0)
                {
                    hotbar.SetItemAt(i, new InventoryItem { type = "", amount = 0 });
                }
                else
                {
                    hotbar.SetItemAt(i, hotbarItem);
                }
            }
        }

        // Затем удаляем из инвентаря
        for (int i = 0; i < 20 && remainingAmount > 0; i++)
        {
            InventoryItem? inventoryItem = inventory.GetItemAt(i);
            if (inventoryItem.HasValue && inventoryItem.Value.type == itemType && inventoryItem.Value.amount > 0)
            {
                int amountToRemove = Mathf.Min(remainingAmount, inventoryItem.Value.amount);
                remainingAmount -= amountToRemove;

                if (inventoryItem.Value.amount - amountToRemove <= 0)
                {
                    inventory.ClearSlot(i);
                }
                else
                {
                    inventory.AddItemAt(i, itemType, inventoryItem.Value.amount - amountToRemove);
                }
            }
        }

        // Обновляем UI
        hotbar.UpdateVisual();
        inventory.RefreshUI();
    }

    // Добавление скрафченного предмета
    private void AddCraftedItem(string itemType, int amount)
    {
        // Пробуем добавить в хотбар
        for (int i = 0; i < 4 && amount > 0; i++)
        {
            InventoryItem hotbarItem = hotbar.GetItemAt(i);
            if (hotbarItem.type == itemType && hotbarItem.amount > 0)
            {
                int spaceLeft = 64 - hotbarItem.amount;
                int amountToAdd = Mathf.Min(amount, spaceLeft);
                hotbarItem.amount += amountToAdd;
                hotbar.SetItemAt(i, hotbarItem);
                amount -= amountToAdd;
            }
        }

        for (int i = 0; i < 4 && amount > 0; i++)
        {
            InventoryItem hotbarItem = hotbar.GetItemAt(i);
            if (hotbarItem.type == "")
            {
                int amountToAdd = Mathf.Min(amount, 64);
                hotbar.SetItemAt(i, new InventoryItem { type = itemType, amount = amountToAdd });
                amount -= amountToAdd;
            }
        }

        // Если осталось, добавляем в инвентарь
        if (amount > 0)
        {
            if (!inventory.AddItem(itemType, amount))
            {
                Debug.LogWarning($"Failed to add {amount} of {itemType} to inventory after crafting!");
            }
        }

        // Обновляем UI
        hotbar.UpdateVisual();
        inventory.RefreshUI();
    }
}