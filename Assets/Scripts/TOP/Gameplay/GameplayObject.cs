using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;
using System.Collections.Generic;
using SLS.MenuCore;
using RageRooster.World;
using RageRooster.SaveSystem;
using SLS.ObjectUtilities;
using RageRooster.Systems;
using Utilities;

#if UNITY_EDITOR
#endif

/// <summary>
/// A Global System managing the core gameplay systems and lifecycle. A singleton that persists as long as gameplay is running. <br/>
/// Provides static access to important gameplay-related properties and methods. <br/>
/// To begin gameplay, use methods such as <see cref="BeginSaveFile(int)"/> or <see cref="BeginEditor()"/>.
/// </summary>
[DefaultExecutionOrder(ExecutionOrders.Gameplay), Obsolete]
public class GameplayObject : MonoBehaviour
{

}
