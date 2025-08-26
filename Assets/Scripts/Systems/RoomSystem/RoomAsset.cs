using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

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
        [field: SerializeField] public Vector3 globalCenter { get; protected set; }
        [field: SerializeField] public SceneReference scene { get; protected set; }
        [field: SerializeField] public float loadSceneRadius { get; protected set; }
        [field: SerializeField] public RoomLOD[] lods { get; protected set; }




        //Active Data
        public RoomRoot root { get; protected set; }
        public int distance = RoomLOD.lowestLOD;


        public IEnumerator LoadInto()
        {
            yield return scene.LoadEnum();
            yield return new WaitUntil(()=> root != null);
        }

        public void Connect(RoomRoot root) => this.root = root;

        public void UpdateDistance()
        {
            Vector3 playerPos = PlayerMovementBody.PositionGet;
            int prevDistance = distance;

            distance = GetDistance();

            if(prevDistance == distance) return;

            if(distance == RoomLOD.playerWithin)
            {
                //Load Scene
            }
            else if (prevDistance == RoomLOD.playerWithin)
            {
                //Unload Scene
            }

            {
                lods[prevDistance].Unload();
            }
            {
                lods[distance].Load(area.root);
            }
        }
        public int GetDistance()
        {
            if(RoomManager.currentRoom == this) return RoomLOD.playerWithin;

            if(CompareDistance(loadSceneRadius)) return RoomLOD.sceneLoaded;

            if (!CompareDistance(lods[^1].range)) return RoomLOD.lowestLOD;

            for (int i = 0; i < lods.Length; i++)
            {
                if (CompareDistance(lods[i].range)) return i;
            }

            #if UNITY_EDITOR
            Debug.LogError("I'm not sure how this happened?");
            #endif
            return RoomLOD.lowestLOD;
        }
        private bool CompareDistance(float threshold) => 
            Vector3.SqrMagnitude(PlayerMovementBody.PositionGet - globalCenter) <= threshold * threshold;


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





        public class RoomLOD
        {
            public float range;
            public Prefab prefab;
            public GameObject instance;
            RoomLODState state;

            public enum RoomLODState
            {
                Null,
                Loading,
                Unloading,
                Present,
                LockedToBePresent,
            }

            private AsyncInstantiateOperation currentOP;
            private CoroutinePlus currentCoroutine;

            public void Load(MonoBehaviour runner)
            {
                currentCoroutine = Load().Begin(runner);
                IEnumerator Load()
                {
                    if (instance != null || state > RoomLODState.Null) yield break;
                    state = RoomLODState.Loading;
                    currentOP = prefab.InstantiateAsync(runner.transform);

                    while (!currentOP.isDone) yield return null;

                    instance = currentOP.Result[0] as GameObject;
                    state = RoomLODState.LockedToBePresent;

                    yield return new WaitForSecondsRealtime(20);

                    state = RoomLODState.Present;
                }
            }
            public void CancelLoad()
            {
                if(state != RoomLODState.Loading) return;
                currentOP.Cancel();
                currentCoroutine.StopAuto();
                state = RoomLODState.Null;
            }
            public void Unload()
            {
                if (instance == null || state < RoomLODState.Present) return;
                Destroy(instance);
                instance = null;
                state = RoomLODState.Null;
            }





            public const int playerWithin = -2;
            public const int sceneLoaded = -1;
            public const int lowestLOD = 999;
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