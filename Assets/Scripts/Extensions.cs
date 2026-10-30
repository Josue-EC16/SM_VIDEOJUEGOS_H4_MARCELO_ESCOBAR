using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    private static readonly RaycastHit2D[] surfaceHits = new RaycastHit2D[16];
    private static readonly List<Collider2D> bodyColliders = new List<Collider2D>(4);

    /// <summary>Usa la superficie real del contacto, no la posición del centro del bloque.</summary>
    public static bool HasContactNormal(this Collision2D collision, Vector2 direction, float minimumDot = 0.5f)
    {
        for (int i = 0; i < collision.contactCount; i++)
            if (Vector2.Dot(collision.GetContact(i).normal, direction) > minimumDot) return true;
        return false;
    }

    /// <summary>Proyecta la forma física real; ignora triggers y contactos de otra dirección.</summary>
    public static bool Raycast(this Rigidbody2D rigidbody, Vector2 direction)
    {
        return TrySurface(rigidbody, direction, out _);
    }

    public static bool TryGetGround(this Rigidbody2D rigidbody, out RaycastHit2D ground)
    {
        return TrySurface(rigidbody, Vector2.down, out ground);
    }

    private static bool TrySurface(Rigidbody2D body, Vector2 direction, out RaycastHit2D surface)
    {
        surface = default;
        if (body.bodyType == RigidbodyType2D.Kinematic || direction.sqrMagnitude < 0.0001f) return false;
        body.GetComponents(bodyColliders);
        Collider2D shape = null;
        foreach (Collider2D candidate in bodyColliders)
            if (candidate.enabled && !candidate.isTrigger) { shape = candidate; break; }
        if (shape == null) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1); // Terreno y ascensores: capa Default.
        filter.useTriggers = false;
        Vector2 axis = direction.normalized;
        int count = shape.Cast(axis, filter, surfaceHits, 0.07f);
        float closest = float.PositiveInfinity;
        Bounds bounds = shape.bounds;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = surfaceHits[i];
            if (hit.rigidbody == body || Vector2.Dot(hit.normal, -axis) < 0.65f) continue;
            Bounds other = hit.collider.bounds;
            // Un contacto inicial con el suelo no es una pared. Una pared no es suelo.
            if (Mathf.Abs(axis.x) > 0.5f && (other.max.y <= bounds.min.y + 0.05f || other.min.y >= bounds.max.y - 0.05f)) continue;
            if (axis.y < -0.5f && hit.distance <= 0f && other.max.y > bounds.min.y + 0.08f) continue;
            if (hit.distance >= closest) continue;
            closest = hit.distance;
            surface = hit;
        }
        return surface.collider != null;
    }

    // Checks if the transform is facing another transform in a given direction.
    // For example, if you want to check if the player stomps on an enemy, you
    // would pass the player transform, the enemy transform, and Vector2.down.
    public static bool DotTest(this Transform transform, Transform other, Vector2 testDirection)
    {
        Vector2 direction = other.position - transform.position;
        return Vector2.Dot(direction.normalized, testDirection) > 0.25f;
    }

}
