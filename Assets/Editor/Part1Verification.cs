using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Comprobaciones de referencias y configuración antes de construir el ejecutable.</summary>
public static class Part1Verification
{
    [MenuItem("Educativo/Parte 1/Verificar escenas")]
    public static void Verify()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string scene in new[] { "MainMenu", "1-1", "1-2", "1-3", "GameOver" })
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity");
            Require(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "Un administrador por escena");
            if (scene == "MainMenu")
            {
                MainMenuController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
                Require(menu != null && menu.playButton != null && menu.chooseButton != null && menu.exitButton != null, "Botones principales");
                Require(menu.level1Button != null && menu.level2Button != null && menu.level3Button != null && menu.backButton != null, "Selector completo");
                Require(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length == 3, "Tres botones visibles al iniciar");
                Require(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 7, "Siete botones incluyendo selector");
                Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Entrada del menú principal");
                Require(UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Length == 0, "Menú sin jugador");
                Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Listener del menú principal");
                foreach (Text text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Require(text.font != null, "Fuente del menú principal");
                continue;
            }
            if (scene == "GameOver")
            {
                GameOverMenu menu = UnityEngine.Object.FindFirstObjectByType<GameOverMenu>();
                Require(menu != null && menu.retryButton != null && menu.exitButton != null && menu.controls != null, "Referencias de Game Over");
                Require(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length == 2, "Exactamente dos botones");
                Require(UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Length == 0, "Game Over sin jugador activo");
                Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Un sistema de entrada de menú");
                Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Un listener en Game Over");
                foreach (Text text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                    Require(text.font != null, "Fuente del menú: " + text.name);
                continue;
            }
            Require(UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Length == 1, "Un Mario por escena");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Un listener por escena");
            Require(UnityEngine.Object.FindFirstObjectByType<LevelSettings>().stage == int.Parse(scene.Substring(2)), "Identidad del nivel");
            Require(UnityEngine.Object.FindFirstObjectByType<LevelSettings>().GetComponent<GameManager>() == null, "Configuración separada del singleton");
            foreach (GameObject obj in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj) == 0, "Script faltante: " + obj.name);
                Require(PrefabUtility.GetPrefabInstanceStatus(obj) != PrefabInstanceStatus.MissingAsset, "Prefab faltante: " + obj.name);
            }
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                Require(renderer.sprite != null, "Sprite faltante: " + renderer.name);
                Require(renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null &&
                    renderer.sharedMaterial.shader.name != "Hidden/InternalErrorShader", "Material inválido: " + renderer.name);
            }
            foreach (AnimatedSprite animation in UnityEngine.Object.FindObjectsByType<AnimatedSprite>(FindObjectsSortMode.None))
                Require(animation.sprites != null && animation.sprites.Length > 0 && animation.sprites.All(s => s != null), "Fotograma faltante: " + animation.name);
            foreach (FlagPole flag in UnityEngine.Object.FindObjectsByType<FlagPole>(FindObjectsSortMode.None))
            {
                Require(flag.flag != null && flag.poleBottom != null && flag.castle != null, "Referencias de bandera");
                Require(scene == "1-3" ? flag.finishGame : !flag.finishGame && flag.nextStage == (scene == "1-1" ? 2 : 3), "Destino de bandera");
            }
            if (scene == "1-2")
            {
                MovingPlatform[] platforms = UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
                Require(platforms.Length == 6 && platforms.Count(p => p.ascending) == 3, "Seis plataformas, ambos sentidos");
                Require(UnityEngine.Object.FindObjectsByType<Pipe>(FindObjectsSortMode.None).All(p => p.connection != null), "Tubería de salida conectada");
                Physics2D.SyncTransforms();
                for (int x = 19; x <= 33; x++)
                    Require(Physics2D.OverlapPoint(new Vector2(x + 0.5f, 2.5f), 1) != null, "Columnas continuas: " + x);
            }
            if (scene == "1-3")
            {
                Require(UnityEngine.Object.FindFirstObjectByType<LevelSettings>().underwater, "Inicio bajo el agua");
                Require(UnityEngine.Object.FindObjectsByType<WaterZone>(FindObjectsSortMode.None).Length == 1, "Zona acuática única");
                var fish = UnityEngine.Object.FindObjectsByType<AquaticEnemy>(FindObjectsSortMode.None);
                Require(fish.Length == 4 && fish.All(f => f.visual != null && f.GetComponent<Collider2D>().isTrigger), "Cuatro peces configurados");
                foreach (MeshFilter mesh in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    Require(mesh.sharedMesh != null && mesh.sharedMesh.vertexCount > 0, "Gráfico acuático válido");
                    var material = mesh.GetComponent<MeshRenderer>().sharedMaterial;
                    Require(material != null && material.shader.name == "Sprites/Default", "Material nativo de peces y burbujas");
                }
                var pipe = UnityEngine.Object.FindFirstObjectByType<Pipe>();
                Require(pipe.connection != null && pipe.connection.TryGetComponent(out PipeRegion region) && !region.underwater, "Salida terrestre conectada");
                Physics2D.SyncTransforms();
                for (int x = 1; x < 72; x++) Require(Physics2D.OverlapPoint(new Vector2(x, 1.5f), 1) != null, "Suelo acuático continuo");
            }
        }
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") &&
            (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".mat"))))
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"guid: ([a-f0-9]{32})"))
            {
                string guid = match.Groups[1].Value;
                if (guid.StartsWith("0000000000000000")) continue;
                Require(!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)), "Referencia sin archivo: " + path + " → " + guid);
            }
        foreach (string clip in Enum.GetNames(typeof(SoundEffect)).Concat(new[] { "Surface", "Underground", "Star", "Underwater", "Menu" }))
            Require(AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/RetroAudio/{clip}.wav") != null, "Audio importado: " + clip);
        Require(EditorBuildSettings.scenes.Count(s => s.enabled) == 5, "Cinco escenas habilitadas");
        Require(EditorBuildSettings.scenes[0].path == "Assets/Scenes/MainMenu.unity", "Menú como escena inicial");
        Directory.CreateDirectory("Builds/Verification");
        File.WriteAllText("Builds/Verification/static-checks.txt", "PASS: cinco escenas, referencias, columnas, plataformas, 19 audios, cuatro peces, salida terrestre, menú inicial, selector y Game Over.\n");
        Debug.Log("PART1_STATIC_CHECKS_OK");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("PART1_VERIFY_FAILED: " + message);
    }

    public static void VerifyAndBuild()
    {
        Verify();
        Part1Builder.BuildWindows();
    }

    public static void GenerateVerifyAndBuild()
    {
        Part1Builder.Generate();
        VerifyAndBuild();
    }
}
