using System;
using System.Collections.Generic;
using UnityEngine;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Root data model for the BizSim package registry.
    /// Loaded from a JSON TextAsset embedded in Editor/Resources.
    /// </summary>
    [Serializable]
    public class PackageRegistryData
    {
        public List<PackageRegistryEntry> BizSimPackages = new();
        public List<PackageRegistryEntry> FirebasePackages = new();
        public List<PackageRegistryEntry> GooglePlayPackages = new();
        public PackageRegistryEntry Edm4u;

        /// <summary>
        /// Load the registry from the embedded JSON resource.
        /// Returns an empty registry if the resource is missing.
        /// </summary>
        public static PackageRegistryData Load()
        {
            var json = Resources.Load<TextAsset>("BizSim/PackageRegistry");
            if (json == null)
            {
                Debug.LogWarning("[BizSim.EditorCore] PackageRegistry.json not found in Resources/BizSim/");
                return new PackageRegistryData();
            }

            return JsonUtility.FromJson<PackageRegistryData>(json.text);
        }
    }
}
