using P2P;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lobby handler, handles selectors and can start the game
/// </summary>
public class LobbyMenu : MonoBehaviour
{
    [SerializeField] private Text playerList = null;
    [SerializeField] private Text portText = null;

    [SerializeField] private RectTransform selectorParent = null;
    [SerializeField] private GameObject selectorPrefab = null;

    private MainMenu _mainMenu = null;
    private MainMenu Menu
    {
        get
        {
            if (_mainMenu == null)
                _mainMenu = FindObjectOfType<MainMenu>(true);
            return _mainMenu;
        }
    }

    ObjSyncList<PlayerSelector> selectorList;

    public void OnEnable() {
        UpdatePlayerList(MultiplayerManager.peerManager.PrintAllPeers());
        UpdateListeningPort();

        MultiplayerManager.PacketEvt += RecievePacket;
        MultiplayerManager.DisconnectEvt += Disconnection;

        selectorList = new ObjSyncList<PlayerSelector>(SpawnPlayerSelector(MultiplayerManager.LocalId));

        if (!MultiplayerManager.IsHost) {
            MultiplayerManager.peerManager.GetHostPeer().SendData(
                selectorList.localInstance.GetByteData(),
                PacketType.charSelect,
                PacketValue.addUpdate,
                true);
        }
    }

    public void OnDisable() {
        MultiplayerManager.PacketEvt -= RecievePacket;
        MultiplayerManager.DisconnectEvt -= Disconnection;
    }

    public void Update() {
        // Move the selector
        if (Input.GetKeyDown(KeyCode.A)) {
            selectorList.localInstance.Move(-1);
        } else if (Input.GetKeyDown(KeyCode.D)) {
            selectorList.localInstance.Move(1);
           
        // Lock the selector
        } else if (Input.GetKeyDown(KeyCode.Space)) {
            if (!CheckIfSpotInUse(selectorList.localInstance.index))
                selectorList.localInstance.ToggleLockState();
        }
    }

    public void StartGame() {
        if(ReadyToStart())
            MultiplayerManager.manager.StartGame(selectorList);
    }
    
    private void RecievePacket(RecievedPacket packet) {
        if (packet.type == PacketType.charSelect) {
            // Send out complete list of selectors
            if (packet.value == PacketValue.completeList) {
                selectorList.GetListFromBytes(packet, UpdateSelector);
                
            // Add selector
            } else if (packet.value == PacketValue.addUpdate) {
                UpdateSelector(selectorList.UnpackObj(packet.data), packet.GetTimeDifference());
                MultiplayerManager.peerManager.SendDataToAllPeers(selectorList.GetBytesFromList(), PacketType.charSelect, PacketValue.completeList, true);
            
            // Selector moves or changes
            } else if (packet.value == PacketValue.changeUpdate) {
                UpdateSelector(selectorList.UnpackObj(packet.data), packet.GetTimeDifference());
            }

            // Load the game
        } else if (packet.type == PacketType.startGame) {
            MultiplayerManager.manager.LoadGame(selectorList, packet.GetTimeDifference());
        } 
    }

    // Delete the selectors with the id of the peer
    private void Disconnection(byte id, bool thisPeer) {
        if (!thisPeer) {
            PlayerSelector selector = GetSelectorFromID(id);
            if (selector) {
                selectorList.Remove(selector);
                Destroy(selector.gameObject);
            }
        // Delete all selectors when this peer is leaving
        }else {
            DeleteAllSelectors();
        }
    }


    // Check if the players are all ready
    private bool ReadyToStart() {
        if (MultiplayerManager.IsHost) {
            foreach (PlayerSelector selector in selectorList.GetList()) {
                if (!selector.locked)
                    return false;
            }
        return true;
        }
        return false;
    }

    // Try to leave the lobby
    public void LeaveLobby() {
        try {
            MultiplayerManager.afterDisconnectEvent.AddListener(Menu.BackToJoinMenu);
            MultiplayerManager.manager.LeaveGame(false);
        }
        catch (Exception e) {
            print(e);
        }
    }

    // Update the text for the port which the client is listening on
    public void UpdateListeningPort() {
        portText.text = MultiplayerManager.ListenPort.ToString();
    }
    
    // Update the list of players in the lobby menu
    public void UpdatePlayerList(string peerList) {
        playerList.text = peerList;
    }

    #region Selectors 

    // Check if the character is already selected
    private bool CheckIfSpotInUse(int index) {
        foreach (PlayerSelector selector in selectorList.GetList()) {
            if (selector != selectorList.localInstance)
                if (selector.index == index && selector.locked)
                    return true;
        }
        return false;
    }

    // Spawn a selector
    private PlayerSelector SpawnPlayerSelector(byte id) {
        PlayerSelector selector = Instantiate(selectorPrefab, selectorParent.GetChild(0)).GetComponent<PlayerSelector>();

        selector.AssignId(id);
        selector.parent = selectorParent;

        selectorList?.Add(selector);
        return selector;
    }

    private void UpdateSelector(List<object> properties, float timeDiff) {
        PlayerSelector selector = GetSelectorFromID((byte)properties[0]);

        // If no selector is found spawn a new one
        if (selector == null)
            selector = SpawnPlayerSelector((byte)properties[0]);

        selector.SyncDataToObj(properties, timeDiff);
    }

    private PlayerSelector GetSelectorFromID(byte peerId) {
        return selectorList.GetObjWithId(peerId);
    }

    private void DeleteAllSelectors() {
        selectorList.ExecStatement(selector => Destroy(selector.gameObject));
        selectorList.Reset();
    }
    #endregion
}
