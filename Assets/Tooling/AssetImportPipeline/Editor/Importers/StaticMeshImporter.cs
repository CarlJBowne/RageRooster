using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    abstract public class Importer{}
    public class StaticMeshImporter : Importer
    {
        string newFolder;
        string newSrcFolder;
        
        public void ImportStaticMesh(StaticMesh staticMesh)
        {
            // take the provided asset name and create folder
            newFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(staticMesh.assetCategory, staticMesh.assetName));
            newSrcFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(newFolder, "src")); // create \src folder

            // add assets to \src, standardize asset names in the process
            CopyFileIntoProject(staticMesh, staticMesh.model, newSrcFolder); // Copy FBX into \src

            ModelImporter modelImporter = AssetImporter.GetAtPath(staticMesh.model.destinationPath) as ModelImporter;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelImporter.materialLocation = ModelImporterMaterialLocation.External; // might be legacy maybe avoid...

            foreach (Material i in staticMesh.materials)
            {
                // Create material asset, ensure it's linked to the fbx properly.
                UnityEngine.Material newMaterialAsset;

                switch (i.shader)
                {
                    case Material.Shaders.CelShaderLit:
                        newMaterialAsset = SetupCelShaderLitMaterial();
                        break;
                    case Material.Shaders.UniversalRenderPipelineLit:
                        newMaterialAsset = SetupUrplMaterial(staticMesh, i);
                        break;
                }
            }

            // ??? create the prefab? Profit?
            // possibly something with collision?? if that's not automated???????
            SerializeAssetStructure();



            UnityEngine.Material SetupCelShaderLitMaterial()
            {
                UnityEngine.Material newMaterialAsset = new UnityEngine.Material(Shader.Find("Shader Graphs/CelShaderLit"));
                return newMaterialAsset;
            }

            UnityEngine.Material SetupUrplMaterial(StaticMesh staticMesh, Material i)
            {
                UnityEngine.Material newMaterialAsset = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));
                string materialFilePath = newSrcFolder + "/" + i.GetPrefix() + staticMesh.assetName + "_" + i.customName + ".mat"; // not a great solution for readability... CustomName will need a LOT of formatting to work with this.
                Debug.Log(materialFilePath);
                // The following lines are from a deprecated method, but may be useful for the real implementation:
                //// string materialFileName = newSrcFolder + "/" + new Material().GetPrefix() + staticMesh.assetName + ".mat";
                //// AssetDatabase.CreateAsset(materialAsset, materialFileName);
                //// AssetDatabase.SaveAssets();
                //// AssetDatabase.Refresh();

                // Create textures
                CopyFileIntoProject(staticMesh, i.urplSettings.DiffuseMap, newSrcFolder);
                CopyFileIntoProject(staticMesh, i.urplSettings.MetalnessMap, newSrcFolder);
                CopyFileIntoProject(staticMesh, i.urplSettings.RoughnessMap, newSrcFolder);
                CopyFileIntoProject(staticMesh, i.urplSettings.SpecularMap, newSrcFolder);
                CopyNormalMapIntoProject(staticMesh, i.urplSettings.NormalMap);
                CopyFileIntoProject(staticMesh, i.urplSettings.HeightMap, newSrcFolder);
                CopyFileIntoProject(staticMesh, i.urplSettings.EmissiveMap, newSrcFolder);
                CopyFileIntoProject(staticMesh, i.urplSettings.AlphaMap, newSrcFolder);

                // link them to the material properly.

                return newMaterialAsset;
            }
        }

        private void CopyNormalMapIntoProject(StaticMesh staticMesh, Texture normalMap)
        {
            // make sure normal map is set to "normal"
            CopyFileIntoProject(staticMesh, normalMap, newSrcFolder);
            if (normalMap.AssetExists())
            {
                TextureImporter normalMapImporter = AssetImporter.GetAtPath(normalMap.destinationPath) as TextureImporter;
                normalMapImporter.textureType = TextureImporterType.NormalMap;
                normalMapImporter.SaveAndReimport(); //not sure if this is the right method (the "reimport" part specifically)
            }
        }

        void CopyFileIntoProject(StaticMesh staticMesh, AssetBase asset, string destinationPath)
        {
            if (asset.sourcePath == "No filepath set!") return;

            asset.destinationPath = destinationPath + "/" + asset.CreateFilename(staticMesh.assetName);

            // Copy file
            File.Copy(asset.sourcePath, asset.destinationPath, overwrite: true); // This alters the imported FBX structure WHEN USING AN EXISTING ASSET AS A SOURCE PATH

            // Refresh asset database
            AssetDatabase.ImportAsset(asset.destinationPath);
            Debug.Log("Imported: " + asset.destinationPath);
        }

        void SerializeAssetStructure()
        {
            // Create Json(?) file in /src that helps serialize the Asset Data Structure for future tooling.
        }
    }
}