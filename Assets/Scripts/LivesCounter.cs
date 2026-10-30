using UnityEngine;
using UnityEngine.UI;

/// <summary>Marcador persistente de vidas. Sólo se muestra durante los niveles.</summary>
[RequireComponent(typeof(GameManager))]
public sealed class LivesCounter : MonoBehaviour
{
    private GameManager game;
    private Canvas canvas;
    private Text label;

    private void Awake()
    {
        game = GetComponent<GameManager>();
        var root = new GameObject("Contador de vidas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Siempre debajo del fundido de las transiciones.
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;

        var panel = new GameObject("Panel de vidas", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(24, -24);
        rect.sizeDelta = new Vector2(184, 56);
        var border = panel.GetComponent<Image>();
        border.color = new Color(.65f, .34f, .15f, .95f);
        border.raycastTarget = false;

        var background = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(panel.transform, false);
        Stretch(background.GetComponent<RectTransform>(), 2);
        var image = background.GetComponent<Image>();
        image.color = new Color(.04f, .03f, .025f, .9f);
        image.raycastTarget = false;

        var textObject = new GameObject("Vidas", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(background.transform, false);
        Stretch(textObject.GetComponent<RectTransform>(), 6);
        label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 26;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1, .91f, .73f);
        label.raycastTarget = false;
        SetVisible(false);
    }

    private void OnEnable()
    {
        game.LivesChanged += Refresh;
        Refresh(game.lives);
    }

    private void OnDisable() => game.LivesChanged -= Refresh;

    public void SetVisible(bool visible) => canvas.gameObject.SetActive(visible);

    // Sólo cambia el texto cuando cambia el valor: sin consultas ni asignaciones cada fotograma.
    private void Refresh(int lives) => label.text = "VIDAS: " + lives;

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}
