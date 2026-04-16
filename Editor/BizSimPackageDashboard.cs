using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Unified dashboard window for all BizSim Google Play packages.
    /// Menu: BizSim → Package Dashboard
    ///
    /// Features:
    /// - Firebase detection and define management
    /// - BizSim package status overview
    /// - Google Play plugin status overview
    /// - Scripting define symbol management
    /// - Non-blocking: all detection is instant (AppDomain-based)
    /// </summary>
    public class BizSimPackageDashboard : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<PackageInfo> _packages;
        private List<PackageInfo> _firebasePackages;
        private List<PackageInfo> _bizSimPackages;
        private List<PackageInfo> _googlePlayPackages;
        private List<PackageInfo> _utilityPackages;
        private PackageRegistryData _registry;
        private PackageInstallQueue _installQueue;
        private bool _firebaseDetailsFoldout = true;
        private bool _bizSimFoldout = true;
        private bool _googlePlayFoldout = true;
        private bool _utilityFoldout = true;
        private bool _definesFoldout;

        [MenuItem("BizSim/Package Dashboard", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<BizSimPackageDashboard>("BizSim Dashboard");
            window.minSize = new Vector2(480, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _installQueue = new PackageInstallQueue();
            _installQueue.OnItemCompleted += (_, _) =>
            {
                RefreshPackages();
                Repaint();
            };
            _installQueue.OnAllCompleted += () => Repaint();

            RefreshPackages();
        }

        private void OnFocus()
        {
            RefreshPackages();

            if (_registry != null)
                RemoteVersionChecker.CheckAll(_registry, () => Repaint());
        }

        private void RefreshPackages()
        {
            _registry = PackageRegistryData.Load();
            _packages = PackageDetector.ScanAll(_registry);
            _firebasePackages = _packages.Where(p => p.Category == PackageCategory.Firebase).ToList();
            _bizSimPackages = _packages.Where(p => p.Category == PackageCategory.BizSim).ToList();
            _googlePlayPackages = _packages.Where(p => p.Category == PackageCategory.GooglePlay).ToList();
            _utilityPackages = _packages.Where(p => p.Category == PackageCategory.BizSimUtility).ToList();

            // Sync scan results back into registry entries so HasUpdate works
            SyncRegistryInstalledState();
        }

        private void SyncRegistryInstalledState()
        {
            if (_registry == null || _packages == null)
                return;

            foreach (var pkg in _packages)
            {
                var entry = FindRegistryEntry(pkg.AssemblyName);
                if (entry == null) continue;

                entry.IsInstalled = pkg.IsInstalled;
                entry.InstalledVersion = pkg.Version;
            }
        }

        private void OnGUI()
        {
            bool isInstalling = _installQueue != null && _installQueue.IsProcessing;

            DrawToolbar();
            DrawSummaryBar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (isInstalling)
                GUI.enabled = false;

            DrawFirebaseSection();
            GUILayout.Space(6);
            DrawBizSimSection();
            GUILayout.Space(6);
            DrawGooglePlaySection();
            GUILayout.Space(6);
            DrawUtilitySection();
            GUILayout.Space(6);
            DrawDefineSymbolsSection();

            if (isInstalling)
            {
                GUI.enabled = true;
                EditorGUILayout.Space(4);
                var rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, 0.5f, $"Installing... ({_installQueue.Remaining} remaining)");
            }

            EditorGUILayout.EndScrollView();

            if (isInstalling)
                GUI.enabled = true;
        }

        // ─────────────────────────────────────────────
        // Toolbar
        // ─────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("BizSim Package Dashboard", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (RemoteVersionChecker.IsChecking)
                GUILayout.Label("Checking for updates...", EditorStyles.miniLabel);

            if (GUILayout.Button("↻ Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshPackages();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────
        // Summary Bar
        // ─────────────────────────────────────────────

        private void DrawSummaryBar()
        {
            int totalPackages = _packages?.Count ?? 0;
            int installed = _packages?.Count(p => p.IsInstalled) ?? 0;
            int updatesAvailable = CountUpdatesAvailable();

            string summary = $"{installed}/{totalPackages} packages installed";
            if (updatesAvailable > 0)
                summary += $" | {updatesAvailable} update(s) available";

            EditorGUILayout.LabelField(summary, EditorStyles.centeredGreyMiniLabel);
        }

        private int CountUpdatesAvailable()
        {
            if (_registry == null) return 0;

            int count = 0;
            foreach (var entry in _registry.BizSimPackages)
            {
                if (entry.HasUpdate) count++;
            }
            return count;
        }

        // ─────────────────────────────────────────────
        // Firebase Section
        // ─────────────────────────────────────────────

        private void DrawFirebaseSection()
        {
            var firebasePackages = _firebasePackages;
            if (firebasePackages == null) return;
            int installedCount = firebasePackages.Count(p => p.IsInstalled);

            _firebaseDetailsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_firebaseDetailsFoldout,
                $"  Firebase Integration  ({installedCount}/{firebasePackages.Count} modules)");

            if (_firebaseDetailsFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Main status
                bool analyticsInstalled = PackageDetector.IsFirebaseAnalyticsInstalled();
                string version = PackageDetector.GetFirebaseAnalyticsVersion();
                bool definePresent = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();

                // Status row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Firebase SDK:", GUILayout.Width(100));
                if (analyticsInstalled)
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.green;
                    string versionLabel = !string.IsNullOrEmpty(version) ? $"Installed (v{version})" : "Installed";
                    EditorGUILayout.LabelField(versionLabel, EditorStyles.boldLabel);
                    GUI.color = oldColor;
                }
                else
                {
                    var oldColor = GUI.color;
                    GUI.color = new Color(1f, 0.5f, 0f);
                    EditorGUILayout.LabelField("Not Found", EditorStyles.boldLabel);
                    GUI.color = oldColor;
                }
                EditorGUILayout.EndHorizontal();

                // Define row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("BIZSIM_FIREBASE:", GUILayout.Width(100));
                if (definePresent)
                {
                    var platforms = BizSimDefineManager.GetPlatformsWithFirebaseDefine();
                    var oldColor = GUI.color;
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField($"Active ({string.Join(", ", platforms)})", EditorStyles.boldLabel);
                    GUI.color = oldColor;
                }
                else
                {
                    var oldColor = GUI.color;
                    GUI.color = new Color(1f, 0.5f, 0f);
                    EditorGUILayout.LabelField("Missing", EditorStyles.boldLabel);
                    GUI.color = oldColor;
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(4);

                // Action buttons
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = analyticsInstalled && !definePresent;
                if (GUILayout.Button("Add BIZSIM_FIREBASE", GUILayout.Height(26)))
                {
                    BizSimDefineManager.AddFirebaseDefineAllPlatforms();
                    ShowNotification(new GUIContent("BIZSIM_FIREBASE added"));
                    // Force immediate UI refresh after define change
                    EditorApplication.delayCall += () => Repaint();
                }
                GUI.enabled = true;

                GUI.enabled = definePresent;
                if (GUILayout.Button("Remove BIZSIM_FIREBASE", GUILayout.Height(26)))
                {
                    if (EditorUtility.DisplayDialog("Remove Firebase Define",
                        "Remove BIZSIM_FIREBASE from all platforms?", "Remove", "Cancel"))
                    {
                        BizSimDefineManager.RemoveFirebaseDefineAllPlatforms();
                        ShowNotification(new GUIContent("BIZSIM_FIREBASE removed"));
                        EditorApplication.delayCall += () => Repaint();
                    }
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Module grid
                EditorGUILayout.LabelField("Installed Modules:", EditorStyles.miniLabel);
                DrawPackageGrid(firebasePackages);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────
        // BizSim Packages Section
        // ─────────────────────────────────────────────

        private void DrawBizSimSection()
        {
            var bizSimPackages = _bizSimPackages;
            if (bizSimPackages == null) return;
            int installedCount = bizSimPackages.Count(p => p.IsInstalled);

            _bizSimFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_bizSimFoldout,
                $"  BizSim Packages  ({installedCount}/{bizSimPackages.Count})");

            if (_bizSimFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawPackageGrid(bizSimPackages);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────
        // Google Play Section
        // ─────────────────────────────────────────────

        private void DrawGooglePlaySection()
        {
            var googlePackages = _googlePlayPackages;
            if (googlePackages == null) return;
            int installedCount = googlePackages.Count(p => p.IsInstalled);

            _googlePlayFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_googlePlayFoldout,
                $"  Google Play Plugins  ({installedCount}/{googlePackages.Count})");

            if (_googlePlayFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawPackageGrid(googlePackages);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────
        // BizSim Utilities Section
        // ─────────────────────────────────────────────

        private void DrawUtilitySection()
        {
            var utilityPackages = _utilityPackages;
            if (utilityPackages == null || utilityPackages.Count == 0) return;

            int installedCount = utilityPackages.Count(p => p.IsInstalled);

            _utilityFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_utilityFoldout,
                $"  BizSim Utilities  ({installedCount}/{utilityPackages.Count})");

            if (_utilityFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawPackageGrid(utilityPackages);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────
        // Scripting Defines Section
        // ─────────────────────────────────────────────

        private void DrawDefineSymbolsSection()
        {
            _definesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_definesFoldout,
                "  Scripting Define Symbols");

            if (_definesFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var platforms = BizSimDefineManager.GetRelevantPlatforms();

                foreach (var platform in platforms)
                {
                    EditorGUILayout.LabelField(platform.ToString(), EditorStyles.boldLabel);

                    bool hasFirebase = BizSimDefineManager.IsFirebaseDefinePresent(platform);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    DrawStatusDot(hasFirebase);
                    EditorGUILayout.LabelField("BIZSIM_FIREBASE", GUILayout.Width(160));

                    if (hasFirebase)
                    {
                        if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60)))
                        {
                            BizSimDefineManager.RemoveDefine(BizSimDefineManager.FIREBASE_DEFINE, platform);
                            EditorApplication.delayCall += () => Repaint();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(60)))
                        {
                            BizSimDefineManager.AddDefine(BizSimDefineManager.FIREBASE_DEFINE, platform);
                            EditorApplication.delayCall += () => Repaint();
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────
        // Shared Drawing Utilities
        // ─────────────────────────────────────────────

        private void DrawPackageGrid(List<PackageInfo> packages)
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 30) / 200f));
            int col = 0;

            EditorGUILayout.BeginHorizontal();

            foreach (var pkg in packages)
            {
                if (col > 0 && col % columns == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                DrawPackageCard(pkg);
                col++;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPackageCard(PackageInfo pkg)
        {
            var entry = FindRegistryEntry(pkg.AssemblyName);

            EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(200));

            // Row 1: Name + Version
            EditorGUILayout.BeginHorizontal();
            DrawStatusDot(pkg.IsInstalled, entry != null && entry.HasUpdate);
            EditorGUILayout.LabelField(pkg.DisplayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            string versionText = entry?.CurrentVersion
                ?? pkg.Version
                ?? (pkg.IsInstalled ? "Installed" : "---");
            EditorGUILayout.LabelField(versionText, EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // Row 2: Release date + Play Core version (if available)
            if (entry != null && pkg.IsInstalled)
            {
                bool hasDate = !string.IsNullOrEmpty(entry.ReleaseDate);
                bool hasPlayCore = !string.IsNullOrEmpty(entry.PlayCoreVersion);
                if (hasDate || hasPlayCore)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    if (hasDate)
                        EditorGUILayout.LabelField(entry.ReleaseDate, EditorStyles.miniLabel, GUILayout.Width(80));
                    if (hasPlayCore)
                        EditorGUILayout.LabelField($"Play Core: {entry.PlayCoreVersion}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Row 3: Action buttons
            EditorGUILayout.BeginHorizontal();

            if (!pkg.IsInstalled && entry != null && !string.IsNullOrEmpty(entry.GitRepoUrl))
            {
                if (GUILayout.Button("Install", GUILayout.Height(20)))
                {
                    _installQueue.Enqueue(new InstallRequest(entry.PackageId, entry.GitInstallUrl));
                    if (!_installQueue.IsProcessing) _installQueue.ProcessNext();
                }
            }
            else if (pkg.IsInstalled)
            {
                if (entry != null && entry.HasUpdate)
                {
                    if (GUILayout.Button($"Update {entry.LatestTag}", GUILayout.Height(20), GUILayout.Width(90)))
                    {
                        _installQueue.Enqueue(new InstallRequest(entry.PackageId, entry.GitInstallUrl));
                        if (!_installQueue.IsProcessing) _installQueue.ProcessNext();
                    }
                }

                if (entry != null && !string.IsNullOrEmpty(entry.ConfigWindowTypeName))
                {
                    if (GUILayout.Button("Configure", GUILayout.Height(20), GUILayout.Width(70)))
                        OpenConfigWindow(entry);
                }

                if (entry != null)
                {
                    if (GUILayout.Button("...", GUILayout.Height(20), GUILayout.Width(22)))
                        ShowPackageContextMenu(entry, pkg);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Right-click context menu on the whole card
            if (entry != null && Event.current.type == EventType.ContextClick
                && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                ShowPackageContextMenu(entry, pkg);
                Event.current.Use();
            }
        }

        private PackageRegistryEntry FindRegistryEntry(string assemblyName)
        {
            if (_registry == null || string.IsNullOrEmpty(assemblyName))
                return null;

            foreach (var entry in _registry.BizSimPackages)
            {
                if (entry.AssemblyName == assemblyName)
                    return entry;
            }

            foreach (var entry in _registry.FirebasePackages)
            {
                if (entry.AssemblyName == assemblyName)
                    return entry;
            }

            foreach (var entry in _registry.GooglePlayPackages)
            {
                if (entry.AssemblyName == assemblyName)
                    return entry;
            }

            if (_registry.Edm4u != null && _registry.Edm4u.AssemblyName == assemblyName)
                return _registry.Edm4u;

            return null;
        }

        private void ShowPackageContextMenu(PackageRegistryEntry entry, PackageInfo pkg)
        {
            var menu = new GenericMenu();

            if (pkg.IsInstalled && !string.IsNullOrEmpty(entry.ConfigWindowTypeName))
                menu.AddItem(new GUIContent("Open Configuration..."), false, () => OpenConfigWindow(entry));

            menu.AddSeparator("");

            if (!string.IsNullOrEmpty(entry.GitHubRepoName))
            {
                menu.AddItem(new GUIContent("View on GitHub"), false,
                    () => Application.OpenURL($"https://github.com/BizSim-Game-Studios/{entry.GitHubRepoName}"));
                menu.AddItem(new GUIContent("Copy Git Install URL"), false,
                    () =>
                    {
                        EditorGUIUtility.systemCopyBuffer = entry.GitInstallUrl;
                        ShowNotification(new GUIContent("URL copied!"));
                    });
            }

            if (pkg.IsInstalled && entry.SampleFolders != null && entry.SampleFolders.Length > 0)
            {
                menu.AddSeparator("");
                foreach (var sample in entry.SampleFolders)
                {
                    string s = sample; // closure capture
                    menu.AddItem(new GUIContent($"Import Sample/{s}"), false,
                        () => ImportSample(entry, s));
                }
            }

            if (pkg.IsInstalled && entry.DocFiles != null && entry.DocFiles.Length > 0)
            {
                menu.AddSeparator("");
                foreach (var doc in entry.DocFiles)
                {
                    string d = doc; // closure capture
                    menu.AddItem(new GUIContent($"Open Documentation/{d}"), false,
                        () => OpenDocumentation(entry, d));
                }
            }

            menu.ShowAsContext();
        }

        private void OpenConfigWindow(PackageRegistryEntry entry)
        {
            // Try assembly scan to find the editor window type
            var type = System.AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return System.Type.EmptyTypes; }
                })
                .FirstOrDefault(t => t.FullName == entry.ConfigWindowTypeName);

            if (type != null && typeof(EditorWindow).IsAssignableFrom(type))
                EditorWindow.GetWindow(type);
            else
                Debug.LogWarning($"[BizSim.EditorCore] Configuration window not found: {entry.ConfigWindowTypeName}");
        }

        private void ImportSample(PackageRegistryEntry entry, string sampleName)
        {
            string src = $"Packages/{entry.PackageId}/Samples~/{sampleName}";
            string dst = $"Assets/Samples/{entry.PackageId}/{entry.CurrentVersion ?? "unknown"}/{sampleName}";

            if (!System.IO.Directory.Exists(src))
            {
                Debug.LogWarning($"[BizSim.EditorCore] Sample not found: {src}");
                return;
            }

            string parentDir = System.IO.Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(parentDir))
                System.IO.Directory.CreateDirectory(parentDir);

            FileUtil.CopyFileOrDirectory(src, dst);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent($"Imported: {sampleName}"));
        }

        private static void OpenDocumentation(PackageRegistryEntry entry, string docFile)
        {
            string path = $"Packages/{entry.PackageId}/Documentation~/{docFile}";
            string fullPath = System.IO.Path.GetFullPath(path);
            if (System.IO.File.Exists(fullPath))
                System.Diagnostics.Process.Start(fullPath);
            else
                Debug.LogWarning($"[BizSim.EditorCore] Documentation not found: {path}");
        }

        private static void DrawStatusDot(bool active, bool hasUpdate = false)
        {
            var rect = GUILayoutUtility.GetRect(12, 14, GUILayout.Width(12));
            rect.y += 3;
            rect.width = 8;
            rect.height = 8;

            var oldColor = GUI.color;
            if (hasUpdate)
                GUI.color = new Color(1f, 0.6f, 0f); // orange for update available
            else if (active)
                GUI.color = Color.green;
            else
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 1f);
            GUI.color = oldColor;
        }
    }
}
