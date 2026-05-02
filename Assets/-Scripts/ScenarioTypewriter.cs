using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioTypewriter : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text nextText;
    [SerializeField] private float charactersPerSecond = 24f;
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float nextFadeDuration = 0.5f;
    [SerializeField] private string nextSceneName = "1_GameScene";

    private float elapsed;
    private int totalCharacters;
    private float nextFadeElapsed;
    private bool isTyping = true;
    private bool isFadingNext;
    private bool canLoadNextScene;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        if (targetText == null)
        {
            enabled = false;
            return;
        }

        targetText.ForceMeshUpdate();
        totalCharacters = targetText.textInfo.characterCount;
        targetText.maxVisibleCharacters = 0;

        if (nextText != null)
        {
            SetTextAlpha(nextText, 0f);
        }
    }

    private void OnEnable()
    {
        elapsed = 0f;
        nextFadeElapsed = 0f;
        isTyping = true;
        isFadingNext = false;
        canLoadNextScene = false;

        if (targetText != null)
        {
            targetText.maxVisibleCharacters = 0;
        }

        if (nextText != null)
        {
            SetTextAlpha(nextText, 0f);
        }
    }

    private void Update()
    {
        if (targetText == null || totalCharacters <= 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                CompleteTyping();
                return;
            }

            if (canLoadNextScene)
            {
                SceneManager.LoadScene(nextSceneName);
                return;
            }
        }

        if (isFadingNext)
        {
            UpdateNextFade();
            return;
        }

        if (!isTyping)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float revealTime = elapsed - startDelay;

        if (revealTime <= 0f)
        {
            return;
        }

        int visibleCharacters = Mathf.Clamp(Mathf.FloorToInt(revealTime * charactersPerSecond), 0, totalCharacters);
        targetText.maxVisibleCharacters = visibleCharacters;

        if (visibleCharacters >= totalCharacters)
        {
            CompleteTyping();
        }
    }

    private void CompleteTyping()
    {
        isTyping = false;
        targetText.maxVisibleCharacters = totalCharacters;
        StartNextFade();
    }

    private void StartNextFade()
    {
        if (nextText == null)
        {
            canLoadNextScene = true;
            return;
        }

        nextFadeElapsed = 0f;
        isFadingNext = true;
        SetTextAlpha(nextText, 0f);
    }

    private void UpdateNextFade()
    {
        if (nextText == null)
        {
            isFadingNext = false;
            canLoadNextScene = true;
            return;
        }

        nextFadeElapsed += Time.deltaTime;
        float alpha = nextFadeDuration <= 0f ? 1f : Mathf.Clamp01(nextFadeElapsed / nextFadeDuration);
        SetTextAlpha(nextText, alpha);

        if (alpha >= 1f)
        {
            isFadingNext = false;
            canLoadNextScene = true;
        }
    }

    private static void SetTextAlpha(TMP_Text textComponent, float alpha)
    {
        Color color = textComponent.color;
        color.a = alpha;
        textComponent.color = color;
    }
}
