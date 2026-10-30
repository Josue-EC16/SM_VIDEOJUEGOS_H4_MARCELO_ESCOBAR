using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Construye sólo el nuevo nivel y menú; conserva los mapas 1-1 y 1-2 existentes.</summary>
public static class CampaignBuilder
{
    private const string Art = "Assets/Aquatic";
    private static readonly Color Water = new Color(.03f, .27f, .43f);
    private static Material pixelMaterial;
    private static Mesh fishMesh, bubbleMesh;

    [MenuItem("Educativo/Campaña/Crear acuático y menú")]
    public static void Generate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateArt();
        CreateWaterLevel();
        EditorSceneManager.OpenScene("Assets/Scenes/1-2.unity");
        foreach (var flag in Object.FindObjectsByType<FlagPole>(FindObjectsSortMode.None))
        {
            flag.nextWorld = 1; flag.nextStage = 3; flag.finishGame = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(flag);
        }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        MainMenuBuilder.Create();
        Part1Builder.RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("CAMPAIGN_GENERATED");
    }

    public static void GenerateVerifyAndBuild()
    {
        Generate();
        Part1Verification.VerifyAndBuild();
    }

    private static void CreateArt()
    {
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets", "Aquatic");
        pixelMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Pixel.mat");
        if (pixelMaterial == null)
        {
            pixelMaterial = new Material(Shader.Find("Sprites/Default"));
            pixelMaterial.mainTexture = Texture2D.whiteTexture;
            pixelMaterial.SetColor("_Color", Color.white);
            pixelMaterial.SetColor("_RendererColor", Color.white);
            pixelMaterial.SetVector("_Flip", Vector4.one);
            AssetDatabase.CreateAsset(pixelMaterial, Art + "/Pixel.mat");
        }
        fishMesh = PixelMesh("Fish", new[] {
            ".......YYY......", ".....WWWWWW.....", "....WWWWWWWE....", "RR.WWWWWWWWEK...",
            "RRRWWWRRRWWWWK..", "RRRWWRRRRRWWWW..", "RR.WWWRRRWWWWW..", "....WWWWWWWWW...",
            ".....WWYYYWW....", "........YY......" }, .0625f);
        bubbleMesh = PixelMesh("Bubble", new[] { ".CC.", "C..W", "C..W", ".WW." }, .06f);
    }

    // Cuadrículas propias de color por vértice: sin texturas externas ni shaders personalizados.
    private static Mesh PixelMesh(string name, string[] rows, float pixel)
    {
        string path = Art + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        var vertices = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
        for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++)
        {
            char value = rows[y][x];
            if (value == '.') continue;
            Color color = value == 'R' ? new Color(.95f, .25f, .13f) : value == 'Y' ? new Color(1, .72f, .15f)
                : value == 'K' ? new Color(.06f, .08f, .12f) : value == 'E' ? Color.white
                : value == 'C' ? new Color(.25f, .8f, 1, .7f) : new Color(.91f, .95f, 1);
            int start = vertices.Count;
            float left = (x - rows[0].Length * .5f) * pixel, top = (rows.Length * .5f - y) * pixel;
            vertices.Add(new Vector3(left, top)); vertices.Add(new Vector3(left + pixel, top));
            vertices.Add(new Vector3(left + pixel, top - pixel)); vertices.Add(new Vector3(left, top - pixel));
            for (int i = 0; i < 4; i++) colors.Add(color);
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
        mesh.uv = new Vector2[vertices.Count]; mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void CreateWaterLevel()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Game Manager", typeof(GameManager));
        var settings = new GameObject("Configuración del nivel").AddComponent<LevelSettings>();
        settings.stage = 3; settings.underwater = true; settings.background = Water;
        settings.cameraBounds = new Vector2(0, 72);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(SideScrollingCamera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(12, 6.5f, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 7;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Water;
        camera.allowHDR = camera.allowMSAA = false;
        var mario = Instance("Mario", new Vector2(3, 5));
        cameraObject.GetComponent<SideScrollingCamera>().trackedObject = mario.transform;
        Transform terrain = new GameObject("Fondo y arrecifes").transform;
        Transform decor = new GameObject("Plantas y burbujas").transform;
        Transform enemies = new GameObject("Peces").transform;
        Transform coins = new GameObject("Monedas").transform;
        Transform exterior = new GameObject("Salida exterior").transform;
        Ground(new Vector2(36, 1), 72, terrain, new Color(.48f, .72f, 1));
        Solid("Límite izquierdo", new Vector2(-.5f, 7), new Vector2(1, 14), terrain);
        Solid("Límite superior", new Vector2(36, 13.5f), new Vector2(72, 1), terrain);
        Solid("Límite derecho", new Vector2(72.5f, 7), new Vector2(1, 14), terrain);
        var zone = Solid("Agua", new Vector2(36, 7.5f), new Vector2(72, 11), terrain);
        zone.GetComponent<BoxCollider2D>().isTrigger = true;
        zone.AddComponent<WaterZone>();
        foreach (int x in new[] { 20, 39, 54 })
            for (int dx = 0; dx < 3; dx++) for (int y = 0; y < (dx == 1 ? 3 : 2); y++)
            {
                var block = Instance("HardBlock", new Vector2(x + dx + .5f, y + 2.5f), terrain);
                block.GetComponent<SpriteRenderer>().color = new Color(.55f, .8f, 1);
                PrefabUtility.RecordPrefabInstancePropertyModifications(block.GetComponent<SpriteRenderer>());
            }
        for (int x = 7; x < 65; x += 6)
        {
            var plant = Instance("Bush1", new Vector2(x, 2.5f), decor);
            plant.transform.localScale = new Vector3(.65f, 1.5f, 1);
            plant.GetComponent<SpriteRenderer>().color = new Color(.2f, .8f, .8f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(plant.GetComponent<SpriteRenderer>());
            var bubble = MeshVisual("Burbuja", bubbleMesh, new Vector2(x + 1, 3 + x % 8), decor);
            bubble.AddComponent<OceanBubble>();
        }
        for (int i = 0; i < 20; i++)
            Instance("Coin", new Vector2(8 + i * 2.7f, 6.5f + Mathf.Sin(i * .65f) * 2), coins);
        foreach (Vector2 point in new[] { new Vector2(18, 6), new Vector2(33, 5.6f), new Vector2(47, 8), new Vector2(58, 6) })
        {
            var fish = new GameObject("Pez", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(AquaticEnemy));
            fish.transform.SetParent(enemies); fish.transform.position = point; fish.layer = LayerMask.NameToLayer("Enemy");
            var body = fish.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = fish.GetComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(.95f, .55f);
            fish.GetComponent<AquaticEnemy>().visual = MeshVisual("Aspecto", fishMesh, point, fish.transform).transform;
        }
        Ground(new Vector2(96, 1), 36, exterior, Color.white);
        VerticalPipe(83, 4, exterior);
        for (int x = 0; x < 8; x++) for (int y = 0; y <= x; y++)
            Instance("HardBlock", new Vector2(92.5f + x, 2.5f + y), exterior);
        Instance("Hill1", new Vector2(102, 3.5f), exterior);
        Instance("Cloud1", new Vector2(88, 11), exterior);
        Instance("Cloud2", new Vector2(108, 11), exterior);
        var castle = Instance("Castle", new Vector2(111, 4.5f), exterior);
        var flag = Instance("Flag Pole", new Vector2(104, 2), exterior).GetComponent<FlagPole>();
        flag.castle = castle.transform.Find("Entrance");
        if (flag.castle == null) flag.castle = castle.transform;
        flag.finishGame = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(flag);
        var destination = new GameObject("Destino exterior", typeof(PipeRegion));
        destination.transform.SetParent(exterior); destination.transform.position = new Vector2(83, 3.5f);
        destination.GetComponent<PipeRegion>().cameraBounds = new Vector2(78, 114);
        var mouth = Solid("Salida acuática (D)", new Vector2(66.2f, 3.75f), new Vector2(1.6f, 2f), terrain);
        mouth.GetComponent<BoxCollider2D>().isTrigger = true;
        // Abarcar la abertura visible completa permite entrar también nadando junto al suelo.
        mouth.GetComponent<BoxCollider2D>().offset = new Vector2(.8f, -.25f);
        var pipe = mouth.AddComponent<Pipe>(); pipe.connection = destination.transform;
        pipe.enterKeyCode = KeyCode.D; pipe.enterDirection = Vector3.right; pipe.exitDirection = Vector3.up;
        Visual("Boca de salida", "PipeConnection", new Vector2(68, 4), terrain);
        var stem = Visual("Tallo de salida", "PipeBottom", new Vector2(69, 9.25f), terrain).GetComponent<SpriteRenderer>();
        stem.drawMode = SpriteDrawMode.Tiled; stem.size = new Vector2(2, 7.5f);
        Solid("Cuerpo de salida", new Vector2(69, 7.75f), new Vector2(2, 10.5f), terrain);
        WorldLabel("NADA: ESPACIO / W / ↑\nBAJA: S / ↓", new Vector2(7, 11.4f), terrain);
        WorldLabel("SALIDA →\nPULSA D", new Vector2(63, 3.4f), terrain);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/1-3.unity");
    }

    private static GameObject Instance(string name, Vector2 position, Transform parent = null)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab"));
        obj.transform.SetParent(parent, false); obj.transform.position = position;
        return obj;
    }
    private static GameObject Solid(string name, Vector2 position, Vector2 size, Transform parent)
    {
        var obj = new GameObject(name, typeof(BoxCollider2D));
        obj.transform.SetParent(parent); obj.transform.position = position;
        obj.GetComponent<BoxCollider2D>().size = size; return obj;
    }
    private static GameObject Visual(string name, string sprite, Vector2 position, Transform parent)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(parent); obj.transform.position = position;
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + sprite + ".png");
        renderer.sortingOrder = 4; return obj;
    }
    private static GameObject MeshVisual(string name, Mesh mesh, Vector2 position, Transform parent)
    {
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent); obj.transform.position = position;
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = pixelMaterial; renderer.sortingOrder = 5;
        return obj;
    }
    private static void Ground(Vector2 position, float width, Transform parent, Color tint)
    {
        var ground = Instance("Ground", position, parent);
        var renderer = ground.GetComponent<SpriteRenderer>(); renderer.size = new Vector2(width, 2); renderer.color = tint;
        ground.GetComponent<BoxCollider2D>().size = new Vector2(width, 2);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ground.GetComponent<BoxCollider2D>());
    }
    private static void VerticalPipe(float x, float top, Transform parent)
    {
        Visual("Tubería exterior", "PipeTop", new Vector2(x, top - .5f), parent);
        var stem = Visual("Tallo exterior", "PipeBottom", new Vector2(x, 2.5f), parent).GetComponent<SpriteRenderer>();
        stem.drawMode = SpriteDrawMode.Tiled; stem.size = new Vector2(2, 1);
        Solid("Colisión exterior", new Vector2(x, 3), new Vector2(2, 2), parent);
    }
    private static void WorldLabel(string text, Vector2 position, Transform parent)
    {
        var obj = new GameObject("Ayuda", typeof(TextMesh)); obj.transform.SetParent(parent); obj.transform.position = position;
        var label = obj.GetComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center; label.fontSize = 64; label.characterSize = .065f; label.color = new Color(.7f, .94f, 1);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        obj.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        obj.GetComponent<MeshRenderer>().sortingOrder = 6;
    }
}
