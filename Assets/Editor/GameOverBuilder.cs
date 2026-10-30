using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Construye una pantalla independiente con referencias serializadas y dos botones.</summary>
public static class GameOverBuilder
{
    private static readonly Color Cream = new Color(1f, 0.91f, 0.73f);
    private static readonly Color Bronze = new Color(0.65f, 0.34f, 0.15f);
    private static Font font;

    [MenuItem("Educativo/Game Over/Crear escena")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Game Manager", typeof(GameManager));
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.02f, 0.018f);
        camera.allowHDR = false;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject canvasObject = new GameObject("Game Over", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(GameOverMenu));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform root = canvasObject.transform;

        Image background = Panel("Fondo", root, Vector2.zero, Vector2.zero, new Color(0.025f, 0.02f, 0.018f));
        RectTransform backdrop = background.rectTransform;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        Panel("Borde", root, new Vector2(0f, -20f), new Vector2(600f, 460f), Bronze);
        Panel("Panel", root, new Vector2(0f, -20f), new Vector2(592f, 452f), new Color(0.065f, 0.045f, 0.035f));
        Label("Marca", root, "SUPER MARIO BROS.", new Vector2(0f, 316f), new Vector2(600f, 30f), 18, Bronze);
        Image mario = Panel("Mario", root, new Vector2(0f, 245f), new Vector2(64f, 64f), Color.white);
        mario.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mario_Small_Death.png");
        mario.preserveAspect = true;
        Text title = Label("Título", root, "GAME OVER", new Vector2(0f, 124f), new Vector2(570f, 100f), 72, Cream);
        Outline shadow = title.gameObject.AddComponent<Outline>();
        shadow.effectColor = new Color(0.3f, 0.1f, 0.025f);
        shadow.effectDistance = new Vector2(4f, -4f);
        Label("Mensaje", root, "Siempre puedes volver a intentarlo.", new Vector2(0f, 55f), new Vector2(560f, 40f), 20, new Color(0.73f, 0.65f, 0.54f));

        GameOverMenu menu = canvasObject.GetComponent<GameOverMenu>();
        menu.retryButton = MakeButton(root, "Volver a intentar", "VOLVER A INTENTAR", -25f, new Color(0.25f, 0.12f, 0.055f));
        menu.exitButton = MakeButton(root, "Salir", "SALIR", -121f, new Color(0.17f, 0.055f, 0.035f));
        menu.controls = canvasObject.GetComponent<CanvasGroup>();
        Navigation retryNavigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = menu.exitButton, selectOnDown = menu.exitButton };
        Navigation exitNavigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = menu.retryButton, selectOnDown = menu.retryButton };
        menu.retryButton.navigation = retryNavigation;
        menu.exitButton.navigation = exitNavigation;
        Label("Controles", root, "RATÓN  /  FLECHAS + ENTER", new Vector2(0f, -217f), new Vector2(560f, 32f), 16, Bronze);

        Image ground = Panel("Suelo", root, Vector2.zero, new Vector2(0f, 64f), Color.white);
        ground.rectTransform.anchorMin = new Vector2(0f, 0f);
        ground.rectTransform.anchorMax = new Vector2(1f, 0f);
        ground.rectTransform.pivot = new Vector2(0.5f, 0f);
        ground.rectTransform.anchoredPosition = Vector2.zero;
        ground.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GroundBlock.png");
        ground.type = Image.Type.Tiled;
        ground.pixelsPerUnitMultiplier = 100f / 64f;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameOver.unity");
        Debug.Log("GAME_OVER_SCENE_CREATED");
    }

    internal static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    internal static Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = obj.GetComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        return label;
    }

    internal static Button MakeButton(Transform parent, string name, string text, float y, Color color)
    {
        Panel(name + " borde", parent, new Vector2(0f, y), new Vector2(508f, 76f), Bronze);
        Image graphic = Panel(name, parent, new Vector2(0f, y), new Vector2(500f, 68f), color);
        graphic.raycastTarget = true;
        Button button = graphic.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.5f, 1.35f, 1.15f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.75f, 0.65f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        Label(name + " texto", graphic.transform, text, Vector2.zero, new Vector2(480f, 60f), 24, Cream);
        return button;
    }
}
