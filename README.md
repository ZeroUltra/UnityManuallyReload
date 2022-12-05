## Unity 手动编译 Reload 脚本

这是个自定义reload domain工具,加快工作流,减少等待.测试版本是Unity2021,理论上来说2020以上都可.

Unity2021(2020还好)不知是哪个版本,明显感觉编译reload时间冗长🥱😪😯

脚本地址:[UnityManualReload (github.com)](https://github.com/ZeroUltra/UnityManualReload/blob/main/ScriptCompileReloadTools.cs)

### 在Unity中遇到的问题

在unity工作流中,`修改脚本->编译脚本->reload domain(重载域)-> 进入play`

通过区分assembly能加快编译,但是reload domain 却很慢,每次编译之后都要reload domain,而且进入播放前也会reload domain

示例:

![0](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052102596.gif)

写程序经常会`Ctrl+s`,一旦保存,就会重新编译,继而触发reload. 有时候会返回Unity编辑器,只是查看场景,并不想reload,会让我们漫长等待.

Unity有个Enter Play Mode Setting  [可配置的进入运行模式 - Unity 手册](https://docs.unity.cn/cn/2021.3/Manual/ConfigurableEnterPlayMode.html)

![image-20221105210343196](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052103211.png)

禁用`Reload Domain` 可以快速进入播放模式.但是每次修改完脚本还是会重新reload. 还有就是对于`静态数据如果没有重新reload 还是会保持之前的数据`(**建议不要禁用,真的很坑**) 具体查看:https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html

当然有些通过禁用`Auto refresh`,使用`ctrl+r`,来手动刷新也可以,但是如果导入的是图片等其他资源,也要刷新.

所以还是要手动reload最可靠

### 如何解决频繁Reload

我们要做的就是,添加新脚本或者修改脚本后,经过确认无误之后,我们才reload,而且在进入 play模式,如果已经reload,不会二次reload

unity 提供了两个API `EditorApplication.LockReloadAssemblies();`和` EditorApplication.UnlockReloadAssemblies();`一个加锁,一个解锁.

配合` Enter Play Mode Setting` 就可以大大减少时间.

效果图:

![111](https://raw.githubusercontent.com/ZeroUltra/MediaLibrary/main/Imgs/202211052126333.gif)



### 使用方法

脚本导入Editor文件夹之后,菜单栏`Tools->Tools/开启手动Reload Domain`  ,然后需要reload 时候按下`Ctrl+t`即可.

`如果开启,新建脚本或者导入插件的时候,都手动reload 一下`

~~需要注意的是,因为脚本禁用了`reload domain`,如果运行前没有手动reload,静态数据不会清空,可能会产生一些问题~~

**新改进2022年11月8日15:32:33**

* 每次进入play模式之前,会检查是否需要reload,已经reload就不用了,没有的话自动reload,这样能保证每次数据的正确性

### 参考

[Unity 关闭脚本编译 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/441996008)