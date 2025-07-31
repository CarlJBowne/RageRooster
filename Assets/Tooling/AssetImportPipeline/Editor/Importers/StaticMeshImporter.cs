using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public class StaticMeshImporter
    {
        string newFolder;
        string newSrcFolder;
        public void ImportStaticMesh(StaticMesh staticMesh)
        {
            // TODO Maybe make a .json file for easier serialization for future updates?


            // take the provided asset name and create folder
            newFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(staticMesh.assetCategory, staticMesh.assetName));
            // create \src folder
            newSrcFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(newFolder, "src"));
            // add assets to \src, standardize asset names in the process
            CopyAssetIntoProject(staticMesh, staticMesh.model, newSrcFolder);

            foreach (Material i in staticMesh.materials)
            {
                // Create material asset, ensure it's linked to the fbx properly.
                UnityEngine.Material materialAsset;

                switch (i.shader)
                {
                    case Material.Shaders.CelShaderLit:
                        materialAsset = new UnityEngine.Material(Shader.Find("Shader Graphs/CelShaderLit"));
                        break;
                    case Material.Shaders.UniversalRenderPipelineLit:
                        materialAsset = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));
                        // The following lines are from a deprecated method, but may be useful for the real implementation:
                            // string materialFileName = newSrcFolder + "/" + new Material().GetPrefix() + staticMesh.assetName + ".mat";
                            // AssetDatabase.CreateAsset(materialAsset, materialFileName);
                            // AssetDatabase.SaveAssets();
                            // AssetDatabase.Refresh();

                        // Create textures
                        CopyAssetIntoProject(staticMesh, i.urplSettings.DiffuseMap, newSrcFolder);
                        CopyAssetIntoProject(staticMesh, i.urplSettings.RoughnessMap, newSrcFolder);

                        // make sure normal map is set to "normal"
                        CopyAssetIntoProject(staticMesh, i.urplSettings.NormalMap, newSrcFolder);
                        if (i.urplSettings.NormalMap.AssetExists())
                        {
                            TextureImporter normalMapImporter = AssetImporter.GetAtPath(i.urplSettings.NormalMap.destinationPath) as TextureImporter;
                            normalMapImporter.textureType = TextureImporterType.NormalMap;
                            normalMapImporter.SaveAndReimport(); //not sure if this is the right method (the "reimport" part specifically)
                        }

                        CopyAssetIntoProject(staticMesh, i.urplSettings.HeightMap, newSrcFolder);
                        CopyAssetIntoProject(staticMesh, i.urplSettings.EmissiveMap, newSrcFolder);

                        // link them to the material properly.

                        break;
                }
            }

            // ??? create the prefab? Profit?
            // possibly something with collision?? if that's not automated???????
            CreateJson();
        }

        void CopyAssetIntoProject(StaticMesh staticMesh, AssetBase asset, string destinationPath)
        {
            if (asset.sourcePath == "No filepath set!") return;

            asset.destinationPath = destinationPath + "/" + asset.CreateFilename(staticMesh.assetName);

            // Copy file
            File.Copy(asset.sourcePath, asset.destinationPath, overwrite: true); // This alters the imported FBX structure WHEN USING AN EXISTING ASSET AS A SOURCE PATH

            // Refresh asset database
            AssetDatabase.ImportAsset(asset.destinationPath);
            Debug.Log("Imported: " + asset.destinationPath);
        }
        
        void CreateJson()
        {
            // Create file in /src that helps serialize the Asset Data Structure for future tooling.
        }
    }
}