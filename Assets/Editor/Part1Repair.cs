using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repara la escena guardada sin regenerar el mapa ni reemplazar sus metadatos.</summary>
public static class Part1Repair
{
    [System.Serializable] private class Cell { public int x; public int y; public string kind; }
    [System.Serializable] private class Layout { public Cell[] cells; }
    [MenuItem("Educativo/Parte 1/Reparar materiales y salida")]
    public static void Repair()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/1-2.unity");
        FillInitialColumns();
        Material original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Goomba.prefab")
            .GetComponent<SpriteRenderer>().sharedMaterial;
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.GetComponent<Goomba>() != null || renderer.GetComponent<Koopa>() != null ||
                renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null ||
                renderer.sharedMaterial.shader.name == "Educational/UndergroundPalette" ||
                renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                renderer.sharedMaterial = original;
                renderer.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        SpriteRenderer connection = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.sprite != null && r.sprite.name == "PipeConnection");
        BoxCollider2D solid = Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == "Cuerpo de salida");
        BoxCollider2D roof = Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name.Contains("superior"));
        if (connection != null && solid != null && roof != null)
        {
            // El tallo visible ocupa las dos columnas derechas del sprite de conexión.
            Bounds art = connection.bounds;
            float top = roof.bounds.min.y;
            float x = art.max.x - 1f;
            solid.transform.position = new Vector3(x, (art.min.y + top) * 0.5f, 0f);
            solid.size = new Vector2(2f, top - art.min.y);
            solid.offset = Vector2.zero;

            GameObject stem = GameObject.Find("Tallo visible de salida");
            if (stem == null) stem = new GameObject("Tallo visible de salida", typeof(SpriteRenderer));
            stem.transform.SetParent(connection.transform.parent, false);
            stem.transform.position = new Vector3(x, (art.max.y + top) * 0.5f, 0f);
            SpriteRenderer visual = stem.GetComponent<SpriteRenderer>();
            visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PipeBottom.png");
            visual.sharedMaterial = original;
            visual.drawMode = SpriteDrawMode.Tiled;
            visual.size = new Vector2(2f, top - art.max.y);
            visual.sortingOrder = connection.sortingOrder;

            Pipe exit = Object.FindFirstObjectByType<Pipe>();
            BoxCollider2D entrance = exit.GetComponent<BoxCollider2D>();
            entrance.offset = new Vector2(0.8f, 0f);
            entrance.size = new Vector2(1.6f, 1.4f);
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("PART1_REPAIR_OK: sprites originales y colisión de salida alineada con sus gráficos.");
    }

    public static void RepairVerifyAndBuild()
    {
        Repair();
        GameOverBuilder.Create();
        Part1Builder.RegisterScenes();
        Part1Verification.VerifyAndBuild();
    }

    private static void FillInitialColumns()
    {
        Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Editor/UndergroundLayout.json"));
        SpriteRenderer[] existing = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        SpriteRenderer first = existing.First(r => r.sprite != null && r.sprite.name == "HardBlock" &&
            r.transform.position.x >= 19f && r.transform.position.x < 34f);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Underground/Hard.prefab");
        foreach (Cell cell in layout.cells.Where(c => c.kind == "hard" && c.x >= 19 && c.x <= 33))
        {
            Vector3 position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            if (existing.Any(r => r.sprite != null && r.sprite.name == "HardBlock" && Vector3.Distance(r.transform.position, position) < 0.01f)) continue;
            GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            block.transform.SetParent(first.transform.parent, false);
            block.transform.position = position;
            block.name = $"Columna inicial ({cell.x},{cell.y})";
        }
        foreach (EntityMovement enemy in Object.FindObjectsByType<EntityMovement>(FindObjectsSortMode.None))
        {
            int x = Mathf.FloorToInt(enemy.transform.position.x);
            Cell[] column = layout.cells.Where(c => c.kind == "hard" && c.x == x && x >= 19 && x <= 33).ToArray();
            if (column.Length == 0) continue;
            float surface = column.Max(c => c.y) + 1f;
            if (enemy.transform.position.y < surface + 0.5f)
            {
                Vector3 position = enemy.transform.position;
                position.y = surface + 0.5f;
                enemy.transform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
            }
        }
    }
}
