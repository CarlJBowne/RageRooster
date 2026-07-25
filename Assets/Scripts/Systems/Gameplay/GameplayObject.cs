using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;
using System.Collections.Generic;
using SLS.MenuCore;
using RageRooster.RoomSystem;
using RageRooster.Systems.SaveSystem;
using Utilities.ObjectPooling;
using RageRooster.Systems;
using Utilities;

#if UNITY_EDITOR
#endif

/// <summary>
/// A Global System managing the core gameplay systems and lifecycle. A singleton that persists as long as gameplay is running. <br/>
/// Provides static access to important gameplay-related properties and methods. <br/>
/// To begin gameplay, use methods such as <see cref="BeginSaveFile(int)"/> or <see cref="BeginEditor()"/>.
/// </summary>
[DefaultExecutionOrder(ExecutionOrders.Gameplay)]
public class GameplayObject : MonoBehaviour
{




    #region Instance Fields

    [SerializeField] Transform cameraTransform;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] UIHUDSystem uI;
    [SerializeField] SettingsMenu settingsMenu;
    [SerializeField] DontDestroyMeOnLoad overlayPrefab;
    [SerializeField] Player inputPlayer;
    [SerializeField] UIHUDSystem inputUI;
    [SerializeField] Cameras inputCams;
    [SerializeField] StudioEventEmitter musicEmitter;
    [SerializeField] StudioEventEmitter musicEmitter2;

    #endregion Instance Fields

    public void Awake()
    {
        Instantiate(overlayPrefab);
        DontDestroyOnLoad(gameObject);
        inputPlayer.Awake();
        inputUI.Awake();
        inputCams.Awake();
        GlobalPool.poolParent = transform.Find("PooledObjects");
        GlobalPool.Get.Initialize();
        Overlay.OverALL.Alpha = 1;
        Overlay.UnderHUD.ResetState();
        Overlay.BetweenUI.ResetState();
    }

    private void Update()
    {
        Gameplay.onUpdate?.Invoke();
    }

    private void OnDestroy()
    {
        Gameplay.onDestroy?.Invoke();
        //EnemyCullingGroup.DeInitialize();
    }

}
