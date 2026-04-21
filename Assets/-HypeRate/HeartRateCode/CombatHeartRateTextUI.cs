using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHeartRateTextUI : MonoBehaviour
{
    [SerializeField] private CombatHeartRateVisualizationController visualizationController;
    [SerializeField] private Text legacyBpmText;
    [SerializeField] private TMP_Text tmpBpmText;
    [SerializeField] private string prefix = "<3 ";
    [SerializeField] private string suffix = " bpm";

    private void Reset()
    {
        TryAutoBind();
    }

    private void Awake()
    {
        TryAutoBind();
    }

    private void Update()
    {
        int heartRate = visualizationController != null
            ? visualizationController.GetActiveHeartRate()
            : hyperateSocket.CurrentHeartRate;

        string displayText = heartRate <= 0
            ? prefix + "--" + suffix
            : prefix + heartRate + suffix;

        if (legacyBpmText != null)
        {
            legacyBpmText.text = displayText;
        }

        if (tmpBpmText != null)
        {
            tmpBpmText.text = displayText;
        }
    }

    private void TryAutoBind()
    {
        if (visualizationController == null)
        {
            visualizationController = FindFirstObjectByType<CombatHeartRateVisualizationController>();
        }

        if (legacyBpmText == null)
        {
            legacyBpmText = GetComponent<Text>();
        }

        if (tmpBpmText == null)
        {
            tmpBpmText = GetComponent<TMP_Text>();
        }
    }
}
