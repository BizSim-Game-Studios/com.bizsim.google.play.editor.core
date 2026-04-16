using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Handles UPM package removal with dependency analysis and confirmation.
    /// Shows a detailed dialog before removing, listing dependent assemblies.
    /// </summary>
    public static class PackageRemoveHandler
    {
        static RemoveRequest _current;

        /// <summary>True while a remove operation is in flight.</summary>
        public static bool IsRemoving => _current != null;

        /// <summary>
        /// Show a confirmation dialog and remove the package if confirmed.
        /// Lists the package info and any assemblies that reference it.
        /// </summary>
        public static void RequestRemove(PackageRegistryEntry entry, string installedVersion, Action onComplete)
        {
            var dependents = FindDependentAssemblies(entry.AssemblyName);
            string message = BuildConfirmationMessage(entry, installedVersion, dependents);

            bool confirmed = EditorUtility.DisplayDialog(
                $"Remove {entry.DisplayName}?",
                message,
                "Remove",
                "Cancel");

            if (!confirmed) return;

            _current = Client.Remove(entry.PackageId);

            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (_current == null || !_current.IsCompleted) return;

                EditorApplication.update -= poll;
                bool success = _current.Status == StatusCode.Success;
                _current = null;

                if (success)
                    UnityEngine.Debug.Log($"[BizSim.EditorCore] Removed {entry.PackageId}");
                else
                    UnityEngine.Debug.LogError($"[BizSim.EditorCore] Failed to remove {entry.PackageId}");

                onComplete?.Invoke();
            };

            EditorApplication.update += poll;
        }

        /// <summary>
        /// Find all assemblies in the project that reference the given assembly name.
        /// </summary>
        public static List<string> FindDependentAssemblies(string assemblyName)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(assemblyName)) return result;

            try
            {
                var allAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
                var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

                var combined = new List<Assembly>();
                if (allAssemblies != null) combined.AddRange(allAssemblies);
                if (editorAssemblies != null) combined.AddRange(editorAssemblies);

                foreach (var asm in combined)
                {
                    if (asm.assemblyReferences == null) continue;

                    bool references = asm.assemblyReferences.Any(r =>
                        System.IO.Path.GetFileNameWithoutExtension(r.name) == assemblyName
                        || r.name == assemblyName);

                    if (references)
                        result.Add(asm.name);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[BizSim.EditorCore] Dependency scan failed: {e.Message}");
            }

            return result;
        }

        static string BuildConfirmationMessage(
            PackageRegistryEntry entry,
            string installedVersion,
            List<string> dependents)
        {
            var lines = new List<string>();
            lines.Add($"Package: {entry.DisplayName}");
            lines.Add($"ID: {entry.PackageId}");
            if (!string.IsNullOrEmpty(installedVersion))
                lines.Add($"Version: {installedVersion}");

            lines.Add("");
            lines.Add("This will remove the package from Packages/manifest.json.");

            if (!string.IsNullOrEmpty(entry.EditorInitDefine))
            {
                lines.Add("");
                lines.Add($"Scripting define '{entry.EditorInitDefine}' will become inactive.");
            }

            if (dependents.Count > 0)
            {
                lines.Add("");
                lines.Add($"⚠ {dependents.Count} assembly(ies) reference this package:");
                foreach (var dep in dependents)
                    lines.Add($"  • {dep}");
                lines.Add("");
                lines.Add("These assemblies will have compile errors after removal!");
            }

            return string.Join("\n", lines);
        }
    }
}
