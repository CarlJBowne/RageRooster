using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RageRooster.RoomSystem;
using System.IO;

public static class LevelDesignTools
{
    [MenuItem("File/Create Area", priority = 0)]
    public static void CreateArea()
    {
        string targetPath = EditorUtility.SaveFilePanel("Create New Area", "Assets/World/Areas", "New Area", "asset").Replace(Application.dataPath, "Assets");
        string name = Path.GetFileNameWithoutExtension(targetPath);

        var Area = ScriptableObject.CreateInstance<AreaAsset>();
        AssetDatabase.CreateAsset(Area, targetPath);

        AreaRegistry.Editor_AddArea(Area);

        //Copy AreaTemplate scene from Assets/Templates/AreaTemplate.unity
        string templatePath = "Assets/Editor/AreaTemplate.unity";
        string scenePath = targetPath.Replace(".asset", "_Scene.unity");

        if (!AssetDatabase.CopyAsset(templatePath, scenePath)) return;

        AreaAsset.Editor.Setup(Area, name, AssetDatabase.LoadAssetAtPath<Object>(scenePath));

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        AreaRoot.Editor.AttachAsset(scene.GetRootGameObjects()[0].GetComponent<AreaRoot>(), Area);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"Successfully created new Area: {name}. Note that it cannot be automatically regsitered in the build settings, YOU have to do that.");
    }
    [MenuItem("File/Create Room", priority = 0)]
    public static void CreateRoom()
    {

    }
















}