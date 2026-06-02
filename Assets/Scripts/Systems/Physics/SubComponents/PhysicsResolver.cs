using System;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities.Xtensions.VisualElements;
using System.Linq;
using Unity.VisualScripting;
using Utilities.Xtensions.Unity;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace RageRooster.Physics
{
    /// <summary>
    /// Abstract base class for movement resolvers. A resolver is responsible for translating a proposed movement vector into collisions, sliding, landing and other movement effects for its owning <see cref="PhysicsBody"/>.
    /// </summary>
    [System.Serializable, RequireComponent(typeof(PhysicsBody)), ExecuteAlways]
    public abstract partial class PhysicsResolver : MonoBehaviour
    {
        #region Relations

        /// <summary>
        /// Initialize this resolver with its owning <see cref="PhysicsBody"/>.
        /// This must be called by the owner during Awake/Start before using the resolver.
        /// </summary>
        /// <param name="body">The owning PhysicsBody.</param>
        public void Init(PhysicsBody body) => this.Body = body;

        /// <summary>
        /// The owning PhysicsBody instance. Available after <see cref="Init"/> is called.
        /// </summary>
        [field: SerializeField] public PhysicsBody Body { get; private set; }

        /// <summary>
        /// Convenience properties that forward to the owning body. These provide quick
        /// access to common state frequently used by resolvers.
        /// </summary>
        protected Vector3 Position => Body.Position;
        protected Velocity stepZeroVelocity => Body.Velocity;
        protected GroundState Ground => Body.Ground;
        protected AnchorPoint anchor => Body.Ground.anchor;
        protected Direction direction => Body.Direction;
        protected PhysicsResolver Next => Body.Resolvers.Active;

        #endregion

        public virtual void Reset()
        {
            if (!TryGetComponent(out PhysicsBody pb))
            { DestroyImmediate(this); return; }
            pb.Resolvers.resolvers.Add(this);
            this.Body = pb;
        }

        /// <summary>
        /// Lifecycle hooks and the main Move contract for resolvers.
        /// </summary>
        public virtual void OnStart() { }
        /// <summary>
        /// Called when this resolver becomes the active resolver for a PhysicsBody.
        /// </summary>
        public virtual void Enter() { }
        /// <summary>
        /// Called when this resolver is no longer active for a PhysicsBody.
        /// </summary>
        public virtual void Exit() { }
        /// <summary>
        /// Called before the main resolver Move invocation for per-frame setup.
        /// </summary>
        public virtual void FixedUpdateFormer() { }
        /// <summary>
        /// Called after the main resolver Move invocation for per-frame teardown.
        /// </summary>
        public virtual void FixedUpdateLatter() { }
        /// <summary>
        /// Process the supplied movement vector (<paramref name="stepVelocity"/>)
        /// for this resolver's domain. The implementation is responsible for
        /// performing collision sweeps, updating body position and optionally
        /// delegating remaining movement to the next resolver via the owning
        /// body's resolver selection.
        /// </summary>
        /// <param name="stepVelocity">The movement vector to process, typically velocity * deltaTime.</param>
        public abstract void Move(Vector3 stepVelocity);

        public bool ContinueCheck(Vector3 vel) =>
            vel.sqrMagnitude < float.Epsilon || ++Body.Step >= Body.maxPhysicsSteps;
        public bool ContinueCheck(float hitDistance) =>
            hitDistance == -1 || ++Body.Step >= Body.maxPhysicsSteps;

        public void ChooseNext() => Body.Resolvers.Update();
        public void ChooseNext(int target) => Body.Resolvers.Update(target);
        public void ChooseNext(PhysicsResolver target) => Body.Resolvers.Update(target);

        protected void Print(Func<string> value)
        {
            if (!Body.Debug.DisplayDebugString) return;
            Body.Debug.AppendLine(value?.Invoke());
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }

        private void OnDestroy()
        {
            this.GetExecutionDetails(out bool gameIsEditor, out bool gameIsPlaying, out bool objectSceneIsLoaded);
            if (!gameIsEditor || !objectSceneIsLoaded) return;
            Body.Resolvers.resolvers.Remove(this);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(PhysicsResolver), true)]
    public class Editor : UnityEditor.PropertyDrawer
    {
        SerializedProperty property;
        VisualElement root;
        Foldout foldout;
        Button GetButton;
    
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.property = property;
            root = new();
            foldout = new()
            {
                text = property.displayName
            };
            foldout.BindProperty(property);
            root.Add(foldout);
            GetButton = new(ButtonPress);
            GetButton.style.width = new Length(40, LengthUnit.Percent);
            GetButton.style.height = 14;
            GetButton.style.position = Position.Absolute;
            GetButton.style.top = 0;
            GetButton.style.right = 0;
            root.Add(GetButton);

            Build();

            Undo.undoRedoPerformed += Build;

            return root;
        }

        void Build()
        {
            foldout.contentContainer.Clear();
            GetButton.text = property.objectReferenceValue != null
                ? property.objectReferenceValue.GetType().Name.Replace("PhysResolver", "")
                : "Select";
            if (property.objectReferenceValue != null)
                foldout.Add(new InspectorElement(new SerializedObject(property.objectReferenceValue)));
        }

        void ButtonPress()
        {
            GenericMenu Menu = new();

            PhysicsResolver[] existingResolvers = (property.serializedObject.targetObject as Component).GetComponents<PhysicsResolver>();

            for (int i = 0; i < existingResolvers.Length; i++)
            {
                int t = i;
                Menu.AddItem(new($"{i + 1} : {existingResolvers[t].GetType().Name.Replace("PhysResolver", "")}"), 
                    property.objectReferenceValue == existingResolvers[t], 
                    () => PostMenuE(existingResolvers[t]));
            }
                

            Menu.AddSeparator("");

            Type[] subTypes = GetSubtypes();
            for (int i = 0; i < subTypes.Length; i++)
            {
                int t = i;
                Menu.AddItem(new($"+ {subTypes[t].Name.Replace("PhysResolver", "")}"), false, () => PostMenuN(subTypes[t]));
            }


            if (property.objectReferenceValue != null)
            {
                Menu.AddSeparator("");
                Menu.AddItem(new("Nullify"), false, PostMenuNull);
            }

            Menu.ShowAsContext();
        }
        void PostMenuE(PhysicsResolver input)
        {
            property.objectReferenceValue = input;
            property.serializedObject.ApplyModifiedProperties();
            Build();
        }
        void PostMenuN(Type targetType)
        {
            PhysicsResolver newRes = (property.serializedObject.targetObject as Component).gameObject.AddComponent(targetType) as PhysicsResolver;
            property.objectReferenceValue = newRes;
            newRes.hideFlags = HideFlags.HideInInspector;
            property.serializedObject.ApplyModifiedProperties();
            Build();
        }
        void PostMenuNull()
        {
            property.objectReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
            Build();
        }

        public static Type[] GetSubtypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    !t.IsAbstract &&
                    // For interfaces, include implementers; for classes, include strict subclasses only.
                    t.IsSubclassOf(typeof(PhysicsResolver)) && (t.IsPublic || t.IsNestedPublic || t.IsNestedFamORAssem || t.IsNestedFamily)
                )
                .ToArray();
        }

    }
#endif 
}