using UnityEngine;

public class WaterSource : MonoBehaviour
{
    public float interactRange = 3f;
    public Player player;
    public float thirstRestoreAmount = 20f; // Сколько жажды восстанавливается при питье напрямую

    void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogWarning("Player not found! Please add a Player to the scene.");
            }
        }
    }

    void Update()
    {
        // Логика перемещена в Player.cs, здесь оставляем только ссылку на игрока
    }
}