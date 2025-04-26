using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField] private string resourceType = "Wood"; // Тип ресурса или предмета (например, "Wood", "Iron")
    [SerializeField] private int amount = 10; // Количество ресурса или предмета
    [SerializeField] private bool isDroppedItem = false; // Флаг: true — выброшенный предмет, false — стационарный ресурс
    [SerializeField] private AudioClip harvestSound; // Звук для сбора стационарного ресурса

    void Start()
    {
        // Проверки на корректность данных
        if (string.IsNullOrEmpty(resourceType))
        {
            Debug.LogWarning($"Resource on {gameObject.name} has no Resource Type assigned!");
        }
        if (amount <= 0)
        {
            Debug.LogWarning($"Resource on {gameObject.name} has invalid Amount: {amount}");
        }

        if (isDroppedItem)
        {
            // Если это выброшенный предмет, добавляем физику для падения
            EnsurePhysicsComponents();
            Debug.Log($"Dropped Resource initialized on {gameObject.name}: {resourceType} x{amount}");
        }
        else
        {
            Debug.Log($"Static Resource initialized on {gameObject.name}: {resourceType} x{amount}");
        }
    }

    // Инициализация ресурса или предмета
    public void Initialize(string type, int amt, bool dropped = false)
    {
        resourceType = type;
        amount = amt;
        isDroppedItem = dropped;

        if (isDroppedItem)
        {
            EnsurePhysicsComponents();
        }

        Debug.Log($"Resource {gameObject.name} initialized with {resourceType} x{amount}, Dropped: {isDroppedItem}");
    }

    // Добавляем физику для выброшенных предметов
    private void EnsurePhysicsComponents()
    {
        // Добавляем Rigidbody, если его нет
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
        }

        // Добавляем SphereCollider, если его нет
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            collider.isTrigger = false;
        }

        // Устанавливаем масштаб для выброшенных предметов
        transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    }

    // Сбор стационарного ресурса (например, дерева)
    public void Harvest()
    {
        if (isDroppedItem)
        {
            Debug.LogWarning($"Cannot harvest a dropped item! Use Pickup instead for {resourceType} x{amount}");
            return;
        }

        Debug.Log($"Harvested {amount} {resourceType}");
        if (harvestSound != null)
        {
            Debug.Log($"Playing sound: {harvestSound.name}, length: {harvestSound.length}");
            AudioSource.PlayClipAtPoint(harvestSound, transform.position, 1.0f);
        }
        else
        {
            Debug.LogWarning("Harvest sound is not assigned!");
        }
        Destroy(gameObject);
    }

    // Геттеры для типа и количества
    public string GetResourceType()
    {
        return resourceType;
    }

    public int GetAmount()
    {
        return amount;
    }

    // Проверка, является ли объект выброшенным предметом
    public bool IsDroppedItem()
    {
        return isDroppedItem;
    }
}