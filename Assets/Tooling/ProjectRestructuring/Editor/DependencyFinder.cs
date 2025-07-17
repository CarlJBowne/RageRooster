using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using ProjectRestructuring;
using System.Linq;

namespace ProjectRestructuring
{
    public class DependencyFinder
    {
        List<string> processedPaths = new List<string>(); // this might not be efficient. Alternatively might be brilliant.

        public List<string> GetPrefabsToSort(string folderToScan) // Scans the project to find appropriate files.
        {
            List<string> prefabs = new List<string>();
            prefabs = AssetDatabase.FindAssets("t:Prefab", new[] {folderToScan}).ToList();
            prefabs = prefabs.Select(AssetDatabase.GUIDToAssetPath).ToList();
            return prefabs;
        }

        public void OrganizeDependencies(string path)
        {
            // Debug.Log(path);
            // return;

            AssetBase asset = SortAssetTypeByExtension(path); // This might be silly here if it needs to be a prefab anyway... also now it requires a cast for AssembleAssetStructure().

            if (asset is Prefab)
            {
                if (!ValidateAssetAutomationCompatibility(asset)) return;

                AssetStructure assetStructure = AssembleAssetStructure(asset as Prefab);

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
                    if (AssetDatabase.GetMainAssetTypeAtPath(i) == typeof(Prefab))
                    {
                        Debug.Log("Asset " + asset.path + " has a prefab as a dependency and cannot be trivially processed. Aborting.");
                        return false;
                    }
                }

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
            //if (Material.extensions.Contains(assetFileExtension)) { return new Material(path); } // This one needs to be more bulletproof. ".asset" is used for a LOT.
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
            string newLocationRoot = "Assets/Tooling/ProjectRestructuring/TestData/DestinationLocation";

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
                newFolderName += loop.ToString();
                newFolderPath += loop.ToString();
            }


            AssetDatabase.CreateFolder(newLocationRoot, newFolderName);

            return;


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

            //assetStructure.DebugAssetStructure();

            void OldTestImplementation()
            {
                // OLD TEST IMPLEMENTATION ~ Version 0.0
                //string newLocation = "Assets/Tooling/ProjectRestructuring/TestData/DestinationLocation";

                //newLocation = AssetDatabase.CreateFolder(newLocation, System.IO.Path.GetFileNameWithoutExtension(path));
                //Debug.Log("New folder is " + newLocation);

                ////AssetDatabase.MoveAsset(path, newLocation + GetFilename(path));

                //newLocation = AssetDatabase.CreateFolder(newLocation, "src");


                //foreach (string assetPath in AssetDatabase.GetDependencies(path))
                //{
                //    string newPath = newLocation + GetFilename(assetPath);
                //    //AssetDatabase.MoveAsset(assetPath, newPath);
                //    //Debug.Log(newPath);
                //}
            }
        }

        List<AssetBase> GetDependencies(AssetBase asset)
        {
            List<AssetBase> dependencies = new List<AssetBase>();
            foreach (string i in AssetDatabase.GetDependencies(asset.path).ToList<string>())
            {
                if (SortAssetTypeByExtension(i) != null)
                {
                    dependencies.Add(SortAssetTypeByExtension(i));
                }

            }
            return dependencies;
        }


        void MoveAsset(AssetBase asset, string newFolderPath)
        {
            string newAssetPath = newFolderPath + "/" + PRUtilities.GetFilename(asset.path);
            // AssetDatabase.CopyAsset(asset.path, newAssetPath);
            AssetDatabase.MoveAsset(asset.path, newAssetPath);
        }
    }
}
