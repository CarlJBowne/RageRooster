using System.Collections;
using System.Collections.Generic;
using RageRooster.Core;
using RageRooster.SaveSystem;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using SLS.EditorUtilities.ComponentHeaders;
using static RageRooster.Services;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace RageRooster.World
{
    [ExecuteInEditMode]
    /// <summary>
    /// An entrance to a Room. MonoBehavior that triggers entering the Room when colliding with the Player.
    /// <br/>A pure-data representation of this entrance, <see cref="RoomEntrance.Data"/> is stored in a <see cref="RoomAsset"/> for runtime loading.
    /// </summary>
    public class RoomEntrance : RoomActor
    {


        /// <summary>
        /// The distance radius at which the room will begin loading.
        /// </summary>
        public float loadRadius = 20f;
        /// <summary>
        /// The distance radius at which the room will unload.
        /// </summary>
        public float unloadRadius = 30f;
        /// <summary>
        /// The distance radius at which the room's LOD will be loaded.
        /// </summary>
        public float lodRadius = 50f;
        /// <summary>
        /// The direction of the entrance inward into the room. <br/>
        /// If the player is on the near side of this transition it will not trigger loading nor entering the room.
        /// </summary>
        public Vector3 direction = Vector3.forward;

        /// <summary>
        /// If true, entrance directional checks ignore vertical difference (project to horizontal plane).
        /// This helps avoid odd behavior when player approaches from above/below.
        /// </summary>
        public bool ignoreVerticalAngle = true;

        [HeaderItem(true)] public new Collider collider;

        /// <summary>
        /// An optional <see cref="SpawnPoint"/> this entrance can set the player's respawn location to when entered.
        /// </summary>
        public SpawnPoint spawnPoint;
        /// <summary>
        /// Whether the <see cref="spawnPoint"/> should only be set on death reloads, and not normal transitions."/>
        /// </summary>
        public bool forDeathOnly = false;




#if UNITY_EDITOR
        public override void OnRegister()
        {
            Root.EntranceActors.Add(this);
            Root.asset.entrances.Add(GetData());
        }
        public override void OnDeregister()
        {
            Root.asset.entrances.RemoveAt(Root.EntranceActors.IndexOf(this));
            Root.EntranceActors.Remove(this);
        }
        public override void OnSave() => Root.asset.entrances[Root.EntranceActors.IndexOf(this)] = GetData();
#endif

        public void OnTriggerEnter(Collider other)
        {
            if (other != IPlayer.Self?.Collider || Root.asset == RoomManager.currentRoom) return;
            RoomManager.EnterRoom(Root.asset);
            if (spawnPoint != null)
                if (forDeathOnly) Services.SaveSystem.DeathDestination.Value = spawnPoint.GetDestination(); else Services.SaveSystem.CurrentDestination.Value = spawnPoint.GetDestination();
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            Vector3 worldDir = transform.TransformDirection(direction);

            if (worldDir.sqrMagnitude <= 1e-6f) return;


            // directional arrow
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(worldDir.normalized, transform.up), 20, UnityEngine.EventType.Repaint);

            Color gizmoColor = new(0.2f, 1f, 0.2f, 1f);

            // Draw horizontal arcs (semi-circles) when ignoring vertical, otherwise draw spheres
            if (ignoreVerticalAngle)
            {
                // Project direction onto horizontal plane and use the opposite as the start vector for the semicircle
                Vector3 worldDirHorizontal = new(worldDir.x, 0f, worldDir.z);
                if (worldDirHorizontal.sqrMagnitude <= 1e-6f) return;
                else
                {
                    Vector3 fromVector = -worldDirHorizontal.normalized.Rotated(-90, Vector3.up); // opposite direction on the horizontal plane

                    // Draw filled semicircles using Handles.DrawSolidArc
                    UnityEditor.Handles.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, .02f);
                    UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, fromVector, 180f, loadRadius);
                    UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, fromVector, 180f, unloadRadius);

                    // wire outlines for the semicircles
                    UnityEditor.Handles.color = gizmoColor;
                    UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, fromVector, 180f, loadRadius);
                    UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, fromVector, 180f, lodRadius);

                    UnityEditor.Handles.color = Color.white;

                    Vector3 colliderRefPosition = collider.ClosestPoint(transform.position); 
                    Plane projectionPlane = new(worldDirHorizontal, colliderRefPosition);
                    float dis = projectionPlane.GetDistanceToPoint(transform.position);
                    //if (dis == 0) dis = 10;


                    void DrawRadiusQuad(float radius, bool filled)
                    {
                        Vector3 center = transform.position;

                        // rim endpoints of the semicircle
                        Vector3 p0 = center + fromVector.normalized * radius;
                        Vector3 p1 = center + (-fromVector.normalized) * radius; // 180 deg opposite

                        // project endpoints onto the plane 
                        Vector3 q0 = p0 - (worldDirHorizontal * dis);
                        Vector3 q1 = p1 - (worldDirHorizontal * dis);

                        // Draw filled quad with very low alpha fill to match original style
                        UnityEditor.Handles.color = new(gizmoColor.r, gizmoColor.g, gizmoColor.b, filled ? 0.02f : 1f);

                        if (filled) UnityEditor.Handles.DrawAAConvexPolygon(new Vector3[4] { p0, p1, q1, q0 });
                        else
                        {
                            UnityEditor.Handles.DrawLine(p0, q0);
                            UnityEditor.Handles.DrawLine(p1, q1);
                        }
                    }

                    DrawRadiusQuad(loadRadius, true);
                    DrawRadiusQuad(loadRadius, false);

                    DrawRadiusQuad(unloadRadius, true);
                    DrawRadiusQuad(lodRadius, false);
                }
            }
            else
            {
                // Draw filled semicircles using Handles.DrawSolidArc
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, .02f);
                Gizmos.DrawSphere(transform.position, loadRadius);
                Gizmos.DrawSphere(transform.position, unloadRadius);

                // wire outlines for the semicircles
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, loadRadius);
                Gizmos.DrawWireSphere(transform.position, lodRadius);

                Gizmos.color = Color.white;
            }

            UnityEditor.Handles.color = Color.white;
#endif
        }

        public Data GetData() => new()
        {
            point = transform.position,
            direction = direction.magnitude != 0 ? transform.TransformDirection(direction) : Vector3.forward,
            loadRadiusSQR = loadRadius * loadRadius,
            unloadRadiusSQR = unloadRadius * unloadRadius,
            lodRadiusSQR = lodRadius * lodRadius,
            colliderDepth = GetColliderDepth(),
            ignoreVertical = ignoreVerticalAngle
        };

        private float GetColliderDepth()
        {
            Vector3 point = IgnoreVertical(transform.position);
            Vector3 dir = IgnoreVertical(transform.TransformDirection(direction));
            return new Plane(dir, point).GetDistanceToPoint(collider != null ? collider.ClosestPoint(point) : point);
        }

        /// <summary>
        /// Packaged data about this entrance to be saved into a <see cref="RoomAsset"/>.
        /// </summary>
        [System.Serializable]
        public struct Data
        {
            public Vector3 point;
            public Vector3 direction;
            public float loadRadiusSQR;
            public float unloadRadiusSQR;
            public float lodRadiusSQR;
            public float colliderDepth;
            public bool ignoreVertical;
            public float distanceSquared { get; private set; }
            public int strip { get; private set; }
            // 3 = Within Load Radius
            // 2 = Within Unload Radius
            // 1 = Within LOD Radius
            // 0 = Outside or N/A

            public static implicit operator float(Data D) => D.distanceSquared;

            public void UpdateDistance()
            {
                Vector3 player = IgnoreVertical(IPlayer.Self?.Position ?? Vector3.zero);

                float DOT = Vector3.Dot(point - (IPlayer.Self?.Position ?? Vector3.zero), direction);
                distanceSquared = Vector3.SqrMagnitude((IPlayer.Self?.Position ?? Vector3.zero) - point);

                if (DOT > 0)
                {
                    strip = distanceSquared < loadRadiusSQR ? 3
                    : distanceSquared < unloadRadiusSQR ? 2
                    : distanceSquared < lodRadiusSQR ? 1
                    : 0;
                }
                else
                {
                    if (colliderDepth <= 0.02f)
                    {
                        strip = 0;
                        return;
                    }

                    // This is a biiiiiiiiiiiiiiiiit of a nuclear option for preventing the space between 
                    // the reference point and the transition collider from becoming a place where the player could
                    // cause unintended loading behavior, but it's probably fine.
                    Plane P = new(direction, point);
                    if (P.GetDistanceToPoint(player) <= colliderDepth)
                    {
                        distanceSquared = (P.ClosestPointOnPlane(player) - point).sqrMagnitude;
                        strip = distanceSquared < loadRadiusSQR ? 3
                            : distanceSquared < unloadRadiusSQR ? 2
                            : distanceSquared < lodRadiusSQR ? 1
                            : 0;
                    }
                    else strip = 0;
                }
            }

            Vector3 IgnoreVertical(Vector3 input) => ignoreVertical ? new(input.x, 0, input.z) : input;
        }

        Vector3 IgnoreVertical(Vector3 input) => ignoreVerticalAngle ? new(input.x, 0, input.z) : input;

#if UNITY_EDITOR
        [ContextMenu("Add Spawn Point")]
        private void AddSpawnPoint()
        {
            GameObject G = new("SpawnPoint");
            G.transform.SetParent(transform);
            G.transform.localPosition = Vector3.zero;
            G.transform.localRotation = Quaternion.identity;
            spawnPoint = G.AddComponent<SpawnPoint>();
            UnityEditor.Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Room Entrance");
        }

        [UnityEditor.MenuItem("GameObject/Create Room Entrance", false, 0)]
        public static void CreateRoomEntrance()
        {
            GameObject newObject = new("Room Entrance");
            UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Create Room Entrance");
            RoomEntrance entrance = newObject.AddComponent<RoomEntrance>();
            var parent = UnityEditor.Selection.activeTransform;
            if (parent != null)
            {
                UnityEditor.Undo.SetTransformParent(newObject.transform, parent.transform, "Create Room Entrance");
                newObject.transform.localPosition = Vector3.zero;
                newObject.transform.localRotation = Quaternion.identity;
            }
            UnityEditor.Selection.activeGameObject = newObject;
            newObject.AddComponent<BoxCollider>().isTrigger = true;
            entrance.AddSpawnPoint();
        }
#endif

    }

}