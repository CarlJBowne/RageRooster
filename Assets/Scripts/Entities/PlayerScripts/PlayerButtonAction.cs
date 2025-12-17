using System;
using System.Collections;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract class PlayerButtonAction
{
    protected virtual void Begin()
    {
        PlayerController.CurrentPlayerButtonAction = this;
        StartRoutine();
    }
    protected virtual void Finish()
    {
        PlayerController.CurrentPlayerButtonAction = null;
        StopRoutine();
    }
    public abstract void Press();
    public abstract void Release();
    protected abstract IEnumerator HoldRoutine();
    protected CoroutinePlus coroutine;

    protected void StartRoutine() => CoroutinePlus.Begin(ref coroutine, HoldRoutine(), Player.Controller);
    protected void StopRoutine()
    {
        if (coroutine)
        {
            coroutine.StopAuto();
            coroutine = null;
        }
    }

    [System.Serializable]
    public class BasicPush : PlayerButtonAction
    {
        public UltEvent pressEvent;
        public UltEvent releaseEvent;
        public override void Press()
        {
            Begin();
            pressEvent?.Invoke();
        }
        public override void Release()
        {
            releaseEvent?.Invoke();
            Finish();
        }
        protected override IEnumerator HoldRoutine()
        {yield return null;}

#if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(BasicPush))]
        public class Editor : BaseEditor
        {
            public override VisualElement TypeDisplay(SerializedProperty property) 
            {
                var container = new VisualElement();
                var pressLabel = new Label("Press Event:");
                container.Add(pressLabel);
                var pressField = new PropertyField(property.FindPropertyRelative("pressEvent"));
                container.Add(pressField);
                var releaseLabel = new Label("Release Event:");
                container.Add(releaseLabel);
                var releaseField = new PropertyField(property.FindPropertyRelative("releaseEvent"));
                container.Add(releaseField);
                return container;
            }
        }
#endif
    }
    [System.Serializable]
    public class TapOrHold : PlayerButtonAction
    {
        public UltEvent pressInstantEvent;
        public UltEvent releaseInstantEvent;
        public UltEvent tapEvent;
        public float holdTime = 0.3f;
        public UltEvent holdEvent;
        public bool autoFinishHold = true;
        private bool pastHold = false;
        public override void Press()
        {
            Begin();
            pressInstantEvent?.Invoke();
            pastHold = false;
        }
        public override void Release()
        {
            releaseInstantEvent?.Invoke();
            if(pastHold) holdEvent?.Invoke();
            else tapEvent?.Invoke();
            Finish();
        }

        protected override IEnumerator HoldRoutine()
        {
            yield return new WaitForSeconds(holdTime);
            pastHold = true;
            if (autoFinishHold) Release();
        }

#if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(TapOrHold))]
        public class Editor : BaseEditor
        {
            public override VisualElement TypeDisplay(SerializedProperty property)
            {
                var container = new VisualElement();
                var pressInstantLabel = new Label("Press Instant Event:");
                container.Add(pressInstantLabel);
                var pressInstantField = new PropertyField(property.FindPropertyRelative("pressInstantEvent"));
                container.Add(pressInstantField);
                var releaseInstantLabel = new Label("Release Instant Event:");
                container.Add(releaseInstantLabel);
                var releaseInstantField = new PropertyField(property.FindPropertyRelative("releaseInstantEvent"));
                container.Add(releaseInstantField);
                var tapLabel = new Label("Tap Event:");
                container.Add(tapLabel);
                var tapField = new PropertyField(property.FindPropertyRelative("tapEvent"));
                container.Add(tapField);
                var holdTimeLabel = new Label("Hold Time:");
                container.Add(holdTimeLabel);
                var holdTimeField = new PropertyField(property.FindPropertyRelative("holdTime"));
                container.Add(holdTimeField);
                var holdEventLabel = new Label("Hold Event:");
                container.Add(holdEventLabel);
                var holdEventField = new PropertyField(property.FindPropertyRelative("holdEvent"));
                container.Add(holdEventField);
                var autoFinishHoldLabel = new Label("Auto Finish Hold:");
                container.Add(autoFinishHoldLabel);
                var autoFinishHoldField = new PropertyField(property.FindPropertyRelative("autoFinishHold"));
                container.Add(autoFinishHoldField);
                return container;
            }
        }
#endif
    }
    [System.Serializable]
    public class TapHoldOrLongHold : PlayerButtonAction
    {
        // Fields matching original PlayerButtonActions
        public UltEvent pressEvent;
        public UltEvent releaseEvent;
        public UltEvent tapEvent;
        public float holdTime = 0.3f;
        public UltEvent holdEvent;
        public UltEvent holdReleaseEvent;
        public float longHoldTime = 1.2f;
        public UltEvent longHoldEvent;
        public bool autoFinishLongHold = true;

        // Delegate to be invoked on release to produce correct behavior (tap / hold-release / long-hold)
        private Action releaseResult = null;

        public override void Press()
        {
            // If any immediate events are configured, preserve event-driven behavior and do not lock-in.
            if (releaseEvent != null
                || tapEvent != null
                || holdEvent != null
                || holdReleaseEvent != null
                || longHoldEvent != null
                )
            {
                // still invoke the press event immediately
                pressEvent?.Invoke();
                return;
            }

            // Otherwise begin locked-in hold routine.
            Begin();
            pressEvent?.Invoke();
        }

        public override void Release()
        {
            releaseEvent?.Invoke();
            releaseResult?.Invoke();
            Finish();
        }

        protected override IEnumerator HoldRoutine()
        {
            float time = 0f;

            // Default release action is a tap unless overwritten by a hold or long-hold.
            releaseResult = () => tapEvent?.Invoke();

            // Handle normal hold threshold.
            if (holdEvent != null || holdReleaseEvent != null)
            {
                while (time < holdTime)
                {
                    time += Time.deltaTime;
                    yield return null;
                }

                holdEvent?.Invoke();
                releaseResult = () => holdReleaseEvent?.Invoke();
            }

            // Handle long-hold threshold.
            if (longHoldEvent != null)
            {
                while (time < longHoldTime)
                {
                    time += Time.deltaTime;
                    yield return null;
                }

                releaseResult = () => longHoldEvent?.Invoke();
                if (autoFinishLongHold) Release();
            }
        }

#if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(TapHoldOrLongHold))]
        public class Editor : BaseEditor
        {
            public override VisualElement TypeDisplay(SerializedProperty property)
            {
                var container = new VisualElement();
                var pressLabel = new Label("Press Event:");
                container.Add(pressLabel);
                var pressField = new PropertyField(property.FindPropertyRelative("pressEvent"));
                container.Add(pressField);
                var releaseLabel = new Label("Release Event:");
                container.Add(releaseLabel);
                var releaseField = new PropertyField(property.FindPropertyRelative("releaseEvent"));
                container.Add(releaseField);
                var tapLabel = new Label("Tap Event:");
                container.Add(tapLabel);
                var tapField = new PropertyField(property.FindPropertyRelative("tapEvent"));
                container.Add(tapField);
                var holdTimeLabel = new Label("Hold Time:");
                container.Add(holdTimeLabel);
                var holdTimeField = new PropertyField(property.FindPropertyRelative("holdTime"));
                container.Add(holdTimeField);
                var holdEventLabel = new Label("Hold Event:");
                container.Add(holdEventLabel);
                var holdEventField = new PropertyField(property.FindPropertyRelative("holdEvent"));
                container.Add(holdEventField);
                var holdReleaseLabel = new Label("Hold Release Event:");
                container.Add(holdReleaseLabel);
                var holdReleaseField = new PropertyField(property.FindPropertyRelative("holdReleaseEvent"));
                container.Add(holdReleaseField);
                var longHoldTimeLabel = new Label("Long Hold Time:");
                container.Add(longHoldTimeLabel);
                var longHoldTimeField = new PropertyField(property.FindPropertyRelative("longHoldTime"));
                container.Add(longHoldTimeField);
                var longHoldEventLabel = new Label("Long Hold Event:");
                container.Add(longHoldEventLabel);
                var longHoldEventField = new PropertyField(property.FindPropertyRelative("longHoldEvent"));
                container.Add(longHoldEventField);
                var autoFinishLongHoldLabel = new Label("Auto Finish Long Hold:");
                container.Add(autoFinishLongHoldLabel);
                var autoFinishLongHoldField = new PropertyField(property.FindPropertyRelative("autoFinishLongHold"));
                container.Add(autoFinishLongHoldField);
                return container;
            }
        }
#endif
    }
    [System.Serializable]
    public class TargetDependant : PlayerButtonAction
    {
        [SerializeReference] public PlayerButtonAction hasMeleeTarget;
        [SerializeReference] public PlayerButtonAction hasRangedTarget;
        [SerializeReference] public PlayerButtonAction noTarget;

        public PlayerButtonAction Choose() =>
            TargetingManager.MeleeChannel.CurrentTarget ? hasMeleeTarget
            : TargetingManager.RangedChannel.CurrentTarget ? hasRangedTarget
            : noTarget;

        protected override void Begin() => PlayerController.CurrentPlayerButtonAction = Choose();

        public override void Press() => Choose().Press();
        public override void Release() => Choose().Release();
        protected override IEnumerator HoldRoutine() => Choose().HoldRoutine();


#if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(TargetDependant))]
        public class Editor : BaseEditor
        {
            private int selectedType = 2;

            public override VisualElement TypeDisplay(SerializedProperty property)
            {
                var container = new VisualElement();

                // Button row
                var buttonRow = new VisualElement();
                buttonRow.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
                buttonRow.style.marginBottom = 4;

                // Content area that will be swapped
                var content = new VisualElement();

                // Helper: update content area based on selectedType
                void UpdateContent()
                {
                    content.Clear();

                    SerializedProperty prop;
                    switch (selectedType)
                    {
                        case 0:
                            prop = property.FindProperty("hasMeleeTarget");
                            break;
                        case 1:
                            prop = property.FindProperty("hasRangedTarget");
                            break;
                        default:
                            prop = property.FindProperty("noTarget");
                            break;
                    }

                    var field = new PropertyField(prop);
                    content.Add(field);
                }

                // Create buttons and wire up click handlers
                var meleeBtn = new Button(() =>
                {
                    selectedType = 0;
                    UpdateContent();
                })
                { text = "Melee" };

                var rangedBtn = new Button(() =>
                {
                    selectedType = 1;
                    UpdateContent();
                })
                { text = "Ranged" };

                var noneBtn = new Button(() =>
                {
                    selectedType = 2;
                    UpdateContent();
                })
                { text = "No property.managedReferenceValue" };

                // Optional: simple visual cue for selected button (update text)
                void RefreshButtonLabels()
                {
                    meleeBtn.text = selectedType == 0 ? "[Melee]" : "Melee";
                    rangedBtn.text = selectedType == 1 ? "[Ranged]" : "Ranged";
                    noneBtn.text = selectedType == 2 ? "[No property.managedReferenceValue]" : "No property.managedReferenceValue";
                }

                // Wrap click handlers to also refresh labels
                meleeBtn.clicked += () => RefreshButtonLabels();
                rangedBtn.clicked += () => RefreshButtonLabels();
                noneBtn.clicked += () => RefreshButtonLabels();

                buttonRow.Add(meleeBtn);
                buttonRow.Add(rangedBtn);
                buttonRow.Add(noneBtn);

                container.Add(buttonRow);
                container.Add(content);

                // Initialize display
                UpdateContent();
                RefreshButtonLabels();

                return container;
            }
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(PlayerButtonAction), true)]
    public class BaseEditor : PropertyDrawer
    {
        // Known concrete subtypes to present in the menu.
        // Keep this list in sync with available implementations.
        private static readonly Type[] Subtypes = new[]
        {
        typeof(PlayerButtonAction.BasicPush),
        typeof(PlayerButtonAction.TapOrHold),
        typeof(PlayerButtonAction.TapHoldOrLongHold),
        typeof(PlayerButtonAction.TargetDependant),
        };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            container.style.marginTop = 2;
            container.style.marginBottom = 2;

            // If there's no instance yet, show add button that opens a menu
            if (property.managedReferenceValue == null)
            {
                var addButton = new Button(() => ShowAddMenu(property)) { text = "+ Add Action" };
                addButton.style.unityTextAlign = TextAnchor.MiddleCenter;
                addButton.style.marginLeft = 2;
                addButton.style.marginRight = 2;
                addButton.style.paddingLeft = 6;
                addButton.style.paddingRight = 6;
                container.Add(addButton);
                return container;
            }

            // There's an active managed reference instance
            var instance = property.managedReferenceValue;
            var type = instance.GetType();

            // Header row: Type label + change-type button
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 4;

            var headerLabel = new Label($"Action Type: {type.Name}");
            headerLabel.AddToClassList("bold-label");
            headerLabel.style.flexGrow = 1;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(headerLabel);

            var changeBtn = new Button(() => ShowChangeMenu(property))
            {
                text = "Change Type"
            };
            headerRow.Add(changeBtn);

            // Context (right-click) menu to delete the managed reference
            headerLabel.RegisterCallback<ContextClickEvent>(evt =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete Action"), false, () =>
                {
                    if (property.serializedObject != null && property.serializedObject.targetObject != null)
                        Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, "Delete Action");

                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.ShowAsContext();
                evt.StopPropagation();
            });

            container.Add(headerRow);

            // Body: display the fields of the managed reference.
            // A PropertyField for the property will show the serialized children of the managed reference.
            var field = new PropertyField(property, null);
            // Expand children immediately
            field.SetEnabled(true);
            container.Add(field);

            return container;
        }

        private void ShowAddMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            // Add "None" option
            menu.AddItem(new GUIContent("None"), false, () =>
            {
                if (property.serializedObject != null && property.serializedObject.targetObject != null)
                    Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, "Set Action - None");

                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            foreach (var t in Subtypes)
            {
                menu.AddItem(new GUIContent(t.Name), false, () =>
                {
                    if (property.serializedObject != null && property.serializedObject.targetObject != null)
                        Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, $"Add Action - {t.Name}");

                    object instance = CreateInstanceOf(t);
                    property.managedReferenceValue = instance;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private void ShowChangeMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();

            foreach (var t in Subtypes)
            {
                menu.AddItem(new GUIContent(t.Name), false, () =>
                {
                    if (property.serializedObject != null && property.serializedObject.targetObject != null)
                        Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, $"Change Action - {t.Name}");

                    object instance = CreateInstanceOf(t);
                    property.managedReferenceValue = instance;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Action"), false, () =>
            {
                if (property.serializedObject != null && property.serializedObject.targetObject != null)
                    Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, "Delete Action");

                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.ShowAsContext();
        }

        // Create instance of type. Prefer parameterless constructor; if none, try FormatterServices.
        private static object CreateInstanceOf(Type t)
        {
            try
            {
                // Prefer parameterless constructor
                var ci = t.GetConstructor(Type.EmptyTypes);
                if (ci != null)
                    return Activator.CreateInstance(t);

                // Fallback: create uninitialized then let Unity handle serialization fields assignment
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        public virtual VisualElement TypeDisplay(SerializedProperty property) => null;
    }
#endif
}