using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Fundido persistente independiente de las c?maras y del tiempo del juego.</summary>
public sealed class SceneTransition : MonoBehaviour
{
    private CanvasGroup overlay;

    private void Awake()
    {
        GameObject canvasObject = new GameObject("Fundido de escena", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        overlay = canvasObject.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        GameObject imageObject = new GameObject("Negro", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = Color.black;
    }

    public IEnumerator FadeTo(float target)
    {
        float start = overlay.alpha;
        for (float elapsed = 0f; elapsed < 0.3f; elapsed += Time.unscaledDeltaTime)
        {
            overlay.alpha = Mathf.Lerp(start, target, elapsed / 0.3f);
            yield return null;
        }
        overlay.alpha = target;
    }
}
