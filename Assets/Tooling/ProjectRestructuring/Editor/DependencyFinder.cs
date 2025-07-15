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
        public void OrganizeDependencies(string path)
        {
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
        AssetBase SortAssetTypeByExtension(string path)
        {
            string assetFileExtension = PRUtilities.GetFileExtension(path);
 
            if (Prefab.extensions.Contains(assetFileExtension)) { return new Prefab(path); }
            if (Model.extensions.Contains(assetFileExtension)) { return new Model(path); }
            if (Texture.extensions.Contains(assetFileExtension)) { return new Texture(path); }
            //if (Material.extensions.Contains(assetFileExtension)) { return new Material(path); } // This one needs to be more bulletproof. ".asset" is used for a LOT.
            return null;
        }
        bool ValidateAssetAutomationCompatibility(AssetBase asset) // Make sure the asset doesn't have prefabs as dependencies. This tool works mostly for normal models and things.
        {
            //List<string> dependencies = GetDependencies(asset);
            
            // Temp early return.
            return true;
        }
        AssetStructure AssembleAssetStructure(Prefab prefab)
        {
            List<AssetBase> dependencies = GetDependencies(prefab);

            AssetStructure assetStructure = new AssetStructure();
            assetStructure.finalAssetPrefab = prefab;

   

            assetStructure.RenameAssets();
            
            return assetStructure;

            void SortDependencies()
            {
                
            }
        }
        void MoveAssetStructureToNewLocation(AssetStructure assetStructure)
        {
            assetStructure.DebugAssetStructure();
        }
        List<AssetBase> GetDependencies(AssetBase asset)
        {
            List<AssetBase> dependencies = new List<AssetBase>();
            foreach (string i in AssetDatabase.GetDependencies(asset.path).ToList<string>())
            {
                dependencies.Add(SortAssetTypeByExtension(i));
            }
            return dependencies;
        }
    }
}
