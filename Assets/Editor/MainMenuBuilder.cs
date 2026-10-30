using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Menú inicial y selector con navegación de teclado y ratón.</summary>
public static class MainMenuBuilder
{
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Game Manager", typeof(GameManager));
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .02f, .018f);
        camera.allowHDR = false;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var root = new GameObject("Main Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(MainMenuController));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        Transform canvas = root.transform;
        Color bronze = new Color(.65f, .34f, .15f);
        Color cream = new Color(1, .91f, .73f);
        GameOverBuilder.Panel("Borde", canvas, new Vector2(0, -18), new Vector2(620, 550), bronze);
        GameOverBuilder.Panel("Panel", canvas, new Vector2(0, -18), new Vector2(612, 542), new Color(.065f, .045f, .035f));
        GameOverBuilder.Label("Marca", canvas, "AVENTURA EDUCATIVA", new Vector2(0, 324), new Vector2(600, 30), 18, bronze);
        var icon = GameOverBuilder.Panel("Mario", canvas, new Vector2(0, 277), new Vector2(48, 48), Color.white);
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mario_Small_Idle.png");
        icon.preserveAspect = true;
        var menu = root.GetComponent<MainMenuController>();
        menu.controls = root.GetComponent<CanvasGroup>();
        menu.title = GameOverBuilder.Label("Título", canvas, "SUPER MARIO", new Vector2(0, 207), new Vector2(590, 82), 58, cream);
        menu.subtitle = GameOverBuilder.Label("Mensaje", canvas, "Tres niveles, una aventura.", new Vector2(0, 143), new Vector2(590, 36), 21, cream);
        menu.mainPanel = Panel("Inicio", canvas);
        menu.levelPanel = Panel("Selector", canvas);
        Color green = new Color(.10f, .25f, .12f), brown = new Color(.25f, .12f, .055f), red = new Color(.17f, .055f, .035f);
        menu.playButton = GameOverBuilder.MakeButton(menu.mainPanel.transform, "Jugar", "JUGAR", 49, green);
        menu.chooseButton = GameOverBuilder.MakeButton(menu.mainPanel.transform, "Elegir nivel", "ELEGIR NIVEL", -49, brown);
        menu.exitButton = GameOverBuilder.MakeButton(menu.mainPanel.transform, "Salir", "SALIR", -147, red);
        menu.level1Button = GameOverBuilder.MakeButton(menu.levelPanel.transform, "Nivel 1-1", "1-1   SUPERFICIE", 55, green);
        menu.level2Button = GameOverBuilder.MakeButton(menu.levelPanel.transform, "Nivel 1-2", "1-2   SUBTERRÁNEO", -28, brown);
        menu.level3Button = GameOverBuilder.MakeButton(menu.levelPanel.transform, "Nivel 1-3", "1-3   ACUÁTICO", -111, new Color(.045f, .17f, .27f));
        menu.backButton = GameOverBuilder.MakeButton(menu.levelPanel.transform, "Volver", "VOLVER", -194, red);
        Link(menu.playButton, menu.chooseButton, menu.exitButton);
        Link(menu.level1Button, menu.level2Button, menu.level3Button, menu.backButton);
        menu.levelPanel.SetActive(false);
        GameOverBuilder.Label("Controles", canvas, "RATÓN / FLECHAS + ENTER   ·   ESC: MENÚ", new Vector2(0, -263), new Vector2(600, 30), 16, bronze);
        var ground = GameOverBuilder.Panel("Suelo", canvas, Vector2.zero, new Vector2(0, 56), Color.white);
        ground.rectTransform.anchorMin = Vector2.zero;
        ground.rectTransform.anchorMax = new Vector2(1, 0);
        ground.rectTransform.pivot = new Vector2(.5f, 0);
        ground.rectTransform.anchoredPosition = Vector2.zero;
        ground.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GroundBlock.png");
        ground.type = Image.Type.Tiled;
        ground.pixelsPerUnitMultiplier = 100f / 56f;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    private static GameObject Panel(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return obj;
    }

    private static void Link(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                selectOnDown = buttons[(i + 1) % buttons.Length] };
    }
}
