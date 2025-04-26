using UnityEngine;

public class SurvivalStats : MonoBehaviour
{
    public float hunger = 100f;
    public float thirst = 100f;
    public float stamina = 100f;
    public float fatigue = 0f;

    public float hungerDecreaseRate = 2f;
    public float thirstDecreaseRate = 3f;
    public float staminaRecoveryRate = 5f;
    public float fatigueIncreaseRate = 1f;
    public float fatigueDecreaseRate = 2f;

    private float dayNightMultiplier = 1f; // Модификатор для усталости от времени суток

    void Update()
    {
        hunger -= hungerDecreaseRate * Time.deltaTime;
        thirst -= thirstDecreaseRate * Time.deltaTime;

        if (hunger < 0f) hunger = 0f;
        if (thirst < 0f) thirst = 0f;

        if (IsMoving())
        {
            fatigue += fatigueIncreaseRate * dayNightMultiplier * Time.deltaTime;
        }
        else
        {
            fatigue -= fatigueDecreaseRate * Time.deltaTime;
        }

        if (fatigue > 100f) fatigue = 100f;
        if (fatigue < 0f) fatigue = 0f;

        if (!IsMoving())
        {
            stamina += staminaRecoveryRate * Time.deltaTime;
        }
        if (stamina > 100f) stamina = 100f;
        if (stamina < 0f) stamina = 0f;
    }

    public void RestoreThirst(float amount)
    {
        thirst += amount;
        if (thirst > 100f) thirst = 100f;
    }

    public bool CanSprint()
    {
        return stamina > 0f && fatigue < 80f;
    }

    public void ConsumeStamina(float amount)
    {
        stamina -= amount;
        fatigue += amount * 0.5f;
        if (stamina < 0f) stamina = 0f;
        if (fatigue > 100f) fatigue = 100f;
    }

    public void ConsumeItem(string itemType)
    {
        if (itemType == "Herbs")
        {
            hunger += 10f;
            if (hunger > 100f) hunger = 100f;
            Debug.Log("Consumed Herbs, hunger restored by 10");
        }
        else if (itemType == "Food")
        {
            hunger += 20f;
            if (hunger > 100f) hunger = 100f;
            Debug.Log("Consumed Food, hunger restored by 20");
        }
    }

    public float GetHunger()
    {
        return hunger;
    }

    public float GetThirst()
    {
        return thirst;
    }

    public float GetFatigue()
    {
        return fatigue;
    }

    bool IsMoving()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            Vector3 velocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            return velocity.magnitude > 0.1f;
        }
        return false;
    }

    public void SetDayNightMultiplier(float multiplier)
    {
        dayNightMultiplier = multiplier;
    }
}