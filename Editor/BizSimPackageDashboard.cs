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
            RefreshPackages();
        }

        private void OnFocus()
        {
            RefreshPackages();
        }

        private void RefreshPackages()
        {
            _packages = PackageDetector.ScanAll();
            _firebasePackages = _packages.Where(p => p.Category == PackageCategory.Firebase).ToList();
            _bizSimPackages = _packages.Where(p => p.Category == PackageCategory.BizSim).ToList();
            _googlePlayPackages = _packages.Where(p => p.Category == PackageCategory.GooglePlay).ToList();
            _utilityPackages = _packages.Where(p => p.Category == PackageCategory.BizSimUtility).ToList();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawFirebaseSection();
            GUILayout.Space(6);
            DrawBizSimSection();
            GUILayout.Space(6);
            DrawGooglePlaySection();
            GUILayout.Space(6);
            DrawUtilitySection();
            GUILayout.Space(6);
            DrawDefineSymbolsSection();

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

            if (GUILayout.Button("↻ Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshPackages();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
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
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 30) / 180f));
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
            EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(160));

            // Title
            EditorGUILayout.LabelField(pkg.DisplayName, EditorStyles.boldLabel);

            // Status
            EditorGUILayout.BeginHorizontal();
            DrawStatusDot(pkg.IsInstalled);

            if (pkg.IsInstalled)
            {
                string versionText = pkg.Version != null && pkg.Version != "Installed"
                    ? $"v{pkg.Version}"
                    : "Installed";
                EditorGUILayout.LabelField(versionText, EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Not Found", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatusDot(bool active)
        {
            var rect = GUILayoutUtility.GetRect(12, 14, GUILayout.Width(12));
            rect.y += 3;
            rect.width = 8;
            rect.height = 8;

            var oldColor = GUI.color;
            GUI.color = active ? Color.green : new Color(0.5f, 0.5f, 0.5f);
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 1f);
            GUI.color = oldColor;
        }
    }
}
