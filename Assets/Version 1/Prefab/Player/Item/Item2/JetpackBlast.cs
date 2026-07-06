// ============================================================
// FILE: JetpackBlast.cs
// PATH: Assets/Version 1/Prefab/Player/Item/Item2/JetpackBlast.cs
// ============================================================
using UnityEngine;
using MOMO.AI.Monster;

public class JetpackBlast : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private int _damage;
    private float _lifetime = 3f;

    public void Launch(Vector3 direction, float speed, int damage)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;

        Destroy(gameObject, _lifetime);
    }

    void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // ชนศัตรู
        var enemy = other.GetComponent<MonsterHealth>();
        if (enemy != null)
        {
            // ✅ แก้ไข: ส่ง attackerPosition ด้วย
            enemy.TakeDamage(_damage, transform.position);
            Debug.Log($"[JetpackBlast] 💥 Hit {other.name} for {_damage} damage!");
            Destroy(gameObject);
            return;
        }

        // ชนพื้น/กำแพง
        if (!other.CompareTag("Player"))
        {
            Debug.Log("[JetpackBlast] 💥 Hit terrain");
            Destroy(gameObject);
        }
    }
}