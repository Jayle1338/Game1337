using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class DayNightCycle : MonoBehaviour
{
    public Light directionalLight; // Основной источник света (солнце/луна)
    public float dayDuration = 600f; // Длительность одного дня в секундах (10 минут)
    private float currentTime = 0f; // Текущее время в цикле (0 - утро, 0.5 - ночь)
    private SurvivalStats playerSurvivalStats; // Ссылка на SurvivalStats игрока
    private Enemy[] enemies; // Все враги на сцене
    private Volume volume; // Глобальный Volume для управления HDRP
    private VisualEnvironment visualEnvironment; // Компонент для управления типом неба
    private Exposure exposure; // Компонент для управления экспозицией
    private HDAdditionalLightData lightData; // Дополнительные данные для Directional Light в HDRP
    private PhysicallyBasedSky physicallyBasedSky; // Компонент для Physically Based Sky
    private HDRISky hdriSky; // Компонент для HDRI Sky

    // Цвета света для разных времён суток
    private Color dayColor = new Color(1f, 0.95f, 0.8f); // Тёплый дневной свет
    private Color nightColor = new Color(0.1f, 0.15f, 0.3f); // Более тёмный ночной свет

    void Start()
    {
        // Ищем DirectionalLight, если не назначен
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
            if (directionalLight == null)
            {
                Debug.LogError("Directional Light not found in the scene! Please add one.");
            }
            else if (directionalLight.type != LightType.Directional)
            {
                Debug.LogError("Assigned light is not a Directional Light! Please assign a Directional Light.");
            }
        }

        // Получаем HDAdditionalLightData для управления светом в HDRP
        lightData = directionalLight.GetComponent<HDAdditionalLightData>();
        if (lightData == null)
        {
            Debug.LogError("HDAdditionalLightData component not found on Directional Light!");
        }

        // Ищем Volume на сцене
        volume = FindObjectOfType<Volume>();
        if (volume == null)
        {
            Debug.LogError("Volume component not found in the scene! Please add a Global Volume.");
        }
        else
        {
            // Получаем компоненты VisualEnvironment и Exposure из профиля Volume
            if (!volume.sharedProfile.TryGet(out visualEnvironment))
            {
                Debug.LogError("VisualEnvironment component not found in Volume Profile! Please add it.");
            }
            if (!volume.sharedProfile.TryGet(out exposure))
            {
                Debug.LogError("Exposure component not found in Volume Profile! Please add it.");
            }
            // Проверяем тип неба и получаем соответствующий компонент
            if (visualEnvironment != null)
            {
                int skyType = visualEnvironment.skyType.value;
                if (skyType == (int)SkyType.PhysicallyBased) // Physically Based Sky
                {
                    if (!volume.sharedProfile.TryGet(out physicallyBasedSky))
                    {
                        Debug.LogError("PhysicallyBasedSky component not found in Volume Profile! Please add it.");
                    }
                }
                else if (skyType == (int)SkyType.HDRI) // HDRI Sky
                {
                    if (!volume.sharedProfile.TryGet(out hdriSky))
                    {
                        Debug.LogError("HDRISky component not found in Volume Profile! Please add it.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Unsupported Sky Type: {skyType}. Please use Physically Based Sky or HDRI Sky.");
                }
            }
        }

        // Ищем SurvivalStats игрока
        playerSurvivalStats = FindObjectOfType<SurvivalStats>();
        if (playerSurvivalStats == null)
        {
            Debug.LogError("SurvivalStats not found on Player! Please ensure the Player has this component.");
        }

        // Ищем всех врагов на сцене
        enemies = FindObjectsOfType<Enemy>();
        if (enemies.Length == 0)
        {
            Debug.LogWarning("No enemies found in the scene!");
        }

        // Устанавливаем начальное время (утро)
        currentTime = 0f;
    }

    void Update()
    {
        // Обновляем время
        currentTime += Time.deltaTime / dayDuration;
        if (currentTime >= 1f) currentTime = 0f; // Зацикливаем день

        // Обновляем угол света (солнце/луна движется по небу)
        float sunAngle = currentTime * 360f - 90f; // От -90 (утро) до 270 (утро следующего дня)
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 45f, 0f);
            Debug.Log($"Sun Angle: {sunAngle}, Light Rotation: {directionalLight.transform.rotation.eulerAngles}");
        }

        // Обновляем цвет и интенсивность света через HDAdditionalLightData
        float dayNightLerp = Mathf.Sin(currentTime * Mathf.PI * 2f) * 0.5f + 0.5f; // Плавный переход (0 - ночь, 1 - день)
        if (lightData != null)
        {
            lightData.color = Color.Lerp(nightColor, dayColor, dayNightLerp);
            lightData.intensity = Mathf.Lerp(0.5f, 5f, dayNightLerp); // В HDRP интенсивность в люксах
            Debug.Log($"Light Color: {lightData.color}, Intensity: {lightData.intensity} lux, Lerp: {dayNightLerp}");
        }

        // Обновляем экспозицию для затемнения сцены ночью
        if (exposure != null)
        {
            exposure.fixedExposure.Override(Mathf.Lerp(-2f, 0f, dayNightLerp)); // Экспозиция: ночь темнее, день светлее
            Debug.Log($"Exposure: {exposure.fixedExposure.value}");
        }

        // Обновляем вращение неба в зависимости от типа неба
        if (physicallyBasedSky != null)
        {
            physicallyBasedSky.rotation.Override(sunAngle);
            Debug.Log($"PhysicallyBasedSky Rotation: {physicallyBasedSky.rotation.value}");
        }
        else if (hdriSky != null)
        {
            hdriSky.rotation.Override(sunAngle);
            Debug.Log($"HDRISky Rotation: {hdriSky.rotation.value}");
        }

        // Влияние на усталость игрока
        if (playerSurvivalStats != null)
        {
            playerSurvivalStats.SetDayNightMultiplier(IsNight() ? 1.5f : 1f); // Ночью усталость растёт быстрее
        }

        // Увеличиваем урон врагов ночью
        foreach (Enemy enemy in enemies)
        {
            enemy.SetDamageMultiplier(IsNight() ? 1.5f : 1f); // Ночью враги наносят на 50% больше урона
        }
    }

    public bool IsNight()
    {
        // Ночь: с 0.4 до 0.6 по времени цикла (примерно с 18:00 до 6:00 в игровом времени)
        return currentTime > 0.4f && currentTime < 0.6f;
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }
}