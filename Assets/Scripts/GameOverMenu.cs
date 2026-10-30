using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Las dos únicas acciones del final de partida: comenzar de cero o cerrar el juego.</summary>
public sealed class GameOverMenu : MonoBehaviour
{
    public Button retryButton;
    public Button exitButton;
    public CanvasGroup controls;

    private void Awake()
    {
        controls.interactable = false;
        retryButton.onClick.AddListener(Retry);
        exitButton.onClick.AddListener(Exit);
    }

    private IEnumerator Start()
    {
        // No aceptar entradas mientras termina el fundido de la escena anterior.
        yield return null;
        while (GameManager.Instance != null && GameManager.Instance.IsTransitioning) yield return null;
        controls.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    public void Retry()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsTransitioning) return;
        controls.interactable = false;
        GameManager.Instance.NewGame();
    }

    public void Exit()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTransitioning) return;
        controls.interactable = false;
        if (GameManager.Instance != null) GameManager.Instance.QuitGame();
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        if (exitButton != null) exitButton.onClick.RemoveListener(Exit);
    }
}
