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
            // Take the provided asset name and create folder
            newFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(staticMesh.assetCategory, staticMesh.assetName));
            newSrcFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(newFolder, "src")); // create \src folder

            // Add assets to \src, standardize asset names in the process
            CopyFileIntoProject(staticMesh, staticMesh.model, newSrcFolder); // Copy FBX into \src

            // Set ModelImporter settings to allow using external material assets instead of internally generated subassets
            ModelImporter modelImporter = AssetImporter.GetAtPath(staticMesh.model.destinationPath) as ModelImporter;

            foreach (Material i in staticMesh.materials)
            {
                // Create material asset
                UnityEngine.Material newMaterialAsset = null;
                switch (i.shader)
                {
                    case Material.Shaders.CelShaderLit:
                        newMaterialAsset = SetupCslMaterial(staticMesh, i);
                        break;
                    case Material.Shaders.UniversalRenderPipelineLit:
                        newMaterialAsset = SetupUrplMaterial(staticMesh, i);
                        break;
                }

                // Ensure it's linked to the fbx properly
                AssetImporter.SourceAssetIdentifier id = new AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material), i.fbxMaterialSlotName);
                modelImporter.AddRemap(id, newMaterialAsset);
                modelImporter.SaveAndReimport();
            }

            // Create the prefab
            GameObject modelGameObject = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(staticMesh.model.destinationPath)) as GameObject;
            staticMesh.prefab.destinationPath = newFolder + "/" + staticMesh.prefab.CreateFilename(staticMesh.assetName) + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(modelGameObject, staticMesh.prefab.destinationPath);
            Object.DestroyImmediate(modelGameObject);

            // TODO: possibly something with collision here?

            // Create serialization for easier future automation
            SerializeAssetStructure();

            UnityEngine.Material CreateMaterialAssetWithShader(StaticMesh staticMesh, Material i, string shaderPath)
            {
                UnityEngine.Material newMaterialAsset = new UnityEngine.Material(Shader.Find(shaderPath));
                string materialFilePath = newSrcFolder + "/" + i.GetPrefix() + i.GetMaterialName(staticMesh) + ".mat";
                AssetDatabase.CreateAsset(newMaterialAsset, materialFilePath);
                return newMaterialAsset;
            }
            UnityEngine.Material SetupCslMaterial(StaticMesh staticMesh, Material i)
            {
                var sm = staticMesh; // shorter :D
                UnityEngine.Material newMaterialAsset = CreateMaterialAssetWithShader(sm, i, "Shader Graphs/CelShaderLit");

                // Create textures and link them to the material properly
                CopyTextureIntoProjectAndAssignToMaterial(sm, i.cslSettings.BaseColor, i, newMaterialAsset, "_Texture");
                CopyNormalMapIntoProjectAndAssignToMaterial(sm, i.cslSettings.NormalMap, i, newMaterialAsset, "_Normal");
                CopyTextureIntoProjectAndAssignToMaterial(sm, i.cslSettings.HeightMap, i, newMaterialAsset, "_Height");
                CopyTextureIntoProjectAndAssignToMaterial(sm, i.cslSettings.AO, i, newMaterialAsset, "_AO");

                return newMaterialAsset;
            }

            // Not yet implemented, old draft.
            UnityEngine.Material SetupUrplMaterial(StaticMesh staticMesh, Material i)
            {
                return SetupCslMaterial(staticMesh, i);
                // var sm = staticMesh; // shorter :D
                // UnityEngine.Material newMaterialAsset = CreateMaterialAssetWithShader(sm, i, "Universal Render Pipeline/Lit");

                // // Create textures and link them to the material properly.
                // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.DiffuseMap, i, newMaterialAsset, "_BaseMap");
                // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.MetalnessMap, i, newMaterialAsset, "_Metallic"); // might be _MetallicGlossMap
                // // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.RoughnessMap, i, newMaterialAsset, "_Smoothness"); // TODO: Invert for Smoothness if a roughness map is given. also make sure it's packaged in the right texture
                // // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.SpecularMap, i, newMaterialAsset, "");
                // CopyNormalMapIntoProjectAndAssignToMaterial(sm, i.urplSettings.NormalMap, i, newMaterialAsset, "_BumpMap");
                // newMaterialAsset.EnableKeyword("_NORMALMAP");
                // // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.HeightMap, i, newMaterialAsset, "");
                // // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.EmissiveMap, i, newMaterialAsset, "");
                // // CopyTextureIntoProjectAndAssignToMaterial(sm, i.urplSettings.AlphaMap, i, newMaterialAsset, "");


                // return newMaterialAsset;
            }
        }

        private void CopyNormalMapIntoProjectAndAssignToMaterial(StaticMesh staticMesh, Texture normalMap, Material i, UnityEngine.Material newMaterialAsset, string textureSlotName)
        {
            CopyTextureIntoProjectAndAssignToMaterial(staticMesh, normalMap, i, newMaterialAsset, textureSlotName);

            if (!normalMap.AssetExists()) return;

            TextureImporter normalMapImporter = AssetImporter.GetAtPath(normalMap.destinationPath) as TextureImporter;
            normalMapImporter.textureType = TextureImporterType.NormalMap;
            normalMapImporter.SaveAndReimport();
        }
        void CopyTextureIntoProjectAndAssignToMaterial(StaticMesh staticMesh, Texture texture, Material i, UnityEngine.Material newMaterialAsset, string textureSlotName)
        {
            CopyFileIntoProject(staticMesh, texture, newSrcFolder, i.GetMaterialName(staticMesh));
            UnityEngine.Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(texture.destinationPath);
            newMaterialAsset.SetTexture(textureSlotName, texture2D);
        }
        void CopyFileIntoProject(StaticMesh staticMesh, AssetBase asset, string destinationPath, string customName = "")
        {
            if (asset.sourcePath == "No filepath set!") return;

            if (customName == "") customName = staticMesh.assetName;
            asset.destinationPath = destinationPath + "/" + asset.CreateFilename(customName);

            // Copy file
            File.Copy(asset.sourcePath, asset.destinationPath, overwrite: true);

            // Refresh asset database
            AssetDatabase.ImportAsset(asset.destinationPath);
            Debug.Log("Imported: " + asset.destinationPath);
        }
        void SerializeAssetStructure()
        {
            // TODO: Create Json(?) file in /src that helps serialize the Asset Data Structure for future tooling.
        }
    }
}