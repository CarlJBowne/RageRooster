using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        public static void ShowWindow()
        {
            window = GetWindow<StaticMeshWindow>("Importing Asset (Example)");
        }
        private Vector2 scrollPosition;
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            actionTypeIndex = EditorGUILayout.Popup("Action Type", actionTypeIndex, new[] { "Create New Asset", "Update Existing Asset" });
            if (actionTypeIndex == 0)
                CreateStaticMeshWindow();
            else if (actionTypeIndex == 1)
                GUILayout.Label("Not yet implemented, sorry!");

            EditorGUILayout.EndScrollView();
        }

        private void CreateStaticMeshWindow()
        {
            // Which folder should this asset go it?
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

            // What is the asset's name?
            GUILayout.Label("Asset name");
            GUI.SetNextControlName("MyTextField");
            staticMesh.assetName = GUILayout.TextField(staticMesh.assetName);
            GUILayout.Space(spaceSize);

            // Model path
            staticMesh.model = CreateImportButton("Model Path (must be .fbx!)", staticMesh.model, "fbx") as Model;
            GUILayout.Space(spaceSize);

            // Handle textures
            if (FbxContainsEmbeddedTextures(staticMesh.model))
            {
                // ???
            }
            else
            {
                staticMesh.materialTypeIndex = EditorGUILayout.Popup("Material Type", staticMesh.materialTypeIndex, new[] { "PBR", "Something else" });
                staticMesh.transparent = GUILayout.Toggle(staticMesh.transparent, "Transparent");

                if (staticMesh.materialTypeIndex == 0)
                {
                    // PBR Texture paths
                    staticMesh.PbrTextures.DiffuseMap = CreateImportButton("Diffuse path", staticMesh.PbrTextures.DiffuseMap, "png") as Texture;
                    staticMesh.PbrTextures.RoughnessMap = CreateImportButton("Roughness path", staticMesh.PbrTextures.RoughnessMap, "png") as Texture;
                    staticMesh.PbrTextures.NormalMap = CreateImportButton("Normal path", staticMesh.PbrTextures.NormalMap, "png") as Texture;
                    staticMesh.PbrTextures.HeightMap = CreateImportButton("Height path", staticMesh.PbrTextures.HeightMap, "png") as Texture;
                    staticMesh.PbrTextures.EmissiveMap = CreateImportButton("Emissive path", staticMesh.PbrTextures.EmissiveMap, "png") as Texture;
                    if (staticMesh.transparent) staticMesh.PbrTextures.AlphaMap = CreateImportButton("Alpha path", staticMesh.PbrTextures.AlphaMap, "png") as Texture;
                }
                else GUILayout.Label("Sorry, this isn't a real option. Choice was an illusion all along.");

                GUILayout.Space(20);
            }

            // Handle material
            staticMesh.shouldCreateNewMaterial = GUILayout.Toggle(staticMesh.shouldCreateNewMaterial, "Create new material");
            if (!staticMesh.shouldCreateNewMaterial)
            {
                GUILayout.Label("Sorry, haven't implemented a way to reuse materials yet :(");
            }
            GUILayout.Space(20);

            // Finalize import dialog
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

        AssetBase CreateImportButton(string header, AssetBase asset, string forceType = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(header, GUILayout.Width(150));
            GUILayout.Label(""); // might do "|" or something idk
            GUILayout.Label(asset.sourcePath);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFilePanel("Select File", "", forceType);
                if (!string.IsNullOrEmpty(path))
                {
                    asset.sourcePath = path;
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                Debug.Log("Reset asset!");

                asset = (AssetBase)Activator.CreateInstance(asset.GetType()); // resets asset
            }

            GUILayout.EndHorizontal();

            return asset;
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
            // take the provided asset name and create folder
            string newFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(staticMesh.assetCategory, staticMesh.assetName));
            // create \src folder
            string newSrcFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(newFolder, "src"));
            // add assets to \src, standardize asset names in the process
            CopyAssetIntoProject(staticMesh.model, newSrcFolder);

            if (staticMesh.materialTypeIndex == 0) // magic numbers yay
            {
                CopyAssetIntoProject(staticMesh.PbrTextures.DiffuseMap, newSrcFolder);

                CopyAssetIntoProject(staticMesh.PbrTextures.RoughnessMap, newSrcFolder);

                // make sure normal map is set to "normal"
                CopyAssetIntoProject(staticMesh.PbrTextures.NormalMap, newSrcFolder);
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(staticMesh.PbrTextures.NormalMap.destinationPath)))
                {
                    TextureImporter normalMapImporter = AssetImporter.GetAtPath(staticMesh.PbrTextures.NormalMap.destinationPath) as TextureImporter;
                    normalMapImporter.textureType = TextureImporterType.NormalMap;
                    normalMapImporter.SaveAndReimport();
                }


                CopyAssetIntoProject(staticMesh.PbrTextures.HeightMap, newSrcFolder);
                CopyAssetIntoProject(staticMesh.PbrTextures.EmissiveMap, newSrcFolder);
            }

            // create material (if it's blank? i think? or maybe the textures shouldn't appear if it's not... also the "update" functionality)

            
            // apply textures to material

            // ??? create the prefab? Profit?
            // possibly something with collision?? if that's not automated???????
        }
        void CopyAssetIntoProject(AssetBase asset, string destinationPath)
        {
            if (asset.sourcePath == "No filepath set!") return;

            asset.destinationPath = destinationPath + "/" + asset.CreateFilename(staticMesh.assetName);

            // Copy file
            File.Copy(asset.sourcePath, asset.destinationPath, overwrite: true);

            // Refresh asset database
            AssetDatabase.ImportAsset(asset.destinationPath);
            Debug.Log("Imported: " + asset.destinationPath);
        }

        bool FbxContainsEmbeddedTextures(Model model)
        {
            // if it does, return true.
            return false;
        }
    }
}