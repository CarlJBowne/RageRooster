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

        [field: SerializeField] public string displayName { get; protected set; } = "INSERT_DISPLAY_NAME";
        [field: SerializeField] public AreaAsset area { get; protected set; }
        [field: SerializeField] public Vector3 globalCenter { get; protected set; }
        [field: SerializeField] public SceneReference scene { get; protected set; }
        [field: SerializeField] public RoomLOD lod { get; protected set; }

        [field: SerializeField] public List<RoomEntrance.Data> entrances { get; protected set; } = new();


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
        public RoomState state 
        { 
            get; 
            protected set; 
        }
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

            Vector3 player = PlayerMovementBody.PositionGet;
            if (state is RoomState.Present)
            {
                if (!WithinUnloadRange(player)) 
                    SceneUnload().Begin(area.root);
            }
            else
            {
                if (WithinLoadRange(player))
                {
                    SceneLoad().Begin(area.root);
                    return;
                }
                else if(state is RoomState.Lowest && WithinLodRange(player))
                {
                    state = RoomState.LODS;
                    lod.TurnOn();
                }
                else if (state is RoomState.LODS && !WithinLodRange(player))
                {
                    state = RoomState.Lowest;
                    lod.TurnOff();
                }
            }
        }

        #region Range Calculators
        private bool WithinLoadRange(Vector3 player)
        {
            // Early exit if there are no transitions
            if (entrances == null || entrances.Count == 0)
                return false;

            // Use foreach, but return immediately on first match
            foreach (RoomEntrance.Data item in entrances)
            {
                if (item.direction != Vector3.zero && Vector3.Dot(item.point - player, item.direction) < 0) continue;
                if (Vector3.SqrMagnitude(player - item.point) < item.loadRadius * item.loadRadius)
                    return true;
            }
            return false;
        }
        private bool WithinUnloadRange(Vector3 player)
        {
            // Early exit if there are no transitions
            if (entrances == null || entrances.Count == 0)
                return false;

            // Use foreach, but return immediately on first match
            foreach (RoomEntrance.Data item in entrances)
            {
                if (item.direction != Vector3.zero && Vector3.Dot(item.point - player, item.direction) < 0) continue;
                if (Vector3.SqrMagnitude(player - item.point) < item.unloadRadius * item.unloadRadius)
                    return true;
            }
            return false;
        }
        private bool WithinLodRange(Vector3 player)
        {
            // Early exit if there are no transitions
            if (entrances == null || entrances.Count == 0)
                return false;

            // Use foreach, but return immediately on first match
            foreach (RoomEntrance.Data item in entrances)
            {
                if (item.direction != Vector3.zero && Vector3.Dot(item.point - player, item.direction) < 0) continue;
                if (Vector3.SqrMagnitude(player - item.point) < item.lodRadius * item.lodRadius)
                    return true;
            }
            return false;
        }
        #endregion










        public IEnumerator PrepEnter()
        {
            yield return scene.LoadEnum();
            yield return null;
            root = scene.GetRootScript<RoomRoot>();
            if (root == null) yield return new WaitUntil(() => root != null);
            state = RoomState.Present;
            yield return null; 
        }
        public IEnumerator PrepSurrounding()
        {
            if (this == RoomManager.currentRoom) yield break;
            Vector3 player = PlayerMovementBody.PositionGet;
            if (WithinLoadRange(player))
            {
                yield return SceneLoad();
                state = RoomState.Present;
            }
            else
            {
                if (WithinLodRange(player))
                {
                    state = RoomState.LODS;
                    yield return lod.Load();
                }
                else
                {
                    state = RoomState.Lowest;
                }
            }
        }




        public IEnumerator SceneLoad()
        {
            if (scene.Loaded || state >= RoomState.Loading) yield break;
            state = RoomState.Loading;
            AsyncOperation op = scene.LoadAsync();

            while (!op.isDone) yield return null;

            yield return null;
            root = scene.GetRootScript<RoomRoot>();
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
            if(scene.state == SceneState.Loaded)
            {
                yield return scene.UnloadEnum();
            }
            else if (scene.state == SceneState.Unloading)
            {
                yield return new WaitUntil(() => scene.state == SceneState.Valid);
            }
            else if (scene.state == SceneState.Loading)
            {
                yield return new WaitUntil(() => scene.state == SceneState.Loaded);
                yield return scene.UnloadEnum();
            }

            //for (int i = 0; i < lods.Length; i++) lods[i].CompleteUnload();
            lod.CompleteUnload();
            state = RoomState.Null;
            currentLOD = -1;
        }

        private void OnDisable()
        {
            state = RoomState.Null;
        }

        [System.Serializable]
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
                if(prefab.readOnlyObject == null) yield break;

                if (loaded == true) yield break;
                currentOP = prefab.InstantiateAsync(RoomManager.currentArea.root.transform);

                if (currentOP == null) yield break;
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

                if (GUILayout.Button($"Area: {areaAsset.displayName}", linkStyle))
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

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.displayName), backingField: true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.scene), backingField: true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.lod), backingField: true));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(roomAsset, "Modified Room Asset");
                EditorUtility.SetDirty(roomAsset);
            }

            // Display foldable, uneditable list of transitions
            SerializedProperty transitionsProp = serializedObject.FindProperty(nameof(RoomAsset.entrances), backingField: true);
            bool transitionsFoldout = EditorPrefs.GetBool("RoomAsset_EntrancesFoldout", true);
            transitionsFoldout = EditorGUILayout.Foldout(transitionsFoldout, "Entrances", true);
            EditorPrefs.SetBool("RoomAsset_EntrancesFoldout", transitionsFoldout);

            if (transitionsFoldout)
            {
                if (transitionsProp != null && transitionsProp.isArray)
                {
                    int count = transitionsProp.arraySize;
                    if (count == 0)
                    {
                        EditorGUILayout.LabelField("No Entrances attached.");
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < count; i++)
                        {
                            SerializedProperty itemProp = transitionsProp.GetArrayElementAtIndex(i);
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.PropertyField(itemProp, new($"Entrance {i + 1}"), true);
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Entrances property not found.");
                }
            }
        }
        protected void OnDisable()
        {
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }

#endif
}