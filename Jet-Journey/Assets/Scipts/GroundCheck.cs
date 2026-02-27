using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float crashForceThreshold = 8f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    bool hasExploded = false;
   
    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // Only react to other cars
        if (collision.gameObject.CompareTag("AICar"))
        {
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce >= crashForceThreshold)
            {
                Explode();
            }
        }
    }
    void Explode()
    {
        hasExploded = true;

        // Spawn VFX
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Play Sound
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        // Destroy car
        Destroy(gameObject);
    }
}