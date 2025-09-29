using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetImportPipeline
{
    public static class Utilities
    {
        public static string GetFilename(string path)
        {
            return System.IO.Path.GetFileName(path);
        }

        public static string GetFilenameWithoutExtension(string path)
        {
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static string GetFileExtension(string path)
        {
            return System.IO.Path.GetExtension(path);
        }

        public static string GetPathWithoutFilename(string path)
        {
            return System.IO.Path.GetDirectoryName(path);
        }
    }
}
