using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Construcción reproducible. Sólo se ejecuta desde el menú o una orden explícita.</summary>
public static class Part1Builder
{
    [Serializable] private class Cell { public int x; public int y; public string kind; }
    [Serializable] private class Reward { public int x; public int y; public string item; public int hits; }
    [Serializable] private class Point { public int x; public int y; }
    [Serializable] private class PipeData { public int x; public int top; }
    [Serializable] private class Layout
    {
        public Cell[] cells;
        public Reward[] rewards;
        public Point[] goombas;
        public Point[] koopas;
        public PipeData[] pipes;
    }
    private const string Variants = "Assets/Prefabs/Underground";
    private static Material palette;

    [MenuItem("Educativo/Parte 1/Regenerar nivel y conexiones")]
    public static void Generate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (!AssetDatabase.IsValidFolder(Variants)) AssetDatabase.CreateFolder("Assets/Prefabs", "Underground");
        palette = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UndergroundPalette.mat");
        if (palette == null)
        {
            palette = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UndergroundPalette.shader"));
            AssetDatabase.CreateAsset(palette, "Assets/Materials/UndergroundPalette.mat");
        }
        // Al regenerar, reparar también cualquier referencia de shader desactualizada.
        palette.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UndergroundPalette.shader");
        EditorUtility.SetDirty(palette);
        CreateVariants();
        ConfigureOriginal();
        CreateUnderground();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/1-1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/1-2.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("PART1_GENERATED: 1-1 y 1-2 conectados.");
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
    private static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{name}.prefab");
    private static GameObject Instance(string name, Vector2 position, Transform parent = null)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(name));
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        return obj;
    }

    private static void CreateVariants()
    {
        Variant("Brick", "Brick", "UndergroundBrick", obj => obj.GetComponent<BlockHit>().breakable = true);
        Variant("MysteryBlock", "Mystery", null, null);
        Variant("Coin", "Coin", "Coin_Underground", null);
        Variant("HardBlock", "Hard", null, obj => obj.GetComponent<SpriteRenderer>().sharedMaterial = palette);
    }

    private static void Variant(string source, string name, string sprite, Action<GameObject> configure)
    {
        GameObject obj = Instance(source, Vector2.zero);
        if (sprite != null) obj.GetComponent<SpriteRenderer>().sprite = Sprite(sprite);
        obj.name = "Underground " + name;
        configure?.Invoke(obj);
        PrefabUtility.SaveAsPrefabAsset(obj, $"{Variants}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(obj);
    }

    private static void ConfigureOriginal()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/1-1.unity");
        // La configuración debe descargarse con la escena, nunca acompañar al singleton.
        LevelSettings settings = UnityEngine.Object.FindObjectsByType<LevelSettings>(FindObjectsSortMode.None)
            .FirstOrDefault(s => s.GetComponent<GameManager>() == null);
        if (settings == null) settings = new GameObject("Configuración del nivel").AddComponent<LevelSettings>();
        foreach (LevelSettings old in UnityEngine.Object.FindObjectsByType<LevelSettings>(FindObjectsSortMode.None))
            if (old != settings) UnityEngine.Object.DestroyImmediate(old);
        foreach (GameObject empty in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (empty.name == "Configuración del nivel" && empty.GetComponents<Component>().Length == 1)
                UnityEngine.Object.DestroyImmediate(empty);
        settings.world = settings.stage = 1;
        settings.cameraBounds = new Vector2(0f, 227f);
        foreach (FlagPole flag in UnityEngine.Object.FindObjectsByType<FlagPole>(FindObjectsSortMode.None))
        {
            flag.nextWorld = 1;
            flag.nextStage = 2;
            PrefabUtility.RecordPrefabInstancePropertyModifications(flag);
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void CreateUnderground()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Game Manager", typeof(GameManager));
        LevelSettings settings = new GameObject("Configuración del nivel").AddComponent<LevelSettings>();
        settings.stage = 2;
        settings.underground = true;
        settings.background = Color.black;
        settings.cameraBounds = new Vector2(0f, 171f);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(SideScrollingCamera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(12f, 6.5f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        Player player = Instance("Mario", new Vector2(3f, 10f)).GetComponent<Player>();
        cameraObject.GetComponent<SideScrollingCamera>().trackedObject = player.transform;

        Transform terrain = new GameObject("Terreno subterráneo").transform;
        Transform blocks = new GameObject("Bloques interactivos").transform;
        Transform enemies = new GameObject("Enemigos").transform;
        Transform items = new GameObject("Monedas").transform;
        Transform elevators = new GameObject("Ascensores").transform;
        Transform pipes = new GameObject("Tuberías").transform;
        Transform exterior = new GameObject("Salida exterior").transform;
        Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Editor/UndergroundLayout.json"));
        foreach (Cell cell in layout.cells)
        {
            Vector2 position = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            GameObject obj;
            switch (cell.kind)
            {
                case "ground": obj = Instance("UndergroundBlock", position, terrain); break;
                case "hard": obj = Instance("Underground/Hard", position, terrain); break;
                case "coin": obj = Instance("Underground/Coin", position, items); break;
                case "question": obj = Instance("Underground/Mystery", position, blocks); break;
                default:
                    obj = Instance("Underground/Brick", position, blocks);
                    BlockHit hit = obj.GetComponent<BlockHit>();
                    hit.breakable = cell.y < 12 && cell.x != 0 && cell.x < 160;
                    Reward reward = layout.rewards.FirstOrDefault(r => r.x == cell.x && r.y == cell.y);
                    if (reward != null) { hit.item = Prefab(reward.item); hit.maxHits = reward.hits; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(hit);
                    break;
            }
            if (cell.kind == "question" && cell.x == 10)
            {
                BlockHit hit = obj.GetComponent<BlockHit>();
                hit.item = Prefab("MagicMushroom");
                PrefabUtility.RecordPrefabInstancePropertyModifications(hit);
            }
            obj.name += $" ({cell.x},{cell.y})";
        }
        foreach (Point point in layout.goombas) SpawnEnemy("Goomba", point, enemies);
        foreach (Point point in layout.koopas) SpawnEnemy("Koopa", point, point.x > 200 ? exterior : enemies);
        foreach (PipeData pipe in layout.pipes) VerticalPipe(pipe.x, pipe.top, pipes);
        for (int i = 0; i < 3; i++)
        {
            Elevator(new Vector2(141f, -2f + i * 6f), false, elevators);
            Elevator(new Vector2(156f, -2f + i * 6f), true, elevators);
        }
        // La cubierta impide que una rotura abra la ruta secreta, fuera de esta entrega.
        Collider("Límite superior", new Vector2(85.5f, 14.5f), new Vector2(171f, 3f), terrain);
        Collider("Límite izquierdo", new Vector2(-0.5f, 7f), new Vector2(1f, 15f), terrain);
        Collider("Cierre del recorrido", new Vector2(170.5f, 7f), new Vector2(1f, 15f), terrain);
        GameObject barrier = Collider("Death Barrier", new Vector2(125f, -3f), new Vector2(252f, 1f));
        barrier.GetComponent<BoxCollider2D>().isTrigger = true;
        barrier.AddComponent<DeathBarrier>();

        // La zona exterior pertenece al mismo nivel; sólo la bandera cambia de escena.
        GameObject ground = Instance("Ground", new Vector2(230f, 1f), exterior);
        ground.GetComponent<SpriteRenderer>().size = new Vector2(40f, 2f);
        ground.GetComponent<BoxCollider2D>().size = new Vector2(40f, 2f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ground.GetComponent<SpriteRenderer>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(ground.GetComponent<BoxCollider2D>());
        VerticalPipe(214f, 4f, exterior);
        for (int x = 0; x < 8; x++) for (int y = 0; y <= x; y++)
            Instance("HardBlock", new Vector2(222.5f + x, 2.5f + y), exterior);
        Instance("Hill1", new Vector2(234f, 3.5f), exterior);
        Instance("Cloud1", new Vector2(220f, 11f), exterior);
        Instance("Cloud2", new Vector2(242f, 10f), exterior);
        GameObject castle = Instance("Castle", new Vector2(245f, 4.5f), exterior);
        FlagPole flag = Instance("Flag Pole", new Vector2(238f, 2f), exterior).GetComponent<FlagPole>();
        flag.castle = castle.transform.Find("Entrance");
        if (flag.castle == null) flag.castle = castle.transform;
        flag.nextWorld = flag.nextStage = 1;
        PrefabUtility.RecordPrefabInstancePropertyModifications(flag);

        Transform destination = new GameObject("Destino exterior").transform;
        destination.SetParent(exterior);
        destination.position = new Vector2(215f, 3.5f);
        PipeRegion region = destination.gameObject.AddComponent<PipeRegion>();
        region.cameraBounds = new Vector2(210f, 250f);
        GameObject mouth = Collider("Tubería de salida (D)", new Vector2(164.2f, 5.75f), new Vector2(0.6f, 1.4f), pipes);
        mouth.GetComponent<BoxCollider2D>().isTrigger = true;
        Pipe exit = mouth.AddComponent<Pipe>();
        exit.connection = destination;
        exit.enterKeyCode = KeyCode.D;
        exit.enterDirection = Vector3.right;
        exit.exitDirection = Vector3.up;
        Visual("Conexión de salida", "PipeConnection", new Vector2(166f, 6f), pipes);
        Collider("Cuerpo de salida", new Vector2(166f, 8f), new Vector2(2f, 12f), pipes);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/1-2.unity");
    }

    private static void SpawnEnemy(string prefab, Point point, Transform parent)
    {
        GameObject obj = Instance(prefab, new Vector2(point.x + 0.5f, point.y + 0.5f), parent);
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (point.x < 200) renderer.sharedMaterial = palette;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }

    private static GameObject Collider(string name, Vector2 position, Vector2 size, Transform parent = null)
    {
        GameObject obj = new GameObject(name, typeof(BoxCollider2D));
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.GetComponent<BoxCollider2D>().size = size;
        return obj;
    }

    private static GameObject Visual(string name, string sprite, Vector2 position, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = Sprite(sprite);
        renderer.sortingOrder = 4;
        return obj;
    }

    private static void VerticalPipe(float x, float top, Transform parent)
    {
        Visual("Boca de tubería", "PipeTop", new Vector2(x + 1f, top - 0.5f), parent);
        float height = top - 3f;
        if (height > 0f)
        {
            GameObject body = Visual("Cuerpo de tubería", "PipeBottom", new Vector2(x + 1f, 2f + height * 0.5f), parent);
            body.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
            body.GetComponent<SpriteRenderer>().size = new Vector2(2f, height);
        }
        Collider("Colisión de tubería", new Vector2(x + 1f, (top + 2f) * 0.5f), new Vector2(2f, top - 2f), parent);
    }

    private static void Elevator(Vector2 position, bool ascending, Transform parent)
    {
        GameObject obj = Visual(ascending ? "Ascensor ascendente" : "Ascensor descendente", "HardBlock", position, parent);
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(4f, 0.5f);
        renderer.color = new Color(1f, 0.75f, 0.45f);
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(4f, 0.5f);
        Rigidbody2D body = obj.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        MovingPlatform platform = obj.AddComponent<MovingPlatform>();
        platform.ascending = ascending;
        platform.bottom = -4f;
        platform.top = 14f;
    }

    public static void BuildWindows()
    {
        string output = Path.GetFullPath("Builds/Windows/SuperMarioEducativo.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Falló la compilación: " + report.summary.result);
        Debug.Log("PART1_WINDOWS_BUILD_OK: " + output);
    }
}
