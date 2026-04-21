using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrders.GameplaySystems)]
public class Cameras : MonoBehaviour
{
    private static Cameras instance;

    #region Instance Fields

    [SerializeField] CinemachineBrain inputBrain;
    [SerializeField] CinemachineFreeLook inputNormalCamera;
    [SerializeField] CinemachineVirtualCamera inputAimingCamera;
    [SerializeField] CinemachineVirtualCameraBase inputDialogueCamera;
    [SerializeField] CinemachineVirtualCameraBase inputCutsceneCamera;

    #endregion Instance Fields

    public void Awake()
    {
        if (instance != null)
        {
            if (instance != this) Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this);

        RealCamera.brain = inputBrain;
        RealCamera.camera = inputBrain.GetComponent<Camera>();
        RealCamera.transform = inputBrain.transform;

        normalCamera = inputNormalCamera;
        aimingCamera = inputAimingCamera;
        dialogueCamera = inputDialogueCamera;
        cutsceneCamera = inputCutsceneCamera;

        currentVirtualCamera = normalCamera;
        normalCamera.Priority = 10;
        aimingCamera.Priority = 0;
        aimingCamera.gameObject.SetActive(false);
        dialogueCamera.Priority = 0;
        dialogueCamera.gameObject.SetActive(false);
    }

    public static class RealCamera
    {
        public static Camera camera;
        public static Transform transform;
        public static CinemachineBrain brain;
    }

    public static CinemachineVirtualCameraBase currentVirtualCamera;
    public static CinemachineFreeLook normalCamera;
    public static CinemachineVirtualCamera aimingCamera;
    //public static Transform aimCameraReflection;
    public static CinemachineVirtualCameraBase dialogueCamera;
    public static CinemachineVirtualCameraBase cutsceneCamera;


    public static void SetTargetVirtualCamera(CinemachineVirtualCameraBase newTarget)
    {
        currentVirtualCamera.Priority = 0;
        currentVirtualCamera.gameObject.SetActive(false);
        currentVirtualCamera = newTarget;
        currentVirtualCamera.Priority = 10;
        currentVirtualCamera.gameObject.SetActive(true);
    }

    public static void LockPrimary(bool lockPosition = true, bool lockRotation = false)
    {
        normalCamera.m_Follow = lockPosition ? null : Player.Transform;
        normalCamera.m_LookAt = lockRotation ? null : Player.Transform;
    }

}
