using UnityEditor;
using UnityEngine;
using Utilities.Xtensions.Unity;

namespace RageRooster.RoomSystem
{
    [ExecuteInEditMode]
    public abstract class RoomActor : MonoBehaviour, IRoomActor
    {
        [field: SerializeField] public RoomRoot Root { get; set; }

        public void Reset() => IRoomActor.RegisterWithRoot(this);



        public void OnDestroy()
        {
#if UNITY_EDITOR
            this.GetExecutionDetails(out bool gameIsEditor, out bool gameIsPlaying, out bool objectSceneIsLoaded);
            if (gameIsEditor && objectSceneIsLoaded && !gameIsPlaying) IRoomActor.DeregisterWithRoot(this);
#endif

        }

#if UNITY_EDITOR
        public virtual void OnRegister() { }
        public virtual void OnDeregister() { }
        public virtual void OnSave() { }
#endif

    }

    /// <summary>
    /// An interface representing objects with an important connection to the <see cref="RoomRoot"/>/<see cref="RoomAsset"/> they belong to. <br/>
    /// <see cref="OnRegister"/>, <see cref="OnDeregister"/> and <see cref="OnSave"/> optional overrides exist.
    /// </summary>
    public interface IRoomActor
    {
        RoomRoot Root { get; set; }

        public void Reset();

#if UNITY_EDITOR
        /// <summary>
        /// Runs AFTER being Registered to a <see cref="RoomRoot"/>
        /// </summary>
        public void OnRegister() { }
        /// <summary>
        /// Runs BEFORE being Deregistered from a <see cref="RoomRoot"/>
        /// </summary>
        /// <param name="disconnectedRoot"></param>
        public void OnDeregister() { }
        /// <summary>
        /// Runs while the scene owning a <see cref="RoomRoot"/> is being saved to project.
        /// </summary>
        public void OnSave() { }

        public static void RegisterWithRoot(IRoomActor actor, bool ignoreStatus = false)
        {
            if (actor.Root != null && !ignoreStatus) DeregisterWithRoot(actor);

            actor.Root = RoomRoot.Find(actor as Component);
            if (actor.Root != null)
            {
                actor.Root.RoomActors.AddU(actor as Component);
                actor.OnRegister();
                EditorUtility.SetDirty(actor.Root);
                EditorUtility.SetDirty(actor as Component);
            }
        }
        public static void DeregisterWithRoot(IRoomActor actor)
        {
            if (actor.Root == null) return;
            actor.OnDeregister();
            actor.Root.RoomActors.Remove(actor);
            EditorUtility.SetDirty(actor.Root);
            actor.Root = null;
            EditorUtility.SetDirty(actor as Component);
        }
#endif

    }

    public static class _RoomActorExtensions
    {

    }
}

