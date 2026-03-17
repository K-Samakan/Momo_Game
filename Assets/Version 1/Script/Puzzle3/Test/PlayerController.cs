using System.Collections;
using UnityEngine;
namespace Puzzle3.Test
{
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask blockingLayer;
    private bool isMoving = false;

    // Update is called once per frame
    void Update()
    {
        if (isMoving) return;

        var movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) movement = Vector3.forward;
        if (Input.GetKey(KeyCode.S)) movement = Vector3.back;
        if (Input.GetKey(KeyCode.A)) movement = Vector3.left;
        if (Input.GetKey(KeyCode.D)) movement = Vector3.right;

        if (movement != Vector3.zero)
        {
            TryToMove(movement);
        }
    }

    private void TryToMove(Vector3 direction)
    {
        var targetPosition = transform.position + direction;

        if (!Physics.Raycast(transform.position, direction, out RaycastHit hit, 1f, blockingLayer))
        {
            StartCoroutine(MoveToPosition(targetPosition));
        }
        else if (hit.collider.CompareTag("Block"))
        {
            var block = hit.collider.GetComponent<BlockControl>();
            if (block != null && block.TryToPushBlock(direction, moveSpeed))
            {
                StartCoroutine(MoveToPosition(targetPosition));
            }
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        isMoving = false;    
    }
}
}