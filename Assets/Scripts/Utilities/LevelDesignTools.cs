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

        var Area = ScriptableObject.CreateInstance<AreaAsset>();
        AssetDatabase.CreateAsset(Area, targetPath);

        //Copy AreaTemplate scene from Assets/Templates/AreaTemplate.unity
        string templatePath = "Assets/Templates/AreaTemplate.unity";
        string scenePath = targetPath.Replace(".asset", "_Scene.unity");

        if (!AssetDatabase.CopyAsset(templatePath, scenePath)) return;

        Area.Editor_Setup(Path.GetFileNameWithoutExtension(targetPath), AssetDatabase.LoadAssetAtPath<Object>(scenePath));

        var scene = EditorSceneManager.GetSceneByPath(scenePath);

        scene.GetRootGameObjects()[0].GetComponent<AreaRoot>().Editor_Setup(Area);

    }
    [MenuItem("File/Create Room", priority = 0)]
    public static void CreateRoom()
    {

    }
















}