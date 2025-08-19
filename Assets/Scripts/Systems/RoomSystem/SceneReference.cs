using UnityEngine;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct SceneReference
{
    [field: SerializeField] public string sceneName { get; private set; }
    [field: SerializeField] public int buildIndex { get; private set; }
    [field: SerializeField] public string scenePath { get; private set; }
    // Remove serialization for managerScene, only set at runtime
    public Scene managerScene { get; private set; }
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
    public SceneState state { get; private set; }

    public readonly bool Loaded => state == SceneState.Loaded;
    public readonly bool Valid => state >= SceneState.Valid;

    public SceneReference(string sceneName)
    {
        this.sceneName = sceneName;
        this.scenePath = null;
        this.buildIndex = -1;
        this.managerScene = default;
        this.asyncOperation = null;

        Scene runtimeScene = SceneManager.GetSceneByName(sceneName);
        if (runtimeScene.IsValid())
        {
            state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
            scenePath = runtimeScene.path;
            buildIndex = runtimeScene.buildIndex;
            managerScene = runtimeScene;
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
        this.managerScene = default;
        this.asyncOperation = null;

        Scene runtimeScene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (runtimeScene.IsValid())
        {
            state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
            sceneName = runtimeScene.name;
            scenePath = runtimeScene.path;
            managerScene = runtimeScene;
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
        this.managerScene = default;
        this.asyncOperation = null;

        Scene runtimeScene = SceneManager.GetSceneByPath(scenePath);
        if (runtimeScene.IsValid())
        {
            this.sceneName = runtimeScene.name;
            state = runtimeScene.isLoaded ? SceneState.Loaded : SceneState.Valid;
            buildIndex = runtimeScene.buildIndex;
            managerScene = runtimeScene;
        }
        else state = SceneState.INVALID;
#if UNITY_EDITOR
        asset = null;
#endif
    }

#if UNITY_EDITOR

    [field: SerializeField] public Object asset { get; private set; }

    public SceneReference(Object sceneAsset)
    {
        asset = sceneAsset;
        this.sceneName = null;
        this.scenePath = null;
        this.buildIndex = -1;
        this.managerScene = default;
        this.asyncOperation = null;

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
                managerScene = runtimeScene;
            }
            else state = SceneState.INVALID;
        }
        else state = SceneState.NULL;
    }

    internal void OnValidate()
    {
        this.managerScene = default;
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
                managerScene = runtimeScene;
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
    }


#endif

    public void LoadSingle()
    {
        if(!Valid) throw new System.InvalidOperationException("Invalid Scene.");
        if(Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public AsyncOperation LoadSingleAsync()
    {
        if (!Valid) throw new System.InvalidOperationException("Invalid Scene.");
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        state = SceneState.Loading;
        asyncOperation.completed += FinishLoad;
        return asyncOperation;
    }

    public void Load()
    {
        if (!Valid) throw new System.InvalidOperationException("Invalid Scene.");
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
    public AsyncOperation LoadAsync()
    {
        if (!Valid) throw new System.InvalidOperationException("Invalid Scene.");
        if (Loaded) throw new System.InvalidOperationException("Scene is already loaded.");
        asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        state = SceneState.Loading;
        asyncOperation.completed += FinishLoad;
        return asyncOperation;
    }
    public AsyncOperation Unload()
    {
        if (!Valid) throw new System.InvalidOperationException("Invalid Scene.");
        if (!Loaded) throw new System.InvalidOperationException("Scene is not loaded.");
        asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
        state = SceneState.Unloading;
        asyncOperation.completed += FinishUnload;
        return asyncOperation;
    }

    private void FinishLoad(AsyncOperation op) => state = SceneState.Loaded;
    private void FinishUnload(AsyncOperation op) => state = SceneState.Valid;

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneObjectDrawer : PropertyDrawer
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

        // Draw the asset field, limiting to scene assets only
        Rect assetRect = position;
        assetRect.width -= EditorGUIUtility.singleLineHeight + 2; // Reserve space for warning icon

        SerializedProperty assetProp = property.FindPropertyRelative("<asset>k__BackingField");
        EditorGUI.BeginChangeCheck();
        // Use ObjectField with a filter for SceneAsset type
        assetProp.objectReferenceValue = EditorGUI.ObjectField(
            assetRect,
            label,
            assetProp.objectReferenceValue,
            typeof(UnityEditor.SceneAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            // Force update of SceneObject struct after asset change
            Object targetObject = property.serializedObject.targetObject;
            var so = (SceneReference)fieldInfo.GetValue(targetObject);

            Object newAsset = assetProp.objectReferenceValue;
            if (newAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(newAsset);
                if (path.EndsWith(".unity")) so = new SceneReference(newAsset);
            }
            else so = new SceneReference();

            fieldInfo.SetValue(targetObject, so);
            property.serializedObject.Update();
            property.serializedObject.ApplyModifiedProperties();
        }

        SceneRefState state = SceneRefState.Null;

        // Draw warning icon if not in build list
        UnityEngine.Object asset = assetProp.objectReferenceValue;
        string scenePath = property.FindPropertyRelative("<scenePath>k__BackingField").stringValue;

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

        Rect iconRect = position;
        iconRect.x = assetRect.xMax + 2;
        iconRect.width = EditorGUIUtility.singleLineHeight;
        iconRect.height = EditorGUIUtility.singleLineHeight;

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
                icon = EditorGUIUtility.IconContent("TestPassed").image as Texture2D; // Green checkmark icon
                break;
        }

        GUIContent iconContent = new(icon, tooltip);
        if (GUI.Button(iconRect, iconContent, GUIStyle.none))
            if (state is SceneRefState.NotInList or SceneRefState.InListButDisabled)
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));

        EditorGUI.EndProperty();
    }
}


#endif