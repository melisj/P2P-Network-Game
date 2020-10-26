using System;
using System.Collections.Generic;
using UnityEngine;

namespace P2P {

    /// <summary>
    /// Attribute for flagging a property to be synced up
    /// </summary>
    class SyncData : Attribute {
        public enum TypeOfData {
            normal,
            ip
        }

        TypeOfData type;

        public SyncData(TypeOfData type = TypeOfData.normal) {
            this.type = type;
        }

        public TypeOfData GetDataType() {
            return type;
        }
    }

    /// <summary>
    /// Interface for objects which should be synced
    /// </summary>
    public interface ISyncObj {
        [SyncData] byte id { get; set; }

        void AssignId(byte id); // Assign an id
        void SyncDataToObj(List<object> objs, float timeDiff); // Sync property list to this object
        List<byte> GetByteData(); // Get this object in byte representation
    }

    public enum PacketType : byte
    {
        ping,
        connectToNetwork,
        returnNetwork,
        disconnectNetwork,
        charSelect,
        startGame,
        playerMove,
        playerAttack,
        projectile,
        enemy,
        interactable,
        objectMove
    }

    // The data type being send
    public enum PacketValue : byte
    {
        confirmation,
        completeList,
        addUpdate,
        removeUpdate,
        changeUpdate
    }

    /// <summary>
    /// Packet ready for queueing
    /// </summary>
    public class Packet 
    {
        // Space reserved for default values
        public static int Reserved = 14;
        public static ushort CurrentPacketId = 0;

        public List<byte> data = new List<byte>();
        public byte peerId;
        public ushort packetId;
        public string ip;
        public ushort port;
        public PacketType type;
        public PacketValue value;

        public bool reliable;

        public Packet(List<byte> data,
            byte peerId,
            PacketType type,
            PacketValue value,
            string ip,
            ushort port,
            bool reliable = false
            ) {

            this.type = type;
            this.value = value;
            this.peerId = peerId;
            this.ip = ip;
            this.port = port;
            this.packetId = CurrentPacketId;
            this.reliable = reliable;

            this.data.Add((byte)type);
            this.data.Add((byte)value);
            this.data.Add(peerId);
            this.data.Add(BitConverter.GetBytes(reliable)[0]);
            this.data.AddRange(BitConverter.GetBytes(CurrentPacketId));
            this.data.AddRange(BitConverter.GetBytes(DateTimeOffset.Now.ToUnixTimeMilliseconds()));
            this.data.AddRange(data);

            CurrentPacketId++;
        }
    }

    /// <summary>
    /// Packet which has been recieved 
    /// Data is seperated from the packet info
    /// </summary>
    public class RecievedPacket
    {
        public const float DESTROY_TIME = 5f;

        public List<byte> data = new List<byte>();
        public byte peerId;
        public ushort packetId;
        public string ip;
        public ushort port;
        public PacketType type;
        public PacketValue value;
        public long timeSend;
        private float destructionTime;

        public RecievedPacket(List<byte> data,
            byte peerId,
            PacketType type,
            PacketValue value,
            string ip,
            ushort port,
            long timeSend,
            ushort packetId = 0
            ) {

            this.type = type;
            this.value = value;
            this.peerId = peerId;
            this.ip = ip;
            this.port = port;
            this.timeSend = timeSend;
            this.packetId = packetId;
            this.data = data;
            destructionTime = 0;
        }

        public float GetTimeDifference() {
            return Mathf.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - timeSend) / 1000f; 
        }

        public void CheckForDestruction(float timeIncrement) {
            destructionTime += timeIncrement;
            if(destructionTime >= DESTROY_TIME)
                MultiplayerManager.reciever.RemoveRecievedPacket(this);
        }
    }

    /// <summary>
    /// The status of the packet being send
    /// Keeps track of the reliable packet for resending
    /// </summary>
    public class PacketStatus
    {
        public const float RESEND_TIME = 0.5f;
        public const float MAX_TIMES_SEND = 4;

        public ushort packetId;
        public float timeoutTime;
        public byte resendTimes;
        public Packet packet;
        public Action<PacketStatus> succes;
        Action<PacketStatus> fail;

        public PacketStatus(Packet packet, ushort packetId, Action<PacketStatus> succesCallback, Action<PacketStatus> failCallback) {
            timeoutTime = 0;
            resendTimes = 0;
            this.packet = packet;
            this.packetId = packetId;
            succes = succesCallback;
            fail = failCallback;
        }

        // has timeout for resending reached
        public bool CheckForResending(float timeIncrement) {
            timeoutTime += timeIncrement;
            if (timeoutTime > RESEND_TIME) {
                ResendPacket();
                return true;
            }
            return false;
        }

        // Send packet again or disconnect player which does not respond
        public void ResendPacket() {
            resendTimes++;
            timeoutTime = 0;

            if (resendTimes >= MAX_TIMES_SEND) {
                Debug.LogError("Message never got confirmed, dropping packet: " + packetId + " -=- Type: " + packet.type);
                fail?.Invoke(this);

                // Ping peer to check if still connected
                Peer peerToPing = null;
                if (packet != null)
                     peerToPing = MultiplayerManager.peerManager?.GetPeerWithIp(packet.ip, packet.port);
                if (peerToPing != null) {
                    // Delete player
                    if (packet.type == PacketType.ping) {
                        MultiplayerManager.InvokeDisconnect(peerToPing.id);
                        MultiplayerManager.SendDisconnection(peerToPing, false);
                    // Ping again
                    } else {
                        peerToPing.Ping();
                    }
                }
                
                // Remove the packet from the waiting line
                MultiplayerManager.sender.RemoveWaitingPacket(this);
            } else {
                Debug.LogError("Resending packet: " + packetId + " -=- Times send: " + resendTimes);
                MultiplayerManager.sender.QueuePacket(packet, null, null, false);
            }
        }
    }
}
