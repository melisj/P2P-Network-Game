using System;
using System.Collections.Generic;
using UnityEngine;

namespace P2P
{
    public class Peer : ISyncObj
    {
        [SyncData()] public byte id { get; set; }
        [SyncData(SyncData.TypeOfData.ip)] public string ip { get; set; }
        [SyncData()] public ushort port { get; set; }
        [SyncData()] public string name { get; set; }
        [SyncData()] public bool isHost { get; set; }

        public Peer(byte id, string ip, ushort port, string name, bool isHost = false) {
            this.id = id;
            this.ip = ip;
            this.port = port;
            this.name = name;
            if(isHost)
                MultiplayerManager.peerManager.SetHost(this);
        }

        // Send data to this peer
        public void SendData(
            List<byte> send,
            PacketType type,
            PacketValue value,
            bool sendReliable = false,
            Action<PacketStatus> success = null,
            Action<PacketStatus> fail = null
            ) {
            MultiplayerManager.sender.QueuePacket(
                new Packet(send, MultiplayerManager.peerManager.GetLocalPeer().id, type, value, ip, port, sendReliable),
                success,
                fail
                );
        }

        // Ping this peer
        public void Ping() {
            List<byte> idInfo = new List<byte>() { id };
            SendData(idInfo, PacketType.ping, PacketValue.addUpdate, true);
        }

        public void SyncDataToObj(List<object> objs, float timeDiff) {
            DataConverter.ApplyFieldsToInstance(this, objs);
            // Set host when it is flagged
            if(isHost)
                MultiplayerManager.peerManager.SetHost(this);
        }

        public List<byte> GetByteData() {
            return DataConverter.ConvertObjectToByte(this);
        }

        // No implemetation needed
        public void AssignId(byte id) { }
    }
}