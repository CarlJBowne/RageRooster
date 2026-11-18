using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// An abstract class representing an individual SaveFile of indeterminant Stream Version. It will attempt to load the most recent version of the SaveFile, and if it fails, it will attempt to load the next most recent version, and so on until it either finds a valid SaveFile or determines that no SaveFile exists with the appropriate parameters.
    /// </summary>
    public abstract class SaveFile
    {

        public object[] parameters { get; protected set; }

        public abstract Dictionary<string, Type> AllFileVersions { get; }
        public virtual FileVersion Create(Type T) => FileVersion.Create(T, parameters);
        public Type DesiredFileVersion => AllFileVersions.First().Value;

        public FileVersion ActiveFileVersion => active;
        private FileVersion active;


        public SaveFile(params object[] newParameters)
        {
            EstablishParams(newParameters);
            active = Create(DesiredFileVersion);
            if (active.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
            {
                if (failReason == FileOpMessage.FileVersionMismatch)
                {
                    active = Create(AllFileVersions[version]);
                    if (active.MatchVersion(out _).IfFail(out failReason))
                    {
                        active = Create(DesiredFileVersion);
                    }
                }
                else
                {
                    active = null;
                    foreach (var pair in AllFileVersions)
                    {
                        if (pair.Value == DesiredFileVersion) continue;
                        active = Create(pair.Value);
                        if (!active.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                        else if (failReason == FileOpMessage.FileVersionMismatch)
                        {
                            active = Create(AllFileVersions[version]);
                            if (!active.MatchVersion(out _).IfFail(out failReason)) return;
                            else active = null;
                        }
                    }
                    active ??= Create(DesiredFileVersion);
                }
            }

        }

        public void LoadFromFile()
        {
            if (!active.Valid) return;
            PreLoad();
            active.LoadFromFile();
            PostLoad();
        }
        public void SaveToFile()
        {
            PreSave();
            if (active.GetType() != DesiredFileVersion)
            {
                active.DeleteFile();
                active = Create(DesiredFileVersion);
            }
            active.SaveToFile();
            PostSave();
        }

        public void ExportDisplayData<T>(out T result)
        {
            if (active == null) throw new Exception($"SaveFile with params {parameters} has no FileVersion assigned. Cannot export Menu Display Data.");
            active.ExportDisplayData(out object resultObj);
            if (resultObj is not T) throw new Exception($"SaveFile has FileVersion {active.GetType().Name} which returned an object of type {resultObj.GetType().Name}, which cannot be cast to the requested type {typeof(T).Name}");
            result = (T)resultObj;
        }

        public Action PreLoad;
        public Action PreSave;
        public Action PostLoad;
        public Action PostSave;

        public virtual void EstablishParams(params object[] parameters) => this.parameters = parameters;
    }
}
