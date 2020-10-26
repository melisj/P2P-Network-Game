using P2P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for objects that travel at constant velocity and have hit detection for entities
/// </summary>
public class Projectile : MonoBehaviour, ISyncObj
{
    public enum Owner : byte {
        Player,
        Enemy
    }

    [SyncData] public byte id { get; set; }
    [SyncData] public byte projectileId { get; set; }
    [SyncData] public Owner owner { get; set; }
    [SyncData] public byte ownerId { get; set; }
    [SyncData] public Vector2 position { get; set; }
    [SyncData] public Vector2 direction { get; set; }
    [SyncData] public byte idHit { get; set; }

    public float speed = 5;
    public byte damage = 10;

    private Rigidbody2D rb;
    public Collider2D collider;

    public void OnEnable() {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        idHit = 255;
    }

    void Update() {
        position += direction * speed * Time.deltaTime;
        rb.MovePosition(position);
    }

    public void SetTransform(Vector2 pos, Vector2 dir) {
        position = pos;
        direction = dir;
        SetTransform();
    }

    public void SetTransform() {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0, 0, Quaternion.LookRotation(direction).eulerAngles.z);
    }

    public void AssignId(byte id) {
        this.id = id;
        ownerId = (byte)Mathf.FloorToInt(id / 10);
    }

    // Recieve a projectile and sync it up based on the timediff
    public void SyncDataToObj(List<object> objs, float timeDiff) {
        DataConverter.ApplyFieldsToInstance(this, objs);
        position += direction * (speed * timeDiff);
        SetTransform();
    }

    public List<byte> GetByteData() {
        return DataConverter.ConvertObjectToByte(this);
    }

    // Delete this projectile from the active projectiles
    private void RemoveThis() {
        GameManager.ProjectileM.RemoveProjectile(this);
    }

    // If the projectile collides with something it gets removed when this projectile is localy simulated
    // Any other projectile (enemies) will be simulated on the host.
    private void OnTriggerEnter2D(Collider2D collision) {
        if (owner == Owner.Enemy && MultiplayerManager.IsHost ||
            ownerId == MultiplayerManager.LocalId) {
            RemoveThis();
        }
    }
}