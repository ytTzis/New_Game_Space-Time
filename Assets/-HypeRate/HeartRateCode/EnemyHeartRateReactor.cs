using UnityEngine;

public class EnemyHeartRateReactor : MonoBehaviour
{
    [Header("References")]
    public HeartRateArousalSystem arousalSystem;

    [Header("Base Enemy Stats")]
    public float baseAggroRange = 8f;
    public float baseAttackInterval = 2f;
    public float baseAttackDesire = 1f;

    [Header("Runtime Enemy Stats")]
    public float currentAggroRange; //仇恨范围
    public float currentAttackInterval; //攻击间隔
    public float currentAttackDesire; //攻击欲望

    [Header("Smooth")]
    public float smoothSpeed = 3f;

    private float targetAggroRange;
    private float targetAttackInterval;
    private float targetAttackDesire;

    void Start()
    {
        if (arousalSystem == null)
        {
            Debug.LogError("EnemyHeartRateReactor: arousalSystem is missing.");
            enabled = false;
            return;
        }

        arousalSystem.OnStateChanged += HandleStateChanged;

        currentAggroRange = baseAggroRange;
        currentAttackInterval = baseAttackInterval;
        currentAttackDesire = baseAttackDesire;

        ApplyTargets(arousalSystem.currentState);
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
        currentAggroRange = Mathf.Lerp(
            currentAggroRange,
            targetAggroRange,
            Time.deltaTime * smoothSpeed
        );

        currentAttackInterval = Mathf.Lerp(
            currentAttackInterval,
            targetAttackInterval,
            Time.deltaTime * smoothSpeed
        );

        currentAttackDesire = Mathf.Lerp(
            currentAttackDesire,
            targetAttackDesire,
            Time.deltaTime * smoothSpeed
        );

        // ===== 这里把参数接到敌人AI =====
        // enemyAI.aggroRange = currentAggroRange;
        // enemyAI.attackInterval = currentAttackInterval;
        // enemyAI.attackDesire = currentAttackDesire;
        // =====================================
    }

    void HandleStateChanged(HeartRateArousalSystem.ArousalState state)
    {
        ApplyTargets(state);
    }

    void ApplyTargets(HeartRateArousalSystem.ArousalState state)
    {
        switch (state)
        {
            case HeartRateArousalSystem.ArousalState.Calm:
                targetAggroRange = baseAggroRange * 0.85f;
                targetAttackInterval = baseAttackInterval * 1.1f;
                targetAttackDesire = baseAttackDesire * 0.9f;
                break;

            case HeartRateArousalSystem.ArousalState.Alert:
                targetAggroRange = baseAggroRange;
                targetAttackInterval = baseAttackInterval;
                targetAttackDesire = baseAttackDesire;
                break;

            case HeartRateArousalSystem.ArousalState.Tense:
                targetAggroRange = baseAggroRange * 1.25f;
                targetAttackInterval = baseAttackInterval * 0.8f;
                targetAttackDesire = baseAttackDesire * 1.3f;
                break;

            case HeartRateArousalSystem.ArousalState.Panic:
                targetAggroRange = baseAggroRange * 1.5f;
                targetAttackInterval = baseAttackInterval * 0.65f;
                targetAttackDesire = baseAttackDesire * 1.6f;
                break;
        }
    }
}