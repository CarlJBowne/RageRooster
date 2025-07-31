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
            // take the provided asset name and create folder
            newFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(staticMesh.assetCategory, staticMesh.assetName));
            // create \src folder
            newSrcFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(newFolder, "src"));
            // add assets to \src, standardize asset names in the process
            CopyAssetIntoProject(staticMesh, staticMesh.model, newSrcFolder);

            if (staticMesh.materialTypeIndex == 0) // magic numbers yay
            {
                CopyAssetIntoProject(staticMesh, staticMesh.PbrTextures.DiffuseMap, newSrcFolder);

                CopyAssetIntoProject(staticMesh, staticMesh.PbrTextures.RoughnessMap, newSrcFolder);

                // make sure normal map is set to "normal"
                CopyAssetIntoProject(staticMesh, staticMesh.PbrTextures.NormalMap, newSrcFolder);
                if (staticMesh.PbrTextures.NormalMap.AssetExists())
                // if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(staticMesh.PbrTextures.NormalMap.destinationPath)))
                {
                    TextureImporter normalMapImporter = AssetImporter.GetAtPath(staticMesh.PbrTextures.NormalMap.destinationPath) as TextureImporter;
                    normalMapImporter.textureType = TextureImporterType.NormalMap;
                    normalMapImporter.SaveAndReimport();
                }


                CopyAssetIntoProject(staticMesh, staticMesh.PbrTextures.HeightMap, newSrcFolder);
                CopyAssetIntoProject(staticMesh, staticMesh.PbrTextures.EmissiveMap, newSrcFolder);
            }

            // create material (if it's blank? i think? or maybe the textures shouldn't appear if it's not... also the "update" functionality)
            CreateMaterialAssetInProject(staticMesh);

            // apply textures to material

            // ??? create the prefab? Profit?
            // possibly something with collision?? if that's not automated???????
        }
        void CopyAssetIntoProject(StaticMesh staticMesh, AssetBase asset, string destinationPath)
        {
            if (asset.sourcePath == "No filepath set!") return;

            asset.destinationPath = destinationPath + "/" + asset.CreateFilename(staticMesh.assetName);

            // Copy file
            File.Copy(asset.sourcePath, asset.destinationPath, overwrite: true); // THIS is altering the imported FBX structure WHEN USING AN EXISTING ASSET AS A SOURCE PATH

            // Refresh asset database
            AssetDatabase.ImportAsset(asset.destinationPath);
            Debug.Log("Imported: " + asset.destinationPath);
        }

        void CreateMaterialAssetInProject(StaticMesh staticMesh)
        {
            UnityEngine.Material materialAsset = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));

            AssignTextureToMaterial(staticMesh.PbrTextures.DiffuseMap, "_BaseMap");
            // AssignTextureToMaterial(staticMesh.PbrTextures.MetalnessMap, "")

            string materialFileName = newSrcFolder + "/" + new Material().GetPrefix() + staticMesh.assetName + ".mat";
            AssetDatabase.CreateAsset(materialAsset, materialFileName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            void AssignTextureToMaterial(Texture texture, string shaderTextureName)
            {
                if (texture.AssetExists())
                    materialAsset.SetTexture(shaderTextureName, AssetDatabase.LoadAssetAtPath<Texture2D>(texture.destinationPath));
            }
        }
    }
}