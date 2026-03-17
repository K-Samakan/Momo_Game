using UnityEngine;

public class JumpPadforCol : MonoBehaviour
{
    [SerializeField] private float jumpPower;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(transform.up*jumpPower);
        }
    }
}
