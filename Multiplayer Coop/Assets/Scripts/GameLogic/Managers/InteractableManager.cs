using P2P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactables are stored here and can be found by their collider instance id
/// </summary>
public class InteractableManager : MonoBehaviour
{
    ObjSyncList<Interactable> interactables;
    Dictionary<long, Interactable> colliderIdWithInteractable = new Dictionary<long, Interactable>();
    public static int maxEnemies = 20;

    private void Awake() {
        MultiplayerManager.PacketEvt += PacketHandler;
        interactables = new ObjSyncList<Interactable>(null);

        // Assign ids to the objects
        Interactable[] interactableList = FindObjectsOfType<Interactable>();
        byte i = 0;
        foreach (Interactable interactable in interactableList) {
            interactable.AssignId(i);
            interactables.Add(interactable);
            i++;
        }
    }

    public void RegisterInteractable(long colliderId, Interactable instance) {
        colliderIdWithInteractable.Add(colliderId, instance);
    }

    public void UnregisterInteractable(long colliderId) {
        colliderIdWithInteractable.Remove(colliderId);
    }

    public Interactable GetInteractable(long colliderId) {
        colliderIdWithInteractable.TryGetValue(colliderId, out Interactable instance);
        return instance;
    }


    private void OnDisable() {
        MultiplayerManager.PacketEvt -= PacketHandler;
    }

    private void PacketHandler(RecievedPacket packet) {
        if (packet.type == PacketType.interactable) {
            if (packet.value == PacketValue.changeUpdate) {
                InvokeInteraction(packet);
            }
        }
    }

    private void InvokeInteraction(RecievedPacket packet) {
        interactables.UnpackAndSyncObj(packet).InvokeEvents();
    }
}
