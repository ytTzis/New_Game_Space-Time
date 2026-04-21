using System.Collections.Generic;
using UGG.Combat;
using UGG.Health;
using UnityEngine;

public class CombatHeartRateVisualizationController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerCombatSystem playerCombatSystem;
    [SerializeField] private PlayerHealthSystem playerHealthSystem;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private GameObject nonCombatUIRoot;
    [SerializeField] private GameObject[] uiObjectsHiddenInCombat;
    [SerializeField] private GameObject combatUIRoot;

    [Header("Combat Detection")]
    [SerializeField] private AICombatSystem[] enemySensors;
    [SerializeField] private float enemyCombatDistance = 8f;

    [Header("Lighting Response")]
    [SerializeField] private bool affectMainDirectionalLight = true;
    [SerializeField] private bool affectResponsiveLights = true;
    [SerializeField] private Light[] responsiveLights;
    [SerializeField] private float responsiveLightPulseAmount = 0.6f;

    [Header("Health Color Stages")]
    [SerializeField] private Color highHealthColor = new Color(0.15f, 0.95f, 0.85f);
    [SerializeField] private Color midHealthColor = new Color(1.0f, 0.65f, 0.2f);
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.2f, 0.65f);
    [SerializeField, Range(0f, 1f)] private float midHealthThreshold = 0.66f;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.33f;

    [Header("Heart Pulse")]
    [SerializeField] private float combatBaseIntensity = 1.2f;
    [SerializeField] private float pulseIntensityAmount = 0.8f;
    [SerializeField] private float transitionSpeed = 4f;
    [SerializeField] private int fallbackHeartRate = 72;
    [SerializeField] private int minimumHeartRate = 45;
    [SerializeField] private int maximumHeartRate = 180;

    [Header("Debug Heart Rate")]
    [SerializeField] private bool useDebugHeartRate;
    [SerializeField] private int debugHeartRate = 72;

    [Header("Debug Combat State")]
    [SerializeField] private bool useDebugCombatState;
    [SerializeField] private bool debugIsInCombat;

    private static readonly string[] PersistentNonCombatUIChildNames = { "LeftText", "StressVignette" };

    private Color defaultLightColor;
    private float defaultLightIntensity;
    private Color[] responsiveLightDefaultColors;
    private float[] responsiveLightDefaultIntensities;

    private void Reset()
    {
        TryAutoBind();
    }

    private void Awake()
    {
        TryAutoBind();
        CacheDefaultLightState();
    }

    private void OnValidate()
    {
        ClampHeartRateRange();
        TryAutoBindCombatHiddenUI();
    }

    private void Update()
    {
        bool isInCombat = GetActiveCombatState();
        UpdateUIState(isInCombat);
        UpdateLights(isInCombat);
    }

    private void TryAutoBind()
    {
        if (playerCombatSystem == null)
        {
            playerCombatSystem = FindFirstObjectByType<PlayerCombatSystem>();
        }

        if (playerHealthSystem == null)
        {
            playerHealthSystem = FindFirstObjectByType<PlayerHealthSystem>();
        }

        if (playerRoot == null && playerCombatSystem != null)
        {
            playerRoot = playerCombatSystem.transform.root;
        }

        if (mainDirectionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light sceneLight in lights)
            {
                if (sceneLight.type == LightType.Directional)
                {
                    mainDirectionalLight = sceneLight;
                    break;
                }
            }
        }

        if ((responsiveLights == null || responsiveLights.Length == 0) && mainDirectionalLight != null)
        {
            responsiveLights = new[] { mainDirectionalLight };
        }

        if (enemySensors == null || enemySensors.Length == 0)
        {
            enemySensors = FindObjectsByType<AICombatSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        TryAutoBindCombatHiddenUI();
    }

    private void TryAutoBindCombatHiddenUI()
    {
        if (nonCombatUIRoot == null || (uiObjectsHiddenInCombat != null && uiObjectsHiddenInCombat.Length > 0))
        {
            return;
        }

        List<GameObject> autoHiddenObjects = new List<GameObject>();
        Transform nonCombatRootTransform = nonCombatUIRoot.transform;

        for (int i = 0; i < nonCombatRootTransform.childCount; i++)
        {
            Transform child = nonCombatRootTransform.GetChild(i);
            if (child == null || ShouldPersistOutsideCombat(child.name))
            {
                continue;
            }

            autoHiddenObjects.Add(child.gameObject);
        }

        uiObjectsHiddenInCombat = autoHiddenObjects.ToArray();
    }

    private static bool ShouldPersistOutsideCombat(string childName)
    {
        foreach (string persistentChildName in PersistentNonCombatUIChildNames)
        {
            if (childName == persistentChildName)
            {
                return true;
            }
        }

        return false;
    }

    private void CacheDefaultLightState()
    {
        if (mainDirectionalLight != null)
        {
            defaultLightColor = mainDirectionalLight.color;
            defaultLightIntensity = mainDirectionalLight.intensity;
        }

        if (responsiveLights == null)
        {
            responsiveLightDefaultColors = null;
            responsiveLightDefaultIntensities = null;
            return;
        }

        responsiveLightDefaultColors = new Color[responsiveLights.Length];
        responsiveLightDefaultIntensities = new float[responsiveLights.Length];

        for (int i = 0; i < responsiveLights.Length; i++)
        {
            Light responsiveLight = responsiveLights[i];
            if (responsiveLight == null)
            {
                continue;
            }

            responsiveLightDefaultColors[i] = responsiveLight.color;
            responsiveLightDefaultIntensities[i] = responsiveLight.intensity;
        }
    }

    private void ClampHeartRateRange()
    {
        if (maximumHeartRate < minimumHeartRate)
        {
            maximumHeartRate = minimumHeartRate;
        }
    }

    private bool IsInCombat()
    {
        if (playerCombatSystem != null && playerCombatSystem.GetCurrentTarget() != null)
        {
            return true;
        }

        if (enemySensors == null)
        {
            return false;
        }

        foreach (AICombatSystem sensor in enemySensors)
        {
            if (sensor == null)
            {
                continue;
            }

            Transform currentTarget = sensor.GetCurrentTarget();
            if (currentTarget == null)
            {
                continue;
            }

            if (playerRoot == null || currentTarget.root == playerRoot)
            {
                if (sensor.GetCurrentTargetDistance() > enemyCombatDistance)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private bool GetActiveCombatState()
    {
        if (useDebugCombatState)
        {
            return debugIsInCombat;
        }

        return IsInCombat();
    }

    private void UpdateUIState(bool isInCombat)
    {
        bool hasCombatHiddenUI = uiObjectsHiddenInCombat != null && uiObjectsHiddenInCombat.Length > 0;

        if (nonCombatUIRoot != null && !hasCombatHiddenUI)
        {
            nonCombatUIRoot.SetActive(!isInCombat);
        }

        if (uiObjectsHiddenInCombat != null)
        {
            foreach (GameObject uiObject in uiObjectsHiddenInCombat)
            {
                if (uiObject == null)
                {
                    continue;
                }

                uiObject.SetActive(!isInCombat);
            }
        }

        if (combatUIRoot != null)
        {
            combatUIRoot.SetActive(isInCombat);
        }
    }

    private void UpdateLights(bool isInCombat)
    {
        Color targetColor = GetCombatColorFromHealth();
        float pulseValue = GetPulseValueFromHeartRate();
        float mainLightTargetIntensity = combatBaseIntensity + pulseValue * pulseIntensityAmount;

        UpdateMainDirectionalLight(isInCombat, targetColor, mainLightTargetIntensity);
        UpdateResponsiveLights(isInCombat, targetColor, pulseValue);
    }

    private void UpdateMainDirectionalLight(bool isInCombat, Color targetColor, float targetIntensity)
    {
        if (!affectMainDirectionalLight || mainDirectionalLight == null)
        {
            return;
        }

        if (!isInCombat)
        {
            mainDirectionalLight.color = Color.Lerp(
                mainDirectionalLight.color,
                defaultLightColor,
                Time.deltaTime * transitionSpeed);

            mainDirectionalLight.intensity = Mathf.Lerp(
                mainDirectionalLight.intensity,
                defaultLightIntensity,
                Time.deltaTime * transitionSpeed);
            return;
        }

        mainDirectionalLight.color = Color.Lerp(
            mainDirectionalLight.color,
            targetColor,
            Time.deltaTime * transitionSpeed);

        mainDirectionalLight.intensity = Mathf.Lerp(
            mainDirectionalLight.intensity,
            targetIntensity,
            Time.deltaTime * transitionSpeed);
    }

    private void UpdateResponsiveLights(bool isInCombat, Color targetColor, float pulseValue)
    {
        if (!affectResponsiveLights || responsiveLights == null || responsiveLights.Length == 0)
        {
            return;
        }

        if (responsiveLightDefaultColors == null || responsiveLightDefaultColors.Length != responsiveLights.Length)
        {
            CacheDefaultLightState();
        }

        for (int i = 0; i < responsiveLights.Length; i++)
        {
            Light responsiveLight = responsiveLights[i];
            if (responsiveLight == null)
            {
                continue;
            }

            Color defaultColor = responsiveLightDefaultColors[i];
            float defaultIntensity = responsiveLightDefaultIntensities[i];

            if (!isInCombat)
            {
                responsiveLight.color = Color.Lerp(
                    responsiveLight.color,
                    defaultColor,
                    Time.deltaTime * transitionSpeed);

                responsiveLight.intensity = Mathf.Lerp(
                    responsiveLight.intensity,
                    defaultIntensity,
                    Time.deltaTime * transitionSpeed);
                continue;
            }

            float targetIntensity = defaultIntensity + pulseValue * responsiveLightPulseAmount;

            responsiveLight.color = Color.Lerp(
                responsiveLight.color,
                targetColor,
                Time.deltaTime * transitionSpeed);

            responsiveLight.intensity = Mathf.Lerp(
                responsiveLight.intensity,
                targetIntensity,
                Time.deltaTime * transitionSpeed);
        }
    }

    private Color GetCombatColorFromHealth()
    {
        if (playerHealthSystem == null)
        {
            return highHealthColor;
        }

        // Temporarily disable HealthNormalized-driven color stages until
        // the project's player health system exposes a normalized HP value.
        // float healthNormalized = playerHealthSystem.HealthNormalized;
        // if (healthNormalized <= lowHealthThreshold)
        // {
        //     return lowHealthColor;
        // }
        //
        // if (healthNormalized <= midHealthThreshold)
        // {
        //     return midHealthColor;
        // }

        return highHealthColor;
    }

    public int GetActiveHeartRate()
    {
        int heartRate = useDebugHeartRate ? debugHeartRate : hyperateSocket.CurrentHeartRate;
        if (heartRate <= 0)
        {
            heartRate = fallbackHeartRate;
        }

        return Mathf.Clamp(heartRate, minimumHeartRate, maximumHeartRate);
    }

    private float GetPulseValueFromHeartRate()
    {
        float beatsPerSecond = GetActiveHeartRate() / 60f;
        float pulse = Mathf.Sin(Time.time * beatsPerSecond * Mathf.PI * 2f);
        return (pulse + 1f) * 0.5f;
    }
}
