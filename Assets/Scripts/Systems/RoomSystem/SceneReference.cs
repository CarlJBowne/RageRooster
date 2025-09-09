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
    [field: SerializeField] public string sceneName { get; private set; }
    [field: SerializeField] public int buildIndex { get; private set; }
    [field: SerializeField] public string scenePath { get; private set; }
    // Remove serialization for managerScene, only set at runtime

    public Scene managedScene
    {
        get
        {
            if (_managedScene.IsValid()) return _managedScene;
            if (buildIndex != -1)
                _managedScene = SceneManager.GetSceneByBuildIndex(buildIndex);
            else if (!string.IsNullOrEmpty(sceneName))
                _managedScene = SceneManager.GetSceneByName(sceneName);
            return _managedScene;
        }
    } private Scene _managedScene;

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
    [field: SerializeField] public SceneState state { get; private set; } = SceneState.NULL;

    public bool Loaded => state == SceneState.Loaded;
    public bool Valid => state >= SceneState.Valid;

    public bool isSerialized = false;


    public SceneReference(string sceneName)
    {
        this.sceneName = sceneName;
        this.scenePath = null;
        this.buildIndex = -1;
        this.asyncOperation = null;

        Scene runtimeScene = SceneManager.GetSceneByName(sceneName);
        if (runtimeScene.IsValid())
        {
            state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
            scenePath = runtimeScene.path;
            buildIndex = runtimeScene.buildIndex;
        }
        else state = SceneState.INVALID;
#if UNITY_EDITOR
        asset = null;
#endif
    }

    public SceneReference(int buildIndex)
    {
        this.buildIndex = buildIndex;
        this.sceneName = null;
        this.scenePath = null;
        this.asyncOperation = null;

        Scene runtimeScene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (runtimeScene.IsValid())
        {
            state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
            sceneName = runtimeScene.name;
            scenePath = runtimeScene.path;
        }
        else state = SceneState.INVALID;
#if UNITY_EDITOR
        asset = null;
#endif
    }

    public SceneReference(string sceneName, string folderPath)
    {
        this.sceneName = sceneName;
        this.scenePath = $"{folderPath}{sceneName}.unity";
        this.buildIndex = -1;
        this.asyncOperation = null;

        Scene runtimeScene = SceneManager.GetSceneByPath(scenePath);
        if (runtimeScene.IsValid())
        {
            this.sceneName = runtimeScene.name;
            state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
            buildIndex = runtimeScene.buildIndex;
        }
        else state = SceneState.INVALID;
#if UNITY_EDITOR
        asset = null;
#endif
    }


    public void OnBeforeSerialize()
    {
        var trace = new System.Diagnostics.StackTrace().GetFrame(1);
        if (trace != null && trace.GetMethod().Name != "DoEditorSerialize") return;

        //Debug.Log($"Serializing Scene Reference : {sceneName}");

#if UNITY_EDITOR
        if (asset != null)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!path.EndsWith(".unity")) throw new System.ArgumentException("SceneObject constructor expects a scene asset.");
            scenePath = path;
            sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            Scene runtimeScene = SceneManager.GetSceneByPath(scenePath);
            if (runtimeScene.IsValid())
            {
                state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
                buildIndex = runtimeScene.buildIndex;
            }
            else
            {
                state = SceneState.INVALID;
            }
        }
        else
        {
            sceneName = null;
            buildIndex = -1;
            scenePath = null;
            state = SceneState.NULL;
        }
#endif
    }
    public void OnAfterDeserialize()
    {
        if (state > SceneState.Valid) state = SceneState.Valid;
        //if(managerScene == default && Application.isPlaying)
        //{
        //    if (buildIndex != -1) managerScene = SceneManager.GetSceneByBuildIndex(buildIndex);
        //    else if (!string.IsNullOrEmpty(sceneName)) managerScene = SceneManager.GetSceneByName(sceneName);
        //}
        //Debug.Log($"Deserialized Scene Reference : {sceneName}");
    }

    public void Validate()
    {
        if (!Valid)throw new System.InvalidOperationException("Invalid Scene at runtime.");
    }

    public void ValidateAsset()
    {
#if UNITY_EDITOR
        if (asset != null)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!path.EndsWith(".unity")) throw new System.ArgumentException("SceneObject constructor expects a scene asset.");
            scenePath = path;
            sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            Scene runtimeScene = SceneManager.GetSceneByPath(scenePath);
            if (runtimeScene.IsValid())
            {
                state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
                buildIndex = runtimeScene.buildIndex;
            }
            else
            {
                state = SceneState.INVALID;
            }
        }
        else
        {
            sceneName = null;
            buildIndex = -1;
            scenePath = null;
            state = SceneState.NULL;
        }
#endif

    }


#if UNITY_EDITOR

    [field: SerializeField] public UnityEngine.Object asset { get; private set; }

    public SceneReference(UnityEngine.Object sceneAsset)
    {
        asset = sceneAsset;
        this.sceneName = null;
        this.scenePath = null;
        this.buildIndex = -1;
        this.asyncOperation = null;

        OnBeforeSerialize();
    }


#endif

    public void LoadSingle()
    {
        Validate();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public AsyncOperation LoadSingleAsync()
    {
        Validate();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        state = SceneState.Loading;
        asyncOperation.completed += FinishLoad;
        return asyncOperation;
    }
    public IEnumerator LoadSingleEnum()
    {
        Validate();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        state = SceneState.Loading;
        while(!asyncOperation.isDone) yield return null;
        FinishLoad(asyncOperation);
    }

    public void Load()
    {
        Validate();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
    public AsyncOperation LoadAsync()
    {
        Validate();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        state = SceneState.Loading;
        asyncOperation.completed += FinishLoad;
        return asyncOperation;
    }
    public IEnumerator LoadEnum()
    {
        Validate();
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        state = SceneState.Loading;
        while (!asyncOperation.isDone) yield return null;
        FinishLoad(asyncOperation);
    }

    public AsyncOperation UnloadAsync()
    {
        Validate();
        if (!Loaded) throw new System.InvalidOperationException("Scene is not loaded.");
        asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
        state = SceneState.Unloading;
        asyncOperation.completed += FinishUnload;
        return asyncOperation;
    }
    public IEnumerator UnloadEnum()
    {
        Validate();
        if (!Loaded) throw new System.InvalidOperationException("Scene is not loaded.");
        asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
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


    public GameObject GetRootGameObject() => Loaded ? managedScene.GetRootGameObjects()[0] : null;

    public GameObject[] GetRootGameObjects() => Loaded ? managedScene.GetRootGameObjects() : null;

    public T GetRootScript<T>() where T : Component => Loaded ? managedScene.GetRootGameObjects()[0].GetComponent<T>() : null;
    public bool TryGetRootScript<T>(out T result) where T : Component
    {
        if (!Loaded)
        {
            result = null;
            return false;
        }
        return managedScene.GetRootGameObjects()[0].TryGetComponent(out result);
    }
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
            EditorGUIUtility.singleLineHeight * 4
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

            // Use reflection to call ValidateAsset() on the SceneReference instance
            var sceneRef = GetTargetObjectOfProperty(property) as SceneReference;
            if (sceneRef != null)
            {
                var method = typeof(SceneReference).GetMethod("ValidateAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
            EditorGUI.IntField(detailRect, "Build Index:", property.FindPropertyRelative(nameof(SceneReference.buildIndex).BackingField()).intValue);
            detailRect.y += EditorGUIUtility.singleLineHeight;
            SerializedProperty stateProp = property.FindPropertyRelative(nameof(SceneReference.state).BackingField());
            EditorGUI.EnumPopup(detailRect, "State: ", (SceneReference.SceneState)stateProp.enumValueIndex);
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