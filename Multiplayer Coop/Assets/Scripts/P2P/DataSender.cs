using System.Collections.Generic;
using System;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;

namespace P2P
{
    /// <summary>
    /// Class for handling packets queueing and sending
    /// </summary>
    public class DataSender
    {
        Queue<Packet> packets = new Queue<Packet>();
        public List<PacketStatus> waitingForConfirmation = new List<PacketStatus>();
        Thread thread;
        UdpClient client;

        public DataSender() {
            client = NetworkTools.CreateUdpClient();
            thread = new Thread(new ThreadStart(Update));
            thread.Start();

            MultiplayerManager.PacketEvt += RecievePacket;
        }

        // Recieve confirmation packets
        private void RecievePacket(RecievedPacket packet) {
            if(packet.value == PacketValue.confirmation) {
                ushort id = BitConverter.ToUInt16(packet.data.ToArray(), 0);

                foreach (PacketStatus status in waitingForConfirmation) {
                    if (status.packetId == id) {
                        status.succes?.Invoke(status);
                        Debug.Log("Packet confirmed: " + id);
                        RemoveWaitingPacket(status);
                        return;
                    }
                }
                Debug.LogError("No packet found to confirm: " + id);
            }
        }

        // Remove a packet from the reliable list
        public void RemoveWaitingPacket(PacketStatus status) {
            waitingForConfirmation.Remove(status);
        }

        // Stop the thread and delete all outgoing packets
        public void OnDisable() {
            packets.Clear();
            waitingForConfirmation.Clear();

            client?.Close();
            thread?.Abort();

            MultiplayerManager.PacketEvt -= RecievePacket;
        }

        // Queue a packet to be send
        public void QueuePacket(
            Packet packet, 
            Action<PacketStatus> succesCallback = null, 
            Action<PacketStatus> failCallback = null, 
            bool addPacketForConfirmation = true
            ) {
            packets.Enqueue(packet);
            if (packet.reliable && addPacketForConfirmation)
                waitingForConfirmation.Add(new PacketStatus(packet, packet.packetId, succesCallback, failCallback));
        }

        // Update loop for sending data
        private void Update() {
            while (true) {
                while (packets.Count != 0) {
                    Packet packet = packets.Dequeue();
                    SendData(packet.data, packet.ip, packet.port);
                }

                Thread.Sleep(10);
            }
        }

        // Send data on the udp client
        private void SendData(List<byte> data, string ip, int toPort) {
            try {
                client.Send(data.ToArray(), data.Count, ip, toPort);
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
}