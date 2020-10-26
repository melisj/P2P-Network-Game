using P2P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy which has a sight range and searches any players within it
/// The enemy is simulated on the host
/// </summary>
public class BaseEnemy : MonoBehaviour, ISyncObj, IEntity
{
    [SyncData] public byte id { get; set; }
    [SyncData] public byte health { get; set; }
    [SyncData] public Vector2 position { get; set; }
    [SyncData] public byte targetId { get; set; }

    public IEntity targetObj;
    public GameObject[] projectileIds;

    public Transform healthBar;
    private float currentTime = 0;
    private float sightRange = 7;

    public void OnEnable() {
        GameManager.AddEntity(this);
    }

    public void OnDisable() {
        GameManager.RemoveEntity(this);
    }

    public void AssignId(byte id) {
        this.id = id;
        targetId = byte.MaxValue;
        projectileIds = new GameObject[ProjectileManager.maxProjectilesPerObject];
        health = 100;
        position = transform.position;
    }

    public List<byte> GetByteData() {
        return DataConverter.ConvertObjectToByte(this);
    }

    public void SyncDataToObj(List<object> objs, float timeDiff) {
        DataConverter.ApplyFieldsToInstance(this, objs);
    }

    public void Death() {
        MultiplayerManager.peerManager.SendDataToAllPeers(GetByteData(), PacketType.enemy, PacketValue.removeUpdate, true);
        GameManager.EnemyM.DeleteEnemy(this);
    }

    public void Update() {
        if (MultiplayerManager.IsHost) {
            // Has target
            if (targetObj != null) {
                currentTime += Time.deltaTime;
                Shoot();

                // Target is too far or out of sight
                if (Vector2.Distance(((BasePlayer)targetObj).transform.position, transform.position) > sightRange) { 
                    targetObj = null;
                    targetId = byte.MaxValue;
                }
            } else {
                // Has target id but no target, get it from the enemymanager
                if (targetId != byte.MaxValue) {
                    targetObj = GameManager.EnemyM.GetTargetPlayer(targetId);
                } else {
                    // Check for all player (for performance this check could be done every 1/10 of a sec)
                    CheckForTarget();
                }
            }

            // Kill the enemy when health hits 0 or above max health (as byte does not have negative values)
            if (health == 0 || health > 100f)
                Death();
        }

        // Set healthbar locally
        if (health > 100f)
            health = 0;
        healthBar.localScale = new Vector3(health / 100f, healthBar.localScale.y, 1);
    }

    // Check for each player if it is in range
    private void CheckForTarget() {
        foreach (BasePlayer player in MultiplayerManager.playerManager.players.GetList()) {
            if (Vector2.Distance(player.transform.position, transform.position) < sightRange) {
                targetId = player.id;
                MultiplayerManager.peerManager.SendDataToAllPeers(GetByteData(), PacketType.enemy, PacketValue.changeUpdate, true);
                return;
            }
        }
    }

    // Spawn a projectile and send the projectile
    private void Shoot() {
        if (currentTime > 1f) {
            currentTime = 0;
            int i = GeneralTools.GetFirstNullIndexFromArray(projectileIds);
            if (i != -1) {
                Vector2 direction = (((BasePlayer)targetObj).transform.position - transform.position).normalized;
                
                Projectile projectile = GameManager.ProjectileM.SpawnProjectile(
                    1, 
                    (Vector2)transform.position + direction, 
                    direction, 
                    Projectile.Owner.Enemy, 
                    id, 
                    (byte)i);

                projectileIds[i] = projectile.gameObject;
                MultiplayerManager.peerManager.SendDataToAllPeers(projectile.GetByteData(), PacketType.projectile, PacketValue.addUpdate, true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Projectile")) {
            GameManager.ProjectileM.GetProjectile(collision.GetInstanceID()).idHit = id;
        }
    }
}
