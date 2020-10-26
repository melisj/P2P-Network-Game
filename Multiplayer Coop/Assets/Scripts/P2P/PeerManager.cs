using System;
using System.Collections.Generic;

namespace P2P
{
    /// <summary>
    /// Manager for peers to help with managing peers in the network
    /// </summary>
    public class PeerManager
    {

        ObjSyncList<Peer> peerList;
        Peer hostPeer;

        public PeerManager(ushort listenPort, string name, bool isHost = false) {
            peerList = new ObjSyncList<Peer>(new Peer(0, NetworkTools.PublicIp, listenPort, name));
            if(isHost)
                SetHost(peerList.localInstance);
        }

        public Peer GetLocalPeer() {
            return peerList.localInstance;
        }

        public int GetPeerCount() {
            return peerList.Count();
        }

        // Add a peer or return a already existing peer
        public Peer TryAddPeer(string ip, ushort port, string name, bool newID = true, byte id = 0) {
            Peer tryPeer = GetPeerWithIp(ip, port);
            if (tryPeer == null) {
                Peer newPeer = new Peer(newID ? GetNewPeerId() : id, ip, port, name);
                peerList.Add(newPeer);
                return newPeer;
            } 
            return tryPeer;
        }

        // Remove a peer from the list
        public void RemovePeer(Peer peer) {
            peerList.Remove(peer);
            if (peer == hostPeer)
                GetNewHost();
        }

        /// <summary>
        /// Get all the peers as a byte list
        /// </summary>
        /// <returns> Byte list </returns>
        public List<byte> GetFullListOfPeers() {
            return peerList.GetBytesFromList();
        }

        public void RetrievePeersFromBytes(RecievedPacket packet) {
            peerList.GetListFromBytes(packet, RecievePeer);
        }

        /// <summary>
        /// Print out a list of peers
        /// </summary>
        /// <returns> peer list string </returns>
        public string PrintAllPeers() {
            string list = "";
            peerList.ExecStatement(peer => {
                list += peer.id + " -- " + peer.name + "\n";
            });
            return list;
        }

        // Callback function for retrieving from list 
        private void RecievePeer(List<object> properties, float timeDiff) {
            RecievePeer(properties, timeDiff, false);
        }

        // Retrieve peer data from property list
        public Peer RecievePeer(List<object> properties, float timeDiff, bool changeIds = true) {
            Peer peer = TryAddPeer((string)properties[1], (ushort)properties[2], (string)properties[3], changeIds, (byte)properties[0]);
            
            // Prevent overwrite from client data
            if (changeIds)
                properties[0] = peer.id;

            peer.SyncDataToObj(properties, timeDiff);
            return peer;
        }

        public List<object> GetPeerDataFromBytes(List<byte> bytes) {
            return peerList.UnpackObj(bytes);
        }

        public Peer GetPeerFromBytes(List<byte> bytes) {
            return peerList.UnpackAndGetObj(bytes);
        }

        public Peer GetHostPeer() {
            return hostPeer;
        }

        /// <summary>
        /// Send data to all the peers
        /// </summary>
        /// <param name="data"> byte data </param>
        /// <param name="type"> type of data </param>
        /// <param name="value"> value in the packet </param>
        /// <param name="sendReliable"> send this packet reliable </param>
        /// <param name="success"> only when reliable </param>
        /// <param name="fail"> only when reliable </param>
        public void SendDataToAllPeers(
            List<byte> data, 
            PacketType type, 
            PacketValue value, 
            bool sendReliable = false,
            Action<PacketStatus> success = null,
            Action<PacketStatus> fail = null) {
            peerList.ExecStatement(peer => {
                if (peer != peerList.localInstance)
                    peer.SendData(data, type, value, sendReliable, success, fail);
            });
        }

        /// <summary>
        /// Send a ping to all peers
        /// </summary>
        public void PingAllPeers() {
            peerList.ExecStatement(peer => {
                if (peer != peerList.localInstance)
                    peer.Ping();
            });
        }

        public Peer GetPeerWithId(byte id) {
            return peerList.GetObjWithId(id);
        }

        public Peer GetPeerWithIp(string ip, ushort port) {
            foreach(Peer peer in peerList.GetList()) {
                if (ip == peer.ip && port == peer.port)
                    return peer;
            }
            return null;
        }

        // Get a new id for a connecting peer
        private byte GetNewPeerId(byte id = 0) {
            foreach (Peer peer in peerList.GetList()) { 
                if (peer.id == id)
                    return GetNewPeerId((byte)(id + 1));
            };
            return id;
        }

        // Get a new host if host left
        private void GetNewHost() {
            byte lowestIndex = byte.MaxValue;
            Peer newHost = null;

            peerList.ExecStatement(peer => {
                if (lowestIndex > peer.id) {
                    newHost = peer;
                    lowestIndex = peer.id;
                }
            });

            if (newHost != null)
                SetHost(newHost);
        }

        // Set a new host
        public void SetHost(Peer peer) {
            peer.isHost = true;
            hostPeer = peer;
            MultiplayerManager.HostId = hostPeer.id;
            MultiplayerManager.IsHost = MultiplayerManager.LocalId == hostPeer.id;
        }
    }
}