using UnityEngine;

public class PlayerStateEffects : MonoBehaviour
{
    [Header("References")]
    public HeartRateArousalSystem arousalSystem;

    [Header("Player Runtime Effects")]
    public float staminaConsumeMultiplier = 1f;  //体力消耗倍率
    public float staminaRecoveryMultiplier = 1f; //体力恢复倍率
    public float hearingMultiplier = 1f; //听觉倍率
    public float tinnitusIntensity = 0f; //耳鸣强度
    public float screenShakeIntensity = 0f; //屏幕抖动强度

    [Header("Smooth")]
    public float effectSmoothSpeed = 5f;

    private float targetStaminaConsume = 1f;
    private float targetStaminaRecovery = 1f;
    private float targetHearing = 1f;
    private float targetTinnitus = 0f;
    private float targetScreenShake = 0f;

    void Start()
    {
        if (arousalSystem == null)
        {
            Debug.LogError("PlayerStateEffects: arousalSystem is missing.");
            enabled = false;
            return;
        }

        arousalSystem.OnStateChanged += HandleStateChanged;

        ApplyStateTargets(arousalSystem.currentState);
    }

    void OnDestroy()
    {
        if (arousalSystem != null)
        {
            arousalSystem.OnStateChanged -= HandleStateChanged;
        }
    }

    void Update()
    {
        staminaConsumeMultiplier = Mathf.Lerp(
            staminaConsumeMultiplier,
            targetStaminaConsume,
            Time.deltaTime * effectSmoothSpeed
        );

        staminaRecoveryMultiplier = Mathf.Lerp(
            staminaRecoveryMultiplier,
            targetStaminaRecovery,
            Time.deltaTime * effectSmoothSpeed
        );

        hearingMultiplier = Mathf.Lerp(
            hearingMultiplier,
            targetHearing,
            Time.deltaTime * effectSmoothSpeed
        );

        tinnitusIntensity = Mathf.Lerp(
            tinnitusIntensity,
            targetTinnitus,
            Time.deltaTime * effectSmoothSpeed
        );

        screenShakeIntensity = Mathf.Lerp(
            screenShakeIntensity,
            targetScreenShake,
            Time.deltaTime * effectSmoothSpeed
        );

        // ===== 这里把参数接到系统 =====
        // playerStamina.consumeMultiplier = staminaConsumeMultiplier;
        // playerStamina.recoveryMultiplier = staminaRecoveryMultiplier;
        // audioController.SetHearingMultiplier(hearingMultiplier);
        // audioController.SetTinnitusIntensity(tinnitusIntensity);
        // cameraEffect.SetShakeIntensity(screenShakeIntensity);
        // ==================================
    }

    void HandleStateChanged(HeartRateArousalSystem.ArousalState state)
    {
        ApplyStateTargets(state);
    }

    void ApplyStateTargets(HeartRateArousalSystem.ArousalState state)
    {
        switch (state)
        {
            case HeartRateArousalSystem.ArousalState.Calm:
                targetStaminaConsume = 0.85f;
                targetStaminaRecovery = 1.25f;
                targetHearing = 1.3f;
                targetTinnitus = 0f;
                targetScreenShake = 0f;
                break;

            case HeartRateArousalSystem.ArousalState.Alert:
                targetStaminaConsume = 1f;
                targetStaminaRecovery = 1f;
                targetHearing = 1f;
                targetTinnitus = 0.05f;
                targetScreenShake = 0.03f;
                break;

            case HeartRateArousalSystem.ArousalState.Tense:
                targetStaminaConsume = 1.5f;
                targetStaminaRecovery = 0.8f;
                targetHearing = 0.9f;
                targetTinnitus = 0.35f;
                targetScreenShake = 0.15f;
                break;

            case HeartRateArousalSystem.ArousalState.Panic:
                targetStaminaConsume = 2f;
                targetStaminaRecovery = 0.5f;
                targetHearing = 0.75f;
                targetTinnitus = 0.8f;
                targetScreenShake = 0.35f;
                break;
        }
    }
}