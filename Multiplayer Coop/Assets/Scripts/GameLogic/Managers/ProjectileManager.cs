using P2P;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every entity can only have max 10 projectiles, as the limit of id is 255 as it is a byte (ISyncObj dictates it)
/// </summary>
public class ProjectileManager : MonoBehaviour
{
    ObjSyncList<Projectile> projectiles;
    Dictionary<long, Projectile> colliderIdWithProjectiles = new Dictionary<long, Projectile>();
    List<Projectile> flaggedForDestruction = new List<Projectile>();
    public static int maxProjectilesPerObject = 10;

    private void Awake()
    {
        MultiplayerManager.PacketEvt += PacketHandler;
        projectiles = new ObjSyncList<Projectile>(null);
        StartCoroutine(DestroyFlaggedProjectiles());
    }

    private void OnDisable() {
        MultiplayerManager.PacketEvt -= PacketHandler;
        StopAllCoroutines();
    }

    private void PacketHandler(RecievedPacket packet) {
        if(packet.type == PacketType.projectile) {
            if(packet.value == PacketValue.addUpdate) {
                RecieveNewProjectile(packet.data, packet.GetTimeDifference());
            } else if(packet.value == PacketValue.removeUpdate) {
                RecieveRemoveProjectile(packet);
            }
        }
    }
    
    // Spawn a recieved projectile (mostly from enemies as players do it via attacks)
    private void RecieveNewProjectile(List<byte> data, float timeDiff) {
        List<object> properties = projectiles.UnpackObj(data);
        SpawnProjectile((byte)properties[1]).SyncDataToObj(properties, timeDiff);
    }

    // Spawn a projectile (Should be done with object pooling)
    public Projectile SpawnProjectile(byte prefabIndex) {
        GameObject prefab = GameData.instance.projectilePrefabs[prefabIndex];
        Projectile projectile = Instantiate(prefab).GetComponent<Projectile>();
        projectiles.Add(projectile);
        colliderIdWithProjectiles.Add(projectile.collider.GetInstanceID(), projectile);
        return projectile;
    }

    // Spawn and set variables of the projectile
    public Projectile SpawnProjectile(byte prefabIndex, Vector2 pos, Vector2 dir, Projectile.Owner owner, byte ownerId, byte localId, float timeDiff = 0) {
        Projectile projectile = SpawnProjectile(prefabIndex);
        projectile.AssignId((byte)(ownerId * maxProjectilesPerObject + localId));
        projectile.projectileId = prefabIndex;
        projectile.owner = owner;
        projectile.SetTransform(pos + dir * projectile.speed * timeDiff, dir);
        Debug.LogError("TimeDiff: " + timeDiff + " s");

        return projectile;
    }

    private void RecieveRemoveProjectile(RecievedPacket packet) {
        DestroyProjectile(projectiles.UnpackAndSyncObj(packet));
    }

    public void RemoveProjectile(Projectile projectile) {
        if (!flaggedForDestruction.Contains(projectile)) {
            projectile.gameObject.SetActive(false);
            flaggedForDestruction.Add(projectile);
        }
    }

    // Do damage and remove all entries of this projectile and flag it for destruction
    // This is so to maintain a reference of the projectile until the end of the collision detection
    public void DestroyProjectile(Projectile projectile) {
        DoDamage(projectile);
        colliderIdWithProjectiles.Remove(projectile.collider.GetInstanceID());
        projectiles.Remove(projectile);
        flaggedForDestruction.Remove(projectile);
        Destroy(projectile.gameObject);
    }

    public Projectile GetProjectile(long colliderId) {
        colliderIdWithProjectiles.TryGetValue(colliderId, out Projectile instance);
        return instance;
    }

    // Damage the object it hit with the id
    public void DoDamage(Projectile projectile) {
        if (projectile.idHit != 255) {
            if(projectile.idHit == MultiplayerManager.LocalId || projectile.idHit >= 3)
                GameManager.GetEntityFromID(projectile.idHit).health -= projectile.damage;
        }
    }

    // Destroy the projectile 
    public IEnumerator DestroyFlaggedProjectiles() {
        yield return new WaitForFixedUpdate();
        for (int i = flaggedForDestruction.Count - 1; i >= 0; i--) {
            MultiplayerManager.peerManager.SendDataToAllPeers(flaggedForDestruction[i].GetByteData(), PacketType.projectile, PacketValue.removeUpdate, true);
            DestroyProjectile(flaggedForDestruction[i]);
        }
        StartCoroutine(DestroyFlaggedProjectiles());
    }
}
