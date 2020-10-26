using P2P;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject playerPrefab;

    public ObjSyncList<BasePlayer> players;
    public Dictionary<int, int> cachedSelectors = new Dictionary<int, int>();

    public enum PlayerType {
        strenght,
        magic,
        flight
    }

    public void OnDisable() {
        MultiplayerManager.PacketEvt -= RecieveConnection;
        MultiplayerManager.DisconnectEvt -= Disconnection;
    }
    private void RecieveConnection(RecievedPacket packet) {
        if (packet.type == PacketType.playerMove) {
            players.UnpackAndSyncObj(packet);
        } else if (packet.type == PacketType.playerAttack && packet.value != PacketValue.confirmation) {
            BasePlayer player = players.GetObjWithId(packet.peerId);

            switch ((BasePlayer.PlayerAttack)packet.data[0]) {
                case BasePlayer.PlayerAttack.Primary:
                    player.RecieveAttack(packet.data, packet.GetTimeDifference());
                    break;
            }
        } 
    }

    private void Disconnection(byte id, bool thisPeer) {
        if(thisPeer)
            DespawnPlayers();
        else
            DespawnPlayer(id);
    }

    public void SpawnAllPlayer() {
        cachedSelectors.TryGetValue(MultiplayerManager.LocalId, out int type);
        BasePlayer localPlayer = SpawnPlayer((PlayerType)type, MultiplayerManager.LocalId);
        players = new ObjSyncList<BasePlayer>(localPlayer);
        Camera.main.transform.SetParent(localPlayer.transform, false);

        for (byte i = 0; i < cachedSelectors.Count; i++) {
            if (i != MultiplayerManager.LocalId) {
                cachedSelectors.TryGetValue(i, out int otherType);
                players.Add(SpawnPlayer((PlayerType)otherType, i));
            }
        }

        cachedSelectors.Clear();
        MultiplayerManager.PacketEvt += RecieveConnection;
        MultiplayerManager.DisconnectEvt += Disconnection;
    }

    private BasePlayer SpawnPlayer(PlayerType type, byte playerId) {
        BasePlayer player = Instantiate(playerPrefab).GetComponent<BasePlayer>();
        player.AssignId(playerId);
        player.transform.position += (Vector3)new Vector2(2 * playerId, 0);

        return player;
    }

    public void DespawnPlayer(byte id) {
        BasePlayer player = players.GetObjWithId(id);
        if (player) {
            players.Remove(player);
            Destroy(player.gameObject);
        }
    }

    public void DespawnPlayers() {
        players.ExecStatement(player => Destroy(player.gameObject));
        players.Reset();
        MultiplayerManager.PacketEvt -= RecieveConnection;
        MultiplayerManager.DisconnectEvt -= Disconnection;
    }

    public BasePlayer GetPlayerWithColliderID(long colliderId) {
        foreach(BasePlayer player in players.GetList()) {
            if (player.interacterCollider.GetInstanceID() == colliderId) {
                return player;
            }
        }
        return null;
    }
}
