using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RageRooster.RoomSystem
{
    [CreateAssetMenu(fileName = "Room", menuName = "ScriptableObjects/Room")]
    public class RoomAsset : ScriptableObject
    {
        //Serialized Data
        [field: SerializeField] public string roomDisplayName { get; protected set; } = "INSERT_DISPLAY_NAME";
        [field: SerializeField] public AreaAsset area { get; protected set; }
        [field: SerializeField] public SceneReference scene { get; protected set; }
        [field: SerializeField] public Prefab adjacentLOD { get; protected set; }

        [field: SerializeField] public Vector3 globalCenter { get; protected set; }
        [field: SerializeField] public float outerRadius { get; protected set; }
        [field: SerializeField] public float innerRadius { get; protected set; }


        //Active Data
        public RoomRoot root { get; protected set; }
        public GameObject adjacentLODInstance { get; protected set; }

        public enum RoomVersionState
        {
            Null,
            Loading,
            Unloading,
            Present,
            LockedToBePresent,
        }
        RoomVersionState alodState = RoomVersionState.Null;
        RoomVersionState sceneState = RoomVersionState.Null;
        bool withinLODRange;
        bool withinLoadRange;

        public IEnumerator LoadInto()
        {
            yield return scene.LoadEnum();
            yield return new WaitUntil(()=> root != null);
        }

        public void Connect(RoomRoot root) => this.root = root;

        public void Update()
        {
            Vector3 playerPos = PlayerStateMachine.Get().transform.position;

            withinLODRange = Vector3.SqrMagnitude(playerPos - globalCenter) <= outerRadius * outerRadius;
            withinLoadRange = Vector3.SqrMagnitude(playerPos - globalCenter) <= innerRadius * innerRadius;

            if(alodState == RoomVersionState.Null && withinLODRange) 
                ALODLoad().Begin(root);
            else if (alodState == RoomVersionState.Present && !withinLODRange) 
                ALODUnload().Begin(root);

            if (sceneState == RoomVersionState.Null && withinLoadRange) 
                SceneLoad().Begin(root);
            else if (sceneState == RoomVersionState.Present && !withinLoadRange) 
                SceneUnload().Begin(root);
        }



        public IEnumerator ALODLoad()
        {
            if (adjacentLODInstance != null || alodState > RoomVersionState.Null) yield break;
            alodState = RoomVersionState.Loading;
            AsyncInstantiateOperation op = adjacentLOD.InstantiateAsync(area.root.transform);

            while (!op.isDone)
            {
                yield return null;
                if (!withinLODRange)
                {
                    op.Cancel();
                    yield break;
                }
            }

            adjacentLODInstance = op.Result[0] as GameObject;
            alodState = RoomVersionState.LockedToBePresent;

            yield return new WaitForSecondsRealtime(20);

            alodState = RoomVersionState.Present;
        }
        public IEnumerator ALODUnload()
        {
            if (adjacentLODInstance == null || alodState < RoomVersionState.Present) yield break;
            Destroy(adjacentLODInstance);
            adjacentLODInstance = null;
            alodState = RoomVersionState.Null;
        }

        public IEnumerator SceneLoad()
        {
            if (scene.Loaded || sceneState > RoomVersionState.Null) yield break;
            sceneState = RoomVersionState.Loading;
            AsyncOperation op = scene.LoadAsync();

            while (!op.isDone) yield return null;

            sceneState = RoomVersionState.LockedToBePresent;

            yield return new WaitForSecondsRealtime(300);

            sceneState = RoomVersionState.Present;
        }
        public IEnumerator SceneUnload()
        {
            if (!scene.Loaded || sceneState < RoomVersionState.Present) yield break;
            sceneState = RoomVersionState.Unloading;
            AsyncOperation op = scene.UnloadAsync();

            while (!op.isDone) yield return null;

            sceneState = RoomVersionState.Null;
        }








    }

#if UNITY_EDITOR

    [CustomEditor(typeof(RoomAsset))]
    public class RoomAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RoomAsset roomAsset = (RoomAsset)target;

            // Draw Area link or orphan warning
            SerializedProperty areaProp = serializedObject.FindProperty(nameof(RoomAsset.area), backingField: true);
            AreaAsset areaAsset = areaProp.objectReferenceValue as AreaAsset;

            GUILayout.Space(8);

            if (areaAsset != null)
            {
                GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
                linkStyle.normal.textColor = new Color(0.2f, 0.5f, 1f);
                linkStyle.fontStyle = FontStyle.Bold;

                if (GUILayout.Button($"Area: {areaAsset.areaDisplayName}", linkStyle))
                {
                    Selection.activeObject = areaAsset;
                    EditorGUIUtility.PingObject(areaAsset);
                }
            }
            else
            {
                GUIStyle redStyle = new GUIStyle(EditorStyles.label);
                redStyle.normal.textColor = Color.red;
                redStyle.fontStyle = FontStyle.Bold;
                GUILayout.Label("ORPHAN ROOM, PLEASE ADD TO AREA", redStyle);
            }

            GUILayout.Space(8);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.roomDisplayName), backingField: true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.scene), backingField: true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.adjacentLOD), backingField: true));
        }
    }

#endif
}