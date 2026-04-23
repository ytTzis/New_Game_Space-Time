using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CombatHeartRateVisualizationController))]
public class CombatHeartRateVisualizationControllerEditor : Editor
{
    private SerializedProperty playerCombatSystem;
    private SerializedProperty playerHealthSystem;
    private SerializedProperty playerRoot;
    private SerializedProperty mainDirectionalLight;
    private SerializedProperty nonCombatUIRoot;
    private SerializedProperty combatUIRoot;

    private SerializedProperty enemySensors;
    private SerializedProperty enemyCombatDistance;

    private SerializedProperty affectMainDirectionalLight;
    private SerializedProperty affectResponsiveLights;
    private SerializedProperty responsiveLights;
    private SerializedProperty responsiveLightPulseAmount;

    private SerializedProperty highHealthColor;
    private SerializedProperty midHealthColor;
    private SerializedProperty lowHealthColor;
    private SerializedProperty midHealthThreshold;
    private SerializedProperty lowHealthThreshold;

    private SerializedProperty combatBaseIntensity;
    private SerializedProperty pulseIntensityAmount;
    private SerializedProperty transitionSpeed;
    private SerializedProperty fallbackHeartRate;
    private SerializedProperty minimumHeartRate;
    private SerializedProperty maximumHeartRate;

    private SerializedProperty useDebugHeartRate;
    private SerializedProperty debugHeartRate;
    private SerializedProperty useDebugCombatState;
    private SerializedProperty debugIsInCombat;

    private MonoScript scriptAsset;

    private void OnEnable()
    {
        playerCombatSystem = serializedObject.FindProperty("playerCombatSystem");
        playerHealthSystem = serializedObject.FindProperty("playerHealthSystem");
        playerRoot = serializedObject.FindProperty("playerRoot");
        mainDirectionalLight = serializedObject.FindProperty("mainDirectionalLight");
        nonCombatUIRoot = serializedObject.FindProperty("nonCombatUIRoot");
        combatUIRoot = serializedObject.FindProperty("combatUIRoot");

        enemySensors = serializedObject.FindProperty("enemySensors");
        enemyCombatDistance = serializedObject.FindProperty("enemyCombatDistance");

        affectMainDirectionalLight = serializedObject.FindProperty("affectMainDirectionalLight");
        affectResponsiveLights = serializedObject.FindProperty("affectResponsiveLights");
        responsiveLights = serializedObject.FindProperty("responsiveLights");
        responsiveLightPulseAmount = serializedObject.FindProperty("responsiveLightPulseAmount");

        highHealthColor = serializedObject.FindProperty("highHealthColor");
        midHealthColor = serializedObject.FindProperty("midHealthColor");
        lowHealthColor = serializedObject.FindProperty("lowHealthColor");
        midHealthThreshold = serializedObject.FindProperty("midHealthThreshold");
        lowHealthThreshold = serializedObject.FindProperty("lowHealthThreshold");

        combatBaseIntensity = serializedObject.FindProperty("combatBaseIntensity");
        pulseIntensityAmount = serializedObject.FindProperty("pulseIntensityAmount");
        transitionSpeed = serializedObject.FindProperty("transitionSpeed");
        fallbackHeartRate = serializedObject.FindProperty("fallbackHeartRate");
        minimumHeartRate = serializedObject.FindProperty("minimumHeartRate");
        maximumHeartRate = serializedObject.FindProperty("maximumHeartRate");

        useDebugHeartRate = serializedObject.FindProperty("useDebugHeartRate");
        debugHeartRate = serializedObject.FindProperty("debugHeartRate");
        useDebugCombatState = serializedObject.FindProperty("useDebugCombatState");
        debugIsInCombat = serializedObject.FindProperty("debugIsInCombat");

        scriptAsset = MonoScript.FromMonoBehaviour((CombatHeartRateVisualizationController)target);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        EditorGUILayout.Space(4f);

        DrawSection("场景引用");
        DrawProperty(playerCombatSystem, "玩家战斗系统");
        DrawProperty(playerHealthSystem, "玩家生命系统");
        DrawProperty(playerRoot, "玩家根节点");
        DrawProperty(mainDirectionalLight, "主方向光");
        DrawProperty(nonCombatUIRoot, "非战斗UI根节点");
        DrawProperty(combatUIRoot, "战斗UI根节点");

        EditorGUILayout.Space(6f);
        DrawSection("战斗检测");
        DrawProperty(enemySensors, "敌人感知器", true);
        DrawProperty(enemyCombatDistance, "敌人判定距离");

        EditorGUILayout.Space(6f);
        DrawSection("灯光响应设置");
        DrawProperty(affectMainDirectionalLight, "启用主方向光响应");
        DrawProperty(affectResponsiveLights, "启用灯组响应");
        DrawProperty(responsiveLights, "响应灯组", true);
        DrawProperty(responsiveLightPulseAmount, "灯组额外脉动强度");

        EditorGUILayout.Space(6f);
        DrawSection("生命值颜色阶段");
        DrawProperty(highHealthColor, "高生命颜色");
        DrawProperty(midHealthColor, "中生命颜色");
        DrawProperty(lowHealthColor, "低生命颜色");
        DrawProperty(midHealthThreshold, "中生命阈值");
        DrawProperty(lowHealthThreshold, "低生命阈值");

        EditorGUILayout.Space(6f);
        DrawSection("心跳脉动");
        DrawProperty(combatBaseIntensity, "战斗基础亮度");
        DrawProperty(pulseIntensityAmount, "主光脉动强度");
        DrawProperty(transitionSpeed, "过渡速度");
        DrawProperty(fallbackHeartRate, "回退心率");
        DrawProperty(minimumHeartRate, "最小心率");
        DrawProperty(maximumHeartRate, "最大心率");

        EditorGUILayout.Space(6f);
        DrawSection("调试心率");
        DrawProperty(useDebugHeartRate, "使用调试心率");
        DrawProperty(debugHeartRate, "调试心率值");

        EditorGUILayout.Space(6f);
        DrawSection("调试战斗状态");
        DrawProperty(useDebugCombatState, "使用调试战斗状态");
        DrawProperty(debugIsInCombat, "调试为战斗中");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("脚本", scriptAsset, typeof(MonoScript), false);
        }
    }

    private static void DrawSection(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private static void DrawProperty(SerializedProperty property, string label, bool includeChildren = false)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label), includeChildren);
    }
}
