using UnityEngine;

/// <summary>Datos de la escena, separados de las vidas y monedas de la partida.</summary>
public sealed class LevelSettings : MonoBehaviour
{
    [Min(1)] public int world = 1;
    [Min(1)] public int stage = 1;
    public bool underground;
    public float cameraHeight = 6.5f;
    public Vector2 cameraBounds = new Vector2(0f, 225f);
    public Color background = new Color(0.36f, 0.58f, 0.99f);

    public void Apply()
    {
        Camera camera = Camera.main;
        if (camera != null && camera.TryGetComponent(out SideScrollingCamera scrolling))
            scrolling.SetRegion(underground, cameraHeight, cameraBounds, background, true);
        GameAudio.Instance?.SetEnvironment(underground);
    }
}
