using P2P;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    ObjSyncList<BaseEnemy> enemies;
    public static int maxEnemies = 20;

    private void Awake() {
        MultiplayerManager.PacketEvt += PacketHandler;
        enemies = new ObjSyncList<BaseEnemy>(null);

        // Assign ids to the objects
        BaseEnemy[] enemyList = FindObjectsOfType<BaseEnemy>();
        byte i = 3;
        foreach (BaseEnemy enemy in enemyList) {
            enemy.AssignId(i);
            enemies.Add(enemy);
            i++;
        }
    }

    private void OnDisable() {
        MultiplayerManager.PacketEvt -= PacketHandler;
    }

    private void PacketHandler(RecievedPacket packet) {
        if (packet.type == PacketType.enemy) {
            if (packet.value == PacketValue.addUpdate) {
                SpawnEnemy();
            } else if (packet.value == PacketValue.changeUpdate) {
                enemies.UnpackAndSyncObj(packet);
            } else if (packet.value == PacketValue.removeUpdate) {
                DeleteEnemy(packet.data);
            }
        }
    }


    private void SpawnEnemy() { }

    private void DeleteEnemy(List<byte> data) {
        DeleteEnemy(enemies.UnpackAndGetObj(data));
    }

    public void DeleteEnemy(BaseEnemy enemy) {
        enemies.Remove(enemy);
        Destroy(enemy.gameObject);
    }

    // Get the target entity by id
    public IEntity GetTargetPlayer(int targetId) {
        return GameManager.GetEntityFromID((byte)targetId);
    }
}
