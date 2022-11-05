using UnityEditor;
using UnityEditor.Compilation;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine;
/// <summary>
/// ____DESC:      
/// </summary>
public class ScriptCompileReloadTools
{
    /*
     * 关于域重载 https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html
     * EditorApplication.LockReloadAssemblies()和 EditorApplication.UnlockReloadAssemblies() 最好成对
     * 如果不小心LockReloadAssemblies3次 但是只UnlockReloadAssemblies了一次 那么还是不会重载 必须也要但是只UnlockReloadAssemblies3次
     */
    static Stopwatch compileSW = new Stopwatch();

    const string menuEnableManualReloadDomain = "Tools/开启手动Reload Domain";
    const string menuDisenableManualReloadDomain = "Tools/关闭手动Reload Domain";
    const string menuRealodDomain = "Tools/Unlock Reload %t";

    const string kManualReloadDomain = "ManualReloadDomain";

    [InitializeOnLoadMethod]
    static void InitCompile()
    {
        //不需要这个可以注释
        CompilationPipeline.compilationStarted -= OncompilationStarted;
        CompilationPipeline.compilationStarted += OncompilationStarted;
        CompilationPipeline.compilationFinished -= OncompilationFinished;
        CompilationPipeline.compilationFinished += OncompilationFinished;

        //编辑器设置 projectsetting->editor->enterPlayModeSetting
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
    }


    //当开始编辑脚本
    private static void OncompilationStarted(object obj)
    {
        compileSW.Start();
        Debug.Log("<color=yellow>开始编译脚本</color>");
    }
    //结束编译
    private static void OncompilationFinished(object obj)
    {
        compileSW.Stop();
        Debug.Log($"<color=yellow>结束编译脚本 耗时:{compileSW.ElapsedMilliseconds} ms</color>");
        compileSW.Reset();
    }


    //开启手动reload
    [MenuItem(menuEnableManualReloadDomain)]
    static void EnableManualReloadDomain()
    {
        Menu.SetChecked(menuEnableManualReloadDomain, true);
        Menu.SetChecked(menuDisenableManualReloadDomain, false);

        PlayerPrefs.SetInt(kManualReloadDomain, 1);
        Debug.Log("<color=cyan>开启手动 Reload Domain</color>");

        EditorApplication.LockReloadAssemblies();
    }
    //关闭手动reload
    [MenuItem(menuDisenableManualReloadDomain)]
    static void DisenableManualReloadDomain()
    {
        Menu.SetChecked(menuEnableManualReloadDomain, false);
        Menu.SetChecked(menuDisenableManualReloadDomain, true);

        PlayerPrefs.SetInt(kManualReloadDomain, 0);

        Debug.Log("<color=cyan>关闭手动 Reload Domain</color>");
        EditorApplication.UnlockReloadAssemblies();
        EditorApplication.UnlockReloadAssemblies();
        EditorApplication.UnlockReloadAssemblies();
    }
    //手动刷新
    [MenuItem(menuRealodDomain)]
    static void ManualReload()
    {
        if (PlayerPrefs.GetInt(kManualReloadDomain, -1) == 1)
        {
            Debug.Log("Reload Domain ......");
            EditorApplication.UnlockReloadAssemblies();
            EditorUtility.RequestScriptReload();
        }
    }

    //当Reload Domain
    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnReloadDomain()
    {
        //重载之后再次锁住
        if (PlayerPrefs.GetInt(kManualReloadDomain, -1) == 1)
        {
           EditorApplication.LockReloadAssemblies();
           Debug.Log("RealodDomain 完成");
        }
    }
}