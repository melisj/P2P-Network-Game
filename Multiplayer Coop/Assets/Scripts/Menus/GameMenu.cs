using P2P;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Handler for the menu ingame
/// </summary>
public class GameMenu : MonoBehaviour
{
    private EventSystem evtSystem;
    private Animator animator;
    [SerializeField] private TextMeshProUGUI pointText = null;
    private bool openMenu;

    private void Start() {
        animator = GetComponent<Animator>();
        evtSystem = FindObjectOfType<EventSystem>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleMenu();
        }   
    }

    // Show the current points of the local player
    public void UpdatePointText(int points) {
        pointText.text = points.ToString();
    }

    // Toggle the animation for the menu
    private void ToggleMenu() {
        openMenu = !openMenu;
        animator.SetBool("open", openMenu);
    }

    // Close the menu
    public void ResumeGame() {
        ToggleMenu();
        evtSystem.SetSelectedGameObject(null);
    }

    // Load the menu scene again
    public void BackToMenu() {
        MultiplayerManager.afterDisconnectEvent.AddListener(LoadMainMenu);
        MultiplayerManager.manager.LeaveGame(true);
    }

    // Load the menu callback
    public void LoadMainMenu() {
        SceneManager.LoadScene(0);
    }

    // Quit the application
    public void QuitGame() {
        MultiplayerManager.afterDisconnectEvent.AddListener(Application.Quit);
        MultiplayerManager.manager.LeaveGame(false);
    }
}
