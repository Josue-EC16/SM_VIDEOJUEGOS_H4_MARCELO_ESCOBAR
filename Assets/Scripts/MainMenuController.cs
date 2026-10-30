using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Inicio de partida y selección libre de nivel, sin conservar datos de otra sesión.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject levelPanel;
    public CanvasGroup controls;
    public Text title;
    public Text subtitle;
    public Button playButton, chooseButton, exitButton;
    public Button level1Button, level2Button, level3Button, backButton;

    private void Awake()
    {
        controls.interactable = false;
        playButton.onClick.AddListener(Play);
        chooseButton.onClick.AddListener(ShowLevels);
        exitButton.onClick.AddListener(Exit);
        level1Button.onClick.AddListener(Level1);
        level2Button.onClick.AddListener(Level2);
        level3Button.onClick.AddListener(Level3);
        backButton.onClick.AddListener(ShowHome);
    }

    private IEnumerator Start()
    {
        yield return null;
        while (GameManager.Instance.IsTransitioning) yield return null;
        controls.interactable = true;
        ShowHome();
    }

    private void Update()
    {
        if (levelPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) ShowHome();
    }

    public void ShowHome()
    {
        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
        title.text = "SUPER MARIO";
        subtitle.text = GameManager.Instance.CompletedRun ? "¡RECORRIDO COMPLETADO!" : "Tres niveles, una aventura.";
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    public void ShowLevels()
    {
        if (GameManager.Instance.IsTransitioning) return;
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
        title.text = "ELEGIR NIVEL";
        subtitle.text = "Comienza donde prefieras.";
        GameAudio.Instance?.Play(SoundEffect.Bump);
        EventSystem.current.SetSelectedGameObject(level1Button.gameObject);
    }

    public void Play() => Begin(1);
    private void Level1() => Begin(1);
    private void Level2() => Begin(2);
    private void Level3() => Begin(3);
    private void Begin(int stage)
    {
        if (GameManager.Instance.IsTransitioning) return;
        controls.interactable = false;
        GameManager.Instance.StartGameAt(stage);
    }

    public void Exit()
    {
        if (GameManager.Instance.IsTransitioning) return;
        controls.interactable = false;
        GameManager.Instance.QuitGame();
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(Play);
        if (chooseButton != null) chooseButton.onClick.RemoveListener(ShowLevels);
        if (exitButton != null) exitButton.onClick.RemoveListener(Exit);
        if (level1Button != null) level1Button.onClick.RemoveListener(Level1);
        if (level2Button != null) level2Button.onClick.RemoveListener(Level2);
        if (level3Button != null) level3Button.onClick.RemoveListener(Level3);
        if (backButton != null) backButton.onClick.RemoveListener(ShowHome);
    }
}
