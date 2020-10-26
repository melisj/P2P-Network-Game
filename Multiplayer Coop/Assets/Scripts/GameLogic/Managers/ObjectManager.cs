using P2P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all physics objects and sends out the objects that changed in the within the time step
/// </summary>
public class ObjectManager : MonoBehaviour
{
    public ObjSyncList<PhysicsObject> objects;
    public static ObjSyncList<PhysicsObject> movedObjList;

    public void OnEnable() {
        MultiplayerManager.PacketEvt += RecieveConnection;
        PhysicsManager.PhysicsEvt += PhysicsHandler;

        objects = new ObjSyncList<PhysicsObject>(null);
        movedObjList = new ObjSyncList<PhysicsObject>(null);

        // Assign ids to the objects
        PhysicsObject[] objList = FindObjectsOfType<PhysicsObject>();
        byte i = 0;
        foreach(PhysicsObject obj in objList) {
            obj.AssignId(i);
            objects.Add(obj);
            i++;
        }
    }

    // Send out a list of objects which changed in the last time step
    private void PhysicsHandler() {
        if (MultiplayerManager.IsHost) {
            if (movedObjList.Count() != 0) {
                MultiplayerManager.peerManager?.SendDataToAllPeers(movedObjList.GetBytesFromList(), PacketType.objectMove, PacketValue.changeUpdate);
                movedObjList.Reset();
            }
        }
    }

    public void OnDisable() {
        MultiplayerManager.PacketEvt -= RecieveConnection;
        PhysicsManager.PhysicsEvt -= PhysicsHandler;
    }

    private void RecieveConnection(RecievedPacket packet) {
        if (packet.type == PacketType.objectMove) {
            objects.GetListFromBytes(packet, SyncObject);
        }
    }

    private void SyncObject(List<object> objs, float timeDiff) {
        PhysicsObject phyObj = objects.GetObjWithId((byte)objs[0]);
        phyObj?.SyncDataToObj(objs, timeDiff);
    }
}
