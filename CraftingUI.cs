using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro; // Для TextMeshPro

public class CraftingUI : MonoBehaviour
{
    private CraftingSystem craftingSystem;
    public GameObject craftingWindow; // Ссылка на объект Canvas с окном крафта
    public Transform categoryContent; // Transform контейнера для списка категорий
    public Transform recipeContent; // Transform контейнера для списка рецептов
    public GameObject categoryButtonPrefab; // Префаб кнопки категории
    public GameObject recipeButtonPrefab; // Префаб кнопки рецепта

    private List<string> categories = new List<string>();
    private string selectedCategory = ""; // Текущая выбранная категория
    private List<CraftingSystem.Recipe> currentRecipes = new List<CraftingSystem.Recipe>();

    void Start()
    {
        craftingSystem = FindObjectOfType<CraftingSystem>();
        if (craftingSystem == null)
        {
            Debug.LogError("CraftingSystem not found for CraftingUI!");
            return;
        }
        Debug.Log("CraftingSystem found.");

        // Изначально окно крафта скрыто
        if (craftingWindow != null)
        {
            craftingWindow.SetActive(false);
            Debug.Log("CraftingWindow found and set to inactive.");
        }
        else
        {
            Debug.LogError("CraftingWindow not assigned in CraftingUI!");
            return;
        }

        // Проверяем наличие префабов и контейнеров
        if (categoryButtonPrefab == null)
        {
            Debug.LogError("CategoryButtonPrefab not assigned in CraftingUI!");
            return;
        }
        Debug.Log("CategoryButtonPrefab assigned.");

        if (recipeButtonPrefab == null)
        {
            Debug.LogError("RecipeButtonPrefab not assigned in CraftingUI!");
            return;
        }
        Debug.Log("RecipeButtonPrefab assigned.");

        if (categoryContent == null)
        {
            Debug.LogError("CategoryContent not assigned in CraftingUI!");
            return;
        }
        Debug.Log("CategoryContent assigned.");

        if (recipeContent == null)
        {
            Debug.LogError("RecipeContent not assigned in CraftingUI!");
            return;
        }
        Debug.Log("RecipeContent assigned.");
    }

    void Update()
    {
        // Синхронизируем видимость окна с CraftingSystem
        if (craftingWindow != null && craftingSystem != null)
        {
            bool shouldBeOpen = craftingSystem.IsCraftingWindowOpen();
            if (craftingWindow.activeSelf != shouldBeOpen)
            {
                craftingWindow.SetActive(shouldBeOpen);
                if (shouldBeOpen)
                {
                    Debug.Log("Crafting window opened, refreshing categories...");
                    RefreshCategories();
                }
                else
                {
                    Debug.Log("Crafting window closed.");
                }
            }
        }
    }

    // Обновление списка категорий
    public void RefreshCategories()
    {
        Debug.Log("Refreshing categories...");
        if (craftingSystem == null || categoryContent == null || categoryButtonPrefab == null)
        {
            Debug.LogError("Cannot refresh categories: Missing craftingSystem, categoryContent, or categoryButtonPrefab!");
            return;
        }

        // Очищаем старые кнопки
        foreach (Transform child in categoryContent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Cleared old category buttons.");

        // Получаем категории
        categories = craftingSystem.GetCategories();
        Debug.Log($"Found {categories.Count} categories: {string.Join(", ", categories)}");

        if (categories.Count == 0)
        {
            Debug.LogWarning("No categories found! Check CraftingSystem recipes.");
            return;
        }

        // Создаём кнопку для каждой категории
        for (int i = 0; i < categories.Count; i++)
        {
            string category = categories[i];
            GameObject categoryButton = Instantiate(categoryButtonPrefab, categoryContent);
            Debug.Log($"Created button for category: {category} at position {categoryButton.transform.position}, scale {categoryButton.transform.localScale}");

            // Проверяем, активен ли объект
            if (!categoryButton.activeSelf)
            {
                Debug.LogWarning($"Category button {category} is not active! Activating...");
                categoryButton.SetActive(true);
            }

            // Проверяем RectTransform
            RectTransform rect = categoryButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"Category button {category} RectTransform - Size: {rect.sizeDelta}, Position: {rect.anchoredPosition}, AnchorMin: {rect.anchorMin}, AnchorMax: {rect.anchorMax}");
            }

            // Настраиваем текст кнопки (ищем TMP_Text)
            TMP_Text buttonText = categoryButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = category.ToUpper();
                Debug.Log($"Set text for category button: {category.ToUpper()}");
                Debug.Log($"Text component position: {buttonText.GetComponent<RectTransform>().anchoredPosition}, scale: {buttonText.transform.localScale}");
            }
            else
            {
                Debug.LogWarning($"No TMP_Text component found on category button: {category}. Trying to find standard Text component...");
                // Пробуем найти стандартный Text
                Text standardText = categoryButton.GetComponentInChildren<Text>();
                if (standardText != null)
                {
                    standardText.text = category.ToUpper();
                    Debug.Log($"Set text for category button (standard Text): {category.ToUpper()}");
                    Debug.Log($"Text component position: {standardText.GetComponent<RectTransform>().anchoredPosition}, scale: {standardText.transform.localScale}");
                }
                else
                {
                    Debug.LogWarning($"No Text component found on category button: {category}. Make sure the prefab has a Text or TMP_Text child!");
                }
            }

            // Добавляем обработчик клика
            Button button = categoryButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectCategory(category));
                Debug.Log($"Added click listener for category: {category}");
            }
            else
            {
                Debug.LogWarning($"No Button component found on category button: {category}. Make sure the prefab has a Button component!");
            }

            // Если это первая категория, выбираем её автоматически
            if (i == 0 && string.IsNullOrEmpty(selectedCategory))
            {
                SelectCategory(category);
            }
        }

        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
        Debug.Log("Forced Canvas update after refreshing categories.");
    }

    // Выбор категории
    public void SelectCategory(string category)
    {
        selectedCategory = category;
        Debug.Log($"Selected category: {selectedCategory}");
        RefreshRecipes();
    }

    // Обновление списка рецептов
    public void RefreshRecipes()
    {
        Debug.Log("Refreshing recipes...");
        if (craftingSystem == null || recipeContent == null || recipeButtonPrefab == null)
        {
            Debug.LogError("Cannot refresh recipes: Missing craftingSystem, recipeContent, or recipeButtonPrefab!");
            return;
        }

        // Очищаем старые кнопки
        foreach (Transform child in recipeContent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Cleared old recipe buttons.");

        // Получаем рецепты для текущей категории
        currentRecipes = craftingSystem.GetRecipesByCategory(selectedCategory);
        Debug.Log($"Found {currentRecipes.Count} recipes in category {selectedCategory}: {string.Join(", ", currentRecipes.ConvertAll(r => r.resultItem))}");

        if (currentRecipes.Count == 0)
        {
            Debug.LogWarning($"No recipes found for category {selectedCategory}!");
            return;
        }

        // Создаём кнопку для каждого рецепта
        for (int i = 0; i < currentRecipes.Count; i++)
        {
            var recipe = currentRecipes[i];
            GameObject recipeButton = Instantiate(recipeButtonPrefab, recipeContent);
            Debug.Log($"Created button for recipe: {recipe.resultItem} at position {recipeButton.transform.position}, scale {recipeButton.transform.localScale}");

            // Проверяем, активен ли объект
            if (!recipeButton.activeSelf)
            {
                Debug.LogWarning($"Recipe button {recipe.resultItem} is not active! Activating...");
                recipeButton.SetActive(true);
            }

            // Проверяем RectTransform
            RectTransform rect = recipeButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"Recipe button {recipe.resultItem} RectTransform - Size: {rect.sizeDelta}, Position: {rect.anchoredPosition}, AnchorMin: {rect.anchorMin}, AnchorMax: {rect.anchorMax}");
            }

            // Настраиваем текст кнопки (ищем TMP_Text)
            TMP_Text buttonText = recipeButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                string ingredientsText = "";
                foreach (var ingredient in recipe.ingredients)
                {
                    ingredientsText += $"{ingredient.itemType} x{ingredient.amount}, ";
                }
                ingredientsText = ingredientsText.TrimEnd(',', ' ');
                buttonText.text = $"{recipe.resultItem} x{recipe.resultAmount} ({ingredientsText})";
                Debug.Log($"Set text for recipe button: {buttonText.text}");
                Debug.Log($"Text component position: {buttonText.GetComponent<RectTransform>().anchoredPosition}, scale: {buttonText.transform.localScale}");
            }
            else
            {
                Debug.LogWarning($"No TMP_Text component found on recipe button: {recipe.resultItem}. Trying to find standard Text component...");
                // Пробуем найти стандартный Text
                Text standardText = recipeButton.GetComponentInChildren<Text>();
                if (standardText != null)
                {
                    string ingredientsText = "";
                    foreach (var ingredient in recipe.ingredients)
                    {
                        ingredientsText += $"{ingredient.itemType} x{ingredient.amount}, ";
                    }
                    ingredientsText = ingredientsText.TrimEnd(',', ' ');
                    standardText.text = $"{recipe.resultItem} x{recipe.resultAmount} ({ingredientsText})";
                    Debug.Log($"Set text for recipe button (standard Text): {standardText.text}");
                    Debug.Log($"Text component position: {standardText.GetComponent<RectTransform>().anchoredPosition}, scale: {standardText.transform.localScale}");
                }
                else
                {
                    Debug.LogWarning($"No Text component found on recipe button: {recipe.resultItem}. Make sure the prefab has a Text or TMP_Text child!");
                }
            }

            // Проверяем, можно ли скрафтить, и меняем цвет текста
            bool canCraft = craftingSystem.CanCraftRecipe(recipe);
            TMP_Text tmpText = recipeButton.GetComponentInChildren<TMP_Text>();
            Text standardTextForColor = recipeButton.GetComponentInChildren<Text>();
            if (tmpText != null)
            {
                tmpText.color = canCraft ? Color.white : Color.gray;
                Debug.Log($"Set color for recipe button {recipe.resultItem}: {(canCraft ? "White (can craft)" : "Gray (cannot craft)")}");
            }
            else if (standardTextForColor != null)
            {
                standardTextForColor.color = canCraft ? Color.white : Color.gray;
                Debug.Log($"Set color for recipe button (standard Text) {recipe.resultItem}: {(canCraft ? "White (can craft)" : "Gray (cannot craft)")}");
            }

            // Добавляем обработчик двойного клика
            RecipeButtonHandler handler = recipeButton.GetComponent<RecipeButtonHandler>();
            if (handler == null)
            {
                handler = recipeButton.AddComponent<RecipeButtonHandler>();
                Debug.Log($"Added RecipeButtonHandler to recipe button: {recipe.resultItem}");
            }
            handler.Setup(this, recipe);
        }

        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
        Debug.Log("Forced Canvas update after refreshing recipes.");
    }

    // Метод для крафта (вызывается из RecipeButtonHandler)
    public void CraftRecipe(CraftingSystem.Recipe recipe)
    {
        if (craftingSystem != null)
        {
            craftingSystem.Craft(recipe);
            Debug.Log($"Crafted {recipe.resultItem} x{recipe.resultAmount}");
            RefreshRecipes(); // Обновляем UI после крафта
        }
    }
}

// Класс для обработки двойного клика на кнопке рецепта
public class RecipeButtonHandler : MonoBehaviour, IPointerClickHandler
{
    private CraftingUI craftingUI;
    private CraftingSystem.Recipe recipe;
    private float lastClickTime = 0f;
    private const float doubleClickTime = 0.3f; // Максимальный интервал для двойного клика

    public void Setup(CraftingUI ui, CraftingSystem.Recipe recipeToCraft)
    {
        craftingUI = ui;
        recipe = recipeToCraft;
        Debug.Log($"RecipeButtonHandler setup for recipe: {recipe.resultItem}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - lastClickTime <= doubleClickTime)
        {
            // Двойной клик — выполняем крафт
            craftingUI.CraftRecipe(recipe);
            lastClickTime = 0f; // Сбрасываем после крафта
            Debug.Log($"Double-click detected, crafting {recipe.resultItem}");
        }
        else
        {
            // Одинарный клик — сохраняем время
            lastClickTime = currentTime;
            Debug.Log($"Single click on {recipe.resultItem}, waiting for double-click...");
        }
    }
}