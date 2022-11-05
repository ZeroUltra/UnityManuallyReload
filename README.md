## Unity 手动编译 Reload 脚本

这是个自定义reload domain工具,加快工作流,减少等待

### 在Unity中遇到的问题

在unity工作流中,`修改脚本->编译脚本->reload domain(重载域)-> 进入play`

通过区分assembly能加快编译,但是reload domain 却很慢,每次编译之后都要reload domain,而且进入播放前也会reload domain

示例:

![0](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052102596.gif)

写程序经常会`Ctrl+s`,一旦保存,就会重新编译,继而触发reload. 有时候会返回Unity编辑器,只是查看场景,并不想reload,会让我们漫长等待.

Unity有个Enter Play Mode Setting  [可配置的进入运行模式 - Unity 手册](https://docs.unity.cn/cn/2021.3/Manual/ConfigurableEnterPlayMode.html)

![image-20221105210343196](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052103211.png)

禁用`Reload Domain` 可以快速进入播放模式.但是每次修改完脚本还是会重新reload. 还有就是对于`静态数据如果没有重新reload 还是会保持之前的数据`(建议不要禁用,真的很坑) 具体查看:https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html

当然有些通过禁用`Auto refresh`,使用`ctrl+r`,来手动刷新也可以,但是如果导入的是图片等其他资源,也要刷新.

所以还是要手动reload最可靠

### 如何解决频繁Reload

我们要做的就是,添加新脚本或者修改脚本后,经过确认无误之后,我们在reload,而且在进入 play模式,如果已经重载过域,不会二次reload

unity 提供了两个API `EditorApplication.LockReloadAssemblies();`和` EditorApplication.UnlockReloadAssemblies();`一个加锁,一个解锁.

配合` Enter Play Mode Setting` 就可以大大减少时间.



效果图:

![111](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052126333.gif)

具体代码如下

```c#
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
    const string menuRealodDomain = "Tools/Unlock Reload %t"; //快捷键 Ctrl+t

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
```

### 使用方法

导入脚本之后,菜单栏`Tools->Tools/开启手动Reload Domain`  ,然后需要reload 时候按下`Ctrl+t`即可

### 参考

[Unity 关闭脚本编译 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/441996008)