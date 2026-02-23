using UnityEngine;

public class AICarLOD : MonoBehaviour
{
    public enum LODLevel { Full, Simple, Fake }
    public LODLevel currentLOD;

    public float fullDist = 40f;
    public float simpleDist = 120f;

    Transform player;
    AICarController controller;

    float fullDistSqr;
    float simpleDistSqr;

    void Awake()
    {
        controller = GetComponent<AICarController>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        fullDistSqr = fullDist * fullDist;
        simpleDistSqr = simpleDist * simpleDist;
    }

    void Update()
    {
        if (!player) return;

        float distSqr = (transform.position - player.position).sqrMagnitude;

        if (distSqr < fullDistSqr)
            controller.movementMode = AICarController.MovementMode.FullPhysics;
        else if (distSqr < simpleDistSqr)
            controller.movementMode = AICarController.MovementMode.Simple;
        else
            controller.movementMode = AICarController.MovementMode.Fake;
    }
}