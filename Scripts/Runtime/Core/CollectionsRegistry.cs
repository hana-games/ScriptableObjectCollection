﻿using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.ScriptableObjectCollections
{
    [DefaultExecutionOrder(-1000)]
    [Preserve]
    public class CollectionsRegistry : ResourceScriptableObjectSingleton<CollectionsRegistry>
    {
        [SerializeField] 
        private List<ScriptableObjectCollection> collections = new List<ScriptableObjectCollection>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LoadOrCreateInstance<CollectionsRegistry>();
        }
        
        public bool IsKnowCollection(ScriptableObjectCollection targetCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection != null && collection.GUID.Equals(targetCollection.GUID, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public void RegisterCollection(ScriptableObjectCollection targetCollection)
        {
            if (collections.Contains(targetCollection))
                return;
            
            collections.Add(targetCollection);
            
            ObjectUtility.SetDirty(this);
        }

        public void UnregisterCollection(ScriptableObjectCollection targetCollection)
        {
            if (!collections.Contains(targetCollection))
                return;

            collections.Remove(targetCollection);
            
            ObjectUtility.SetDirty(this);
        }

        
        public bool TryGetCollectionByName(string targetCollectionName, out ScriptableObjectCollection resultCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection.name.Equals(targetCollectionName, StringComparison.Ordinal))
                {
                    resultCollection = collection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        
        public List<T> GetAllCollectionItemsOfType<T>() where T : ScriptableObjectCollectionItem
        {
            List<T> result = new List<T>();
            List<ScriptableObjectCollectionItem> items = GetAllCollectionItemsOfType(typeof(T));
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObjectCollectionItem scriptableObjectCollectionItem = items[i];
                result.Add((T)scriptableObjectCollectionItem);
            }

            return result;
        }

        public List<ScriptableObjectCollectionItem> GetAllCollectionItemsOfType(Type itemType)
        {
            List<ScriptableObjectCollectionItem> results = new List<ScriptableObjectCollectionItem>();
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (!itemType.IsAssignableFrom(scriptableObjectCollection.GetItemType()))
                    continue;

                results.AddRange(scriptableObjectCollection.Items);
            }

            return results;
        }

        public List<ScriptableObjectCollection> GetCollectionsByItemType<T>() where T : ScriptableObjectCollectionItem
        {
            return GetCollectionsByItemType(typeof(T));
        }

        public List<ScriptableObjectCollection> GetCollectionsByItemType(Type targetCollectionItemType)
        {
            List<ScriptableObjectCollection> result = new List<ScriptableObjectCollection>();

            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (scriptableObjectCollection.GetItemType().IsAssignableFrom(targetCollectionItemType))
                {
                    result.Add(scriptableObjectCollection);
                }
            }

            return result;
        }

        public ScriptableObjectCollection GetCollectionByGUID(string guid)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                if (string.Equals(collections[i].GUID, guid, StringComparison.Ordinal))
                    return collections[i];
            }

            return null;
        }
        
        public bool TryGetCollectionOfType<T>(out T resultCollection) where T: ScriptableObjectCollection
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (scriptableObjectCollection is T collectionT)
                {
                    resultCollection = collectionT;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        public bool TryGetCollectionFromItemType(Type targetType, out ScriptableObjectCollection scriptableObjectCollection)
        {
            List<ScriptableObjectCollection> possibleCollections = new List<ScriptableObjectCollection>();
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection.GetItemType() == targetType || collection.GetItemType().IsAssignableFrom(targetType))
                {
                    possibleCollections.Add(collection);
                }
            }

            if (possibleCollections.Count == 1)
            {
                scriptableObjectCollection = possibleCollections[0];
                return true;
            }

            scriptableObjectCollection = null;
            return false;
        }

        public bool TryGetCollectionFromItemType<TargetType>(out ScriptableObjectCollection<TargetType> scriptableObjectCollection) where TargetType : ScriptableObjectCollectionItem
        {
            if (TryGetCollectionFromItemType(typeof(TargetType), out ScriptableObjectCollection resultCollection))
            {
                scriptableObjectCollection = (ScriptableObjectCollection<TargetType>) resultCollection;
                return true;
            }

            scriptableObjectCollection = null;
            return false;
        }

        public bool TryGetCollectionByGUID(string targetGUID, out ScriptableObjectCollection resultCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (string.Equals(scriptableObjectCollection.GUID, targetGUID, StringComparison.Ordinal))
                {
                    resultCollection = scriptableObjectCollection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }
        
        public bool TryGetCollectionByGUID<T>(string targetGUID, out ScriptableObjectCollection<T> resultCollection) where T : ScriptableObjectCollectionItem
        {
            if (TryGetCollectionByGUID(targetGUID, out ScriptableObjectCollection foundCollection))
            {
                resultCollection = foundCollection as ScriptableObjectCollection<T>;
                return true;
            }

            resultCollection = null;
            return false;
        }
        
        public void ReloadCollections()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            collections.Clear();

            bool changed = false;
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ScriptableObjectCollection>();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                string[] typeGUIDs = AssetDatabase.FindAssets($"t:{type.Name}");

                for (int j = 0; j < typeGUIDs.Length; j++)
                {
                    string typeGUID = typeGUIDs[j];
                    ScriptableObjectCollection collection = 
                        AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(AssetDatabase.GUIDToAssetPath(typeGUID));

                    if (collection == null)
                        continue;

                    if (collections.Contains(collection))
                    {
                        if (!collection.AutomaticallyLoaded)
                        {
                            collections.Remove(collection);
                            changed = true;
                        }

                        continue;
                    }
                    else if (!collection.AutomaticallyLoaded)
                    {
                        continue;
                    }

                    collection.RefreshCollection();
                    collections.Add(collection);
                    changed = true;
                }
            }

            if (changed)
            {
                ObjectUtility.SetDirty(this);
            }

            ValidateGUIDs();
#endif
        }

        public void ValidateGUIDs()
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collectionA = collections[i];
                for (int j = 0; j < collections.Count; j++)
                {
                    ScriptableObjectCollection collectionB = collections[j];

                    if (i == j)
                        continue;

                    if (string.Equals(collectionA.GUID, collectionB.GUID, StringComparison.OrdinalIgnoreCase))
                    {
                        collectionB.GenerateNewGUID();
                        ObjectUtility.SetDirty(collectionB);
                        Debug.LogWarning(
                            $"Found duplicated collection GUID, please regenerate code of collection {collectionB.name}",
                            this
                        );
                    }
                }
                
                collectionA.ValidateGUIDs();
            }
        }

        public void PreBuildProcess()
        {
            RemoveNonAutomaticallyInitializedCollections();
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif
        }

        public void RemoveNonAutomaticallyInitializedCollections()
        {
#if UNITY_EDITOR
            for (int i = collections.Count - 1; i >= 0; i--)
            {
                ScriptableObjectCollection collection = collections[i];

                SerializedObject serializedObject = new SerializedObject(collection);
                SerializedProperty automaticallyLoaded = serializedObject.FindProperty("automaticallyLoaded");
                
                if (automaticallyLoaded.boolValue)
                    continue;

                collections.Remove(collection);
            }
            ObjectUtility.SetDirty(this);
#endif
        }

        public void PostBuildProcess()
        {
            ReloadCollections();
        }

#if UNITY_EDITOR
        public void PrepareForPlayMode()
        {
            for (int i = 0; i < collections.Count; i++)
                collections[i].PrepareForPlayMode();
        }

        public void PrepareForEditorMode()
        {
            for (int i = 0; i < collections.Count; i++)
                collections[i].PrepareForEditorMode();
        }

        public void ClearBadItems()
        {
            for (int i = 0; i < collections.Count; i++)
            {
                collections[i].ClearBadItems();
            }
        }
#endif

        public void Clear()
        {
            for (int i = 0; i < collections.Count; i++)
            {
                collections[i].Clear();
            }
            
            collections.Clear();
        }
    }
}

