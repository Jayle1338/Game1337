using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackRate = 2f;
    public string enemyTag = "Enemy";
    public Transform attackPoint;
    public InventoryUI inventoryUI;

    private float nextAttackTime = 0f;

    void Update()
    {
        bool isInventoryOpen = inventoryUI != null && inventoryUI.IsInventoryOpen;

        if (!isInventoryOpen)
        {
            if (Time.time >= nextAttackTime)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    Attack();
                    nextAttackTime = Time.time + 1f / attackRate;
                }
            }
        }
    }

    void Attack()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag(enemyTag))
            {
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(attackDamage);
                    Debug.Log("Hit enemy!");
                }
            }
        }
    }
}