using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionItem : ScriptableObject, IComparable<ScriptableObjectCollectionItem>
    {
        [SerializeField, HideInInspector]
        private string guid;
        public string GUID
        {
            get
            {
                SyncGUID();
                return guid;
            }
        }

        [SerializeField, HideInInspector]
        private string collectionGUID;
        
        private ScriptableObjectCollection cachedScriptableObjectCollection;
        public ScriptableObjectCollection Collection
        {
            get
            {
                if (cachedScriptableObjectCollection == null)
                {
                    if (!string.IsNullOrEmpty(collectionGUID))
                    {
                        cachedScriptableObjectCollection = CollectionsRegistry.Instance.GetCollectionByGUID(collectionGUID);
                    }
                    else
                    {
                        CollectionsRegistry.Instance.TryGetCollectionFromItemType(GetType(), out cachedScriptableObjectCollection);
                        if (cachedScriptableObjectCollection != null)
                        {
                            Debug.Log($"Collection Item ({this.name}) was missing the Collection GUID, assigned to {cachedScriptableObjectCollection.name}");
                            collectionGUID = cachedScriptableObjectCollection.GUID;
                            ObjectUtility.SetDirty(this);
                        }
                    }
                }
                
                return cachedScriptableObjectCollection;
            }
        }

        public bool IsReference()
        {
            return TryGetReference(out _);
        }
        
        public virtual bool TryGetReference(out ScriptableObjectReferenceItem reference)
        {
            reference = null;
            return false;
        }

        public void SetCollection(ScriptableObjectCollection collection)
        {
            cachedScriptableObjectCollection = collection;
            collectionGUID = cachedScriptableObjectCollection.GUID;
            ObjectUtility.SetDirty(this);
        }

        public int CompareTo(ScriptableObjectCollectionItem other)
        {
            return string.Compare(GUID, other.GUID, StringComparison.Ordinal);
        }

        public override bool Equals(object o)
        {
            ScriptableObjectCollectionItem other = o as ScriptableObjectCollectionItem;
            if (other == null)
                return false;

            return ReferenceEquals(this, other);
        }

        public static bool operator==(ScriptableObjectCollectionItem left, ScriptableObjectCollectionItem right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ScriptableObjectCollectionItem left, ScriptableObjectCollectionItem right)
        {
            return !(left == right);
        }

        private void SyncGUID()
        {
            if (!string.IsNullOrEmpty(guid)) 
                return;
            
            guid = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
                ObjectUtility.SetDirty(this);
            }
#endif
            
        }
        
        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }

        public void ValidateGUID()
        {
            SyncGUID();
        }

        public void GenerateNewGUID()
        {
            guid = string.Empty;
            SyncGUID();
        }
    }
}
