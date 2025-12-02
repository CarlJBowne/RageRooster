using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TargetingManager : MonoBehaviour
{
    // Static runtime state
    public static TargetingManager Instance { get; private set; }
    private static TargetingChannel<InteractionTarget> InteractionChannel;
    private static TargetingChannel<MeleeTarget> MeleeChannel;
    private static TargetingChannel<RangedTarget> RangedChannel;

    public static GameObject InteractionPopup {  get; private set; }

    #region Instance Fields

    [SerializeField] private TargetingRange rangedHipFireRange = new();
    [SerializeField] private TargetingRange rangedAimingRange = new(); 
    [SerializeField] private TargetingRange grabbingRange = new(); 
    [SerializeField] private TargetingRange interactionRange = new();
    [SerializeField] private GameObject interactionPopup;

    #endregion

    // Public API: manage active targets
    public static void AddActiveTarget(Target target)
    {
        if (target is MeleeTarget meleeTarget)
        {
            if (!MeleeChannel.ALLTARGETS.Contains(meleeTarget)) 
                MeleeChannel.ALLTARGETS.Add(meleeTarget);
        }
        else if (target is RangedTarget rangedTarget)
        {
            if (!RangedChannel.ALLTARGETS.Contains(rangedTarget))
                RangedChannel.ALLTARGETS.Add(rangedTarget);
        }
        else if (target is InteractionTarget interactionTarget)
        {
            if (!InteractionChannel.ALLTARGETS.Contains(interactionTarget))
                InteractionChannel.ALLTARGETS.Add(interactionTarget);
        }
    }

    public static void RemoveActiveTarget(Target target)
    {
        if (target is MeleeTarget meleeTarget)
        {
            if (MeleeChannel.ALLTARGETS.Contains(meleeTarget))
                MeleeChannel.ALLTARGETS.Remove(meleeTarget);
        }
        else if (target is RangedTarget rangedTarget)
        {
            if (RangedChannel.ALLTARGETS.Contains(rangedTarget))
                RangedChannel.ALLTARGETS.Remove(rangedTarget);
        }
        else if (target is InteractionTarget interactionTarget)
        {
            if (InteractionChannel.ALLTARGETS.Contains(interactionTarget))
                InteractionChannel.ALLTARGETS.Remove(interactionTarget);
        }
    }

    public static void ToggleAimingDownSights(bool value) => RangedChannel.ChangeRange(value ? Instance.rangedAimingRange : Instance.rangedHipFireRange);



    // Unity lifecycle
    private void Awake()
    {
        Instance = this;

        InteractionChannel = new(interactionRange);
        MeleeChannel = new(grabbingRange);
        RangedChannel = new(rangedHipFireRange);

        InteractionPopup = interactionPopup;
    }


    private void FixedUpdate()
    {
        InteractionChannel.CalculateTargets();
        MeleeChannel.CalculateTargets();
        RangedChannel.CalculateTargets();
    }

    public class TargetingChannel<T> where T : Target
    {
        public List<T> ALLTARGETS { get; private set; }
        public T CurrentTarget { get; private set; }
        public TargetingRange Range { get; private set; }

        public TargetingChannel(TargetingRange range)
        {
            ALLTARGETS = new();
            CurrentTarget = null;
            Range = range;
        }

        public void CalculateTargets()
        {
            int chosenIndex = -1;
            float closestScore = 5f; //Max possible score is 2 (1 distance + 1 angle)

            for (int i = 0; i < ALLTARGETS.Count; i++)
            {
                if (ALLTARGETS[i] == null || ALLTARGETS[i].enabled == false) continue;

                float distance = ALLTARGETS[i].GetDistance(Range);
                float angle = ALLTARGETS[i].GetAngle(Range);

                if (distance > Range.maxDistance || angle > Range.maxAngle)
                {
                    ALLTARGETS[i].TargetState = Target.States.OutOfRange;
                    continue;
                }
                ALLTARGETS[i].TargetState = Target.States.WithinRange;

                float distanceScore = distance / Range.maxDistance;
                float angleScore = angle / Range.maxAngle;
                float finalScore = (distanceScore * Range.distanceAngleWeighting) + (angleScore * (1f - Range.distanceAngleWeighting));

                if (finalScore < closestScore)
                {
                    closestScore = finalScore;
                    chosenIndex = i;
                }
            }
            var ChosenTarget = chosenIndex != -1 ? ALLTARGETS[chosenIndex] : null;

            if(ChosenTarget != CurrentTarget)
            {
                var prevTarget = CurrentTarget;
                CurrentTarget = ChosenTarget;
                if (prevTarget) prevTarget.TargetState = Target.States.WithinRange;
                if (CurrentTarget) CurrentTarget.TargetState = Target.States.Targeted;
                if (prevTarget) prevTarget.OnDeTargeted(CurrentTarget);
                if (CurrentTarget) CurrentTarget.OnTargeted(prevTarget);
            }

        }

        public void ChangeRange(TargetingRange newRange) => Range = newRange;






#if UNITY_EDITOR
        public VisualElement GetEditor()
        {
            // Pseudocode / Plan:
            // 1. Create a root VisualElement and a header label showing the channel type/name.
            // 2. If the editor is not in Play mode, hide the whole element (DisplayStyle.None) and return.
            // 3. Create a vertical list container to hold one row per entry in ALLTARGETS.
            // 4. Create a refresh routine that:
            //    - Clears the list.
            //    - Iterates ALLTARGETS, skipping null/disabled targets.
            //    - For each target, create a horizontal row with:
            //        a) An arrow label ("→") visible only for CurrentTarget.
            //        b) A main label with target.name - bold when WithinRange or Targeted.
            //        c) A small info label showing distance (rounded) and angle.
            //    - Row styling: greyed-out if OutOfRange or disabled, bold & colored for Targeted.
            //    - Register a click/ping handler on the row to ping the object in the editor.
            // 5. Register an EditorApplication.update callback to call refresh each editor tick while playing.
            // 6. Unregister the update callback when the returned root is detached from the panel.
            //
            // Note: Keep UIElements usage simple to avoid heavy allocations. This is intended for editor-only diagnostic view.

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.FlexStart;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;
            root.style.paddingTop = 2;
            root.style.paddingBottom = 2;

            // Header
            var header = new Label($"{typeof(T).Name} Channel");
            header.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            header.style.marginBottom = 4;
            root.Add(header);

            // If not playing, hide entirely (per requirement)
            if (!EditorApplication.isPlaying)
            {
                root.style.display = DisplayStyle.None;
                // When not playing, still return an element so callers won't NRE.
                return root;
            }

            // Container for list of targets
            var list = new VisualElement();
            list.style.flexDirection = FlexDirection.Column;
            list.style.alignItems = Align.FlexStart;
            list.style.width = Length.Percent(100);
            root.Add(list);

            // Small helper to format float nicely
            static string F(float v) => float.IsNaN(v) ? "-" : v.ToString("0.00");

            // Refresh callback to rebuild the list
            void Refresh()
            {
                // Defensive: if root was destroyed, skip
                if (root.panel == null) return;

                list.Clear();

                if (ALLTARGETS == null || ALLTARGETS.Count == 0)
                {
                    var empty = new Label("No active targets");
                    empty.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic);
                    empty.style.color = new StyleColor(new Color(.6f, .6f, .6f));
                    list.Add(empty);
                    return;
                }

                for (int i = 0; i < ALLTARGETS.Count; i++)
                {
                    var t = ALLTARGETS[i];
                    if (t == null) continue;

                    // Row
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.justifyContent = Justify.SpaceBetween;
                    row.style.marginBottom = 2;
                    row.style.width = Length.Percent(100);

                    // Left group (arrow + name)
                    var left = new VisualElement();
                    left.style.flexDirection = FlexDirection.Row;
                    left.style.alignItems = Align.Center;
                    left.style.flexGrow = 1;
                    left.style.minWidth = 0;

                    var arrow = new Label(CurrentTarget == t ? "→" : string.Empty);
                    arrow.style.width = 18;
                    arrow.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                    arrow.style.marginRight = 6;
                    left.Add(arrow);

                    var name = new Label(t.name);
                    // Truncate behavior if inspector is narrow
                    name.style.unityTextAlign = TextAnchor.MiddleLeft;
                    name.style.flexShrink = 1;
                    name.style.minWidth = 0;

                    // Styling based on state
                    if (!t.enabled)
                    {
                        name.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                    }
                    else
                    {
                        switch (t.TargetState)
                        {
                            case Target.States.Targeted:
                                name.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                                name.style.color = new StyleColor(new Color(0.1f, 0.8f, 0.1f));
                                break;
                            case Target.States.WithinRange:
                                name.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                                name.style.color = new StyleColor(new Color(1f, 1f, 1f));
                                break;
                            case Target.States.OutOfRange:
                            default:
                                name.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
                                break;
                        }
                    }

                    left.Add(name);

                    // Right info group (distance / angle)
                    var right = new VisualElement();
                    right.style.flexDirection = FlexDirection.Row;
                    right.style.alignItems = Align.Center;
                    //right.style.gap = 6;

                    float dist = 0f;
                    float ang = 0f;
                    try
                    {
                        dist = t.GetDistance(Range);
                        ang = t.GetAngle(Range);
                    }
                    catch
                    {
                        // ignore any runtime errors when querying target
                    }

                    var info = new Label($"{F(dist)}m • {F(ang)}°");
                    info.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
                    info.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Normal);
                    right.Add(info);

                    row.Add(left);
                    row.Add(right);

                    // Click to ping object in editor and select in hierarchy
                    row.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (t != null)
                        {
                            EditorGUIUtility.PingObject(t.gameObject);
                            Selection.activeGameObject = t.gameObject;
                        }
                    });

                    // Tooltip with full state
                    row.tooltip = $"State: {t.TargetState}\nDistance: {F(dist)}m\nAngle: {F(ang)}°";

                    list.Add(row);
                }
            }

            // Initial populate
            Refresh();

            // Register an editor update callback to refresh while playing
            EditorApplication.CallbackFunction updateCallback = null;
            updateCallback = () =>
            {
                // Only refresh when playing to respect requirement
                if (!EditorApplication.isPlaying)
                {
                    // If play stopped, hide the root and unregister
                    root.style.display = DisplayStyle.None;
                    EditorApplication.update -= updateCallback;
                    return;
                }

                Refresh();
            };

            EditorApplication.update += updateCallback;

            // Ensure we unregister when the UI element is detached/destroyed
            root.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                EditorApplication.update -= updateCallback;
            });

            return root;
        }
#endif
    }

    // Main selection logic


    public void AttemptInteract()
    {
        InteractionTarget target = InteractionChannel.CurrentTarget;
        if (target != null && target.enabled)
        {
            target.OnInteract?.Invoke();
            InteractionPopup.gameObject.SetActive(false);
        }
    }

    public static MeleeTarget GetMeleeTarget() => MeleeChannel.CurrentTarget;

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
