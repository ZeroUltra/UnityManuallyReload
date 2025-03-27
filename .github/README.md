## Unity 手动编译 Reload 脚本

这是一个自定义reload domain工具,`作用是减少Reload Domain次数,从而降低等待`.测试版本是Unity2021,理论上来说2020以上都可.

Unity2021(2020还好)不知是哪个版本,明显感觉编译reload时间冗长🥱😪😯

## 在Unity中遇到的问题

在unity工作流中,`修改脚本->编译脚本->reload domain(重载域)->进入play`

通过区分assembly能加快编译,但是reload domain 却很慢,每次编译之后都要reload domain,且进入play模式也会reload domain

示例:

![0](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052102596.gif)

写程序经常会`Ctrl+s`,一旦保存,就会重新编译,继而触发reload. 有时候会返回Unity编辑器,只是查看场景,并不想reload,会让我们漫长等待.

Unity有个Enter Play Mode Setting  [可配置的进入运行模式 - Unity 手册](https://docs.unity.cn/cn/2021.3/Manual/ConfigurableEnterPlayMode.html)

 ![](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052103211.png)

禁用`Reload Domain` 可以快速进入播放模式.但是每次修改完脚本还是会重新reload. 还有就是对于`静态数据如果没有重新reload 还是会保持之前的数据`(**建议不要禁用,真的很坑**) 具体查看:[Unity - Manual: Domain Reloading (unity3d.com)](https://docs.unity3d.com/2022.3/Documentation/Manual/DomainReloading.html)

当然有些通过禁用`Auto refresh`,使用`ctrl+r`,来手动刷新也可以,但如果导入的是图片等其他资源,也要刷新,就比较麻烦

## 如何解决频繁Reload

需要做的就是,添加新脚本或者修改脚本后,经过确认无误之后,我们才reload,而且在进入 play模式,如果已经reload,不会二次reload

unity 提供了两个API `EditorApplication.LockReloadAssemblies();`和` EditorApplication.UnlockReloadAssemblies();`一个加锁,一个解锁.

配合` Enter Play Mode Setting` 就可以大大减少时间.

效果图:

![111](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052126333.gif)



## 安装方法

两种安装方法

1. 直接下载脚本代码,然后放到Editor文件夹中
2. 使用Unity PackageManager,add git地址

## 使用方法

1. Unity中`Edit->ProjectSetting->Manually Reload Domain` ,勾选上`Enable Manually Reload`
2. 当修改完脚本之后,按下`F5`进行reload domain (会自动检测是否需要reload domain)
3. 如遇一只🔒的情况按下`ctrl+t`强制reload domain

![image-20250327215023103](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202503272150773.png) 



参数说明:

* `Enable Fully Manually Reload` 完全手动Reload(指不会在运行前检测是否需要reload),如果为true,需完全手动触发,完全由自己决定什么时候reload
* `Editor Scripts Manually Reload?` 是否Editor代码也需手动Reload?(当且仅当编辑的代码属于`Editor`才有效, 即如果为`true`,那么editor代码编译完后也不会readlo domain 需要手动调用, 如果为`false`,editor代码编译完后会自动调用reload,在写editor GUI的时候可设置为false,方便快速查看)

## 注意点

* 开启之后,新建脚本,修改脚本或者导入插件的时候,只会编译而不会reload。通过`F5`或者菜单`Tools/Reload Domain/Unlock Reload`,手动reload,(相关快捷键自行修改脚本代码)

* 记住，`Unity必须reload之后，才能调用相关域`。例如新建mono脚本如果不reload是挂不到gameobject上的。

* 当设置`Enable Fully Manually Reload=false`(完全手动模式)时,进入Play模式如果已经reload，则直接进入,如果没有reload则会强制reload（主要是为了重置static数据）; 如果为`Enable Fully Manually Reload=true`,不会执行任何相关reload,进入play模式也不会重置static数据,具体查看 [Unity - Manual: Domain Reloading (unity3d.com)](https://docs.unity3d.com/2022.3/Documentation/Manual/DomainReloading.html)

* 如遇到锁住问题（unity右下角一直出现🔒的情况,可能在导入新插件的时候发生） 按下Ctrl+T强制重载。

需要注意的一点:

​	当勾选`Enable Manually Reload`的时候会自动设置`Enter play mode setting`,如果由于其他操作重新设置了 enter play mode setting,请手动设置成下图正确选项,或者重新勾选`Enable Manually Reload`进行自动设置.

​	![image-20240228000722436](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202402280007905.png)

## 更新日志

#### v1.0.8

* 小修改,如果未启用 `Enable Manually Reload`,将不显示编译和reload耗时信息
* 修改了setting ui界面

#### v1.0.7

* 添加完全手动模式。开启此模式后，需要完全手动reload。

 ![](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202401152354322.png)

#### v1.0.6

* 添加说明
* 如遇到锁住问题 按下Ctrl+Alt+T强制重载

 ![image-20231227195442245](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202312271954071.png)

#### v1.0.5

* 修复当取消`EnableManuallyReload`勾选时,不会正确reload的bug

#### v1.0.4

* 将设置移到ProjectSetting中

* 新增功能`编辑器代码是否手动Reload`

  ![image-20231102201502323](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202311022015470.png)

#### v1.0.3

* 修复设置未保存的问题 (原来使用了Unity`ScriptableSingleton<T>`,使用过程中发现保存了数据但是加载的时候不会反序列化)

#### v1.0.2

* 添加了Unity Package
* 避免了可能不小心手动多次reload情况
* 修改了数据保存代码逻辑
* 修复新建脚本或者assembly的时候没有刷新的问题

* 

#### v1.0.1

* 每次进入play模式之前,会检查是否需要reload,已经reload就不用了,没有的话自动reload,这样能保证每次数据的正确性



`有什么问题,欢迎提Issues`



### 推荐 

[handzlikchris/FastScriptReload](https://github.com/handzlikchris/FastScriptReload)

[Misaka-Mikoto-Tech/UnityScriptHotReload:](https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload)

[【Unity】引擎编译时间优化 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/601065788)

