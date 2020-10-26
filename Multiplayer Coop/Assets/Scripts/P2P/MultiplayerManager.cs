using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace P2P
{
    /// <summary>
    /// Manages connections between players and starts up the game when requested
    /// </summary>
    [RequireComponent(typeof(PlayerManager))]
    public class MultiplayerManager : MonoBehaviour {
        public static MultiplayerManager manager;
        public static DataSender sender;
        public static DataReciever reciever;
        public static PeerManager peerManager;
        public static PlayerManager playerManager;

        private JoinMenu _joinMenu;
        public JoinMenu JoiningMenu
        {
            get
            {
                if (_joinMenu == null)
                    _joinMenu = FindObjectOfType<JoinMenu>(true);
                return _joinMenu;
            }
        }
        private static LobbyMenu _lobbyMenu;
        public static LobbyMenu Lobby
        {
            get
            {
                if (_lobbyMenu == null)
                    _lobbyMenu = FindObjectOfType<LobbyMenu>(true);
                return _lobbyMenu;
            }
        }

        public delegate void PacketEvent(RecievedPacket packet);
        public static event PacketEvent PacketEvt;
        public delegate void DisconnectionEvent(byte peerId, bool thisPeer);
        public static event DisconnectionEvent DisconnectEvt;

        public static UnityEvent afterDisconnectEvent = new UnityEvent();

        public static ushort ListenPort = 11000;
        public static bool InLobby = false;
        public static bool IsConnected = false;
        public const float TIME_BETWEEN_PINGS = 5f;

        public static byte HostId = 0;
        public static byte LocalId = 0;
        public static bool IsHost = false;

        public void OnEnable() {
            if (manager != null)
                Destroy(gameObject);
            else {
                manager = this;
                Enable();
            }
        }

        public void OnDisable() {
            Disable();
        }

        public void Enable() {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            DontDestroyOnLoad(gameObject);
        }

        public void Disable() {
            sender?.OnDisable();
            reciever?.OnDisable();
            StopAllCoroutines();
            PacketEvt -= RecieveConnection;
            DisconnectEvt -= RecieveDisconnect;
            NetworkTools.LocalEndPoint = null;
        }

        private void OnApplicationQuit() {
            if (IsConnected) {
                SendDisconnection(LocalPeer(), false);
            }
        }

        public void Update() {
            if (sender != null) {
                // Update timer for packets send
                foreach (PacketStatus status in sender.waitingForConfirmation) {
                    bool resend = status.CheckForResending(Time.deltaTime);
                    if (resend)
                        break;
                }
            }
            if (reciever != null) {
                // Get a packet and invoke the event
                while (reciever.GetQueueLength != 0) {
                    PacketEvt.Invoke(reciever.GetPacket);
                }
                // Update timer for packets recieved
                if (!DataReciever.RecievedPacketLocked) {
                    for (int i = DataReciever.RecievedPackets.Count - 1; i >= 0; i--) {
                        DataReciever.RecievedPackets[i].CheckForDestruction(Time.deltaTime);
                    }
                }
            }
        }

        public void StartListening(ushort port) {
            ListenPort = port;
            reciever = new DataReciever();
            sender = new DataSender();
            playerManager = GetComponent<PlayerManager>();
            PacketEvt += RecieveConnection;
            DisconnectEvt += RecieveDisconnect;
            StartCoroutine(CheckConnectionStatus());
        }

        // Send out a signal that the game is starting
        public void StartGame(ObjSyncList<PlayerSelector> selectorList) {
            peerManager.SendDataToAllPeers(new List<byte>(), PacketType.startGame, PacketValue.changeUpdate, true);
            LoadGame(selectorList);
        }

        // Load the first level
        public void LoadGame(ObjSyncList<PlayerSelector> selectorList, float additionalTime = 0) {
            // Caching the selectors to be used for the players
            selectorList.ExecStatement(selector => playerManager.cachedSelectors.Add(selector.id, selector.index));
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(1, LoadSceneMode.Single);
            InLobby = false;
        }

        // Open and join a lobby
        public void TryHostLobby(string name) {
            peerManager = new PeerManager(ListenPort, name, true);
            JoinLobby(true);
        }

        // Show the lobby and set variables
        public void JoinLobby(bool asHost) {
            IsHost = asHost;
            LocalId = LocalPeer().id;
            JoiningMenu.JoinLobbySuccesfully();
            InLobby = true;
            IsConnected = true;
        }

        #region Event Recievers

        // Packet event handler
        private void RecieveConnection(RecievedPacket packet) {
            if (peerManager != null) {
                // Get the request to connect
                if (packet.type == PacketType.connectToNetwork && packet.value == PacketValue.addUpdate) {
                    if (InLobby) {
                        // Get the connecting peer and assign it a new Id
                        peerManager.RecievePeer(peerManager.GetPeerDataFromBytes(packet.data), packet.GetTimeDifference());
                        SendCurrentNetwork();
                    }
                }
                // Get the change in the network
                else if (packet.type == PacketType.returnNetwork && packet.value == PacketValue.completeList) {
                    peerManager.RetrievePeersFromBytes(packet);
                    if (!InLobby)
                        JoinLobby(false);
                    UpdateLobbyMenu();
                }
                // Get the request to disconnect
                else if (packet.type == PacketType.disconnectNetwork && packet.value != PacketValue.confirmation) {
                    InvokeDisconnect(packet.data[0]);
                }
            }
        }

        // Disconnect event handler
        private void RecieveDisconnect(byte peerId, bool thisPeer) {
            RemovePeer(peerManager.GetPeerWithId(peerId));
            if (thisPeer && InLobby)
                InLobby = false;
        }

        // Spawn player when loaded
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            playerManager.SpawnAllPlayer();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion

        #region Connection

        // Send out request to connect to the network
        public void TryJoinGame(string ip, ushort port, string name) {
            peerManager = new PeerManager(ListenPort, name);
            List<byte> peerBytes = LocalPeer().GetByteData();
            sender.QueuePacket(new Packet(peerBytes, 0, PacketType.connectToNetwork, PacketValue.addUpdate, ip, port, true));
        }

        // Send the current peers connected to the network
        public static void SendCurrentNetwork() {
            peerManager.SendDataToAllPeers(peerManager.GetFullListOfPeers(),
                PacketType.returnNetwork,
                PacketValue.completeList,
                true, PeerJoinedSuccessFully, DisconnectNonResponsivePeer
                );
        }

        // Callback when a peer does not respond
        private static void DisconnectNonResponsivePeer(PacketStatus status) {
            RemovePeer(peerManager.GetPeerWithIp(status.packet.ip, status.packet.port));
        }

        // Callback when a peer joins
        private static void PeerJoinedSuccessFully(PacketStatus status) {
            UpdateLobbyMenu();
        }

        

        #endregion

        #region Disconnection

        // Invoke the disconnect event
        public static void InvokeDisconnect(byte peerId) {
            if(IsConnected)
                DisconnectEvt.Invoke(peerId, LocalId == peerId);
            if(peerId == LocalId)
                IsConnected = false;
        }

        // Send a disconnect signal to all peers
        public static void SendDisconnection(Peer peer, 
            bool sendAgain, 
            Action<PacketStatus> success = null, 
            Action<PacketStatus> fail = null
            ) {
            peerManager.SendDataToAllPeers(
                peer.GetByteData(), 
                PacketType.disconnectNetwork, 
                PacketValue.changeUpdate, 
                sendAgain, success, fail);
        }

        // Invoke the disconnect event and the extra events which are assigned before the LeaveGame function
        public void WaitForDisconnect(PacketStatus status) {
            InvokeDisconnect(status != null ? status.packet.peerId : LocalId);
            afterDisconnectEvent?.Invoke();
            afterDisconnectEvent.RemoveAllListeners();
            peerManager = null;
        }

        // Send out a disconnect packet to all peers or disconnect immediately when this is the only peer
        public void LeaveGame(bool disableListener) {
            if (disableListener)
                afterDisconnectEvent.AddListener(Disable);
            if (peerManager.GetPeerCount() > 1) {
                SendDisconnection(LocalPeer(), true, WaitForDisconnect, WaitForDisconnect);
            } else {
                WaitForDisconnect(null);
            }
        }

        #endregion

        #region Check Connection

        // Ping all peers to check if they are still connected
        private IEnumerator CheckConnectionStatus() {
            yield return new WaitForSeconds(TIME_BETWEEN_PINGS);

            peerManager?.PingAllPeers();
            StartCoroutine(CheckConnectionStatus());
        }

        #endregion

        #region Help Tools

        // Update list in the lobby menu
        public static void UpdateLobbyMenu() {
            Lobby?.UpdatePlayerList(peerManager.PrintAllPeers());
        }

        private static void RemovePeer(Peer peer) {
            peerManager.RemovePeer(peer);
            UpdateLobbyMenu();
        }

        // Get the peer this client is representing
        public static Peer LocalPeer() {
            return peerManager?.GetLocalPeer();
        }

        #endregion

    }
}