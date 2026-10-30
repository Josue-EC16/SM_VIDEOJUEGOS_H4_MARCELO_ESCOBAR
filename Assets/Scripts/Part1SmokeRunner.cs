#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Pruebas optativas del ejecutable de desarrollo, activadas mediante argumentos.</summary>
public sealed class Part1SmokeRunner : MonoBehaviour
{
    private readonly List<string> results = new List<string>();
    private bool failed;
    private string output;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string[] modes = { "-part1Smoke", "-menuQuitSmoke", "-routeSmoke", "-aquaticSmoke", "-livesSmoke" };
        if (!Environment.GetCommandLineArgs().Any(argument => modes.Contains(argument))) return;
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
        bool menuQuit = Environment.GetCommandLineArgs().Contains("-menuQuitSmoke");
        if (menuQuit) { yield return null; Check(Game.IsMainMenu, "Salir se prueba desde el menú inicial"); }
        else if (Environment.GetCommandLineArgs().Contains("-livesSmoke")) yield return VerifyLivesCounter();
        else if (Environment.GetCommandLineArgs().Contains("-aquaticSmoke"))
        {
            Game.StartGameAt(3); yield return WaitForLoad();
            yield return VerifySwimming();
            foreach (bool big in new[] { false, true })
            {
                Game.StartGameAt(3); yield return WaitForLoad();
                Mario.RestoreState(new PlayerState { big = big, starSeconds = 10 });
                yield return TraverseWater(big);
            }
            Game.GameOver(); yield return WaitForLoad(); yield return null;
        }
        else if (Environment.GetCommandLineArgs().Contains("-routeSmoke"))
        {
            yield return Traverse(false); yield return Traverse(true);
            Game.GameOver(); yield return WaitForLoad(); yield return null;
        }
        else yield return Run();
        GameOverMenu menu = FindFirstObjectByType<GameOverMenu>();
        MainMenuController home = FindFirstObjectByType<MainMenuController>();
        Button exitButton = menuQuit && home != null ? home.exitButton : menu != null ? menu.exitButton : null;
        PointerEventData exitPointer = null;
        if (!failed && exitButton != null)
        {
            Canvas.ForceUpdateCanvases();
            exitPointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, exitButton.transform.position),
                button = PointerEventData.InputButton.Left
            };
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(exitPointer, hits);
            Check(hits.Any(h => h.gameObject == exitButton.gameObject), "Salir recibe el clic del ratón");
        }
        string report = menuQuit ? "menu-quit-results.txt" : Environment.GetCommandLineArgs().Contains("-livesSmoke") ? "lives-results.txt"
            : Environment.GetCommandLineArgs().Contains("-aquaticSmoke") ? "aquatic-results.txt"
            : Environment.GetCommandLineArgs().Contains("-routeSmoke") ? "route-results.txt" : "playmode-results.txt";
        File.WriteAllLines(Path.Combine(output, report), results);
        Debug.Log(failed ? "PART1_SMOKE_FAILED" : "PART1_SMOKE_OK");
        if (!failed && exitButton != null)
        {
            Debug.Log(menuQuit ? "MAIN_MENU_EXIT_BUTTON_TEST" : "GAME_OVER_EXIT_BUTTON_TEST");
            ExecuteEvents.Execute(exitButton.gameObject, exitPointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(3f);
            results.Add("FAIL: el botón Salir no cerró el ejecutable");
            File.WriteAllLines(Path.Combine(output, report), results);
            Application.Quit(1);
            yield break;
        }
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
        yield return new WaitForSecondsRealtime(360f);
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

    private IEnumerator VerifyLivesCounter()
    {
        LivesCounter counter = FindFirstObjectByType<LivesCounter>();
        Text label = counter.GetComponentInChildren<Text>(true);
        Check(Game.IsMainMenu && !label.isActiveAndEnabled, "El menú principal oculta el marcador");
        for (int stage = 1; stage <= 3; stage++)
        {
            Game.StartGameAt(stage);
            yield return WaitForLoad();
            Check(label.isActiveAndEnabled && label.text == "VIDAS: 3", "Tres vidas visibles al empezar 1-" + stage);
            yield return Capture("lives-level-" + stage);
            Mario.Death();
            yield return new WaitForSeconds(4);
            yield return WaitForLoad();
            Check(Game.stage == stage && Game.lives == 2 && label.text == "VIDAS: 2", "Morir descuenta una vida del marcador en 1-" + stage);
            Game.AddLife();
            Check(label.text == "VIDAS: 3", "Una vida extra actualiza el marcador en 1-" + stage);
            for (int coin = 0; coin < 100; coin++) Game.AddCoin();
            Check(Game.coins == 0 && label.text == "VIDAS: 4", "Cien monedas otorgan otra vida visible");
            Game.LoadLevel(1, stage == 3 ? 1 : stage + 1);
            yield return WaitForLoad();
            Check(label.isActiveAndEnabled && label.text == "VIDAS: 4" && FindObjectsByType<LivesCounter>(FindObjectsSortMode.None).Length == 1,
                "El cambio de nivel conserva cuatro vidas y un único marcador");
            Game.ShowMainMenu();
            yield return WaitForLoad();
            Check(!label.isActiveAndEnabled, "El marcador vuelve a ocultarse en el menú");
        }
        Game.StartGameAt(3);
        yield return WaitForLoad();
        Check(label.text == "VIDAS: 3", "Empezar otra partida restablece tres vidas");
        for (int remaining = 2; remaining >= 0; remaining--)
        {
            Mario.Death();
            yield return new WaitForSeconds(4);
            yield return WaitForLoad();
            Check(Game.lives == remaining && label.text == "VIDAS: " + remaining, "Cuenta regresiva de vidas: " + remaining);
        }
        Check(Game.IsGameOver && !label.isActiveAndEnabled, "Cero vidas carga Game Over y oculta el marcador");
        Submit(FindFirstObjectByType<GameOverMenu>().retryButton);
        yield return WaitForLoad();
        Check(Game.stage == 1 && label.isActiveAndEnabled && label.text == "VIDAS: 3", "Volver a intentar muestra tres vidas en 1-1");
        yield return Capture("lives-retry");
        Game.GameOver();
        yield return WaitForLoad();
        yield return null;
    }

    private IEnumerator Run()
    {
        yield return VerifyMenus();
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
        Check(Game.stage == 3 && Game.lives == 2 && Game.coins == 5 && Mario.movement.swimming, "Bandera de 1-2: continuación a 1-3 con progreso");
        yield return VerifySwimming();
        Game.LoadLevel(1, 1);
        yield return WaitForLoad();

        Mario.RestoreState(default);
        Mario.Fall();
        yield return new WaitForSeconds(4f);
        yield return WaitForLoad();
        Check(Game.lives == 1 && Game.stage == 1, "Caída: una sola vida perdida");
        Mario.Death();
        yield return new WaitForSeconds(4f);
        yield return WaitForLoad();
        Check(Game.IsGameOver && Game.lives == 0 && Mario == null, "Perder todas las vidas carga Game Over");
        GameOverMenu gameOver = FindFirstObjectByType<GameOverMenu>();
        Check(gameOver != null && FindObjectsByType<Button>(FindObjectsSortMode.None).Length == 2, "Game Over tiene únicamente Reintentar y Salir");
        yield return null;
        Check(EventSystem.current.currentSelectedGameObject == gameOver.retryButton.gameObject, "El teclado selecciona Volver a intentar");
        ExecuteEvents.Execute(gameOver.retryButton.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down }, ExecuteEvents.moveHandler);
        Check(EventSystem.current.currentSelectedGameObject == gameOver.exitButton.gameObject, "Flecha abajo selecciona Salir");
        ExecuteEvents.Execute(gameOver.exitButton.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Up }, ExecuteEvents.moveHandler);
        Check(EventSystem.current.currentSelectedGameObject == gameOver.retryButton.gameObject, "Flecha arriba vuelve a Reintentar");
        yield return Capture("game-over");
        ExecuteEvents.Execute(gameOver.retryButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        gameOver.retryButton.onClick.Invoke(); // Una segunda petición durante el fundido se ignora.
        yield return WaitForLoad();
        Check(Game.lives == 3 && Game.coins == 0 && Game.world == 1 && Game.stage == 1 && !Mario.big && !Mario.starpower,
            "Volver a intentar reinicia toda la partida en 1-1");
        yield return Traverse(false);
        yield return Traverse(true);
        Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length == 1,
            "Administradores persistentes sin duplicados");
        Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Un solo listener tras las transiciones");
        Game.GameOver();
        yield return WaitForLoad();
        yield return null;
        Check(Game.IsGameOver, "La escena final está preparada para verificar el botón Salir");
    }

    private static void Submit(Button button) => ExecuteEvents.Execute(button.gameObject,
        new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

    private IEnumerator VerifyMenus()
    {
        yield return null;
        Check(Game.IsMainMenu && Mario == null, "El ejecutable inicia en el menú principal");
        MainMenuController menu = FindFirstObjectByType<MainMenuController>();
        Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Length == 3, "Inicio: Jugar, Elegir nivel y Salir");
        Check(EventSystem.current.currentSelectedGameObject == menu.playButton.gameObject, "Jugar tiene el foco inicial");
        Check(Game.GetComponents<AudioSource>().Any(a => a.isPlaying && a.clip != null && a.clip.name == "Menu"), "El menú reproduce su música");
        yield return Capture("main-menu");
        for (int stage = 1; stage <= 3; stage++)
        {
            menu = FindFirstObjectByType<MainMenuController>();
            Submit(menu.chooseButton);
            yield return null;
            Check(menu.levelPanel.activeSelf && !menu.mainPanel.activeSelf && FindObjectsByType<Button>(FindObjectsSortMode.None).Length == 4,
                "Selector con tres niveles y Volver");
            if (stage == 1)
            {
                yield return Capture("level-selector");
                Submit(menu.backButton);
                Check(menu.mainPanel.activeSelf && EventSystem.current.currentSelectedGameObject == menu.playButton.gameObject, "Volver restaura el panel y el foco");
                Submit(menu.chooseButton);
            }
            Submit(stage == 1 ? menu.level1Button : stage == 2 ? menu.level2Button : menu.level3Button);
            yield return WaitForLoad();
            Check(Game.stage == stage && Game.lives == 3 && Game.coins == 0 && !Mario.big && !Mario.starpower,
                "Selección 1-" + stage + ": partida nueva y escena correcta");
            Check(Mario.movement.swimming == (stage == 3), "Movimiento apropiado al seleccionar 1-" + stage);
            if (stage == 3)
            {
                var fish = FindFirstObjectByType<AquaticEnemy>();
                PlaceMario(fish.transform.position);
                yield return new WaitForSeconds(.15f);
                Check(Mario.dead, "Un pez puede derrotar a Mario pequeño");
                yield return new WaitForSeconds(3.8f);
                yield return WaitForLoad();
                Check(Game.stage == 3 && Game.lives == 2 && Mario.movement.swimming && !Mario.dead,
                    "La muerte acuática descuenta una vida y reinicia 1-3 nadando");
            }
            Game.AddCoin(); Game.AddLife(); Mario.RestoreState(new PlayerState { big = true, starSeconds = 5 });
            Game.ShowMainMenu();
            yield return WaitForLoad();
            yield return null;
        }
        menu = FindFirstObjectByType<MainMenuController>();
        Submit(menu.playButton);
        menu.playButton.onClick.Invoke();
        yield return WaitForLoad();
        Check(Game.stage == 1 && Game.lives == 3 && Game.coins == 0 && !Mario.big && !Mario.starpower, "Jugar reinicia desde 1-1 y bloquea doble clic");
    }

    private IEnumerator VerifySwimming()
    {
        Mario.RestoreState(default);
        PlaceMario(new Vector2(5, 6));
        PlayerMovement movement = Mario.movement;
        yield return null;
        Check(Game.GetComponents<AudioSource>().Any(a => a.isPlaying && a.clip != null && a.clip.name == "Underwater"), "El agua reproduce su música");
        movement.simulatedInput = true; movement.simulatedJump = true;
        yield return new WaitForSeconds(.6f);
        Check(Mario.transform.position.y > 7.4f, "Nadar asciende con brazadas repetidas");
        float high = Mario.transform.position.y;
        movement.simulatedJump = false;
        yield return new WaitForSeconds(1.5f);
        Check(Mario.transform.position.y < high - .5f, "Sin brazadas Mario desciende lentamente");
        float beforeDive = Mario.transform.position.y;
        movement.simulatedDive = true;
        yield return new WaitForSeconds(.6f);
        Check(Mario.transform.position.y < beforeDive - 1.5f, "Bajar permite una inmersión más rápida");
        PlaceMario(new Vector2(5, 11.5f));
        movement.simulatedDive = false; movement.simulatedJump = true;
        yield return new WaitForSeconds(1f);
        Check(Mario.capsuleCollider.bounds.max.y <= 13.1f, "El techo acuático retiene al jugador");
        Mario.RestoreState(new PlayerState { big = true });
        PlaceMario(new Vector2(5, 3));
        movement.simulatedJump = false; movement.simulatedDive = true;
        yield return new WaitForSeconds(.7f);
        Check(Mario.capsuleCollider.bounds.min.y >= 1.98f, "Suelo acuático sólido con Mario grande");
        PlaceMario(new Vector2(5, 11.4f));
        movement.simulatedDive = false; movement.simulatedJump = true;
        yield return new WaitForSeconds(.6f);
        Check(Mario.capsuleCollider.bounds.max.y <= 13.1f, "El techo también retiene a Mario grande");
        movement.simulatedInput = false; movement.simulatedDive = false;
        var fish = FindObjectsByType<AquaticEnemy>(FindObjectsSortMode.None).OrderBy(f => f.transform.position.x).First();
        PlaceMario(fish.transform.position);
        yield return new WaitForSeconds(.25f);
        Check(!Mario.big && !Mario.dead, "Un pez reduce a Mario grande y protege de impactos repetidos");
        Mario.RestoreState(new PlayerState { starSeconds = 10 });
        PlaceMario(fish.transform.position);
        yield return new WaitForSeconds(.15f);
        Check(fish == null && !Mario.dead && FindObjectsByType<AquaticEnemy>(FindObjectsSortMode.None).Length == 3, "La estrella derrota al pez sin dañar a Mario");
        int beforeCoin = Game.coins;
        var coin = FindObjectsByType<PowerUp>(FindObjectsSortMode.None).First(p => p.type == PowerUp.Type.Coin);
        PlaceMario(coin.transform.position);
        yield return new WaitForSeconds(.1f);
        Check(coin == null && Game.coins == (beforeCoin + 1) % 100, "Las monedas acuáticas se recogen por contacto");
        PlaceMario(new Vector2(34, 8));
        yield return Capture("aquatic-middle");
        Pipe exit = FindFirstObjectByType<Pipe>();
        PlaceMario(new Vector2(66.5f, 2.51f));
        Check(exit.TryEnter(Mario), "La boca acuática admite a Mario pequeño junto al suelo");
        yield return WaitForPipe();
        Check(Mario.transform.position.x > 78 && !movement.swimming, "La entrada baja llega al exterior con física terrestre");
        // La ruta física se verifica más abajo desde el punto de aparición, con ambos tamaños.
    }

    private IEnumerator TraverseWater(bool big)
    {
        Player player = Mario;
        PlayerMovement movement = player.movement;
        movement.simulatedInput = true; movement.simulatedAxis = 1;
        float timeout = Time.realtimeSinceStartup + 35;
        float refresh = Time.time + 5;
        bool captured = false;
        var pipe = FindFirstObjectByType<Pipe>();
        while (!player.dead && !player.IsTransitioning && Time.realtimeSinceStartup < timeout)
        {
            float x = player.transform.position.x, y = player.transform.position.y;
            movement.simulatedJump = x < 60 && y < 9.5f;
            movement.simulatedDive = x >= 60 && y > 3.7f;
            if (x > 64) pipe.TryEnter(player);
            if (!captured && x > 30)
            {
                captured = true;
                yield return Capture(big ? "aquatic-route-big" : "aquatic-route-small");
            }
            if (Time.time > refresh) { player.Starpower(); refresh = Time.time + 5; }
            yield return null;
        }
        movement.simulatedInput = false; movement.simulatedDive = false; movement.simulatedJump = false;
        Check(player.IsTransitioning && !player.dead, "Recorrido físico acuático con Mario " + (big ? "grande" : "pequeño"));
        yield return WaitForPipe();
        Check(player.transform.position.x > 78 && !movement.swimming && player.big == big, "Tubería acuática restaura movimiento terrestre y conserva tamaño");
        yield return Capture(big ? "aquatic-exit-big" : "aquatic-exit-small");
        yield return DriveTo(101, 18);
        if (player != null && !player.IsTransitioning)
        {
            // Frenar sobre el mástil permite caer en su trigger aunque el salto de la escalera lo sobrepase en altura.
            movement.simulatedInput = true;
            yield return ArriveAt(player, 104);
            movement.simulatedInput = false;
        }
        timeout = Time.realtimeSinceStartup + 10;
        while (!Game.IsMainMenu && Time.realtimeSinceStartup < timeout) yield return null;
        yield return WaitForLoad();
        Check(Game.IsMainMenu && Game.CompletedRun && Mario == null, "Bandera final completa los tres niveles y vuelve al menú");
        yield return Capture(big ? "campaign-completed-big" : "campaign-completed-small");
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
        if (Game.IsResetPending) { yield return new WaitForSeconds(4); yield return WaitForLoad(); }
        Game.LoadLevel(1, 2);
        yield return WaitForLoad();
        Mario.RestoreState(new PlayerState { big = big, starSeconds = 10f });
        if (Environment.GetCommandLineArgs().Contains("-elevatorStart")) PlaceMario(new Vector2(136.1f, 5.5f));
        yield return DriveTo(163.5f, 90f);
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
        Check(Game.stage == 3 && Mario.big == big && Mario.movement.swimming, "Escalera y bandera conectan con el agua conservando tamaño: " + (big ? "grande" : "pequeño"));
        if (Game.stage == 3) yield return TraverseWater(big);
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
        bool crossedElevators = false;
        bool subsurface = Game.stage == 2 && destination < 200f;
        while (player != null && !player.dead && !player.IsTransitioning && (subsurface || player.transform.position.x < destination) && Time.realtimeSinceStartup < timeout)
        {
            Bounds bounds = player.capsuleCollider.bounds;
            if (subsurface && !crossedElevators && player.transform.position.x > 136 && player.transform.position.x < 139)
            {
                crossedElevators = true;
                yield return CrossElevators(player);
                movement.simulatedAxis = 1;
                movement.simulatedJump = true;
                bestX = player.transform.position.x;
                lastProgress = Time.realtimeSinceStartup;
                continue;
            }
            if (subsurface && player.big && !preparedFirstPit && player.transform.position.x > 74f && player.transform.position.x < 80f)
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
            if (subsurface && IsUpperRouteBlocked(player, bounds, lastProgress))
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
            if (subsurface && player.transform.position.x > 162f)
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

    private IEnumerator ArriveAt(Player player, float x, bool keepJump = false)
    {
        PlayerMovement movement = player.movement;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (!keepJump) movement.simulatedJump = movement.simulatedJumpPressed = false;
        float deadline = Time.realtimeSinceStartup + 5f;
        while (player != null && !player.dead && !player.IsTransitioning && Time.realtimeSinceStartup < deadline)
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

    private IEnumerator CrossElevators(Player player)
    {
        // Esperar y apuntar al ascensor es necesario: avanzar siempre a la derecha
        // puede dejar al agente bajo el borde del suelo, según la fase de la plataforma.
        var movement = player.movement;
        yield return ArriveAt(player, 137.1f);
        float deadline = Time.realtimeSinceStartup + 3;
        while (!movement.grounded && !player.dead && Time.realtimeSinceStartup < deadline) yield return null;
        player.Starpower();
        deadline = Time.realtimeSinceStartup + 10;
        while (!player.dead && Time.realtimeSinceStartup < deadline && !FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None)
            .Any(p => !p.ascending && p.transform.position.y > 5.5f && p.transform.position.y < 6.5f)) yield return null;
        movement.simulatedJump = movement.simulatedJumpPressed = true;
        yield return ArriveAt(player, 141, true);
        movement.simulatedJump = false;
        deadline = Time.realtimeSinceStartup + 3;
        while (!movement.grounded && !player.dead && Time.realtimeSinceStartup < deadline) yield return null;
        Debug.Log("PART1_ROUTE: sobre el ascensor descendente " + player.transform.position);
        movement.simulatedAxis = 1;
        deadline = Time.realtimeSinceStartup + 2;
        while (player.transform.position.x < 142.3f && !player.dead && Time.realtimeSinceStartup < deadline) yield return null;
        movement.simulatedAxis = 1; movement.simulatedJump = movement.simulatedJumpPressed = true;
        yield return new WaitForSeconds(.9f);
        yield return ArriveAt(player, 151.7f);
        if (player.dead) yield break;
        deadline = Time.realtimeSinceStartup + 3;
        while (!movement.grounded && !player.dead && Time.realtimeSinceStartup < deadline) yield return null;
        // Subir cuando la plataforma ascendente pasa por la altura del suelo.
        deadline = Time.realtimeSinceStartup + 10;
        MovingPlatform platform = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            platform = FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.ascending && p.transform.position.y > 1.3f && p.transform.position.y < 2.4f);
            if (platform != null) break;
            player.Starpower();
            yield return null;
        }
        movement.simulatedAxis = 1; movement.simulatedJump = movement.simulatedJumpPressed = true;
        yield return ArriveAt(player, 156, true);
        movement.simulatedJump = false;
        // Frenar sobre la plataforma y dejarla ganar la altura del último tramo.
        deadline = Time.realtimeSinceStartup + 6;
        while (!player.dead && (player.transform.position.y < 9.5f || !movement.grounded) && Time.realtimeSinceStartup < deadline)
        {
            float delta = 156 - player.transform.position.x;
            movement.simulatedAxis = Mathf.Clamp(delta, -.4f, .4f);
            player.Starpower();
            yield return null;
        }
        movement.simulatedAxis = 1;
        movement.simulatedJump = movement.simulatedJumpPressed = false;
        deadline = Time.realtimeSinceStartup + 4;
        while (!player.dead && player.transform.position.x < 160.7f && Time.realtimeSinceStartup < deadline) yield return null;
        Debug.Log("PART1_ROUTE: cruce de ascensores con espera y salto físico");
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
            Game.stage == 3 ? (position.x < 75 ? new Vector2(0, 72) : new Vector2(78, 114)) : Game.stage == 2 && position.x < 200 ? new Vector2(0, 171) : Game.stage == 2 ? new Vector2(210, 250) : new Vector2(0, 227),
            Game.stage == 3 && position.x < 75 ? new Color(.03f, .27f, .43f) : Game.stage == 2 && position.x < 200 ? Color.black : new Color(0.36f, 0.58f, 0.99f), true);
        Mario.movement.SetSwimming(Game.stage == 3 && position.x < 75);
        Physics2D.SyncTransforms();
        Mario.SetTransitioning(false);
    }

    private IEnumerator Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
        yield return new WaitForEndOfFrame();
        // El modo batch puede omitir el framebuffer de la ventana oculta; renderizar la cámara explícitamente.
        Camera camera = Camera.main;
        Canvas[] overlays = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder < 1000).ToArray();
        foreach (Canvas menuCanvas in overlays)
        {
            menuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            menuCanvas.worldCamera = camera;
            menuCanvas.planeDistance = 1f;
            menuCanvas.enabled = false;
            menuCanvas.enabled = true;
            foreach (Graphic graphic in menuCanvas.GetComponentsInChildren<Graphic>())
            {
                graphic.enabled = false;
                graphic.enabled = true;
                graphic.SetAllDirty();
            }
            Canvas.ForceUpdateCanvases();
        }
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
        foreach (Canvas menuCanvas in overlays) { menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay; menuCanvas.worldCamera = null; }
        RenderTexture.active = previous;
        target.Release();
        Destroy(target);
        Destroy(image);
    }
}
#endif
