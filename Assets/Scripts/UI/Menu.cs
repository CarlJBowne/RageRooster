using EditorAttributes;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// A Base Menu class intended to form an easy-to-use extensible Menu system. (Admittedly not up to snuff.)
/// </summary>
public class Menu : MonoBehaviour
{
    //Config
    [DisableInPlayMode] public bool isActive;
    [DisableInPlayMode] public Menu parent;
    [SerializeField] public Button defaultSelection;
    [SerializeField] public Selectable[] allButtons;
    [SerializeField] private string dictionaryName;
    [SerializeField] private bool closeOverride;
    [SerializeField, ShowField(nameof(closeOverride))] private UnityEvent closeEvent;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;
    //Data
    public bool isCurrent => Manager.currentMenu == this;
    public bool isSubMenu => parent != null;

    /// <summary>
    /// Called when the script instance is being loaded
    /// </summary>
    protected virtual void Awake()
    {
        if (isActive) Manager.Open(this, true);
        else
        {
            gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(dictionaryName)) Manager.menuDictionary.Add(dictionaryName, this);
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (!string.IsNullOrEmpty(dictionaryName)) Manager.menuDictionary.Remove(dictionaryName);
        isActive = false;
        Manager.Close(this);
    }

    /// <summary>
    /// Opens the menu
    /// </summary>
    public void Open() => Manager.Open(this);

    /// <summary>
    /// Closes the menu (Invokes Override if present.)
    /// </summary>
    public void Close()
    {
        if (!closeOverride) Manager.Close(this);
        else closeEvent?.Invoke();
    }

    /// <summary>
    /// Closes the menu (Closes even if Override is present.)
    /// </summary>
    public void TrueClose() => Manager.Close(this);

    /// <summary>
    /// Enable all of the buttons in a menu. Buttons listed in allButtons.
    /// </summary>
    public void EnableButtons()
    {
        for (int i = 0; i < allButtons.Length; i++)
            if(allButtons[i] != null) allButtons[i].interactable = true;
    }

    /// <summary>
    /// Disables all of the buttons in a menu. Buttons listed in allButtons.
    /// </summary>
    public void DisableButtons()
    {
        for (int i = 0; i < allButtons.Length; i++)
            if (allButtons[i] != null) allButtons[i].interactable = false;
    }

    /// <summary>
    /// Called when the menu is opened
    /// </summary>
    protected virtual void OnOpen()
    {
         if (!openSound.IsNull)
             AudioManager.Get().PlayOneShot(openSound, transform.position);
    }

    /// <summary>
    /// Called when the menu is closed
    /// </summary>
    protected virtual void OnClose()
    {
        if (!closeSound.IsNull)
            AudioManager.Get().PlayOneShot(closeSound, transform.position);
    }

    /// <summary>
    /// The Global Manager in charge of Menus.
    /// </summary>
    public static class Manager
    {
        public static void Initialize()
        {
            Input.Get().Asset.FindAction("Navigate").performed += FocusController;
        }

        public static Menu currentMenu => currentMenus[^1];
        public static List<Menu> currentMenus = new();
        public static Dictionary<string, Menu> menuDictionary = new();
        public static bool disableEscape;
        //public static EventSystem uiEventSystem;

        /// <summary>
        /// Opens the specified menu
        /// </summary>
        /// <param name="menu">The Menu to be opened.</param>
        public static void Open(Menu menu, bool overrideRedundancyCheck = false)
        {
            if (menu.isActive && !overrideRedundancyCheck) return;

            currentMenus.Add(menu);

            menu.isActive = true;
            menu.gameObject.SetActive(true);
            menu.EnableButtons();
            if (currentMenus.Count > 1 && currentMenus[^2])
            {
                currentMenus[^2].DisableButtons();
            }
            menu.defaultSelection.Select();
            menu.OnOpen();
        }

        /// <summary>
        /// Closes the specified menu
        /// </summary>
        /// <param name="menu">The Menu to be closed.</param>
        public static void Close(Menu menu)
        {
            if (!menu.isActive) return;

            currentMenus.Remove(menu);

            menu.gameObject.SetActive(false);
            menu.isActive = false;
            if (currentMenus.Count > 0 && currentMenus[^1] != null)
            {
                currentMenus[^1].EnableButtons();
                currentMenus[^1].defaultSelection.Select();
            }
            menu.OnClose();
        }

        /// <summary>
        /// Handles the escape action (Bound to Escape / Start Button by Default.)
        /// </summary>
        public static void Escape()
        {
            if (PauseMenu.Loaded && !PauseMenu.Active && PauseMenu.canPause)
                PauseMenu.Get().Open();
            else if (currentMenus.Count > 0)
                currentMenus[^1].Close();
        }

        public static void FocusController(InputAction.CallbackContext ctx)
        {
            if (currentMenus.Count > 0 && EventSystem.current.currentSelectedGameObject == null)
                currentMenus[^1].defaultSelection.Select();
        } 

        public static void CloseAllMenus()
        {
            for (int i = currentMenus.Count - 1; i >= 0; i--) Close(currentMenus[i]);
        }
    }
}
