using P2P;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handler for all the main menu's
/// Handles menu changes
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Text portText = null;

    public GameObject[] menus = null; 
    public GameObject currentMenu = null; 

    // Switch the menu if it is a different menu
    public void SwitchMenu(int toMenu) {
        if (currentMenu != menus[toMenu]) {
            currentMenu?.SetActive(false);
            currentMenu = menus[toMenu];
            currentMenu?.SetActive(true);
        }
    }

    // Load the join menu
    public void BackToJoinMenu() {
        SwitchMenu(1);
    }

    // Load the new game
    public void Startgame() {
        int.TryParse(portText.text, out int port);
        MultiplayerManager.manager.StartListening((ushort)port);
        SwitchMenu(1);
    }

    // Stop application
    public void ExitGame() {
        Application.Quit();
    }
}
