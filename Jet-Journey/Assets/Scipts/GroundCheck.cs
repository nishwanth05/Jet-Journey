using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Header("Ground Check Settings")]
    public Transform groundCheckPoint;   // assign empty object under car
    public float checkDistance = 0.5f;
    public LayerMask groundLayer;

    public bool isGrounded;

    void Update()
    {
        isGrounded = Physics.Raycast(
            groundCheckPoint.position,
            Vector3.down,
            checkDistance,
            groundLayer
        );

        Debug.DrawRay(groundCheckPoint.position, Vector3.down * checkDistance,
                      isGrounded ? Color.green : Color.red);

        if (!isGrounded)
        {
            Destroy(gameObject,0.15f);
        }
    }
}