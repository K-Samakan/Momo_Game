using System.Collections;
using UnityEngine;

public class BlockControl : MonoBehaviour
{
    public LayerMask blockingLayer;
    public bool OnGoal = false;
    private bool isMoving = false;
    public bool TryToPushBlock(Vector3 direction, float moveSpeed)
    {
        var targetPosition = transform.position + direction;

        if (!Physics.Raycast(transform.position, direction, out RaycastHit hit, 1f, blockingLayer))
        {
            StartCoroutine(MoveToPosition(targetPosition, moveSpeed));
            return true;
        }

        return false;
    }

    private IEnumerator MoveToPosition(Vector3 target, float moveSpeed)
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            OnGoal = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            OnGoal = false;
        }
    }

}

