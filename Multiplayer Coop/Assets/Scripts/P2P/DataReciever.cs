using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;


namespace P2P
{

    /// <summary>
    /// Class for recieving data from the specified port
    /// </summary>
    public class DataReciever
    {
        Thread thread;
        private static UdpClient client;
        static Queue<RecievedPacket> QueuedPackets = new Queue<RecievedPacket>();
        public static List<RecievedPacket> RecievedPackets = new List<RecievedPacket>();

        // Thread is receiving packet (locking recieved packet list)
        public static bool RecievedPacketLocked;

        // Get the first packet in the list
        public RecievedPacket GetPacket {
            get { return QueuedPackets.Dequeue(); }
        }

        public int GetQueueLength
        {
            get { return QueuedPackets.Count; }
        }


        public DataReciever() {
            StartRecievingData();
        }

        public void OnDisable() {
            StopRecievingData();
        }

        // Remove a packet when it the timer ran out
        public void RemoveRecievedPacket(RecievedPacket status) {
            if(!RecievedPacketLocked)
                RecievedPackets.Remove(status);
        }

        // Check if a packet has already been recieved
        public static bool CheckIfPacketWasAlreadyRecieved(byte peerId, ushort packetId) {
            foreach(RecievedPacket packet in RecievedPackets) {
                if(packet.packetId == packetId && packet.peerId == peerId)
                    return true;
            }
            return false;
        }

        // Create a new thread and get a new udp client
        public void StartRecievingData() {
            client = NetworkTools.CreateUdpClient();
            Debug.Log("Udp client started listening on port " + MultiplayerManager.ListenPort);

            thread = new Thread(new ThreadStart(RecieveData));
            thread.Start();
        }

        // Stop the thread and remove all packets from the queue
        public void StopRecievingData() {
            QueuedPackets.Clear();
            RecievedPackets.Clear();
            
            client?.Close();
            thread?.Abort();
        }

        // Function for recieving data
        public static void RecieveData() {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receiveBytes = null;

            while (true) {
                try {
                    // Recieve data
                    receiveBytes = client.Receive(ref remoteIpEndPoint);
                }
                catch (Exception e) {
                    Debug.Log(e.ToString());
                }

                RecievedPacketLocked = true;
                lock (RecievedPackets) {
                    ParseData(receiveBytes.ToList(), remoteIpEndPoint.Address.ToString(), (ushort)remoteIpEndPoint.Port);
                }
                RecievedPacketLocked = false;
            }
        }

        public static void ParseData(List<byte> rawPacket, string ip, ushort port) {
            List<byte> data = rawPacket.GetRange(Packet.Reserved, rawPacket.Count - Packet.Reserved);
            PacketType type = (PacketType)rawPacket[0];
            PacketValue value = (PacketValue)rawPacket[1];
            byte id = rawPacket[2];
            bool reliable = BitConverter.ToBoolean(rawPacket.ToArray(), 3);
            ushort packetId = BitConverter.ToUInt16(rawPacket.ToArray(), 4);
            long timeSend = BitConverter.ToInt64(rawPacket.ToArray(), 6);

            RecievedPacket recievedPacket = new RecievedPacket(data, id, type, value, ip, port, timeSend, packetId);

            // Check up if the packet was already recieved
            if (!CheckIfPacketWasAlreadyRecieved(id, packetId)) {
                // Queue the packet for processing
                RecievedPackets.Add(recievedPacket);
                QueuedPackets.Enqueue(recievedPacket);
            }

            // Send a confirmation back to the user
            if (reliable) {
                MultiplayerManager.sender.QueuePacket(
                    new Packet(rawPacket.GetRange(4, 2), MultiplayerManager.LocalId, type, PacketValue.confirmation, ip, port)
                    );
            }
        } 
    }
}
