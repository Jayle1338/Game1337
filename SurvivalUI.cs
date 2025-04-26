using UnityEngine;
using UnityEngine.UI;

public class SurvivalUI : MonoBehaviour
{
    public Slider hungerSlider;
    public Slider thirstSlider;
    public Slider fatigueSlider;
    public SurvivalStats survivalStats;
    public float smoothSpeed = 2f; // Скорость сглаживания

    private float currentHungerValue;
    private float currentThirstValue;
    private float currentFatigueValue;

    void Start()
    {
        if (survivalStats == null)
        {
            survivalStats = FindObjectOfType<SurvivalStats>();
            if (survivalStats == null)
            {
                Debug.LogWarning("SurvivalStats not found! Please add it to the scene.");
            }
        }

        if (hungerSlider != null)
        {
            hungerSlider.value = survivalStats.hunger;
            currentHungerValue = survivalStats.hunger;
        }
        if (thirstSlider != null)
        {
            thirstSlider.value = survivalStats.thirst;
            currentThirstValue = survivalStats.thirst;
        }
        if (fatigueSlider != null)
        {
            fatigueSlider.value = survivalStats.fatigue;
            currentFatigueValue = survivalStats.fatigue;
        }
    }

    void Update()
    {
        if (survivalStats != null)
        {
            if (hungerSlider != null)
            {
                currentHungerValue = Mathf.Lerp(currentHungerValue, survivalStats.hunger, smoothSpeed * Time.deltaTime);
                hungerSlider.value = currentHungerValue;
            }
            if (thirstSlider != null)
            {
                currentThirstValue = Mathf.Lerp(currentThirstValue, survivalStats.thirst, smoothSpeed * Time.deltaTime);
                thirstSlider.value = currentThirstValue;
            }
            if (fatigueSlider != null)
            {
                currentFatigueValue = Mathf.Lerp(currentFatigueValue, survivalStats.fatigue, smoothSpeed * Time.deltaTime);
                fatigueSlider.value = currentFatigueValue;
            }
        }
    }
}