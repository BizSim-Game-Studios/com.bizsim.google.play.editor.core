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

            DrawFirebaseSection();
            GUILayout.Space(6);
            DrawBizSimSection();
            GUILayout.Space(6);
            DrawGooglePlaySection();
            GUILayout.Space(6);
            DrawUtilitySection();
            GUILayout.Space(6);
            DrawPlatformSection();
            GUILayout.Space(6);
            DrawDefineSymbolsSection();

            if (isInstalling)
            {
                EditorGUILayout.Space(4);
                string currentPkg = _installQueue.CurrentRequest?.PackageId ?? "package";
                int remaining = _installQueue.Remaining;
                string label = remaining > 0
                    ? $"Installing {currentPkg}... ({remaining} queued)"
                    : $"Installing {currentPkg}...";
                var rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, 0.5f, label);
            }

            EditorGUILayout.EndScrollView();
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

                // BIZSIM_FIREBASE scripting-define status + management. The global
                // "Firebase SDK: vX.Y.Z" row that used to live here was intentionally
                // removed in 1.4.0 (K9.3, Plan H-1) — it read only Firebase.Analytics
                // and mislabelled that single module's version as "the Firebase SDK
                // version", which is incorrect for projects with heterogeneous module
                // versions. Per-module versions are now shown exclusively in the
                // module grid below.
                bool definePresent = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();
                bool anyModuleInstalled = firebasePackages.Any(p => p.IsInstalled);

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

                // Action buttons — enabled when at least ONE Firebase module is
                // installed (not just Analytics). Projects may use Firestore, Auth,
                // or Remote Config without Analytics.
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = anyModuleInstalled && !definePresent;
                if (GUILayout.Button("Add BIZSIM_FIREBASE", GUILayout.Height(26)))
                {
                    BizSimDefineManager.AddFirebaseDefineAllPlatforms();
                    ShowNotification(new GUIContent("BIZSIM_FIREBASE added"));
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

                // Latest-upstream footer. Kept as an informational link rather than a
                // big "update available" banner tied to Analytics. Per-module update
                // badges will replace this in Plan H-2 (RemoteVersionChecker + a
                // secure FirebaseUpdater).
                if (!string.IsNullOrEmpty(RemoteVersionChecker.LatestFirebaseTag))
                {
                    GUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        $"Latest Firebase Unity SDK release: {RemoteVersionChecker.LatestFirebaseTag}",
                        EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("GitHub Releases", EditorStyles.miniButton, GUILayout.Width(100)))
                        Application.OpenURL("https://github.com/firebase/firebase-unity-sdk/releases/latest");
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(6);

                // Per-module grid. Each card shows the module's own installed version
                // (parsed from Assets/Firebase/Editor/{Module}_version-*.txt by
                // PackageDetector.GetFirebaseModuleVersion when assembly attributes
                // return 0.0.0.0, which Firebase DLLs historically did).
                EditorGUILayout.LabelField("Modules:", EditorStyles.miniLabel);
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

                if (!string.IsNullOrEmpty(RemoteVersionChecker.LatestGooglePlayPluginsTag))
                {
                    string latestGp = RemoteVersionChecker.LatestGooglePlayPluginsTag;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Latest release: {latestGp}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("GitHub Releases", EditorStyles.miniButton, GUILayout.Width(100)))
                        Application.OpenURL("https://github.com/google/play-unity-plugins/releases/latest");
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(4);
                }

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
        // Platform Section
        // ─────────────────────────────────────────────

        private bool _platformFoldout;

        private void DrawPlatformSection()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            bool isAndroid = target == BuildTarget.Android;

            _platformFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_platformFoldout,
                $"  Platform  ({target})");

            if (_platformFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Current build target
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Active Build Target:", GUILayout.Width(140));
                var oldColor = GUI.color;
                GUI.color = isAndroid ? Color.green : new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField(target.ToString(), EditorStyles.boldLabel);
                GUI.color = oldColor;
                EditorGUILayout.EndHorizontal();

                if (!isAndroid)
                {
                    EditorGUILayout.HelpBox(
                        "Google Play packages require Android build target. " +
                        "Switch via File → Build Settings → Android.",
                        MessageType.Warning);
                }

                GUILayout.Space(4);
                EditorGUILayout.LabelField("Package Platform Support:", EditorStyles.miniLabel);

                // Platform compatibility table
                DrawPlatformRow("Google Play Packages", "Android + Editor", isAndroid);
                DrawPlatformRow("Firebase SDK", "Android, iOS, Editor", true);
                DrawPlatformRow("BizSim Utilities", "All Platforms", true);

                GUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Switch to Android", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Switch Build Target",
                        "Switch to Android build target?\nThis may take a while.",
                        "Switch", "Cancel"))
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(
                            BuildTargetGroup.Android, BuildTarget.Android);
                    }
                }
                if (GUILayout.Button("Open Build Settings", GUILayout.Height(24)))
                    EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawPlatformRow(string label, string platforms, bool compatible)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            DrawStatusDot(compatible);
            EditorGUILayout.LabelField(label, GUILayout.Width(180));
            EditorGUILayout.LabelField(platforms, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
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

            // Row 2: Release date + per-package native SDK label/version (K9 Plan G; no longer
            // hardcoded "Play Core" — each package reports its own SDK family via NativeSdkLabel).
            if (entry != null && pkg.IsInstalled)
            {
                bool hasDate = !string.IsNullOrEmpty(entry.ReleaseDate);
                bool hasNativeSdk = !string.IsNullOrEmpty(entry.NativeSdkVersion);
                string label = !string.IsNullOrEmpty(entry.NativeSdkLabel)
                    ? entry.NativeSdkLabel
                    : "SDK"; // graceful fallback for entries still on legacy registry without NativeSdkLabel
                if (hasDate || hasNativeSdk)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    if (hasDate)
                        EditorGUILayout.LabelField(entry.ReleaseDate, EditorStyles.miniLabel, GUILayout.Width(80));
                    if (hasNativeSdk)
                        EditorGUILayout.LabelField($"{label}: {entry.NativeSdkVersion}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Row 3: Action buttons
            EditorGUILayout.BeginHorizontal();

            bool isThisPackageInstalling = _installQueue?.CurrentRequest != null
                && entry != null
                && _installQueue.CurrentRequest.Value.PackageId == entry.PackageId;

            bool canInstall = entry != null && !pkg.IsInstalled
                && (!string.IsNullOrEmpty(entry.GitRepoUrl) || entry.ScopedRegistryInstall);

            if (canInstall)
            {
                GUI.enabled = !isThisPackageInstalling && !(_installQueue != null && _installQueue.IsProcessing);
                string installLabel = isThisPackageInstalling ? "Installing..." : "Install";
                if (GUILayout.Button(installLabel, GUILayout.Height(20)))
                {
                    if (entry.ScopedRegistryInstall)
                    {
                        ScopedRegistryConfigurator.EnsureOpenUpmRegistry();
                        _installQueue.Enqueue(new InstallRequest(entry.PackageId, entry.PackageId, true));
                    }
                    else
                    {
                        _installQueue.Enqueue(new InstallRequest(entry.PackageId, entry.GitInstallUrl));
                    }
                    if (!_installQueue.IsProcessing) _installQueue.ProcessNext();
                }
                GUI.enabled = true;
            }
            else if (pkg.IsInstalled)
            {
                if (entry != null && entry.HasUpdate)
                {
                    GUI.enabled = !isThisPackageInstalling && !(_installQueue != null && _installQueue.IsProcessing);
                    string updateLabel = isThisPackageInstalling ? "Updating..." : $"Update {entry.LatestTag}";
                    if (GUILayout.Button(updateLabel, GUILayout.Height(20), GUILayout.Width(100)))
                    {
                        _installQueue.Enqueue(new InstallRequest(entry.PackageId, entry.GitInstallUrl));
                        if (!_installQueue.IsProcessing) _installQueue.ProcessNext();
                    }
                    GUI.enabled = true;
                }

                if (entry != null && !string.IsNullOrEmpty(entry.ConfigWindowTypeName))
                {
                    if (GUILayout.Button("Configure", GUILayout.Height(20), GUILayout.Width(70)))
                        OpenConfigWindow(entry);
                }
            }

            // Context menu and remove — available for all packages with an entry
            if (entry != null)
            {
                if (GUILayout.Button("...", GUILayout.Height(20), GUILayout.Width(22)))
                    ShowPackageContextMenu(entry, pkg);

                if (pkg.IsInstalled)
                {
                    GUI.enabled = !PackageRemoveHandler.IsRemoving;
                    var removeStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        normal = { textColor = new Color(0.9f, 0.3f, 0.3f) }
                    };
                    if (GUILayout.Button("✕", removeStyle, GUILayout.Height(20), GUILayout.Width(22)))
                    {
                        PackageRemoveHandler.RequestRemove(entry, pkg.Version, () =>
                        {
                            RefreshPackages();
                            Repaint();
                        });
                    }
                    GUI.enabled = true;
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

            // Configuration (BizSim packages only)
            if (pkg.IsInstalled && !string.IsNullOrEmpty(entry.ConfigWindowTypeName))
                menu.AddItem(new GUIContent("Open Configuration..."), false, () => OpenConfigWindow(entry));

            // GitHub / source links
            if (!string.IsNullOrEmpty(entry.GitHubRepoName))
            {
                menu.AddSeparator("");
                string repoOrg = entry.Category == PackageCategory.Firebase ? "firebase"
                    : entry.Category == PackageCategory.GooglePlay ? "google"
                    : "BizSim-Game-Studios";
                menu.AddItem(new GUIContent("View on GitHub"), false,
                    () => Application.OpenURL($"https://github.com/{repoOrg}/{entry.GitHubRepoName}"));

                if (!string.IsNullOrEmpty(entry.GitRepoUrl))
                {
                    menu.AddItem(new GUIContent("Copy Git Install URL"), false,
                        () =>
                        {
                            EditorGUIUtility.systemCopyBuffer = entry.GitInstallUrl;
                            ShowNotification(new GUIContent("URL copied!"));
                        });
                }
            }

            // Firebase-specific links
            if (pkg.Category == PackageCategory.Firebase)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Firebase Documentation"), false,
                    () => Application.OpenURL("https://firebase.google.com/docs/unity/setup"));
                menu.AddItem(new GUIContent("Firebase Console"), false,
                    () => Application.OpenURL("https://console.firebase.google.com/"));
                if (pkg.IsInstalled)
                {
                    menu.AddItem(new GUIContent("Download Latest SDK"), false,
                        () => Application.OpenURL("https://github.com/firebase/firebase-unity-sdk/releases/latest"));
                }
            }

            // Google Play-specific links
            if (pkg.Category == PackageCategory.GooglePlay)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Google Play Developer Docs"), false,
                    () => Application.OpenURL("https://developer.android.com/guide/playcore"));
                menu.AddItem(new GUIContent("Play Console"), false,
                    () => Application.OpenURL("https://play.google.com/console/"));
            }

            // Samples
            if (pkg.IsInstalled && entry.SampleFolders != null && entry.SampleFolders.Length > 0)
            {
                menu.AddSeparator("");
                foreach (var sample in entry.SampleFolders)
                {
                    string s = sample;
                    menu.AddItem(new GUIContent($"Import Sample/{s}"), false,
                        () => ImportSample(entry, s));
                }
            }

            // Documentation
            if (pkg.IsInstalled && entry.DocFiles != null && entry.DocFiles.Length > 0)
            {
                menu.AddSeparator("");
                foreach (var doc in entry.DocFiles)
                {
                    string d = doc;
                    menu.AddItem(new GUIContent($"Open Documentation/{d}"), false,
                        () => OpenDocumentation(entry, d));
                }
            }

            // Copy package ID
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy Package ID"), false,
                () =>
                {
                    EditorGUIUtility.systemCopyBuffer = entry.PackageId;
                    ShowNotification(new GUIContent($"Copied: {entry.PackageId}"));
                });

            // Remove
            if (pkg.IsInstalled)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove Package..."), false, () =>
                {
                    PackageRemoveHandler.RequestRemove(entry, pkg.Version, () =>
                    {
                        RefreshPackages();
                        Repaint();
                    });
                });
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
