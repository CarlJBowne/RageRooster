using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening.Plugins.Core.PathCore;
using Newtonsoft.Json.Linq;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// An individual iteration of a File I/O Stream. <br/>
    /// This is used to identify the version of a SaveFile, and to load/save data from/to it.
    /// </summary>
    public abstract class FileVersion
    {
        public static FileVersion Create(Type T, int fileID) => Activator.CreateInstance(T, fileID) as FileVersion;
        public static FileVersion Create(Type T) => Activator.CreateInstance(T) as FileVersion;
        public static FileVersion Create(Type T, params object[] args) => Activator.CreateInstance(T, args) as FileVersion;

        public abstract string version { get; }
        protected virtual List<string> altVersions { get; }
        public string saveRootPath { get; protected set; }
        public int fileID { get; protected set; }
        public JsonFile RootFile { get; protected set; }

        public FileVersion(int fileID)
        {
            this.fileID = fileID;
            Initialize();
        }
        public abstract void Initialize();

        public virtual bool PathExists => RootFile.PathExists;
        public virtual bool FileExists => RootFile.FileExists;
        public virtual bool Exists => RootFile.Exists;
        public virtual bool HasData => RootFile.HasData;
        public virtual bool Valid => RootFile.Valid;
        public virtual FileOpMessage MatchVersion(out string version)
        {
            FileOpMessage fileLoadResult = RootFile.LoadFromFile(out JObject data);
            if (fileLoadResult != FileOpMessage.Success)
            {
                version = $"FILE LOAD ERROR : {fileLoadResult}";
                return fileLoadResult;
            }

            if (data.TryGetValue("Version", out JToken found) ||
                data.TryGetValue("FileVersion", out found) ||
                data.TryGetValue("V", out found) ||
                data.TryGetValue("version", out found) ||
                data.TryGetValue("fileversion", out found) ||
                data.TryGetValue("v", out found) ||
                data.TryGetValue("fileVersion", out found) ||
                data.TryGetValue("schema", out found) ||
                data.TryGetValue("schemaVersion", out found) ||
                data.TryGetValue("Schema", out found) ||
                data.TryGetValue("SchemaVersion", out found) ||
                data.TryGetValue("iteration", out found) ||
                data.TryGetValue("Iteration", out found) ||
                data.TryGetValue("i", out found) ||
                data.TryGetValue("I", out found))
            {
                version = found.ToString();
                return version == this.version ? FileOpMessage.Success
                    : altVersions.Contains(version) ? FileOpMessage.Success 
                    : FileOpMessage.FileVersionMismatch;
            }
            else
            {
                version = "Identification ERROR : No Version Key Found";
                return FileOpMessage.FileMalformed;

            }
        }

        public abstract FileOpMessage LoadFromFile();
        public abstract FileOpMessage SaveToFile();

        public virtual void LoadErrorRevert() { }
        public virtual void SaveErrorRevert() => RootFile.ApplyBackup();

        public virtual void DeleteFile() => RootFile.DeleteFile();

        public abstract void ExportDisplayData(out object result);


        public class Options
        {
            public Type DesiredFileVersion = null;
            public Dictionary<string, Type> AllFileVersions = new();

            public void Initialize(ref FileVersion FileVersion, int fileID)
            {
                FileVersion = Create(DesiredFileVersion, fileID);
                if (FileVersion.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
                {
                    if (failReason == FileOpMessage.FileVersionMismatch && AllFileVersions.ContainsKey(version))
                    {
                        FileVersion = Create(AllFileVersions[version], fileID);
                        if (FileVersion.MatchVersion(out _).IfFail(out failReason))
                        {
                            FileVersion = Create(DesiredFileVersion, fileID);
                        }
                    }
                    else
                    {
                        FileVersion = null;
                        foreach (var pair in AllFileVersions)
                        {
                            if (pair.Value == DesiredFileVersion) continue;
                            FileVersion = Create(pair.Value, fileID);
                            if (!FileVersion.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                            else if (failReason == FileOpMessage.FileVersionMismatch && AllFileVersions.ContainsKey(version))
                            {
                                FileVersion = Create(AllFileVersions[version], fileID);
                                if (!FileVersion.MatchVersion(out _).IfFail(out failReason)) return;
                                else FileVersion = null;
                            }
                        }
                        FileVersion ??= Create(DesiredFileVersion, fileID);
                    }
                }

            }
            public void Initialize(ref FileVersion FileVersion)
            {
                FileVersion = Create(DesiredFileVersion);
                if (FileVersion.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
                {
                    if (failReason == FileOpMessage.FileVersionMismatch)
                    {
                        FileVersion = Create(AllFileVersions[version]);
                        if (FileVersion.MatchVersion(out _).IfFail(out failReason))
                        {
                            FileVersion = Create(DesiredFileVersion);
                        }
                    }
                    else
                    {
                        FileVersion = null;
                        foreach (var pair in AllFileVersions)
                        {
                            if (pair.Value == DesiredFileVersion) continue;
                            FileVersion = Create(pair.Value);
                            if (!FileVersion.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                            else if (failReason == FileOpMessage.FileVersionMismatch)
                            {
                                FileVersion = Create(AllFileVersions[version]);
                                if (!FileVersion.MatchVersion(out _).IfFail(out failReason)) return;
                                else FileVersion = null;
                            }
                        }
                        FileVersion ??= Create(DesiredFileVersion);
                    }
                }

            }
            public void Initialize(ref FileVersion FileVersion, params object[] args)
            {
                FileVersion = Create(DesiredFileVersion, args);
                if (FileVersion.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
                {
                    if (failReason == FileOpMessage.FileVersionMismatch)
                    {
                        FileVersion = Create(AllFileVersions[version], args);
                        if (FileVersion.MatchVersion(out _).IfFail(out failReason))
                        {
                            FileVersion = Create(DesiredFileVersion, args);
                        }
                    }
                    else
                    {
                        FileVersion = null;
                        foreach (var pair in AllFileVersions)
                        {
                            if (pair.Value == DesiredFileVersion) continue;
                            FileVersion = Create(pair.Value, args);
                            if (!FileVersion.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                            else if (failReason == FileOpMessage.FileVersionMismatch)
                            {
                                FileVersion = Create(AllFileVersions[version], args);
                                if (!FileVersion.MatchVersion(out _).IfFail(out failReason)) return;
                                else FileVersion = null;
                            }
                        }
                        FileVersion ??= Create(DesiredFileVersion, args);
                    }
                }

            }
            public void Initialize(ref FileVersion FileVersion, Func<Type, FileVersion> Creator)
            {
                FileVersion = Creator(DesiredFileVersion);
                if (FileVersion.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
                {
                    if (failReason == FileOpMessage.FileVersionMismatch)
                    {
                        FileVersion = Creator(AllFileVersions[version]);
                        if (FileVersion.MatchVersion(out _).IfFail(out failReason))
                        {
                            FileVersion = Creator(DesiredFileVersion);
                        }
                    }
                    else
                    {
                        FileVersion = null;
                        foreach (var pair in AllFileVersions)
                        {
                            if (pair.Value == DesiredFileVersion) continue;
                            FileVersion = Creator(pair.Value);
                            if (!FileVersion.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                            else if (failReason == FileOpMessage.FileVersionMismatch)
                            {
                                FileVersion = Creator(AllFileVersions[version]);
                                if (!FileVersion.MatchVersion(out _).IfFail(out failReason)) return;
                                else FileVersion = null;
                            }
                        }
                        FileVersion ??= Creator(DesiredFileVersion);
                    }
                }

            }
        }
        public class MismatchException : Exception
        {
            public MismatchException(string message) : base(message) { }
        }
    }
}