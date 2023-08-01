using UnityEditor;
using UnityEditor.Compilation;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace Plugins.ManuallyReload
{
    public static class ManuallyReloadDomainTool
    {
        /* 说明
         * 关于域重载 https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html
         * EditorApplication.LockReloadAssemblies()和 EditorApplication.UnlockReloadAssemblies() 成对
         * 如果不小心LockReloadAssemblies3次 但是只UnlockReloadAssemblies了一次 那么还是不会重载 必须也要但是只UnlockReloadAssemblies3次
         */

        const string menuEnableManualReload = "Tools/Reload Domain/Enable Manually Reload Domain";
        const string menuDisenableManualReload = "Tools/Reload Domain/Disable Manually Reload Domain";
        const string menuRealodDomain = "Tools/Reload Domain/Unlock Reload %t";
        const string kFirstEnterUnity = "FirstEnterUnity"; //是否首次进入unity 
        const string kReloadDomainTimer = "ReloadDomainTimer";//计时
        const string logFormat = "<color=yellow>{0}</color>";


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
        //是否手动reload
        static bool IsManuallyReload => ManuallyReloadSetting.instance.IsEnableManuallyReload;

        //判断是否进入了播放模式 每次域重载之后数据都会变回false 
        //加这个原因是为了重置静态数据 https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html
        static bool isEnterPlayFlag = false;

        //是否编译了
        static bool isNewCompile = false;



        [InitializeOnLoadMethod]
        static void Init()
        {
            //**************监听编译 不需要的话可以注释************************
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            //**************************************************************

            //域重载事件监听
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            //编辑器运行模式改变
            //如果不需要自动重置数据请注释下面两行代码
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        
            InitMenu();
        }


        //首次打开检测
        async static void InitMenu()
        {
            await System.Threading.Tasks.Task.Delay(100);
            //判断是否首次打开
            //https://docs.unity.cn/cn/2021.3/ScriptReference/SessionState.html
            if (SessionState.GetBool(kFirstEnterUnity, true))
            {
                SessionState.SetBool(kFirstEnterUnity, false);
                Menu.SetChecked(menuEnableManualReload, IsManuallyReload ? true : false);
                Menu.SetChecked(menuDisenableManualReload, IsManuallyReload ? false : true);

                if (IsManuallyReload)
                {
                    UnlockReloadDomain();
                    LockRealodDomain();
                }
                Debug.LogFormat(logFormat, $"是否手动ReloadDomain : {IsManuallyReload}");
            }
        }


        //运行模式改变
        static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    break;
                //离开edit模式进入play模式
                case PlayModeStateChange.ExitingEditMode:
                    if (isEnterPlayFlag)
                    {
                        //强制加载重置静态数据
                        ForceReloadDomain();
                    }
                    break;
                //进入play模式
                case PlayModeStateChange.EnteredPlayMode:
                    isEnterPlayFlag = true;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }


        static void LockRealodDomain()
        {
            //如果没有锁住 锁住
            if (!IsAssemblyLocked)
                EditorApplication.LockReloadAssemblies();
        }

        static void UnlockReloadDomain()
        {
            //如果锁住了 打开
            if (IsAssemblyLocked)
                EditorApplication.UnlockReloadAssemblies();
        }

        //强制reloaddomain
        static void ForceReloadDomain()
        {
            UnlockReloadDomain();
            EditorUtility.RequestScriptReload();
            
        }

        #region AssembleCompile
        //当开始编译脚本
        private static void OnCompilationStarted(object obj)
        {
            if (IsManuallyReload)
            {
                compileSW.Start();
                Debug.LogFormat(logFormat, "Beging Compile...");
            }
        }

        //结束编译
        private static void OnCompilationFinished(object obj)
        {
            if (IsManuallyReload)
            {
                compileSW.Stop();
                Debug.LogFormat(logFormat, $"End Compile :{compileSW.ElapsedMilliseconds} ms");
                isNewCompile = true;
            }
        }
        #endregion

        #region ReloadDomain
        //开始reload domain
        private static void OnBeforeAssemblyReload()
        {
            if (IsManuallyReload)
            {
                Debug.LogFormat(logFormat, "Begin Reload Domain...");
                //记录时间
                SessionState.SetInt(kReloadDomainTimer, (int)(EditorApplication.timeSinceStartup * 1000));
            }

        }
        //结束reload domain
        private static void OnAfterAssemblyReload()
        {
            if (IsManuallyReload)
            {
                var timeMS = (int)(EditorApplication.timeSinceStartup * 1000) - SessionState.GetInt(kReloadDomainTimer, 0);
                Debug.LogFormat(logFormat, $"End Reload Domain : {timeMS} ms");
                LockRealodDomain();
                isNewCompile = false;
            }
        }
        #endregion



        #region Menu
        [MenuItem(menuEnableManualReload)]
        static void EnableManuallyReloadDomain()
        {
            Debug.LogFormat(logFormat, "Enable Manually Reload Domain");

            Menu.SetChecked(menuEnableManualReload, true);
            Menu.SetChecked(menuDisenableManualReload, false);

            //保存设置
            ManuallyReloadSetting.instance.SetEnableManuallyReload(true);

            //编辑器设置 projectsetting->editor->enterPlayModeSetting
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            LockRealodDomain();
        }

        [MenuItem(menuDisenableManualReload)]
        static void DisenableManuallyReloadDomain()
        {
            Debug.LogFormat(logFormat, "Disable Manually Reload Domain");

            Menu.SetChecked(menuEnableManualReload, false);
            Menu.SetChecked(menuDisenableManualReload, true);

            ManuallyReloadSetting.instance.SetEnableManuallyReload(false);

            UnlockReloadDomain();
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
        //手动刷新
        [MenuItem(menuRealodDomain)]
        static void ManualReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.Log("unity is busy,wait a moment...");
                return;
            }
            if (isNewCompile&&IsManuallyReload)
            {
                ForceReloadDomain();
            }
        }
        #endregion
    }


    /// <summary>
    /// 相关设置
    /// </summary>
    [FilePath("ProjectSettings/ManuallyReloadSetting.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ManuallyReloadSetting : ScriptableSingleton<ManuallyReloadSetting>
    {
        //是否开启了手动reload
        public bool IsEnableManuallyReload;

        public void SetEnableManuallyReload(bool isEnable)
        {
            IsEnableManuallyReload = isEnable;
            Save(true);
        }
    }
}