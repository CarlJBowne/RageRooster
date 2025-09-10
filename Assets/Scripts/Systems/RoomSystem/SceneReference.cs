using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

using System.Reflection;




#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneReference : ISerializationCallbackReceiver
{
    #region Values

    [field: SerializeField] public string sceneName { get; private set; }
    //[field: SerializeField] public int buildIndex { get; private set; } = -1;
    [field: SerializeField] public string scenePath { get; private set; }
    // Remove serialization for managerScene, only set at runtime

    public Scene runtimeScene
    {
        get
        {
            if (_runtimeScene.IsValid()) return _runtimeScene;
            /*if (buildIndex != -1)
                _runtimeScene = SceneManager.GetSceneByBuildIndex(buildIndex);
            else*/ if (!string.IsNullOrEmpty(sceneName))
                _runtimeScene = SceneManager.GetSceneByName(sceneName);
            return _runtimeScene;
        }
    } private Scene _runtimeScene;

    public AsyncOperation asyncOperation { get; private set; }

    public enum SceneState
    {
        NULL = -2,
        INVALID = -1,
        Valid = 0,
        Loaded = 1,
        Loading = 2,
        Unloading = 3
    }
    [field: SerializeField] public SceneState state 
    { 
        get; 
        private set; 
    } = SceneState.NULL;

    public bool Loaded => state == SceneState.Loaded;
    public bool Valid => state >= SceneState.Valid;

    public bool isSerialized = false;

    #endregion Values


    public SceneReference(string sceneName)
    {
        this.sceneName = sceneName;
        //ValidateRuntime();
    }

    /*
    public SceneReference(int buildIndex)
    {
        this.buildIndex = buildIndex;
        //ValidateRuntime();
    }*/

    public SceneReference(string sceneName, string folderPath)
    {
        this.sceneName = sceneName;
        this.scenePath = $"{folderPath}{sceneName}.unity";
        //ValidateRuntime();
    }

    /*[Obsolete]
    public void ValidateRuntime()
    {
        if (Valid) return;

        if(buildIndex == -1 && string.IsNullOrEmpty(sceneName)) throw new System.InvalidOperationException("SceneReference is not properly set up.");

        if (buildIndex != -1)
        {
            _runtimeScene = SceneManager.GetSceneByBuildIndex(buildIndex);
            if (_runtimeScene.IsValid())
            {
                sceneName = _runtimeScene.name;
                scenePath = _runtimeScene.path;
                state = _runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
                return;
            }
        }
        else if (!string.IsNullOrEmpty(sceneName))
        {
            _runtimeScene = SceneManager.GetSceneByName(sceneName);
            if (_runtimeScene.IsValid())
            {
                scenePath = _runtimeScene.path;
                buildIndex = _runtimeScene.buildIndex;
                state = _runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
                return;
            }
        }

        if (!Valid) throw new System.InvalidOperationException("Invalid Scene at runtime.");
    }*/

#if UNITY_EDITOR 

    [field: SerializeField] public UnityEngine.Object asset { get; private set; }

    public SceneReference(UnityEngine.Object sceneAsset)
    {
        asset = sceneAsset;
        ValidateSerialized();
    }

    public void ValidateSerialized()
    {
        //buildIndex = -1;
        sceneName = null;
        scenePath = null;
        state = SceneState.NULL;

        if (asset == null) return;

        string path = AssetDatabase.GetAssetPath(asset);
        if (!path.EndsWith(".unity")) throw new System.ArgumentException("Error 1 : SceneObject constructor expects a scene asset.");
        scenePath = path;
        sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        int buildIndex = 0;
        for (int i = 0, disableds = 0; i < scenes.Length; i++)
        {
            //if (!scenes[i].enabled)
            //{
            //    disableds++;
            //    continue;
            //}
            if (scenes[i].path == scenePath)
            {
                buildIndex = i - disableds;
                break;
            }
        }
            
        if (buildIndex == -1) return;

        state = scenes[buildIndex].enabled ? SceneState.Valid : SceneState.INVALID;
    }

#endif


    public void OnBeforeSerialize()
    {
        //if (state > SceneState.Valid) state = SceneState.Valid;
    }
    public void OnAfterDeserialize()
    {
        if (state > SceneState.Valid) state = SceneState.Valid;
    }


    #region Functionality

    public void LoadSingle()
    {
        //ValidateRuntime();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");

        /*if (buildIndex != -1) SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
        else*/ if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        else throw new Exception("Invalid Scene");
    }
    public AsyncOperation LoadSingleAsync()
    {
        //ValidateRuntime();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");

        asyncOperation = /*buildIndex != -1 ? SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive)
            :*/ !string.IsNullOrEmpty(sceneName) ? SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)
            : throw new Exception("Invalid Scene");

        state = SceneState.Loading;
        asyncOperation.completed += FinishLoad;
        return asyncOperation;
    }
    public IEnumerator LoadSingleEnum()
    {
        //ValidateRuntime();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");

        asyncOperation = /*buildIndex != -1 ? SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive)
            :*/ !string.IsNullOrEmpty(sceneName) ? SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)
            : throw new Exception("Invalid Scene");

        state = SceneState.Loading;
        while(!asyncOperation.isDone) yield return null;
        FinishLoad(asyncOperation);
    }

    public void Load()
    {
        //ValidateRuntime();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        
        /*if(buildIndex != -1) SceneManager.LoadScene(buildIndex, LoadSceneMode.Additive);
        else*/ if(!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        else throw new Exception("Invalid Scene");
    }
    public AsyncOperation LoadAsync()
    {
        //ValidateRuntime();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");

        asyncOperation = 
            /*buildIndex != -1 ? SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive)
            :*/ !string.IsNullOrEmpty(sceneName) ? SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)
            : throw new Exception("Invalid Scene");

        state = SceneState.Loading;
        asyncOperation.completed += FinishLoad;
        return asyncOperation;
    }
    public IEnumerator LoadEnum()
    {
        //ValidateRuntime();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");

        asyncOperation =
            /*buildIndex != -1 ? SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive)
            :*/ !string.IsNullOrEmpty(sceneName) ? SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)
            : throw new Exception("Invalid Scene");

        state = SceneState.Loading;
        while (!asyncOperation.isDone) yield return null;
        FinishLoad(asyncOperation);
    }

    public AsyncOperation UnloadAsync()
    {
        //ValidateRuntime();
        if (!Loaded) throw new System.InvalidOperationException("Scene is not loaded.");

        asyncOperation = /*buildIndex != -1 ? SceneManager.UnloadSceneAsync(buildIndex)
            :*/ !string.IsNullOrEmpty(sceneName) ? SceneManager.UnloadSceneAsync(sceneName)
            : throw new Exception("Invalid Scene");

        state = SceneState.Unloading;
        asyncOperation.completed += FinishUnload;
        return asyncOperation;
    }
    public IEnumerator UnloadEnum()
    {
        //ValidateRuntime();
        if (!Loaded) throw new System.InvalidOperationException("Scene is not loaded.");

        asyncOperation = /*buildIndex != -1 ? SceneManager.UnloadSceneAsync(buildIndex)
            :*/ !string.IsNullOrEmpty(sceneName) ? SceneManager.UnloadSceneAsync(sceneName)
            : throw new Exception("Invalid Scene");

        state = SceneState.Unloading;
        while (!asyncOperation.isDone) yield return null;
        FinishUnload(asyncOperation);
    }

    private void FinishLoad(AsyncOperation op)
    {
        state = SceneState.Loaded;
    }
    private void FinishUnload(AsyncOperation op)
    {
        state = SceneState.Valid;
    }


    public GameObject GetRootGameObject() => Loaded ? runtimeScene.GetRootGameObjects()[0] : null;

    public GameObject[] GetRootGameObjects() => Loaded ? runtimeScene.GetRootGameObjects() : null;

    public T GetRootScript<T>() where T : Component => Loaded ? runtimeScene.GetRootGameObjects()[0].GetComponent<T>() : null;
    public bool TryGetRootScript<T>(out T result) where T : Component
    {
        if (!Loaded)
        {
            result = null;
            return false;
        }
        return runtimeScene.GetRootGameObjects()[0].TryGetComponent(out result);
    }

    #endregion Functionality

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    private enum SceneRefState
    {
        Null,
        NotInList,
        InListButDisabled,
        Valid,
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);



        Rect iconRect = new Rect(
            position.x + EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight,
            position.y,
            EditorGUIUtility.singleLineHeight,
            EditorGUIUtility.singleLineHeight
        );
        Rect detailsRect = new(
            position.x,
            position.y + EditorGUIUtility.singleLineHeight,
            position.width,
            EditorGUIUtility.singleLineHeight * 3
            );


        SerializedProperty assetProp = property.FindPropertyRelative(nameof(SceneReference.asset).BackingField());
        EditorGUI.BeginChangeCheck();
        var asset = EditorGUI.ObjectField(
            position,
            label,
            assetProp.objectReferenceValue,
            typeof(UnityEditor.SceneAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            property.serializedObject.Update();
            assetProp.objectReferenceValue = asset; 
            property.serializedObject.ApplyModifiedProperties();

            // Use reflection to call ValidateAsset() on the SceneReference instance
            var sceneRef = GetTargetObjectOfProperty(property) as SceneReference;
            if (sceneRef != null)
            {
                var method = typeof(SceneReference).GetMethod(nameof(SceneReference.ValidateSerialized), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) method.Invoke(sceneRef, null);
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        // Icon and Tooltip
        SceneRefState state = SceneRefState.Null;
        string scenePath = property.FindPropertyRelative(nameof(SceneReference.scenePath).BackingField()).stringValue;

        if (asset != null && !string.IsNullOrEmpty(scenePath))
        {
            state = SceneRefState.NotInList;
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    state = scenes[i].enabled ? SceneRefState.Valid : SceneRefState.InListButDisabled;
                    break;
                }
            }
        }

        string tooltip = "";
        Texture2D icon = null;

        switch (state)
        {
            case SceneRefState.Null:
                tooltip = "This Scene Reference is Null. Ensure it is filled with a valid scene before use.";
                icon = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
                break;
            case SceneRefState.NotInList:
                tooltip = "This Scene is not in the Build List. Click to open Build Settings.";
                icon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                break;
            case SceneRefState.InListButDisabled:
                tooltip = "This Scene is in the Build List, but is not enabled. Click to open Build Settings.";
                icon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                break;
            case SceneRefState.Valid:
                tooltip = "This Scene is validly set up.";
                icon = EditorGUIUtility.IconContent("TestPassed").image as Texture2D;
                break;
        }

        GUIContent iconContent = new(icon, tooltip);
        if (GUI.Button(iconRect, iconContent, GUIStyle.none))
            if (state is SceneRefState.NotInList or SceneRefState.InListButDisabled)
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));



        bool sceneReferenceDetailsShow = EditorPrefs.GetBool("SceneReference_DetailsShow", true);
        GUIContent dropdownIcon = new(EditorGUIUtility.IconContent(sceneReferenceDetailsShow ? "IN Foldout on" : "IN Foldout").image);
        sceneReferenceDetailsShow = EditorGUI.Foldout(position, sceneReferenceDetailsShow, "", true);
        EditorPrefs.SetBool("SceneReference_DetailsShow", sceneReferenceDetailsShow);



        // Draw dropdown if open
        if (sceneReferenceDetailsShow)
        {
            EditorGUILayout.Space(detailsRect.height);
            EditorGUI.indentLevel++;
            Rect detailRect = position;
            detailRect.y += EditorGUIUtility.singleLineHeight + 2;
            detailRect.height = EditorGUIUtility.singleLineHeight;


            EditorGUI.BeginDisabledGroup(true);
            // Show SceneReference data
            EditorGUI.TextField(detailRect, "Scene Name:", property.FindPropertyRelative(nameof(SceneReference.sceneName).BackingField()).stringValue);
            detailRect.y += EditorGUIUtility.singleLineHeight;
            //EditorGUI.IntField(detailRect, "Build Index:", property.FindPropertyRelative(nameof(SceneReference.buildIndex).BackingField()).intValue);
            //detailRect.y += EditorGUIUtility.singleLineHeight;

            SerializedProperty stateProp = property.FindPropertyRelative(nameof(SceneReference.state).BackingField());
            EditorGUI.EnumPopup(detailRect, "State: ", (SceneReference.SceneState)stateProp.enumValueIndex-2);
            detailRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.EndDisabledGroup();
            EditorGUI.LabelField(detailRect, tooltip);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        if (prop == null) return null;

        string[] path = prop.propertyPath.Replace(".Array.data[", "[")
            .Split('.');
        object obj = prop.serializedObject.targetObject;
        foreach (string element in path)
        {
            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = Convert.ToInt32(
                    element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", "")
                );
                var field = obj.GetType().GetField(elementName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var list = field.GetValue(obj) as IList;
                obj = list[index];
            }
            else
            {
                var field = obj.GetType().GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                obj = field.GetValue(obj);
            }
        }
        return obj;
    }

}


#endif