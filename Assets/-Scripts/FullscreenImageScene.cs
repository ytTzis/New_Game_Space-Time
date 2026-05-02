using UnityEngine;
using UnityEngine.UI;

public class FullscreenImageScene : MonoBehaviour
{
    [SerializeField, InspectorName("全屏图片")] private Texture2D backgroundTexture;
    [SerializeField, InspectorName("保持图片比例")] private bool preserveAspect = true;
    [SerializeField, InspectorName("背景颜色")] private Color backgroundColor = Color.black;

    private void Awake()
    {
        Camera sceneCamera = Camera.main;
        if (sceneCamera != null)
        {
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = backgroundColor;
        }

        CreateFullscreenImage();
    }

    private void CreateFullscreenImage()
    {
        GameObject canvasObject = new GameObject("Fullscreen Image Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("Fullscreen Image", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = backgroundTexture;
        image.color = Color.white;

        if (preserveAspect)
        {
            AspectRatioFitter aspectRatioFitter = imageObject.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspectRatioFitter.aspectRatio = backgroundTexture != null && backgroundTexture.height > 0
                ? (float)backgroundTexture.width / backgroundTexture.height
                : 16f / 9f;
        }
    }
}
