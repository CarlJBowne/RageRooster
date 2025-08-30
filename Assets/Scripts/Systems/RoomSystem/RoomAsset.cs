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
        [field: SerializeField] public float unloadSceneRadius { get; protected set; }
        [field: SerializeField] public RoomLOD[] lods { get; protected set; }




        //Active Data
        public RoomRoot root { get; protected set; }
        public enum RoomState
        {
            Null = -1,
            Lowest,
            LODS,
            Unloading,
            Loading,
            Present,
            Current
        }
        public RoomState state { get; protected set; }
        public int currentLOD { get; protected set; } = -1;


        public void Connect(RoomRoot root) => this.root = root;

        
        public void Enter() => RoomManager.EnterRoom(this);
        internal void _Enter()
        {
            state = RoomState.Current;
        }
        internal void _Exit()
        {
            state = RoomState.Present;
        }


        public void Update()
        {
            if (state is RoomState.Current or RoomState.Unloading or RoomState.Loading) return;

            CalculateDistance();

            if (state is RoomState.Present)
            {
                if (!CompareDistance(unloadSceneRadius)) 
                    SceneUnload().Begin(area.root);
            }
            else
            {
                if (CompareDistance(loadSceneRadius))
                {
                    SceneLoad().Begin(area.root);
                    return;
                }
                else if(state is RoomState.Lowest && CompareDistance(lods[^1].range))
                {
                    state = RoomState.LODS;
                    EnterLod(FindLod(0));
                }
                else if (state is RoomState.LODS)
                {
                    if (!CompareDistance(lods[currentLOD].range)) 
                        EnterLod(FindLod(currentLOD + 1));
                    else 
                        EnterLod(FindLodReverse(currentLOD - 1));
                }
            }
        }

        private float CalculateDistance()
        {
            PlayerDistance = Vector3.SqrMagnitude(PlayerMovementBody.PositionGet - globalCenter);
            return PlayerDistance;
        }
        private float PlayerDistance;
        private bool CompareDistance(float threshold) =>
            PlayerDistance <= threshold * threshold;

        int FindLod(int start)
        {
            for (int i = start; i < lods.Length; i++)
                if (CompareDistance(lods[i].range))
                    return i;
            return -1;
        }
        int FindLodReverse(int start)
        {
            if (lods.Length == 1) return 0;
            int i = start;
            for (; i > 0; i--)
                if (!CompareDistance(lods[i].range))
                    return i + 1;
            return i;
        }
        void EnterLod(int ID)
        {
            if (ID == currentLOD) return;
            if (ID == -1)
            {
                lods[currentLOD].TurnOff();
                currentLOD = -1;
                state = RoomState.Lowest;
            }
            else
            {
                lods[currentLOD].TurnOff();
                currentLOD = ID;
                lods[currentLOD].TurnOn();
            }
        }


        public IEnumerator PrepEnter()
        {
            yield return scene.LoadEnum();
            scene.TryGetRootScript(out RoomRoot root);
            if (root == null) yield return new WaitUntil(() => root != null);
            state = RoomState.Present;
        }
        public IEnumerator PrepSurrounding()
        {
            if(CompareDistance(loadSceneRadius))
            {
                yield return SceneLoad();
                state = RoomState.Present;
            }
            else
            {
                int ID = FindLod(0);
                if (ID != -1)
                {
                    state = RoomState.Lowest;
                }
                else
                {
                    currentLOD = ID;
                    yield return lods[currentLOD].Load();
                    state = RoomState.LODS;
                }
            }
        }




        public IEnumerator SceneLoad()
        {
            if (scene.Loaded || state >= RoomState.Loading) yield break;
            state = RoomState.Loading;
            AsyncOperation op = scene.LoadAsync();

            while (!op.isDone) yield return null;

            scene.TryGetRootScript(out RoomRoot root);
            if (root == null) yield return new WaitUntil(() => root != null);

            state = RoomState.Present;
        }
        public IEnumerator SceneUnload()
        {
            if (!scene.Loaded || state <= RoomState.Unloading) yield break;
            state = RoomState.Unloading;
            AsyncOperation op = scene.UnloadAsync();

            while (!op.isDone) yield return null;

            root = null;

            state = RoomState.LODS;
        }


        public IEnumerator CompleteUnload()
        {
            if(scene.state == SceneReference.SceneState.Loaded)
            {
                yield return scene.UnloadEnum();
            }
            else if (scene.state == SceneReference.SceneState.Unloading)
            {
                yield return new WaitUntil(() => scene.state == SceneReference.SceneState.Valid);
            }
            else if (scene.state == SceneReference.SceneState.Loading)
            {
                yield return new WaitUntil(() => scene.state == SceneReference.SceneState.Loaded);
                yield return scene.UnloadEnum();
            }

            for (int i = 0; i < lods.Length; i++) lods[i].CompleteUnload();
            state = RoomState.Null;
            currentLOD = -1;
        }


        public class RoomLOD
        {
            public float range;
            public Prefab prefab;
            public GameObject instance;
            bool loaded = false;

            private AsyncInstantiateOperation currentOP;
            private CoroutinePlus currentCoroutine;


            public void TurnOn()
            {
                if (loaded)
                {
                    instance.SetActive(true);
                }
                else
                {
                    Load().Begin(RoomManager.currentArea.root);
                }
            }
            public void TurnOff()
            {
                if (loaded)
                {
                    instance.SetActive(false);
                }
                else
                {
                    CancelLoad();
                }
            }

            public IEnumerator Load()
            {
                if (loaded == true) yield break;
                currentOP = prefab.InstantiateAsync(RoomManager.currentArea.root.transform);

                while (!currentOP.isDone) yield return null;

                instance = currentOP.Result[0] as GameObject;
                instance.SetActive(true);
                loaded = true;
            }
            public void CancelLoad()
            {
                if(currentOP == null || currentCoroutine == null) return;
                currentOP.Cancel();
                currentCoroutine.StopAuto();
            }
            public void Unload()
            {
                if (loaded == false) return;
                Destroy(instance);
                instance = null;
                loaded = false;
            }

            public void CompleteUnload()
            {
                instance = null;
                loaded = false;
            }
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.lods), backingField: true));
        }
    }

#endif
}