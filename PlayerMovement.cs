using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float sprintMultiplier = 1.5f;
    public float gravity = -9.81f;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private SurvivalStats survivalStats;
    private Inventory inventory;
    public Hotbar hotbar;
    private InventoryUI inventoryUI; // Добавляем ссылку на InventoryUI

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("CharacterController not found on Player!");
        }

        survivalStats = GetComponent<SurvivalStats>();
        if (survivalStats == null)
        {
            Debug.LogError("SurvivalStats not found on Player!");
        }

        inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Inventory not found on Player!");
        }

        hotbar = FindObjectOfType<Hotbar>();
        if (hotbar == null)
        {
            Debug.LogError("Hotbar not found in the scene!");
        }

        inventoryUI = FindObjectOfType<InventoryUI>(); // Ищем InventoryUI
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI not found in the scene!");
        }
    }

    void Update()
    {
        // Проверяем, открыт ли инвентарь
        bool isInventoryOpen = inventoryUI != null && inventoryUI.IsInventoryOpen;

        if (isInventoryOpen)
        {
            // Если инвентарь открыт, останавливаем движение
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        // Проверка, находится ли игрок на земле
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Движение
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        float currentSpeed = moveSpeed;

        // Спринт
        if (Input.GetKey(KeyCode.LeftShift) && survivalStats != null && survivalStats.CanSprint())
        {
            currentSpeed *= sprintMultiplier;
            survivalStats.ConsumeStamina(5f * Time.deltaTime);
        }

        controller.Move(move * currentSpeed * Time.deltaTime);

        // Прыжок
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (survivalStats != null)
            {
                survivalStats.ConsumeStamina(2f);
            }
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}