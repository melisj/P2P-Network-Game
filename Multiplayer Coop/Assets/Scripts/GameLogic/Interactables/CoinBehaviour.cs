using P2P;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable coin
/// </summary>
public class CoinBehaviour : Interactable
{
    public int points = 10;
    public float spinSpeed = 3;
    public float coinScale = 2;
    private float currentTime = 0;

    // Assign points to the player
    public void GivePointsToPlayer(byte entityId) {
        MultiplayerManager.playerManager.players.GetObjWithId(entityId).GivePoints(points);
        Destroy(gameObject);
    }

    public void Update() {
        currentTime += Time.deltaTime;
        transform.localScale = new Vector2(Mathf.Sin(currentTime * spinSpeed) * coinScale, coinScale);
    }
    
    // Check if local player hit the coin
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            BasePlayer player = MultiplayerManager.playerManager.GetPlayerWithColliderID(collision.GetInstanceID());
            if (player)
                if(player.id == MultiplayerManager.LocalId)
                    InvokeAndSendEvents(player.id);
        }
    }
}

