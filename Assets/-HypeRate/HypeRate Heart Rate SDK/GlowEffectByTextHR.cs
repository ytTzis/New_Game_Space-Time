using UnityEngine;
using UnityEngine.UI;

public class GlowEffectByTextHR : MonoBehaviour
{
    [Header("心率及目标组件")]
    [Tooltip("hyperateSocket脚本正在更新的Text组件")]
    public Text hrTextSource; // <--- 确认这里使用的是 UnityEngine.UI.Text

    [Tooltip("用于显示光晕的 UI Image 组件")]
    public Image glowImage;

    [Header("阈值和效果设置")]
    [Tooltip("触发闪烁和字体变红效果的心率阈值")]
    public int HeartRateThreshold = 80; // 建议设置为 80

    [Tooltip("闪烁效果的最大不透明度 (Alpha值)")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f;

    [Tooltip("闪烁的频率 (每秒闪烁次数)")]
    public float flashFrequency = 5f;

    [Tooltip("光晕淡出速度")]
    public float fadeOutSpeed = 5f;

    private Color targetColor;
    private int currentHeartRate = 0;

    private Color defaultTextColor;

    void Start()
    {
        if (glowImage == null || hrTextSource == null)
        {
            Debug.LogError("请确保已设置 Glow Image 和 Text Source 组件！");
            enabled = false;
            return;
        }

        // ⭐ 关键点：保存 Text 组件的原始颜色
        defaultTextColor = hrTextSource.color;

        // 初始化Image颜色
        targetColor = glowImage.color;
        targetColor.r = 1f;
        targetColor.g = 0f;
        targetColor.b = 0f;
        targetColor.a = 0f;
        glowImage.color = targetColor;

        if (HeartRateThreshold < 80)
        {
            HeartRateThreshold = 80;
        }
    }

    void Update()
    {
        // 1. 从Text组件中读取心率数据
        if (int.TryParse(hrTextSource.text, out int parsedHR))
        {
            currentHeartRate = parsedHR;
        }

        if (currentHeartRate >= HeartRateThreshold)
        {
            // ⭐ 目标功能实现 1：心率达到阈值时，将 Text 字体颜色变为红色
            if (hrTextSource.color != Color.red)
            {
                hrTextSource.color = Color.red;
            }

            // 2. 心率达到阈值：开始闪烁光晕
            float t = Mathf.Sin(Time.time * flashFrequency * Mathf.PI);
            float lerpFactor = (t + 1f) * 0.5f;
            float currentAlpha = Mathf.Lerp(0f, maxAlpha, lerpFactor);

            // 3. 应用 Alpha 值到光晕 Image
            targetColor.a = currentAlpha;
            glowImage.color = targetColor;
        }
        else
        {
            // ⭐ 目标功能实现 2：心率低于阈值时，将 Text 字体颜色恢复
            if (hrTextSource.color != defaultTextColor)
            {
                hrTextSource.color = defaultTextColor;
            }

            // 4. 心率低于阈值：逐渐淡出光晕效果
            if (glowImage.color.a > 0f)
            {
                targetColor.a = Mathf.MoveTowards(targetColor.a, 0f, Time.deltaTime * fadeOutSpeed);
                glowImage.color = targetColor;
            }
        }
    }
}