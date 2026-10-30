using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Comprobaciones de referencias y configuración antes de construir el ejecutable.</summary>
public static class Part1Verification
{
    [MenuItem("Educativo/Parte 1/Verificar escenas")]
    public static void Verify()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string scene in new[] { "1-1", "1-2" })
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity");
            Require(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "Un administrador por escena");
            Require(UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Length == 1, "Un Mario por escena");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Un listener por escena");
            Require(UnityEngine.Object.FindFirstObjectByType<LevelSettings>().stage == (scene == "1-1" ? 1 : 2), "Identidad del nivel");
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
                Require(flag.nextStage == (scene == "1-1" ? 2 : 1), "Destino de bandera");
            }
            if (scene == "1-2")
            {
                MovingPlatform[] platforms = UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
                Require(platforms.Length == 6 && platforms.Count(p => p.ascending) == 3, "Seis plataformas, ambos sentidos");
                Require(UnityEngine.Object.FindObjectsByType<Pipe>(FindObjectsSortMode.None).All(p => p.connection != null), "Tubería de salida conectada");
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
        foreach (string clip in Enum.GetNames(typeof(SoundEffect)).Concat(new[] { "Surface", "Underground", "Star" }))
            Require(AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/RetroAudio/{clip}.wav") != null, "Audio importado: " + clip);
        Require(EditorBuildSettings.scenes.Count(s => s.enabled) == 2, "Dos escenas habilitadas");
        Directory.CreateDirectory("Builds/Verification");
        File.WriteAllText("Builds/Verification/static-checks.txt", "PASS: escenas, referencias, plataformas, destinos y 16 clips.\n");
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
