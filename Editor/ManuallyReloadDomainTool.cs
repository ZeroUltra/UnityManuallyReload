using UnityEditor;
using UnityEditor.Compilation;
using System.Diagnostics;
using UnityEngine;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Assembly = UnityEditor.Compilation.Assembly;
using Debug = UnityEngine.Debug;
using AssemblyFlags = UnityEditor.Compilation.AssemblyFlags;
using System;

namespace Plugins.ManuallyReload
{
    public static class ManuallyReloadDomainTool
    {
        /* 说明
         * 关于域重载 https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html
         * EditorApplication.LockReloadAssemblies()和 EditorApplication.UnlockReloadAssemblies() 成对
         * 如果不小心LockReloadAssemblies3次 但是只UnlockReloadAssemblies了一次 那么还是不会重载 必须也要但是只UnlockReloadAssemblies3次
         */
        public const string logYellow = "<color=yellow>{0}</color>";
        public const string logCyan = "<color=Cyan>{0}</color>";
        public const string logWhite = "<color=White>{0}</color>";

        const string menuRealodDomain = "Tools/Reload Domain/Unlock Reload _F5"; //菜单快捷键 f5
        const string menuForceRealodDomain = "Tools/Reload Domain/Force Unlock Reload %t"; //菜单快捷键(强制重新reload) ctrl+t
        const string kFirstEnterUnity = "FirstEnterUnity"; //是否首次进入unity
        const string kReloadDomainTimer = "ReloadDomainTimer"; //计时

        //https://github.com/INeatFreak/unity-background-recompiler 来自这个库 反射获取是否锁住
        static MethodInfo CanReloadAssembliesMethod;
        static bool IsAssemblyLocked
        {
            get
            {
                if (CanReloadAssembliesMethod == null)
                {
                    // source: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorApplication.bindings.cs#L154
                    CanReloadAssembliesMethod = typeof(EditorApplication).GetMethod("CanReloadAssemblies", BindingFlags.NonPublic | BindingFlags.Static);
                    if (CanReloadAssembliesMethod == null)
                        Debug.LogError("Can't find CanReloadAssemblies method. It might have been renamed or removed.");
                }
                return !(bool)CanReloadAssembliesMethod.Invoke(null, null);
            }
        }

        //编译时间
        static Stopwatch compileSW = new Stopwatch();
        //是否编译了
        static bool isNewCompile = false;
        static bool isReloaded = false; //完全模式下是否reload
        static List<string> listAssembly = new List<string>();

        [InitializeOnLoadMethod]
        static void Init()
        {
            if (SessionState.GetBool(kFirstEnterUnity, true))
            {
                SessionState.SetBool(kFirstEnterUnity, false);

                if (ManuallyReloadSetting.Instance.IsEnableManuallyReload)
                {
                    UnlockReloadDomain();
                    LockRealodDomain();
                }
                string state = ManuallyReloadSetting.Instance.IsEnableManuallyReload ? "Enable Manually Reload !" : "Disable Manually Reload !";
                Debug.LogFormat(logCyan, state);
            }
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += EveryAssemblyCompilationFinished;

            //域重载事件监听
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeDomainReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterDomainReload;

            //编辑器运行模式改变
            //如果不需要自动重置数据请注释下面代码
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        #region Menu
        //手动刷新
        [MenuItem(menuRealodDomain)]
        static void ManualReload()
        {
            if (isNewCompile && ManuallyReloadSetting.Instance.IsEnableManuallyReload)
            {
                ForceReloadDomain();
            }
            else
            {
                //Debug.LogFormat(logWhite, "No need to reload domain,if you want to reload domain,please use ctrl+t to force reload domain!");
                Debug.LogFormat(logWhite, "无需再reload domain,如需请使用ctrl+t强制reload domain!");
            }
        }
        //强制刷新
        [MenuItem(menuForceRealodDomain)]
        static void MenuForceReloadDomain()
        {
            if (ManuallyReloadSetting.Instance.IsEnableManuallyReload)
                ForceReloadDomain();
        }
        #endregion

        //运行模式改变
        static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            //如果没有开启 或者 是完全手动模式直接return
            if (!ManuallyReloadSetting.Instance.IsEnableManuallyReload) return;
            if (ManuallyReloadSetting.Instance.IsFullyManuallyReload) return;
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if (!isReloaded)
                        ForceReloadDomain();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    isReloaded = false;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    break;

            }
        }

        public static void LockRealodDomain()
        {
            //如果没有锁住 锁住
            if (!IsAssemblyLocked)
                EditorApplication.LockReloadAssemblies();
        }

        public static void UnlockReloadDomain()
        {
            //如果锁住了 打开
            if (IsAssemblyLocked)
                EditorApplication.UnlockReloadAssemblies();
        }

        //强制reloaddomain
        public static void ForceReloadDomain()
        {
            UnlockReloadDomain();
            EditorUtility.RequestScriptReload();
        }

        #region AssembleCompile
        //完成编译任何一个dll
        private static void EveryAssemblyCompilationFinished(string assemblypath, CompilerMessage[] infos)
        {
            if (!ManuallyReloadSetting.Instance.IsEnableManuallyReload) return;
            listAssembly.Add(assemblypath);
        }

        //当开始编译脚本
        private static void OnCompilationStarted(object obj)
        {
            if (!ManuallyReloadSetting.Instance.IsEnableManuallyReload) return;
            listAssembly.Clear();
            compileSW.Start();
            Debug.LogFormat(logYellow, "Beging Compile...");
        }

        //结束编译
        private static void OnCompilationFinished(object obj)
        {
            if (!ManuallyReloadSetting.Instance.IsEnableManuallyReload) return;
            compileSW.Stop();
            Debug.LogFormat(logYellow, $"End Compile :{compileSW.ElapsedMilliseconds} ms");
            isNewCompile = true;

            //编辑器代码不手动reload
            if (!ManuallyReloadSetting.Instance.IsEditorUseManuallyReload)
            {
                //获取所有编辑器dll
                var listEditorAssemblys = new List<Assembly>(CompilationPipeline.GetAssemblies(AssembliesType.Editor));
                for (int i = listEditorAssemblys.Count - 1; i >= 0; i--)
                {
                    if (listEditorAssemblys[i].flags != AssemblyFlags.EditorAssembly)
                    {
                        listEditorAssemblys.RemoveAt(i);
                    }
                }
                //判断当前编译的dll是否是edior dll
                bool result = listAssembly.TrueForAll(assemblyPath => listEditorAssemblys.Exists(editAss => editAss.outputPath == assemblyPath));
                //如果都是编辑器代码
                if (result)
                {
                    Debug.LogFormat(logWhite, "[IsEditorUseManuallyReload=false]   Force Reload......");
                    ForceReloadDomain();
                }
            }
        }
        #endregion

        #region ReloadDomain
        //开始reload domain
        private static void OnBeforeDomainReload()
        {
            if (!ManuallyReloadSetting.Instance.IsEnableManuallyReload) return;
            Debug.LogFormat(logYellow, "Begin Reload Domain...");
            //记录时间 由于
            SessionState.SetInt(kReloadDomainTimer, (int)(EditorApplication.timeSinceStartup * 1000));
        }

        //结束reload domain
        private static void OnAfterDomainReload()
        {
            if (!ManuallyReloadSetting.Instance.IsEnableManuallyReload) return;
            var timeMS = (int)(EditorApplication.timeSinceStartup * 1000) - SessionState.GetInt(kReloadDomainTimer, 0);
            Debug.LogFormat(logYellow, $"End Reload Domain : {timeMS} ms");
            if (ManuallyReloadSetting.Instance.IsEnableManuallyReload)
                LockRealodDomain();
            isNewCompile = false;
            isReloaded = true;
        }
        #endregion
    }

    #region 相关设置
    [System.Serializable]
    public class ManuallyReloadSetting : ScriptableObject
    {
        [Space(5)]
        //  [Header("Main Setting")]
        [Tooltip("是否启用手动Reload")]
        public bool IsEnableManuallyReload = false;
        [Tooltip("完全手动Reload(指不会在运行前检测是否需要reload),需完全手动触发")]
        public bool IsFullyManuallyReload = false;
        [Tooltip("是否Editor代码也需手动Reload?当且仅当编辑Editor代码才有效")]
        public bool IsEditorUseManuallyReload;

        //[Header("Other Setting (Optional)")]
        //[Tooltip("显示编译和Reload耗时日志")]
        //public bool ShowCompilationAndReloadLog = true;

        private static string filePath;

        private static ManuallyReloadSetting m_Instance;
        public static ManuallyReloadSetting Instance
        {
            get
            {
                if (string.IsNullOrEmpty(filePath))
                    filePath = Application.dataPath + "/../ProjectSettings/ManuallyReloadSettings.asset";
                if (m_Instance == null)
                {
                    m_Instance = ScriptableObject.CreateInstance<ManuallyReloadSetting>();
                    try
                    {
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(filePath), m_Instance);
                    }
                    catch (System.Exception e) { }
                }
                return m_Instance;
            }
        }

        public void Save()
        {
            File.WriteAllText(filePath, JsonUtility.ToJson(this, true));
        }
    }

    // 注册到setting中
    //https://docs.unity.cn/cn/2022.3/ScriptReference/SettingsProvider.html
    static class ManuallyReloadRegisterToSetting
    {
        static GUIStyle iconStyle;
        static GUIStyle boxStyle;
        static SerializedObject so;
        static SerializedProperty p_isEnableManuallyReload;
        static SerializedProperty p_isFullyManuallyReload;
        static SerializedProperty p_isEditorUseManuallyReload;
        //  static SerializedProperty p_ShowCompilationAndReloadLog;

        static GUIContent guiContentEnable = new GUIContent(" Enable Manually Reload", EditorGUIUtility.IconContent("Refresh").image);
        static GUIContent guiContentFullyManually = new GUIContent(" Enable Fully Manually Reload", EditorGUIUtility.IconContent("d_winbtn_win_restore").image);
        static GUIContent guiContentEditor = new GUIContent(" Editor Scripts Manually Reload?", EditorGUIUtility.IconContent("d_winbtn_win_restore").image);
        //static GUIContent guiContentShowLog = new GUIContent(" Show Compilation And Reload Log", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
        [SettingsProvider]
        public static SettingsProvider CreateMyManuallyReloadProvider()
        {
            var provider = new SettingsProvider("Project/ManuallyReload Setting", SettingsScope.Project)
            {
                label = "Manually Reload Setting",
                titleBarGuiHandler = () =>
                {
                    if (iconStyle == null)
                        iconStyle = GUI.skin.GetStyle("IconButton");
                    if (boxStyle == null)
                    {
                        boxStyle = GUI.skin.box;
                        boxStyle.richText = true;
                        boxStyle.alignment = TextAnchor.MiddleLeft;
                    }
                    if (GUILayout.Button(EditorGUIUtility.IconContent("_Help"), iconStyle))
                        Application.OpenURL("https://github.com/ZeroUltra/UnityManualReload");
                },
                guiHandler = (searchContext) =>
                {
                    EditorGUIUtility.labelWidth = 500;
                    if (so == null)
                    {
                        so = new SerializedObject(ManuallyReloadSetting.Instance);
                        p_isEnableManuallyReload = so.FindProperty(nameof(ManuallyReloadSetting.Instance.IsEnableManuallyReload));
                        p_isFullyManuallyReload = so.FindProperty(nameof(ManuallyReloadSetting.Instance.IsFullyManuallyReload));
                        p_isEditorUseManuallyReload = so.FindProperty(nameof(ManuallyReloadSetting.Instance.IsEditorUseManuallyReload));
                        // p_ShowCompilationAndReloadLog = so.FindProperty(nameof(ManuallyReloadSetting.Instance.ShowCompilationAndReloadLog));
                    }
                    var settings = ManuallyReloadSetting.Instance;
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(p_isEnableManuallyReload, guiContentEnable);
                        if (check.changed)
                        {
                            so.ApplyModifiedPropertiesWithoutUndo();
                            settings.Save();
                            if (settings.IsEnableManuallyReload)
                            {
                                //编辑器设置 projectsetting->editor->enterPlayModeSetting
                                Debug.LogFormat(ManuallyReloadDomainTool.logCyan, "Enable Manually Reload !");
                                EditorSettings.enterPlayModeOptionsEnabled = true;
                                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                                ManuallyReloadDomainTool.LockRealodDomain();
                            }
                            else
                            {
                                Debug.LogFormat(ManuallyReloadDomainTool.logCyan, "Disable Manually Reload !");
                                ManuallyReloadDomainTool.ForceReloadDomain();
                                EditorSettings.enterPlayModeOptionsEnabled = false;
                            }
                        }
                    }
                    GUISerializedPropertyBool(p_isFullyManuallyReload, !p_isEnableManuallyReload.boolValue, guiContentFullyManually);
                    GUISerializedPropertyBool(p_isEditorUseManuallyReload, !p_isEnableManuallyReload.boolValue, guiContentEditor);
                    //GUISerializedPropertyBool(p_ShowCompilationAndReloadLog, false, guiContentShowLog);

                    GUILayout.Space(10);
                    GUI.DrawTexture(new Rect(425, 107, 22, 22), EditorGUIUtility.IconContent("Locked").image);
                    GUILayout.Box(new GUIContent($"脚本编译之后，按下 <color=red>F5</color> 进行重载(Realod Domain)" +
                                                          $"\n\n如遇编译锁住，按下 <color=red>Ctrl+T</color> 强制进行重载(Unity编辑器右下角始终为 <color=yellow>[锁]    </color> 状态时)" +
                                                          $"\n\n如需修改快捷键请自行修改代码", EditorGUIUtility.IconContent("d_console.warnicon").image), boxStyle, GUILayout.MinWidth(520));
                },
                keywords = new string[] { "Reload", "Manually" }
            };
            return provider;
        }

        private static void GUISerializedPropertyBool(SerializedProperty serializedProperty, bool disable, GUIContent guiContent)
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUI.DisabledScope(disable))
                    EditorGUILayout.PropertyField(serializedProperty, guiContent);

                if (check.changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    ManuallyReloadSetting.Instance.Save();
                }
            }
        }
    }
    #endregion

    //当资源导入
    public class ScriptsProcessor : AssetModificationProcessor
    {
        const string metaExtension = ".meta";
        static void OnWillCreateAsset(string assetName)
        {
            if (Path.GetExtension(assetName) == metaExtension)
            {
                //获取创建文件的类型
                assetName = assetName.Replace(metaExtension, "");
                //如果新建脚本或asm
                if (assetName.EndsWith(".cs") || assetName.EndsWith(".asmdef") || assetName.EndsWith(".asmref"))
                {
                    //如果是手动
                    if (ManuallyReloadSetting.Instance.IsEnableManuallyReload)
                    {
                        Debug.Log($"Force Reload, New File: {assetName}");
                        //强制reload domain
                        ManuallyReloadDomainTool.ForceReloadDomain();
                    }
                }
            }
        }

    }

}
