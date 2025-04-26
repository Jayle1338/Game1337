using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public Transform playerBody;
    public InventoryUI inventoryUI;
    private CraftingSystem craftingSystem; // Ссылка на CraftingSystem

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        // Устанавливаем локальную позицию камеры (относительно Player)
        transform.localPosition = new Vector3(0, 0.8f, 0);

        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogError("InventoryUI not found in the scene!");
            }
        }

        // Ищем CraftingSystem
        craftingSystem = FindObjectOfType<CraftingSystem>();
        if (craftingSystem == null)
        {
            Debug.LogError("CraftingSystem not found in the scene!");
        }
    }

    void Update()
    {
        // Проверяем состояния UI
        bool isInventoryOpen = inventoryUI != null && inventoryUI.IsInventoryOpen;
        bool isCraftingOpen = craftingSystem != null && craftingSystem.IsCraftingWindowOpen(); // Добавляем скобки ()

        // Управляем состоянием курсора
        if (isCraftingOpen || isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Разрешаем движение камеры, только если ни одно UI не открыто
        if (!isInventoryOpen && !isCraftingOpen)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}