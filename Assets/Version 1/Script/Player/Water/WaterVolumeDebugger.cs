using UnityEngine;

public class WaterVolumeDebugger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"✅ TRIGGER WORKS! Detected: {other.gameObject.name}");
    }
}
