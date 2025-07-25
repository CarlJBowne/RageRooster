using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace AssetImportPipeline
{
    public class StaticMeshWindow : EditorWindow
    {
        static StaticMeshWindow window; // To avoid any potential lifecycle issues with the window. Hopefully not necessary?
        StaticMesh staticMesh = new StaticMesh();
        int spaceSize = 20;
        int actionTypeIndex = 0;
        bool shouldCreateNewMaterial = true;
        public static void ShowWindow()
        {
            window = GetWindow<StaticMeshWindow>("Importing Asset (Example)");
        }


        void OnGUI()
        {
            actionTypeIndex = EditorGUILayout.Popup("Action Type", actionTypeIndex, new[] { "Create New Asset", "Update Existing Asset" });
            if (actionTypeIndex == 0)
                CreateStaticMeshWindow();
            else if (actionTypeIndex == 1)
                GUILayout.Label("Not yet implemented, sorry!");
        }

        private void CreateStaticMeshWindow()
        {
            GUILayout.Space(spaceSize);
            GUILayout.Label("Category");
            GUILayout.Label(staticMesh.assetCategory);
            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFolderPanel("Select File", "Assets/Art", "");
                if (!string.IsNullOrEmpty(path))
                {
                    staticMesh.assetCategory = path;
                }
            }
            GUILayout.Space(spaceSize);

            GUILayout.Label("Asset name");
            GUI.SetNextControlName("MyTextField");
            staticMesh.assetName = GUILayout.TextField(staticMesh.assetName);
            GUILayout.Space(spaceSize);

            CreateImportButton("Model Path (must be .fbx!)", staticMesh.model, "fbx");
            GUILayout.Space(spaceSize);

            CreateImportButton("Diffuse path", staticMesh.PbrTextures.DiffuseMap, "png");
            CreateImportButton("Roughness path", staticMesh.PbrTextures.RoughnessMap, "png");
            CreateImportButton("Specular path", staticMesh.PbrTextures.SpecularMap, "png");
            CreateImportButton("Normal path", staticMesh.PbrTextures.NormalMap, "png");
            CreateImportButton("Height path", staticMesh.PbrTextures.HeightMap, "png");
            GUILayout.Space(20);

            shouldCreateNewMaterial = GUILayout.Toggle(shouldCreateNewMaterial, "Create new material");
            if (!shouldCreateNewMaterial)
            {
                GUILayout.Label("Sorry, haven't implemented a way to reuse materials yet :(");
            }
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            if (GUILayout.Button("Confirm final import", GUILayout.Width(200)))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Import",
                    "Good work! All strictly necessary information has been entered. :)\n\nAre you ready to import this asset, or would you like to make more adjustments?",
                    "I'm ready",
                    "Go Back"
                );

                if (confirm)
                {
                    ImportStaticMesh();
                    Close();
                }
            }
            GUILayout.Label("");
            GUILayout.EndHorizontal();
        }

        void CreateImportButton(string header, AssetBase asset, string forceType = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(header, GUILayout.Width(150));
            GUILayout.Label(""); // might do "|" or something idk
            GUILayout.Label(asset.assetPath);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFilePanel("Select File", "", forceType);
                if (!string.IsNullOrEmpty(path))
                {
                    asset.assetPath = path;
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50))) asset = (AssetBase)Activator.CreateInstance(asset.GetType()); // resets asset
            GUILayout.EndHorizontal();
        }

        bool ValidateProvidedData()
        {
            // folder is within Assets/Art (?)
            // name is not taken in the destination, or blank, or contains forbidden characters, or has an extension erroneously, or otherwise invalid
            // FBX is present
            // POSSIBLY something regarding internal/external textures?? probably should be done earlier actually.
            // roughness OR specular

            return true;
        }
        void ImportStaticMesh()
        {
            // take the provided asset name
            // create folder
            // create \src folder
            // add assets to \src
            // standardize asset names in the process
            // make sure normal map is set to "normal"
            // create material (if it's blank? i think? or maybe the textures shouldn't appear if it's not... also the "update" functionality)
            // apply textures to material

            // ??? create the prefab? Profit?
            // possibly something with collision?? if that's not automated???????
        }
    }
}