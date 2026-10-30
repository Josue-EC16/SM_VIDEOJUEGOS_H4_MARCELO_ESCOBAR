using UnityEngine;

/// <summary>Región de destino opcional para tuberías de una misma escena.</summary>
public sealed class PipeRegion : MonoBehaviour
{
    public bool underground;
    public bool underwater;
    public float cameraHeight = 6.5f;
    public Vector2 cameraBounds;
    public Color background = new Color(0.36f, 0.58f, 0.99f);

    public void Apply(SideScrollingCamera camera)
    {
        camera.SetRegion(underground, cameraHeight, cameraBounds, background, true);
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null) player.movement.SetSwimming(underwater);
        if (underwater) GameAudio.Instance?.SetTheme(MusicTheme.Underwater);
    }
}
