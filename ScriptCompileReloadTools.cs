using UnityEditor;
using UnityEditor.Compilation;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System;
/// <summary>
/// ____DESC:   手动reload domain 工具 
/// </summary>
public class ScriptCompileReloadTools
{
    /* 说明
     * 关于域重载 https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html
     * EditorApplication.LockReloadAssemblies()和 EditorApplication.UnlockReloadAssemblies() 最好成对
     * 如果不小心LockReloadAssemblies3次 但是只UnlockReloadAssemblies了一次 那么还是不会重载 必须也要但是只UnlockReloadAssemblies3次
     */

    const string menuEnableManualReload = "Tools/开启手动Reload Domain";
    const string menuDisenableManualReload = "Tools/关闭手动Reload Domain";
    const string menuRealodDomain = "Tools/Unlock Reload %t";
    const string kManualReloadDomain = "ManualReloadDomain";
    const string kIsLock = "DomainIsLock";
    const string kFirstEnterUnity = "FirstEnterUnity"; //是否首次进入unity 
    const string kReloadDomainTimer = "ReloadDomainTimer";

    /************************************************/
    static Stopwatch compileSW = new Stopwatch();
    /*需要存贮 因为reload之后静态数据清空了*/
    //加锁此时 成对
    static bool IsLock { get => PlayerPrefs.GetInt(kIsLock, -1) == 1; set => PlayerPrefs.SetInt(kIsLock, value ? 1 : 0); }
    //是否手动reload
    static bool IsManualReload => PlayerPrefs.GetInt(kManualReloadDomain, -1) == 1;
    //缓存数据 域重载之后数据会变成false 如果不是false 那么就要重载
    static bool tempData = false;
    /************************************************/



    [InitializeOnLoadMethod]
    static void InitCompile()
    {
        //**************不需要这个可以注释********************************
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
        EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

        //Bug 首次启动的时候 并不会马上设置
        //if (PlayerPrefs.HasKey(kManualReloadDomain))
        //{
        //    //首次启动的时候 并不会马上设置
        //    Menu.SetChecked(menuEnableManualReload, IsManualReload ? true : false);
        //    Menu.SetChecked(menuDisenableManualReload, IsManualReload ? false : true);
        //}
        FirstCheckAsync();
    }


    async static void FirstCheckAsync()
    {
        await System.Threading.Tasks.Task.Delay(100);
        //https://docs.unity.cn/cn/2021.3/ScriptReference/SessionState.html
        if (SessionState.GetBool(kFirstEnterUnity, true))
        {
            SessionState.SetBool(kFirstEnterUnity, false);
            Menu.SetChecked(menuEnableManualReload, IsManualReload ? true : false);
            Menu.SetChecked(menuDisenableManualReload, IsManualReload ? false : true);
            if (IsManualReload)
            {
                UnlockReloadDomain();
                LockRealodDomain();
            }
        }
    }

    private static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredEditMode:
                break;
            case PlayModeStateChange.ExitingEditMode:
                if (tempData)
                {
                    UnlockReloadDomain();
                    EditorUtility.RequestScriptReload();
                }
                break;
            case PlayModeStateChange.EnteredPlayMode:
                tempData = true;
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
        }
    }

    //当开始编辑脚本
    private static void OnCompilationStarted(object obj)
    {
        if (IsManualReload)
        {
            compileSW.Start();
            Debug.Log("<color=yellow>Begin Compile</color>");
        }
    }
    //结束编译
    private static void OnCompilationFinished(object obj)
    {
        if (IsManualReload)
        {
            compileSW.Stop();
            Debug.Log($"<color=yellow>End Compile 耗时:{compileSW.ElapsedMilliseconds} ms</color>");
            compileSW.Reset();
        }
    }

    private static void OnBeforeAssemblyReload()
    {
        if (IsManualReload)
        {
            Debug.Log("<color=yellow>Begin Reload Domain ......</color>");
            //记录时间
            SessionState.SetInt(kReloadDomainTimer, (int)(EditorApplication.timeSinceStartup * 1000));
        }

    }
    private static void OnAfterAssemblyReload()
    {
        if (IsManualReload)
        {
            var timeMS = (int)(EditorApplication.timeSinceStartup * 1000) - SessionState.GetInt(kReloadDomainTimer, 0);
            Debug.Log($"<color=yellow>End Reload Domain 耗时:{timeMS} ms</color>");
            LockRealodDomain();
        }
    }


    static void LockRealodDomain()
    {
        if (!IsLock)
        {
            IsLock = true;
            EditorApplication.LockReloadAssemblies();
        }
    }

    static void UnlockReloadDomain()
    {
        if (IsLock)
        {
            IsLock = false;
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    [MenuItem(menuEnableManualReload)]
    static void EnableManualReloadDomain()
    {
        Debug.Log("<color=cyan>开启手动 Reload Domain</color>");

        Menu.SetChecked(menuEnableManualReload, true);
        Menu.SetChecked(menuDisenableManualReload, false);

        PlayerPrefs.SetInt(kManualReloadDomain, 1);
        //编辑器设置 projectsetting->editor->enterPlayModeSetting
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        LockRealodDomain();
    }

    [MenuItem(menuDisenableManualReload)]
    static void DisenableManualReloadDomain()
    {
        Debug.Log("<color=cyan>关闭手动 Reload Domain</color>");

        Menu.SetChecked(menuEnableManualReload, false);
        Menu.SetChecked(menuDisenableManualReload, true);

        PlayerPrefs.SetInt(kManualReloadDomain, 0);
        UnlockReloadDomain();
        EditorSettings.enterPlayModeOptionsEnabled = false;
    }
    //手动刷新
    [MenuItem(menuRealodDomain)]
    static void ManualReload()
    {
        if (IsManualReload)
        {
            UnlockReloadDomain();
            EditorUtility.RequestScriptReload();
        }
    }

    //[UnityEditor.Callbacks.DidReloadScripts]
    //static void OnEndReload()
    //{
    //    if (IsManualReload)
    //    {
    //        Debug.Log($"<color=yellow>End Reload Domain</color>");
    //        LockRealodDomain();
    //    }
    //}

}