using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace ProjectRestructuring
{
    public class DependencyFinder
    {
        List<string> processedPaths = new List<string>(); // this might not be efficient. Alternatively might be brilliant.
        // actually it ended up not being used but I think the comment was funny so I'm leaving it, you can't stop me.

        public List<string> GetPrefabsToSort(string folderToScan) // Scans the project to find appropriate files.
        {
            List<string> prefabs = new List<string>();
            prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { folderToScan }).ToList();
            prefabs = prefabs.Select(AssetDatabase.GUIDToAssetPath).ToList();
            return prefabs;
        }

        public void OrganizeDependencies(string path)
        {
            AssetBase asset = SortAssetTypeByExtension(path); // This might be silly here if it needs to be a prefab anyway... also now it requires a cast for AssembleAssetStructure().
            if (asset is Prefab)
            {
                if (!ValidateAssetAutomationCompatibility(asset)) return;

                AssetStructure assetStructure = AssembleAssetStructure(asset as Prefab);

                if (assetStructure.finalAssetPrefab != null)
                    MoveAssetStructureToNewLocation(assetStructure);
            }
            else
            {
                Debug.Log("Move Asset Test button pressed!" + asset.GetType().ToString());
            }


            bool ValidateAssetAutomationCompatibility(AssetBase asset) // Make sure the asset doesn't have prefabs as dependencies. This tool works mostly for normal models and things.
            {
                foreach (string i in AssetDatabase.GetDependencies(asset.path).ToList<string>())
                {
                    if (i == asset.path) continue;
                    if (PRUtilities.GetFileExtension(i) == ".prefab")
                    {
                        Debug.Log("Asset " + asset.path + " has a prefab as a dependency and cannot be trivially processed. Aborting.");
                        return false;
                    }
                }
                Debug.Log("Asset " + asset.path + " validated, proceeding.");
                return true;
            }
        }
        AssetBase SortAssetTypeByExtension(string path)
        {
            string assetFileExtension = PRUtilities.GetFileExtension(path);
 
            if (Prefab.extensions.Contains(assetFileExtension)) { return new Prefab(path); }
            if (Model.extensions.Contains(assetFileExtension)) { return new Model(path); }
            if (Texture.extensions.Contains(assetFileExtension)) { return new Texture(path); }
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(UnityEngine.Material)) { return new Material(path); }
            return null;
        }
        AssetStructure AssembleAssetStructure(Prefab prefab)
        {
            List<AssetBase> dependencies = GetDependencies(prefab);

            AssetStructure assetStructure = new AssetStructure();
            assetStructure.finalAssetPrefab = prefab;

            SortDependencies(dependencies, assetStructure);

            assetStructure.RenameAssets();
            
            return assetStructure;

            void SortDependencies(List<AssetBase> dependencies, AssetStructure assetStructure) // Actually assembles the asset structure. Currently more or less being bypassed.
            {
                assetStructure.unsortedAssets = new List<AssetBase>(dependencies);
                // INITIAL THOUGHT PROCESS IN CASE I DECIDE TO REVIST THIS:
                // foreach (AssetBase dependency in dependencies)
                // {
                //    if (dependency is Texture) // nesting, yay! :D
                //    {
                //        assetStructure.textures.unsortedTextures.Add(dependency as Texture);
                //    }
                //    else if (dependency is Model)
                //    {
                //        assetStructure.fbxAsset = dependency as Model;
                //    }
                // }
            }
        }
        void MoveAssetStructureToNewLocation(AssetStructure assetStructure)
        {
            // Create temporary destination folder for manual processing.
            string newLocationFolderName = "ProjectRestructureDestinationTEMP";
            string newLocationRoot = "Assets/" + newLocationFolderName;
            if (!AssetDatabase.IsValidFolder(newLocationRoot)) AssetDatabase.CreateFolder("Assets", newLocationFolderName);

            // Create new folder
            string newFolderName = PRUtilities.GetFilenameWithoutExtension(assetStructure.finalAssetPrefab.path);
            string newFolderPath = newLocationRoot + "/" + newFolderName;

            int loop = 0;
            string pathToCheck = newFolderPath;
            while (AssetDatabase.IsValidFolder(pathToCheck))
            {
                loop += 1;
                pathToCheck = newFolderPath;
                pathToCheck += loop.ToString();
            }
            if (loop > 0)
            {
                newFolderName += "_NamingConflict_" + loop.ToString();
                newFolderPath += "_NamingConflict_" + loop.ToString();
            }


            AssetDatabase.CreateFolder(newLocationRoot, newFolderName);

            // Move main asset to new folder
            MoveAsset(assetStructure.finalAssetPrefab, newFolderPath);

            // Create sub /src folder
            string srcFolderName = "src";
            AssetDatabase.CreateFolder(newFolderPath, srcFolderName);
            string srcFolderPath = newFolderPath + "/" + srcFolderName;

            // Move main asset's dependencies into /src folder
            foreach (AssetBase i in assetStructure.unsortedAssets)
            {
                MoveAsset(i, srcFolderPath);
            }
        }

        List<AssetBase> GetDependencies(AssetBase asset)
        {
            List<AssetBase> dependencies = new List<AssetBase>();
            foreach (string i in AssetDatabase.GetDependencies(asset.path).ToList<string>())
            {
                bool isInsideAssetsFolder = i.StartsWith("Assets");
                if (SortAssetTypeByExtension(i) != null && isInsideAssetsFolder)
                {
                    dependencies.Add(SortAssetTypeByExtension(i));
                }

            }
            return dependencies;
        }


        void MoveAsset(AssetBase asset, string newFolderPath)
        {
            string newAssetPath = newFolderPath + "/" + PRUtilities.GetFilename(asset.path);
            AssetDatabase.MoveAsset(asset.path, newAssetPath);
            asset.path = newAssetPath;
        }
    }
}
