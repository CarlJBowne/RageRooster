using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UIImage = UnityEngine.UI.Image;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class TargetingManager : MonoBehaviour
{
    // Static runtime state
    public static TargetingManager Instance { get; private set; }

    public static TargetingChannel MeleeChannel { get; private set; } = new();
    public static TargetingChannel RangedChannel { get; private set; } = new();
    public static TargetingChannel InteractionChannel { get; private set; } = new();

    public static bool ShowDebugReticles = false;

    #region Instance Fields

    [SerializeField] private TargetingRange rangedHipFireRange = new();
    [SerializeField] private TargetingRange rangedAimingRange = new();
    [SerializeField] private TargetingRange grabbingRange = new();
    [SerializeField] private TargetingRange interactionRange = new();
    [SerializeField] private GameObject interactionPopup;
    [SerializeField] private UIImage rangedDebugReticle;
    [SerializeField] private UIImage meleeDebugReticle;
    [SerializeField] private UIImage interactionDebugReticle;

    #endregion

    public static void ToggleAimingDownSights(bool value) => RangedChannel.ChangeRange(value ? Instance.rangedAimingRange : Instance.rangedHipFireRange);



    // Unity lifecycle
    private void Awake()
    {
        Instance = this;

        MeleeChannel.Initialize(grabbingRange, meleeDebugReticle);
        RangedChannel.Initialize(rangedHipFireRange, rangedDebugReticle);
        InteractionChannel.Initialize(interactionRange, interactionDebugReticle);

        TargetType.Interactable.InteractionPopup = interactionPopup;

        Input.Debug.ToggleTextOverlay.performed += _ =>
        {
            ShowDebugReticles = !ShowDebugReticles;
            if (!ShowDebugReticles)
            {
                interactionDebugReticle.gameObject.SetActive(false);
                meleeDebugReticle.gameObject.SetActive(false);
                rangedDebugReticle.gameObject.SetActive(false);
            }
        };
    }


    private void FixedUpdate()
    {
        InteractionChannel.CalculateTargets();
        MeleeChannel.CalculateTargets();
        RangedChannel.CalculateTargets();

        if (ShowDebugReticles)
        {
            if (InteractionChannel.CurrentTarget != null)
                interactionDebugReticle.rectTransform.position = Cameras.RealCamera.camera.WorldToScreenPoint(InteractionChannel.CurrentTarget.position);
            if (MeleeChannel.CurrentTarget != null)
                meleeDebugReticle.rectTransform.position = Cameras.RealCamera.camera.WorldToScreenPoint(MeleeChannel.CurrentTarget.position);
            if (RangedChannel.CurrentTarget != null)
                rangedDebugReticle.rectTransform.position = Cameras.RealCamera.camera.WorldToScreenPoint(RangedChannel.CurrentTarget.position);
        }
    }


    // Main selection logic


    public void AttemptInteract()
    {
        TargetType.Interactable target = InteractionChannel.CurrentTargetType<TargetType.Interactable>();
        if (target)
        {
            target.OnInteract?.Invoke();
            TargetType.Interactable.InteractionPopup.SetActive(false);
        }
    }

    public static Target GetMeleeTarget() => MeleeChannel.CurrentTarget;

    /*
    #if UNITY_EDITOR
        [CustomEditor(typeof(TargetingManager))]
        public class Editor : UnityEditor.Editor
        { 

            public override VisualElement CreateInspectorGUI()
            {
                var baseInspector = base.CreateInspectorGUI();
                if (baseInspector == null) baseInspector = new();
                if (Application.isPlaying)
                {
                    baseInspector.Add(MeleeChannel.GetEditor());
                    baseInspector.Add(RangedChannel.GetEditor());
                    baseInspector.Add(InteractionChannel.GetEditor());
                }
                return baseInspector;
            }
        }
    #endif
    */
}

public class TargetingChannel
{
    public readonly List<TargetType> AllTargets = new();
    private TargetType currentTarget;
    public TargetingRange Range { get; private set; }

    public UIImage debugDisplayReticle;

    private bool init = false;


    public void Initialize(TargetingRange range, UIImage debugDisplayReticle)
    {
        if (init) return;
        Range = range;
        this.debugDisplayReticle = debugDisplayReticle;
        init = true;
    }

    public void CalculateTargets()
    {
        if (!init) return;
        int chosenIndex = -1;
        float closestScore = 5f; //Max possible score is 2 (1 distance + 1 angle)

        for (int i = 0; i < AllTargets.Count; i++)
        {
            if (AllTargets[i] == null || AllTargets[i].Target == null || AllTargets[i].Enabled == false) continue;

            float distance = Vector3.Distance(Range.front.position, AllTargets[i].Target.position);
            float angle = Vector3.Angle(Range.front.forward, AllTargets[i].Target.position - Range.front.position);

            if (distance > Range.maxDistance || angle > Range.maxAngle)
            {
                AllTargets[i].TargetState = TargetState.OutOfRange;
                continue;
            }
            AllTargets[i].TargetState = TargetState.WithinRange;

            float distanceScore = distance / Range.maxDistance;
            float angleScore = angle / Range.maxAngle;
            float finalScore = (distanceScore * Range.distanceAngleWeighting) + (angleScore * (1f - Range.distanceAngleWeighting));

            if (finalScore < closestScore)
            {
                closestScore = finalScore;
                chosenIndex = i;
            }
        }
        var ChosenTarget = chosenIndex != -1 ? AllTargets[chosenIndex] : null;

        if (ChosenTarget != currentTarget)
        {
            var prevTarget = currentTarget;
            currentTarget = ChosenTarget;
            if (prevTarget)
            {
                prevTarget.TargetState = TargetState.WithinRange;
                prevTarget.OnDeTargeted(currentTarget);
                prevTarget.Target.UpdateTargetedState();
            }
            if (currentTarget)
            {
                currentTarget.TargetState = TargetState.Targeted;
                currentTarget.OnTargeted(prevTarget);
                currentTarget.Target.UpdateTargetedState();
            }
                

            if (TargetingManager.ShowDebugReticles && debugDisplayReticle != null)
            {
                if (currentTarget != null)
                {
                    debugDisplayReticle.gameObject.SetActive(true);
                    debugDisplayReticle.rectTransform.position = Cameras.RealCamera.camera.WorldToScreenPoint(currentTarget.Target.position);
                }
                else
                {
                    debugDisplayReticle.gameObject.SetActive(false);
                }
            }
        }

    }

    public void ChangeRange(TargetingRange newRange) => Range = newRange;

    public Target CurrentTarget => currentTarget?.Target;
    public TargetType CurrentTargetTypeBase => currentTarget;

    public T CurrentTargetType<T>() where T : TargetType => currentTarget as T;


}

