using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MenuSingleton<MainMenu>
{
    private enum MenuState
    {
        Home,
        Credits
    }

    private MenuState currentState = MenuState.Home;
    private List<Button> menuButtons;
    public GameObject creditsPanel;
    public Button creditsPanelFirstButton;
    public RectTransform creditsContent;
    public DontDestroyMeOnLoad overlayPrefab;

    private int currentButtonIndex = 0;

    private void Start()
    {
        creditsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuButtons = new List<Button>(GetComponentsInChildren<Button>());
        if (menuButtons.Count > 0)
            EventSystem.current.SetSelectedGameObject(menuButtons[currentButtonIndex].gameObject);

        if (Overlay.ActiveOverlays.Count == 0) Instantiate(overlayPrefab);
    }

    private void Update()
    {
        HandleControllerInput();
    }

    private void HandleControllerInput()
    {
        switch (currentState)
        {
            case MenuState.Home:
                HandleHomeInput();
                break;
            case MenuState.Credits:
                HandleCreditsInput();
                break;
        }
    }

    private void HandleHomeInput()
    {
        if (Input.Movement.y > 0)
        {
            NavigateUp();
        }
        else if (Input.Movement.y < 0)
        {
            NavigateDown();
        }

        if (Input.Interact.triggered)
        {
            menuButtons[currentButtonIndex].onClick.Invoke();
        }
    }

    private void HandleCreditsInput()
    {
        // Handle input for credits panel if needed
    }

    private void NavigateUp()
    {
        currentButtonIndex = (currentButtonIndex - 1 + menuButtons.Count) % menuButtons.Count;
        EventSystem.current.SetSelectedGameObject(menuButtons[currentButtonIndex].gameObject);
    }

    private void NavigateDown()
    {
        currentButtonIndex = (currentButtonIndex + 1) % menuButtons.Count;
        EventSystem.current.SetSelectedGameObject(menuButtons[currentButtonIndex].gameObject);
    }

    public void PlayGameDebug()
    {
        Gameplay.BeginMainMenu(0);
        Close();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ShowCredits()
    {
        creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, 0);
        creditsPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(creditsPanelFirstButton.gameObject);
        SetMenuButtonsInteractable(false);
        currentState = MenuState.Credits;
    }

    public void HideCredits()
    {
        creditsPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(menuButtons[currentButtonIndex].gameObject);
        SetMenuButtonsInteractable(true);
        currentState = MenuState.Home;
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        foreach (var button in menuButtons)
        {
            button.interactable = interactable;
        }
    }
}
