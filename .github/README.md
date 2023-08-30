## Unity 手动编译 Reload 脚本

这是一个自定义reload domain工具,加快工作流,减少等待.测试版本是Unity2021,理论上来说2020以上都可.

Unity2021(2020还好)不知是哪个版本,明显感觉编译reload时间冗长🥱😪😯

## 在Unity中遇到的问题

在unity工作流中,`修改脚本->编译脚本->reload domain(重载域)->进入play`

通过区分assembly能加快编译,但是reload domain 却很慢,每次编译之后都要reload domain,且进入play模式也会reload domain

示例:

![0](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052102596.gif)

写程序经常会`Ctrl+s`,一旦保存,就会重新编译,继而触发reload. 有时候会返回Unity编辑器,只是查看场景,并不想reload,会让我们漫长等待.

Unity有个Enter Play Mode Setting  [可配置的进入运行模式 - Unity 手册](https://docs.unity.cn/cn/2021.3/Manual/ConfigurableEnterPlayMode.html)

![image-20221105210343196](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052103211.png)

禁用`Reload Domain` 可以快速进入播放模式.但是每次修改完脚本还是会重新reload. 还有就是对于`静态数据如果没有重新reload 还是会保持之前的数据`(**建议不要禁用,真的很坑**) 具体查看:https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html

当然有些通过禁用`Auto refresh`,使用`ctrl+r`,来手动刷新也可以,但如果导入的是图片等其他资源,也要刷新,就比较麻烦

## 如何解决频繁Reload

需要做的就是,添加新脚本或者修改脚本后,经过确认无误之后,我们才reload,而且在进入 play模式,如果已经reload,不会二次reload

unity 提供了两个API `EditorApplication.LockReloadAssemblies();`和` EditorApplication.UnlockReloadAssemblies();`一个加锁,一个解锁.

配合` Enter Play Mode Setting` 就可以大大减少时间.

效果图:

![111](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052126333.gif)



## 安装方法

1. 直接下载脚本代码,然后放到Editor文件夹中
2. 使用Unity PackageManager,add git地址

## 使用方法

* 菜单栏`Tools/Reload Domain/Enable Manually Reload Domain`  开启手动reload
* 需要手动reload 时候按下`Ctrl+t`快捷键,(或者直接点菜单)

## 更新日志

#### v1.0.1

`如果开启,新建脚本或者导入插件的时候,都手动reload 一下`

* 每次进入play模式之前,会检查是否需要reload,已经reload就不用了,没有的话自动reload,这样能保证每次数据的正确性

#### v1.0.2

* 添加了Unity Package
* 避免了可能不小心手动多次reload情况
* 修改了数据保存代码逻辑
* 修复新建脚本或者assembly的时候没有刷新的问题

#### v1.0.3

* 修复设置未保存的问题 (原来使用了Unity`ScriptableSingleton<T>`,使用过程中发现保存了数据但是加载的时候不会反序列化)

### 参考

[Unity 关闭脚本编译 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/441996008)

### 推荐 

[Misaka-Mikoto-Tech/UnityScriptHotReload: HotReload Unity C# script without exit play mode and keep the running context unchanged (github.com)](https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload)

[【Unity】引擎编译时间优化 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/601065788)

