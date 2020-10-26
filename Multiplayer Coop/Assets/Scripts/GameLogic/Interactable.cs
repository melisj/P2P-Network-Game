using P2P;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Basic interactable can be used for any interaction
/// </summary>
public class Interactable : MonoBehaviour, ISyncObj
{
    [SyncData] public byte id { get; set; }
    [SyncData] public byte idOfInteracter { get; set; }

    [SerializeField] protected UnityEvent<byte> interactEvent;

    private Collider2D collider;

    public void AssignId(byte id) {
        this.id = id;
        collider = GetComponent<Collider2D>();
        GameManager.InteractableM.RegisterInteractable(collider.GetInstanceID(), this);
    }

    public void OnDisable() {
        GameManager.InteractableM.UnregisterInteractable(collider.GetInstanceID());
    }

    public List<byte> GetByteData() {
        return DataConverter.ConvertObjectToByte(this);
    }

    public void SyncDataToObj(List<object> objs, float timeDiff) {
        DataConverter.ApplyFieldsToInstance(this, objs);
    }

    // Invoke event
    public void InvokeEvents() {
        interactEvent?.Invoke(idOfInteracter);
    }

    // Send out to all peers this event triggerd
    public void InvokeAndSendEvents(byte idOfInteracter) {
        this.idOfInteracter = idOfInteracter;
        InvokeEvents();
        MultiplayerManager.peerManager.SendDataToAllPeers(GetByteData(), PacketType.interactable, PacketValue.changeUpdate, true);
    }
}
