using P2P;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handlers for sending out join signals and hosting a lobby
/// </summary>
public class JoinMenu : MonoBehaviour
{
    [SerializeField] private Text toPortField = null;
    [SerializeField] private Text IPField = null;
    [SerializeField] private Text NameField = null;

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

    // Try hosting a lobby
    public void HostLobby() {
        try {
            string name = GetName();
            if (name != null)
                MultiplayerManager.manager.TryHostLobby(name);

        }
        catch (Exception e) {
            print(e);
        }
    }

    // Try joining a lobby
    public void JoinLobby() {
        try {
            string name = GetName();
            if (name != null)
                MultiplayerManager.manager.TryJoinGame(IPField.text, ushort.Parse(toPortField.text), name);
        }
        catch (Exception e) {
            print(e);
        }
    }

    // Load the lobby menu
    public void JoinLobbySuccesfully() {
        Menu.SwitchMenu(2);
    }

    // Get the name from the field 
    private string GetName() {
        string name = NameField.text;
        if (name.Length <= 32 && name.Length > 0) {
            return name;
        } else {
            if (name.Length > 32)
                Debug.LogError("Name too long! 32 limit");
            else
                Debug.LogError("No name entered");
        }
        return null;
    }
}
