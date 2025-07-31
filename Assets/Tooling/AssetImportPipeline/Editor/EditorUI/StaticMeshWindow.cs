using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DependenciesHunter;
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
            SetCategoryUI();
            CreateSeparatorLine();
            SetAssetNameUI();
            CreateSeparatorLine();
            if (SetModelUI())
            {
                CreateSeparatorLine();
                // Extract materials from FBX
                if (!staticMesh.model.hasBeenAnalysed)
                {
                    // Reset any leftover data from previous static mesh paths
                    staticMesh.materials.Clear();

                    // Import a temporary version of the FBX for analysis so AssetDatabase can be used
                    string tempPath = "Assets/Tooling/AssetImportPipeline/Editor/TempFiles/";
                    string tempFbxFilePath = tempPath + "temp.fbx";
                    File.Copy(staticMesh.model.sourcePath, tempFbxFilePath, overwrite: true);
                    AssetDatabase.ImportAsset(tempFbxFilePath);

                    // Analyze the temporary asset
                    foreach (UnityEngine.Object i in AssetDatabase.LoadAllAssetsAtPath(tempFbxFilePath))
                    {
                        if (i is UnityEngine.Material _material)
                        {
                            Material material = new Material();
                            material.customName = i.name;
                            // PSUEDOCODE: Try to get the original Texture paths.
                            // ...this looks like it'd require uFBX again so actually, MAYBE NOT.
                            staticMesh.materials.Add(material);
                        }
                    }

                    // Remove the temporary asset so it doesn't clutter up the project.
                    AssetDatabase.DeleteAsset(tempFbxFilePath);

                    staticMesh.model.hasBeenAnalysed = true;
                }
                if (staticMesh.model.hasBeenAnalysed)
                {
                    GUILayout.Space(spaceSize);
                    foreach (Material i in staticMesh.materials)
                    {
                        GUILayout.Label("Material: " + i.customName);

                        i.shader = (Material.Shaders)EditorGUILayout.Popup("Shader", (int)i.shader, Enum.GetNames(typeof(Material.Shaders)));
                        switch (i.shader)
                        {
                            case Material.Shaders.UniversalRenderPipelineLit:
                                GUILayout.BeginHorizontal();
                                GUILayout.EndHorizontal();
                                i.urplSettings.transparent = GUILayout.Toggle(i.urplSettings.transparent, "Transparent");
                                i.urplSettings.DiffuseMap = CreateImportButton("Diffuse path", i.urplSettings.DiffuseMap, "png") as Texture;
                                i.urplSettings.RoughnessMap = CreateImportButton("Roughness path", i.urplSettings.RoughnessMap, "png") as Texture;
                                i.urplSettings.NormalMap = CreateImportButton("Normal path", i.urplSettings.NormalMap, "png") as Texture;
                                i.urplSettings.HeightMap = CreateImportButton("Height path", i.urplSettings.HeightMap, "png") as Texture;
                                i.urplSettings.EmissiveMap = CreateImportButton("Emissive path", i.urplSettings.EmissiveMap, "png") as Texture;
                                if (i.urplSettings.transparent) i.urplSettings.AlphaMap = CreateImportButton("Alpha path", i.urplSettings.AlphaMap, "png") as Texture;
                                break;
                            case Material.Shaders.CelShaderLit:
                                i.cslSettings.BaseColor = CreateImportButton("Base Color", i.cslSettings.BaseColor, "png") as Texture;
                                break;
                        }

                        CreateSeparatorLine();
                    }

                }




                //OLD_SetPbrTexturesUI();
                //OLD_SetMaterialActionUI();

                GUILayout.Space(spaceSize); GUILayout.Space(spaceSize); GUILayout.Space(spaceSize);
                FinishImportUI();
            }





    
            void SetCategoryUI()
            {
                // Which folder should this asset go in?
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
            }

            void SetAssetNameUI()
            {
                // What is the asset's name?
                GUILayout.Label("Asset name");
                GUI.SetNextControlName("MyTextField");
                staticMesh.assetName = GUILayout.TextField(staticMesh.assetName);
                GUILayout.Space(spaceSize);
            }

            bool SetModelUI()
            {
                // Model path
                staticMesh.model = CreateImportButton("Model Path (must be .fbx!)", staticMesh.model, "fbx") as Model;
                return staticMesh.model.sourcePath != "No filepath set!";
            }

            // void OLD_SetPbrTexturesUI()
            // {
            //     // Handle textures
            //     if (FbxContainsEmbeddedTextures(staticMesh.model))
            //     {
            //         // ???
            //     }
            //     else
            //     {
            //         staticMesh.materialTypeIndex = EditorGUILayout.Popup("Material Type", staticMesh.materialTypeIndex, new[] { "PBR", "Something else" });
            //         staticMesh.transparent = GUILayout.Toggle(staticMesh.transparent, "Transparent");

            //         if (staticMesh.materialTypeIndex == 0)
            //         {
            //             // PBR Texture paths
            //             staticMesh.PbrTextures.DiffuseMap = CreateImportButton("Diffuse path", staticMesh.PbrTextures.DiffuseMap, "png") as Texture;
            //             staticMesh.PbrTextures.RoughnessMap = CreateImportButton("Roughness path", staticMesh.PbrTextures.RoughnessMap, "png") as Texture;
            //             staticMesh.PbrTextures.NormalMap = CreateImportButton("Normal path", staticMesh.PbrTextures.NormalMap, "png") as Texture;
            //             staticMesh.PbrTextures.HeightMap = CreateImportButton("Height path", staticMesh.PbrTextures.HeightMap, "png") as Texture;
            //             staticMesh.PbrTextures.EmissiveMap = CreateImportButton("Emissive path", staticMesh.PbrTextures.EmissiveMap, "png") as Texture;
            //             if (staticMesh.transparent) staticMesh.PbrTextures.AlphaMap = CreateImportButton("Alpha path", staticMesh.PbrTextures.AlphaMap, "png") as Texture;
            //         }
            //         else GUILayout.Label("Sorry, this isn't a real option. Choice was an illusion all along.");

            //         GUILayout.Space(20);
            //     }
            // }

            // void OLD_SetMaterialActionUI()
            // {
            //     // Handle material
            //     staticMesh.shouldCreateNewMaterial = GUILayout.Toggle(staticMesh.shouldCreateNewMaterial, "Create new material");
            //     if (!staticMesh.shouldCreateNewMaterial)
            //     {
            //         GUILayout.Label("Sorry, haven't implemented a way to reuse materials yet :(");
            //     }
            //     GUILayout.Space(20);
            // }

            void FinishImportUI()
            {
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
                        StaticMeshImporter staticMeshImporter = new StaticMeshImporter();
                        staticMeshImporter.ImportStaticMesh(staticMesh);
                        Close();
                    }
                }
                GUILayout.Label("");
                GUILayout.EndHorizontal();
            }
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
                    asset = asset.ResetAsset(false);
                    asset.sourcePath = path;
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                Debug.Log("Reset asset!");
                asset = asset.ResetAsset(false);
            }

            GUILayout.EndHorizontal();

            return asset;
        }

        void CreateSeparatorLine()
        {
            GUILayout.Space(spaceSize);
            int i_height = 1;
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(spaceSize);
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


        bool FbxContainsEmbeddedTextures(Model model)
        {
            // if it does, return true.
            return false;
        }
    }
}