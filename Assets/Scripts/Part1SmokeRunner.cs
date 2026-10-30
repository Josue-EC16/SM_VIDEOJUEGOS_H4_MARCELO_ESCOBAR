#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Pruebas optativas del ejecutable: sólo se activan con -part1Smoke.</summary>
public sealed class Part1SmokeRunner : MonoBehaviour
{
    private readonly List<string> results = new List<string>();
    private bool failed;
    private string output;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Environment.GetCommandLineArgs().Contains("-part1Smoke")) return;
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        GameObject obj = new GameObject("Verificación automática parte 1");
        DontDestroyOnLoad(obj);
        obj.AddComponent<Part1SmokeRunner>();
    }

    private IEnumerator Start()
    {
        output = Path.Combine(Application.dataPath, "..", "Verification");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        StartCoroutine(Watchdog());
        yield return null;
        yield return Run();
        File.WriteAllLines(Path.Combine(output, "playmode-results.txt"), results);
        Debug.Log(failed ? "PART1_SMOKE_FAILED" : "PART1_SMOKE_OK");
        Application.logMessageReceived -= OnLog;
        Application.Quit(failed ? 1 : 0);
    }

    private void OnLog(string condition, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        { failed = true; results.Add("ERROR: " + condition + "\n" + trace); }
        if (type == LogType.Exception)
        {
            File.WriteAllLines(Path.Combine(output, "playmode-results.txt"), results);
            Application.Quit(1);
        }
    }

    private IEnumerator Watchdog()
    {
        yield return new WaitForSecondsRealtime(240f);
        results.Add("FAIL: tiempo máximo de pruebas excedido");
        File.WriteAllLines(Path.Combine(output, "playmode-results.txt"), results);
        Application.Quit(1);
    }

    private void Check(bool condition, string label)
    {
        results.Add((condition ? "PASS: " : "FAIL: ") + label);
        if (!condition) failed = true;
        Debug.Log(results[results.Count - 1]);
    }

    private Player Mario => FindFirstObjectByType<Player>(FindObjectsInactive.Include);
    private GameManager Game => GameManager.Instance;

    private IEnumerator WaitForLoad()
    {
        float timeout = Time.realtimeSinceStartup + 10f;
        while (Game.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
        Check(!Game.IsTransitioning, "La transición finaliza");
        yield return new WaitForFixedUpdate();
    }

    private IEnumerator Run()
    {
        Check(Game.stage == 1 && Game.lives == 3, "Inicio normal en 1-1");
        for (int i = 0; i < 5; i++) Game.AddCoin();
        Mario.RestoreState(new PlayerState { big = true, starSeconds = 8f });
        Game.LoadLevel(1, 2);
        Game.LoadLevel(1, 1);
        yield return WaitForLoad();
        Check(Game.stage == 2 && SceneManager.GetActiveScene().name == "1-2", "Una sola carga ante solicitudes duplicadas");
        Check(Game.coins == 5 && Game.lives == 3 && Mario.big, "Vidas, monedas y tamaño conservados");
        Check(Mario.CaptureState().starSeconds > 7.7f, "La estrella se conserva y pausa durante el fundido");

        yield return VerifySurfaceQueries();

        yield return Capture("underground-entry");
        Mario.RestoreState(new PlayerState { big = true });
        Mario.Hit();
        Mario.Hit();
        Check(!Mario.big && !Mario.dead, "Reducción estable y protección ante daño repetido");
        yield return new WaitForSeconds(0.6f);
        Check(!Mario.big && !Mario.bigRenderer.enabled && Mario.smallRenderer.enabled, "La animación termina con el tamaño lógico");
        Mario.RestoreState(new PlayerState { starSeconds = 0.15f });
        yield return new WaitForSeconds(0.07f);
        Mario.Starpower();
        yield return new WaitForSeconds(0.2f);
        Check(Mario.starpower && Mario.CaptureState().starSeconds > 9f, "La segunda estrella renueva la duración");
        Mario.RestoreState(default);
        Mario.Grow();
        Mario.Death();
        Mario.Death();
        Game.ResetLevel(3f);
        yield return new WaitForSeconds(0.6f);
        Check(!Mario.smallRenderer.enabled && !Mario.bigRenderer.enabled, "La transformación no revive al jugador muerto");
        yield return new WaitForSeconds(3.4f);
        yield return WaitForLoad();
        Check(Game.stage == 2 && Game.lives == 2 && !Mario.big && !Mario.starpower, "Una vida descontada y reinicio limpio en 1-2");

        Game.LoadLevel(1, 1);
        yield return WaitForLoad();
        Pipe down = FindObjectsByType<Pipe>(FindObjectsSortMode.None).First(p => p.connection != null && p.exitDirection == Vector3.zero);
        PlaceMario(down.transform.position + Vector3.up);
        Check(down.TryEnter(Mario) && !down.TryEnter(Mario), "La tubería rechaza la entrada duplicada");
        yield return WaitForPipe();
        Check(Camera.main.transform.position.y < 0f, "La sala bonus original sigue accesible");
        Pipe up = FindObjectsByType<Pipe>(FindObjectsSortMode.None).First(p => p.connection != null && p.exitDirection != Vector3.zero);
        PlaceMario(up.transform.position);
        up.TryEnter(Mario);
        yield return WaitForPipe();
        Check(Camera.main.transform.position.y > 0f && Mario.movement.enabled && Mario.capsuleCollider.enabled, "Regreso de la sala bonus con física restaurada");

        FlagPole originalFlag = FindFirstObjectByType<FlagPole>();
        PlaceMario(new Vector2(originalFlag.transform.position.x + 0.1f, originalFlag.transform.position.y + 6f));
        yield return new WaitForSeconds(7f);
        yield return WaitForLoad();
        Check(Game.stage == 2, "La bandera original conecta 1-1 con 1-2");
        foreach (bool ascending in new[] { false, true })
        {
            MovingPlatform platform = FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None)
                .Where(p => p.ascending == ascending && p.transform.position.y > 3f && p.transform.position.y < 10f).First();
            PlaceMario(platform.transform.position + Vector3.up * 0.76f);
            yield return new WaitForSeconds(0.2f);
            float offset = Mario.transform.position.y - platform.transform.position.y;
            yield return new WaitForSeconds(0.8f);
            float nextOffset = Mario.transform.position.y - platform.transform.position.y;
            Check(Mathf.Abs(nextOffset - offset) < 0.25f && Mario.movement.grounded,
                "Mario viaja sobre el ascensor " + (ascending ? "ascendente" : "descendente"));
            Mario.movement.simulatedInput = true;
            Mario.movement.simulatedJump = Mario.movement.simulatedJumpPressed = true;
            yield return new WaitForSeconds(0.15f);
            Check(Mario.transform.position.y - platform.transform.position.y > 1.5f, "Salto separado de la plataforma");
            Mario.movement.simulatedInput = false;
        }

        Game.LoadLevel(1, 2);
        yield return WaitForLoad();
        Mario.Starpower(); // Las capturas no deben morir al colocarse junto a un enemigo.
        PlaceMario(new Vector2(64f, 2.5f));
        yield return Capture("underground-middle");
        PlaceMario(new Vector2(141f, 10.5f));
        yield return Capture("underground-elevators");
        Pipe exit = FindFirstObjectByType<Pipe>();
        PlaceMario(exit.transform.position);
        exit.TryEnter(Mario);
        yield return WaitForPipe();
        Check(Mario.transform.position.x > 210f && Camera.main.backgroundColor.b > 0.5f, "Salida a la superficie y región de cámara");
        yield return Capture("underground-exit");
        FlagPole flag = FindFirstObjectByType<FlagPole>();
        PlaceMario(new Vector2(flag.transform.position.x + 0.1f, flag.transform.position.y + 6f));
        yield return new WaitForSeconds(7f);
        yield return WaitForLoad();
        Check(Game.stage == 1 && Game.lives == 2 && Game.coins == 5, "Bandera de 1-2: regreso a 1-1 con progreso");

        Mario.RestoreState(default);
        Mario.Fall();
        yield return new WaitForSeconds(4f);
        yield return WaitForLoad();
        Check(Game.lives == 1 && Game.stage == 1, "Caída: una sola vida perdida");
        Mario.Death();
        yield return new WaitForSeconds(4f);
        yield return WaitForLoad();
        Check(Game.lives == 3 && Game.coins == 0 && Game.stage == 1, "Fin de vidas conserva el reinicio provisional");
        yield return Traverse(false);
        yield return Traverse(true);
        Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length == 1,
            "Administradores persistentes sin duplicados");
        Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Un solo listener tras las transiciones");
    }

    private IEnumerator VerifySurfaceQueries()
    {
        // Escenario aislado de geometría, lejos del mapa, sin scripts de control ni enemigos.
        GameObject actor = new GameObject("Prueba de consultas", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
        Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.position = new Vector2(20f, 40f);
        CapsuleCollider2D capsule = actor.GetComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(1f, 2f);
        capsule.offset = new Vector2(0f, 0.5f);
        GameObject floor = new GameObject("Suelo de prueba", typeof(BoxCollider2D));
        floor.transform.position = new Vector2(20f, 39f);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
        Physics2D.SyncTransforms();
        Check(body.TryGetGround(out _), "La cápsula grande reconoce el suelo");
        Check(!body.Raycast(Vector2.right) && !body.Raycast(Vector2.left), "El suelo no bloquea el movimiento horizontal");
        GameObject wall = new GameObject("Pared de prueba", typeof(BoxCollider2D));
        wall.transform.position = new Vector2(20.77f, 40.5f);
        wall.GetComponent<BoxCollider2D>().size = new Vector2(0.5f, 4f);
        Physics2D.SyncTransforms();
        Check(body.Raycast(Vector2.right), "La pared bloquea en la dirección correcta");
        floor.GetComponent<BoxCollider2D>().enabled = false;
        Physics2D.SyncTransforms();
        Check(!body.TryGetGround(out _), "Una pared lateral no cuenta como suelo");
        floor.GetComponent<BoxCollider2D>().enabled = true;
        floor.GetComponent<BoxCollider2D>().isTrigger = true;
        Physics2D.SyncTransforms();
        Check(!body.TryGetGround(out _), "Los triggers no sostienen al jugador");
        floor.GetComponent<BoxCollider2D>().isTrigger = false;
        capsule.size = Vector2.one;
        capsule.offset = Vector2.zero;
        Physics2D.SyncTransforms();
        Check(body.TryGetGround(out _), "La cápsula pequeña reconoce el mismo suelo");
        Check(!body.Raycast(Vector2.left), "La forma pequeña no crea una pared fantasma");
        Destroy(actor);
        Destroy(floor);
        Destroy(wall);
        yield return null;
    }

    private IEnumerator Traverse(bool big)
    {
        Game.LoadLevel(1, 2);
        yield return WaitForLoad();
        Mario.RestoreState(new PlayerState { big = big, starSeconds = 10f });
        yield return DriveTo(163.5f, 65f);
        bool reached = !Mario.dead && Mario.transform.position.x >= 163.3f;
        Check(reached, "Recorrido físico completo del subsuelo con Mario " + (big ? "grande" : "pequeño"));
        if (!reached) { yield return Capture(big ? "route-big-failure" : "route-small-failure"); yield break; }
        Pipe exit = FindFirstObjectByType<Pipe>();
        exit.TryEnter(Mario);
        yield return WaitForPipe();
        yield return DriveTo(239f, 20f);
        float timeout = Time.realtimeSinceStartup + 8f;
        while (Game.stage == 2 && Time.realtimeSinceStartup < timeout) yield return null;
        yield return WaitForLoad();
        Check(Game.stage == 1, "Escalera y bandera superadas con Mario " + (big ? "grande" : "pequeño"));
    }

    private IEnumerator DriveTo(float destination, float seconds)
    {
        Player player = Mario;
        PlayerMovement movement = player.movement;
        movement.simulatedInput = true;
        movement.simulatedAxis = 1f;
        movement.simulatedJump = true;
        float timeout = Time.realtimeSinceStartup + seconds;
        float refreshStar = Time.time + 5f;
        float bestX = player.transform.position.x;
        float lastProgress = Time.realtimeSinceStartup;
        bool preparedFirstPit = false;
        while (player != null && !player.dead && !player.IsTransitioning && (destination < 200f || player.transform.position.x < destination) && Time.realtimeSinceStartup < timeout)
        {
            Bounds bounds = player.capsuleCollider.bounds;
            if (destination < 200f && player.big && !preparedFirstPit && player.transform.position.x > 74f && player.transform.position.x < 80f)
            {
                // Mario grande abre espacio sobre el borde antes de tomar carrera.
                preparedFirstPit = true;
                yield return ArriveAt(player, 79.15f);
                movement.simulatedJump = movement.simulatedJumpPressed = true;
                yield return new WaitForSeconds(0.65f);
                yield return ArriveAt(player, 75f);
                movement.simulatedAxis = 1f;
                movement.simulatedJump = true;
                player.Starpower();
                bestX = player.transform.position.x;
                lastProgress = Time.realtimeSinceStartup;
                Debug.Log("PART1_ROUTE: ladrillo del borde abierto y carrera preparada");
                continue;
            }
            if (player.transform.position.x > bestX + 0.25f)
            {
                bestX = player.transform.position.x;
                lastProgress = Time.realtimeSinceStartup;
            }
            // El laberinto tiene caminos inferiores. Un agente que subió a un hueco cerrado
            // debe retroceder y bajar, igual que el jugador, en lugar de saltar indefinidamente.
            if (IsUpperRouteBlocked(player, bounds, lastProgress))
            {
                Debug.Log("PART1_ROUTE: retroceso físico hacia el pasillo inferior");
                float recovery = Time.realtimeSinceStartup + 3f;
                movement.simulatedAxis = -1f;
                movement.simulatedJump = movement.simulatedJumpPressed = false;
                while (player.capsuleCollider.bounds.min.y > 2.3f && !player.dead && Time.realtimeSinceStartup < recovery)
                    yield return null;
                movement.simulatedAxis = 1f;
                movement.simulatedJump = true;
                bestX = player.transform.position.x;
                lastProgress = Time.realtimeSinceStartup;
                continue;
            }
            bool obstacle = Physics2D.BoxCast(bounds.center, new Vector2(0.1f, bounds.size.y * 0.65f), 0f, Vector2.right, 1.5f, 1).collider != null;
            // Saltar cerca del borde permite pasar bajo el último ladrillo antes del primer hueco.
            bool gap = Physics2D.Raycast(new Vector2(bounds.center.x + 0.75f, bounds.min.y + 0.2f), Vector2.down, 0.7f, 1).collider == null;
            if (movement.grounded && (obstacle || gap)) movement.simulatedJumpPressed = true;
            if (Time.time >= refreshStar)
            {
                player.Starpower();
                refreshStar = Time.time + 5f;
                Debug.Log($"PART1_ROUTE_PROGRESS: {player.transform.position}, grande={player.big}, suelo={movement.grounded}");
            }
            if (destination < 200f && player.transform.position.x > 162f)
                FindFirstObjectByType<Pipe>().TryEnter(player);
            yield return null;
        }
        if (player != null)
        {
            movement.simulatedAxis = 0f;
            movement.simulatedInput = false;
            Debug.Log($"PART1_ROUTE: destino={destination} posición={player.transform.position} muerto={player.dead}");
        }
    }

    private IEnumerator ArriveAt(Player player, float x)
    {
        PlayerMovement movement = player.movement;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        movement.simulatedJump = movement.simulatedJumpPressed = false;
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!player.dead && Time.realtimeSinceStartup < deadline)
        {
            float distance = x - player.transform.position.x;
            if (Mathf.Abs(distance) < 0.08f && Mathf.Abs(body.linearVelocity.x) < 0.5f) break;
            float desiredSpeed = Mathf.Min(movement.moveSpeed, Mathf.Sqrt(2f * movement.moveSpeed * Mathf.Abs(distance)));
            movement.simulatedAxis = Mathf.Sign(distance) * desiredSpeed / movement.moveSpeed;
            yield return null;
        }
        movement.simulatedAxis = 0f;
        yield return new WaitForSeconds(0.15f);
    }

    private static bool IsUpperRouteBlocked(Player player, Bounds bounds, float lastProgress)
    {
        return player.big && player.transform.position.x > 52f && player.transform.position.x < 80f &&
            bounds.min.y > 3f && Time.realtimeSinceStartup - lastProgress > 2f;
    }

    private IEnumerator WaitForPipe()
    {
        float timeout = Time.realtimeSinceStartup + 5f;
        while (Mario.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
        Check(!Mario.IsTransitioning, "Animación de tubería finalizada");
    }

    private void PlaceMario(Vector2 position)
    {
        Mario.SetTransitioning(true);
        Mario.transform.position = position;
        Mario.GetComponent<Rigidbody2D>().position = position;
        Camera.main.GetComponent<SideScrollingCamera>().SetRegion(Game.stage == 2 && position.x < 200, 6.5f,
            Game.stage == 2 && position.x < 200 ? new Vector2(0, 171) : Game.stage == 2 ? new Vector2(210, 250) : new Vector2(0, 227),
            Game.stage == 2 && position.x < 200 ? Color.black : new Color(0.36f, 0.58f, 0.99f), true);
        Physics2D.SyncTransforms();
        Mario.SetTransitioning(false);
    }

    private IEnumerator Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
        yield return new WaitForEndOfFrame();
        // El modo batch puede omitir el framebuffer de la ventana oculta; renderizar la cámara explícitamente.
        Camera camera = Camera.main;
        RenderTexture target = new RenderTexture(1280, 720, 24);
        RenderTexture previous = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        Check(image.GetPixels32().Count(c => c.r > 10 || c.g > 10 || c.b > 10) > 1000, "Escenario visible: " + name);
        Check(image.GetPixels32().Count(c => c.r > 240 && c.g < 10 && c.b > 240) < 100, "Materiales válidos: " + name);
        camera.targetTexture = null;
        RenderTexture.active = previous;
        target.Release();
        Destroy(target);
        Destroy(image);
    }
}
#endif
