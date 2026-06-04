using UnityEngine;
using UnityEngine.AI;

public class EnemyDemo : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        agent.SetDestination(player.position);
    }
}
