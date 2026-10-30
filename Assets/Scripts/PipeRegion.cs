using UnityEngine;

/// <summary>Región de destino opcional para tuberías de una misma escena.</summary>
public sealed class PipeRegion : MonoBehaviour
{
    public bool underground;
    public float cameraHeight = 6.5f;
    public Vector2 cameraBounds;
    public Color background = new Color(0.36f, 0.58f, 0.99f);

    public void Apply(SideScrollingCamera camera)
    {
        camera.SetRegion(underground, cameraHeight, cameraBounds, background, true);
    }
}
