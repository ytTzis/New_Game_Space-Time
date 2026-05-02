using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneIntroCameraTransition : MonoBehaviour
{
    [SerializeField, InspectorName("开场自动播放")] private bool playOnStart = true;
    [SerializeField, InspectorName("镜头目标，可不填")] private Transform target;
    [SerializeField, InspectorName("要移动的相机，可不填")] private Transform cameraTransform;
    [SerializeField, InspectorName("目标观察高度")] private float lookHeight = 1.4f;

    [SerializeField, Header("镜头移动"), InspectorName("开始偏移")] private Vector3 startOffset = new Vector3(0f, 4f, -9f);
    [SerializeField, InspectorName("结束偏移")] private Vector3 endOffset = new Vector3(0f, 2.2f, -4.5f);
    [SerializeField, InspectorName("偏移跟随主角方向")] private bool useTargetRelativeOffset = true;
    [SerializeField, InspectorName("反转主角方向偏移")] private bool invertTargetRelativeOffset;
    [SerializeField, InspectorName("环绕角度")] private float orbitAngle = 35f;
    [SerializeField, InspectorName("转场时长")] private float duration = 2.5f;
    [SerializeField, InspectorName("转场曲线")] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, Header("控制开关"), InspectorName("转场时禁用玩家输入")] private bool disablePlayerInput = true;
    [SerializeField, InspectorName("转场时禁用相机控制")] private bool disableCameraControllers = true;
    [SerializeField, InspectorName("结束后恢复原相机位置")] private bool restoreCameraWhenFinished = true;
    [SerializeField, Header("结束衔接"), InspectorName("结束时黑屏衔接")] private bool fadeWhenFinished = true;
    [SerializeField, InspectorName("黑屏淡入时长")] private float fadeOutDuration = 0.35f;
    [SerializeField, InspectorName("黑屏停留时长")] private float fadeHoldDuration = 0.08f;
    [SerializeField, InspectorName("黑屏淡出时长")] private float fadeInDuration = 0.45f;

    private CharacterInputSystem characterInputSystem;
    private TP_CameraController tpCameraController;
    private UnityTemplateProjects.SimpleCameraController simpleCameraController;
    private CanvasGroup fadeCanvasGroup;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool originalInputEnabled;
    private bool originalTpCameraEnabled;
    private bool originalSimpleCameraEnabled;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayIntro();
        }
    }

    public void PlayIntro()
    {
        ResolveReferences();

        if (target == null || cameraTransform == null)
        {
            Debug.LogWarning("[开场镜头转场] 缺少目标或相机，无法播放。", this);
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        CacheAndDisableControls();

        originalCameraPosition = cameraTransform.position;
        originalCameraRotation = cameraTransform.rotation;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float curvedProgress = transitionCurve.Evaluate(progress);

            float currentOrbitAngle = Mathf.Lerp(orbitAngle, 0f, curvedProgress);
            Vector3 currentOffset = Vector3.Lerp(startOffset, endOffset, curvedProgress);
            Quaternion orbitRotation = Quaternion.Euler(0f, currentOrbitAngle, 0f);

            Vector3 lookPoint = GetLookPoint();
            cameraTransform.position = lookPoint + GetCameraOffset(currentOffset, orbitRotation);
            cameraTransform.rotation = Quaternion.LookRotation(lookPoint - cameraTransform.position, Vector3.up);

            yield return null;
        }

        Vector3 finalLookPoint = GetLookPoint();
        cameraTransform.position = finalLookPoint + GetCameraOffset(endOffset, Quaternion.identity);
        cameraTransform.rotation = Quaternion.LookRotation(finalLookPoint - cameraTransform.position, Vector3.up);

        if (ShouldRestoreCameraWhenFinished())
        {
            if (fadeWhenFinished)
            {
                yield return FadeScreen(1f, fadeOutDuration);
                yield return new WaitForSecondsRealtime(fadeHoldDuration);
            }

            cameraTransform.position = originalCameraPosition;
            cameraTransform.rotation = originalCameraRotation;
        }
        else if (fadeWhenFinished)
        {
            yield return FadeScreen(1f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(fadeHoldDuration);
        }

        RestoreControls();

        if (fadeWhenFinished)
        {
            yield return FadeScreen(0f, fadeInDuration);
        }

        transitionCoroutine = null;
    }

    private bool ShouldRestoreCameraWhenFinished()
    {
        return restoreCameraWhenFinished || disableCameraControllers;
    }

    private void ResolveReferences()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (target == null)
        {
            CharacterInputSystem inputSystem = FindFirstObjectByType<CharacterInputSystem>();
            if (inputSystem != null)
            {
                target = inputSystem.transform;
            }
            else
            {
                GameObject player = GameObject.Find("Player (1)");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        if (characterInputSystem == null)
        {
            characterInputSystem = FindFirstObjectByType<CharacterInputSystem>();
        }

        if (tpCameraController == null)
        {
            tpCameraController = FindFirstObjectByType<TP_CameraController>();
        }

        if (simpleCameraController == null)
        {
            simpleCameraController = FindFirstObjectByType<UnityTemplateProjects.SimpleCameraController>();
        }
    }

    private Vector3 GetLookPoint()
    {
        return target.position + Vector3.up * lookHeight;
    }

    private Vector3 GetCameraOffset(Vector3 offset, Quaternion extraRotation)
    {
        float targetYaw = target.eulerAngles.y;
        if (invertTargetRelativeOffset)
        {
            targetYaw += 180f;
        }

        Quaternion baseRotation = useTargetRelativeOffset
            ? Quaternion.Euler(0f, targetYaw, 0f)
            : Quaternion.identity;

        return baseRotation * extraRotation * offset;
    }

    private void CacheAndDisableControls()
    {
        if (characterInputSystem != null)
        {
            originalInputEnabled = characterInputSystem.enabled;
            if (disablePlayerInput)
            {
                characterInputSystem.enabled = false;
            }
        }

        if (tpCameraController != null)
        {
            originalTpCameraEnabled = tpCameraController.enabled;
            if (disableCameraControllers)
            {
                tpCameraController.enabled = false;
            }
        }

        if (simpleCameraController != null)
        {
            originalSimpleCameraEnabled = simpleCameraController.enabled;
            if (disableCameraControllers)
            {
                simpleCameraController.enabled = false;
            }
        }
    }

    private void RestoreControls()
    {
        if (characterInputSystem != null)
        {
            characterInputSystem.enabled = originalInputEnabled;
        }

        if (tpCameraController != null)
        {
            tpCameraController.enabled = originalTpCameraEnabled;
        }

        if (simpleCameraController != null)
        {
            simpleCameraController.enabled = originalSimpleCameraEnabled;
        }
    }

    private IEnumerator FadeScreen(float targetAlpha, float fadeDuration)
    {
        EnsureFadeCanvas();

        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(timer / fadeDuration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Intro Camera Fade Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        fadeCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        GameObject imageObject = new GameObject("Fade Image", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
    }
}
