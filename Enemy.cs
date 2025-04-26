using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 50f;
    private float damageMultiplier = 1f; // Модификатор урона (увеличивается ночью)

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Enemy hit! Health: " + health);
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }
}