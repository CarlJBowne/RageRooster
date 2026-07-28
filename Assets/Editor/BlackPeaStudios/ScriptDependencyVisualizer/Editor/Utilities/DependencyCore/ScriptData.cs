using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;

namespace BPS
{
    public class ScriptData
    {
        public DependencyAnalytics m_dependencyAnalytics;

        public List<Scripts> m_scripts;
        List<string> _namespaces;
        List<string> _typeNames;
        string _path;


        public ScriptData(bool _monosOnly, List<string> namespaces, List<string> typeNames)
        {
            _namespaces = namespaces;
            _typeNames = typeNames;

            List<Type> primaryTypes = GetObjectTypes();

            CreateScriptsList(ref primaryTypes);

            DoResearchOnTypes();
            if (!_monosOnly)
            {
                List<Type> secondaryTypeList = new();
                List<Scripts> secondaryScriptsList = new();
                int totalScriptsAdded = -1;
                do
                {
                    CheckSecondaryDependencies(ref primaryTypes, ref secondaryTypeList);

                    for (int i = 0; i < secondaryTypeList.Count; i++)
                    {
                        if (m_scripts.Find(x => x.m_type == secondaryTypeList[i]) == null)
                        {
                            Scripts script = new Scripts(secondaryTypeList[i]);
                            secondaryScriptsList.Add(script);
                        }
                    }
                    DoResearchOnTypes(ref secondaryScriptsList);

                    AddToScriptsList(ref secondaryScriptsList, out totalScriptsAdded);

                    DoResearchOnTypes();


                } while (totalScriptsAdded != 0);
            }
        }

        List<Type> GetObjectTypes()
        {
            Assembly rootAssembly = typeof(PlayerController).Assembly;
            List<Type> result = rootAssembly.GetTypes().ToList();

            //foreach (MonoBehaviour mono in Resources.FindObjectsOfTypeAll<MonoBehaviour>()) 
            //    if (!result.Contains(mono.GetType())) 
            //        result.Add(mono.GetType());
            //foreach (ScriptableObject so in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            //    if (!result.Contains(so.GetType())) 
            //        result.Add(so.GetType());



            for (int i = 0; i < result.Count; i++)
            { 
                if (result[i].FullName.Contains('+'))
                {
                    result.RemoveAt(i);
                    i--;
                    continue;
                }

                if(!typeof(MonoBehaviour).IsAssignableFrom(result[i])
                    && !typeof(ScriptableObject).IsAssignableFrom(result[i])
                    && !typeof(Polymorph).IsAssignableFrom(result[i]))
                {
                    result.RemoveAt(i);
                    i--;
                    continue;
                }

                if(typeof(UnityEditor.Editor).IsAssignableFrom(result[i]) 
                    ||  typeof(UnityEditor.PropertyDrawer).IsAssignableFrom(result[i])
                    //|| typeof(PlayerStateBehavior).IsAssignableFrom(result[i])
                    )
                {
                    result.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!string.IsNullOrEmpty(result[i].Namespace))
                {
                    if (result[i].Namespace.StartsWith("UnityEngine") ||
                    result[i].Namespace.StartsWith("UnityEditor") ||
                    result[i].Namespace.StartsWith("Unity") ||
                    result[i].Namespace.StartsWith("Unity.VisualScripting") ||
                    result[i].Namespace.StartsWith("FMODUnity") ||
                    result[i].Namespace.StartsWith("RealtimeCSG") ||
                    result[i].Namespace.StartsWith("TMPro"))
                    {
                        result.RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                if(result[i].GetCustomAttribute(typeof(System.ObsoleteAttribute)) != null)
                {
                    result.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            return result;
        }

        void AddToScriptsList(ref List<Scripts> secondary, out int total)
        {
            total = 0;
            foreach (Scripts s in secondary)
            {
                m_scripts.Add(s);
                total++;
            }
            secondary.Clear();
        }

        void CheckSecondaryDependencies(ref List<Type> monos, ref List<Type> secondaryList)
        {
            foreach (Scripts s in m_scripts)
            {
                CheckFieldInfo(ref monos, ref secondaryList, ref s.m_privateInstanceFields);
                CheckFieldInfo(ref monos, ref secondaryList, ref s.m_privateStaticFields);
                CheckFieldInfo(ref monos, ref secondaryList, ref s.m_publicInstanceFields);
                CheckFieldInfo(ref monos, ref secondaryList, ref s.m_publicStaticFields);
            }
            secondaryList = secondaryList.Distinct<Type>().ToList<Type>();
        }

        void CheckFieldInfo(ref List<Type> monos, ref List<Type> secondaryList, ref FieldInfo[] fieldInfo)
        {
            foreach (FieldInfo f in fieldInfo)
            {
                bool namespaceResult = true;
                foreach (string s in _namespaces)
                {
                    namespaceResult &= f.FieldType.Namespace != s;
                }
                bool nameResult = true;
                foreach (string s in _typeNames)
                {
                    nameResult &= f.FieldType.Name != s;
                }
                if (namespaceResult && nameResult)
                    if (!IsInList(ref monos, f))
                    {
                        if (f.FieldType.IsGenericType)
                        {// ??
                         //foreach(Type t in f.FieldType.GetGenericArguments ())
                         //	secondaryList.Add(t);
                            secondaryList.Add(f.FieldType.GetGenericArguments()[0]);
                            return;
                        }
                        if (f.FieldType.IsArray)
                        {
                            secondaryList.Add(f.FieldType.GetElementType());
                            return;
                        }

                        secondaryList.Add(f.FieldType);
                    }
            }

        }

        void CreateScriptsList(ref List<Type> monos)
        {
            m_scripts = new List<Scripts>();
            for (int i = 0; i < monos.Count; i++)
            {
                Scripts script = new Scripts(monos[i]);
                bool namespaceResult = true;
                foreach (string s in _namespaces)
                {
                    namespaceResult &= script.m_type.Namespace != s;
                }
                bool nameResult = true;
                foreach (string s in _typeNames)
                {
                    nameResult &= script.m_type.Name != s;
                }
                if (namespaceResult && nameResult)
                {

                    m_scripts.Add(script);
                }

            }
        }

        bool IsInList(ref List<Type> list, FieldInfo f)
        {
            foreach (Type t in list)
            {

                if (t == f.FieldType)
                    return true;
            }
            return false;
        }

        bool IsInList(ref List<Type> list, Type t)
        {
            foreach (Type t0 in list)
            {

                if (t0 == t)
                    return true;
            }
            return false;
        }

        void DoResearchOnTypes()
        {
            for (int i = 0; i < m_scripts.Count; i++)
            {
                m_scripts[i].DoResearch();
            }
        }

        void DoResearchOnTypes(ref List<Scripts> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].DoResearch();
            }
        }

        void PrintList(List<Scripts> list)
        {
            foreach (Scripts s in list)
            {
                Debug.Log(s.m_type.Name);
            }
        }
    }

    public class Scripts
    {
        public Type m_type;

        public FieldInfo[] m_publicInstanceFields, m_publicStaticFields, m_privateInstanceFields, m_privateStaticFields;
        public Type[] m_publicInstanceTypes, m_publicStaticTypes, m_privateInstanceTypes, m_privateStaticTypes;
        bool _openable = false;
        public bool openable { get { return _openable; } }

        public Scripts(Type type)
        {
            m_type = type;
            _openable = typeof(MonoBehaviour).IsAssignableFrom(m_type) || typeof(ScriptableObject).IsAssignableFrom(m_type);
        }

        public void DoResearch()
        {
            FindFields(BindingFlags.Instance | BindingFlags.Public, ref m_publicInstanceFields, ref m_publicInstanceTypes);
            FindFields(BindingFlags.Instance | BindingFlags.NonPublic, ref m_privateInstanceFields, ref m_privateInstanceTypes);
            FindFields(BindingFlags.Static | BindingFlags.Public, ref m_publicStaticFields, ref m_publicStaticTypes);
            FindFields(BindingFlags.Static | BindingFlags.NonPublic, ref m_privateStaticFields, ref m_privateStaticTypes);
        }

        void FindFields(BindingFlags bindingFlag, ref FieldInfo[] fields, ref Type[] types)
        {
            fields = m_type.GetFields(bindingFlag);
            types = new Type[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType.IsGenericType)
                {
                    types[i] = fields[i].FieldType.GetGenericArguments()[0];
                }
                else if (fields[i].FieldType.IsArray)
                {
                    types[i] = fields[i].FieldType.GetElementType();
                }
                else
                    types[i] = fields[i].FieldType;
            }
        }

        public void PrintDependencies()
        {
            foreach (FieldInfo f in m_publicInstanceFields)
            {
                Debug.Log(m_type.Name + " ---> " + f.FieldType.Name);
            }
        }
    }
}