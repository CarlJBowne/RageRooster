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
                // Create material asset
                UnityEngine.Material newMaterialAsset;
                switch (i.shader)
                {
                    case Material.Shaders.CelShaderLit:
                        newMaterialAsset = SetupCslMaterial(staticMesh, i);
                        break;
                    case Material.Shaders.UniversalRenderPipelineLit:
                        newMaterialAsset = SetupUrplMaterial(staticMesh, i);
                        break;
                }
                // ensure it's linked to the fbx properly
            }

            // ??? create the prefab? Profit?
            // possibly something with collision?? if that's not automated???????
            SerializeAssetStructure();

            UnityEngine.Material CreateMaterialAssetWithShader(StaticMesh staticMesh, Material i, string shaderPath, out string materialName)
            {
                // some duplicate code here, would be good to simplify (maybe lambdas?)
                UnityEngine.Material newMaterialAsset = new UnityEngine.Material(Shader.Find(shaderPath));
                materialName = staticMesh.assetName + "_" + i.customName;
                string materialFilePath = newSrcFolder + "/" + i.GetPrefix() + materialName + ".mat";
                AssetDatabase.CreateAsset(newMaterialAsset, materialFilePath);
                // The following lines are from a deprecated method, but may be useful for the real implementation:
                    //// string materialFileName = newSrcFolder + "/" + new Material().GetPrefix() + staticMesh.assetName + ".mat";
                    //// AssetDatabase.CreateAsset(materialAsset, materialFileName);
                    //// AssetDatabase.SaveAssets();
                    //// AssetDatabase.Refresh();
                return newMaterialAsset;
            }
            UnityEngine.Material SetupCslMaterial(StaticMesh staticMesh, Material i)
            {
                UnityEngine.Material newMaterialAsset = CreateMaterialAssetWithShader(staticMesh, i, "Shader Graphs/CelShaderLit", out string materialName);

                // Create textures
                CopyFileIntoProject(staticMesh, i.cslSettings.BaseColor, newSrcFolder, materialName);
                CopyNormalMapIntoProject(staticMesh, i.cslSettings.NormalMap, materialName);
                CopyFileIntoProject(staticMesh, i.cslSettings.HeightMap, newSrcFolder, materialName);
                CopyFileIntoProject(staticMesh, i.cslSettings.AO, newSrcFolder, materialName);

                // Link them to the material properly


                return newMaterialAsset;
            }

            UnityEngine.Material SetupUrplMaterial(StaticMesh staticMesh, Material i)
            {
                UnityEngine.Material newMaterialAsset = CreateMaterialAssetWithShader(staticMesh, i, "Universal Render Pipeline/Lit", out string materialName);

                // Create textures
                CopyFileIntoProject(staticMesh, i.urplSettings.DiffuseMap, newSrcFolder, materialName);
                CopyFileIntoProject(staticMesh, i.urplSettings.MetalnessMap, newSrcFolder, materialName);
                CopyFileIntoProject(staticMesh, i.urplSettings.RoughnessMap, newSrcFolder, materialName);
                CopyFileIntoProject(staticMesh, i.urplSettings.SpecularMap, newSrcFolder, materialName);
                CopyNormalMapIntoProject(staticMesh, i.urplSettings.NormalMap, materialName);
                CopyFileIntoProject(staticMesh, i.urplSettings.HeightMap, newSrcFolder, materialName);
                CopyFileIntoProject(staticMesh, i.urplSettings.EmissiveMap, newSrcFolder, materialName);
                CopyFileIntoProject(staticMesh, i.urplSettings.AlphaMap, newSrcFolder, materialName);

                // link them to the material properly.


                return newMaterialAsset;
            }
        }

        private void CopyNormalMapIntoProject(StaticMesh staticMesh, Texture normalMap, string materialName)
        {
            // make sure normal map is set to "normal"
            CopyFileIntoProject(staticMesh, normalMap, newSrcFolder, materialName);
            if (normalMap.AssetExists())
            {
                TextureImporter normalMapImporter = AssetImporter.GetAtPath(normalMap.destinationPath) as TextureImporter;
                normalMapImporter.textureType = TextureImporterType.NormalMap;
                normalMapImporter.SaveAndReimport(); //not sure if this is the right method (the "reimport" part specifically)
            }
        }

        void CopyFileIntoProject(StaticMesh staticMesh, AssetBase asset, string destinationPath, string customName = "")
        {
            if (asset.sourcePath == "No filepath set!") return;

            if (customName == "") customName = staticMesh.assetName;
            asset.destinationPath = destinationPath + "/" + asset.CreateFilename(customName);

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