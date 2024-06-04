# 森海克斯修改版写频软件 说明

[![downloads](https://img.shields.io/github/downloads/SydneyOwl/shx8x00-freq-writer-enhanced/total)](https://github.com/SydneyOwl/shx8x00-freq-writer-enhanced/releases?style=for-the-badge)
[![downloads@latest](https://img.shields.io/github/downloads/SydneyOwl/shx8x00-freq-writer-enhanced/latest/total)](https://github.com/SydneyOwl/shx8x00-freq-writer-enhanced/releases/latest?style=for-the-badge)
![version](https://img.shields.io/github/v/tag/sydneyowl/shx8x00-freq-writer-enhanced?label=version&style=flat-square?style=for-the-badge)
![senhaix](https://github.com/sydneyowl/senhaix-freq-writer-enhanced/actions/workflows/SenhaixFreqWriterBuild.yml/badge.svg)

> [!important]
> + 如有必要，请在任何操作前首先进行备份操作！
> + 本项目旨在为**森海克斯8x00以及gt12**提供一个**跨平台**写频方案，因此从`v0.2.2`
    及以后将停止维护winform版本（windows单平台）的写频软件 （**如无重大错误不再更新**
    ），但如果您有需要，仍然可以从[此处](https://github.com/SydneyOwl/senhaix-freq-writer-enhanced/releases/tag/v0.2.2)
    下载最后一个支持的版本，文件名为`xxx-Freq-Writer-v0.2.2.zip`。
> + “全局调试”功能仅用于开发使用，可能降低写频速度，如无需要建议不要开启！

## 简介

该写频软件使用`net6.0`+`Avalonia`
对森海克斯8600/8800/GT12的原官方写频软件（winform）进行了重构，并合并入一个软件中，提供对Windows、Linux、macOS三端的支持，在实现官方软件提供的所有功能基础上同时加入了其他功能，例如高级信道编辑以及蓝牙写频等。

目前适配情况：

+ 森海克斯8x00：已开发完成
+ 森海克斯GT12：**mac端仍在测试中！麻烦使用macos的友台在issues中给下反馈~您可以在beta版本的release中或action中下载**

| SHX8X00                      | GT12                         |
|------------------------------|------------------------------|
| ![](./readme_image/8x00.png) | ![](./readme_image/gt12.png) |

## 功能说明

### 运行平台

| -         | 森海克斯8800/8600/GT12通用版（Windows/Linux/macOS）                                                    | 森海克斯8800/8600 winform版（停止维护）                  | GT-12 winform版（停止维护） |
|-----------|-----------------------------------------------------------------------------------------------|-----------------------------------------------|----------------------|
| 支持的平台(理论) | windows7 sp1及以上[^1] / Ubuntu 16.04, 18.04, 20.04+ 或其他Linux发行版 / macOS 10.15+ (x64, Arm64)[^2] | 蓝牙版支持windows 8及以上[^3]，无蓝牙版支持windows xp sp2及以上 | windows 8及以上         |

[^1]: Windows 7 SP1 is supported
with [Extended Security Updates](https://learn.microsoft.com/troubleshoot/windows-client/windows-7-eos-faq/windows-7-extended-security-updates-faq)
installed.
[^2]: .NET 6 is supported in the Rosetta 2 x64 emulator.
[^3]:低于win10可能无法使用蓝牙写频（仅支持8800），且可能需要安装runtime

### 支持的功能

| -                                    | 森海克斯8800/8600/GT12通用版（Windows/Linux/macOS）[^4] | 森海克斯8800/8600 winform版（停止维护） | GT-12  winform版（停止维护） |
| ------------------------------------ | ------------------------------------------------------- | --------------------------------------- | ---------------------------- |
| 原有的所有功能                       | :white_check_mark:                                      | :white_check_mark:                      | :white_check_mark:           |
| 高级信道编辑（顺序调整、复制粘贴等） | :white_check_mark:                                      | :white_check_mark:                      | :white_check_mark:           |
| 蓝牙写频（试验性）                   | 仅8800[^5]                                              | 仅8800、仅蓝牙版支持                    | :heavy_minus_sign:           |
| （以下为支持的插件）                 |                                                         |                                         |                              |
| 开机画面修改                         | :white_check_mark:                                      | :white_check_mark:                      | :heavy_minus_sign:           |
| 打星助手                             | :white_check_mark:                                      | :white_check_mark:                      | :white_check_mark:           |

[^4]:该版本自带runtime，无需额外安装

[^5]:使用go rpc server实现（linux和macOS）

### 其他说明

#### 蓝牙

+ 插件默认只编译了x64的版本，需要其他版本请自行编译。
+ windows上没啥值得注意的，可以不勾选RPC方式写频~（当然勾了也行）
+ linux和mac端同样直接写频即可，默认已勾选RPC

#### 图片

##### v0.2.2后

![](./readme_image/macos.jpg)

![](./readme_image/so50.png)

![](./readme_image/BootImg.png)

---

##### v0.2.2前

<img src="./readme_image/dep-ble.png" style="zoom:75%;" />

<img src="./readme_image/dep-sat.png" style="zoom:75%;" />

## 编译指引

如有需要，您可以在`Github Actions`中直接下载`Nightly Build`。

![](./readme_image/artifact.png)

如您使用`Rider`，按照`SenhaixFreqWriter.csproj`中的注释选择合适的框架，直接publish即可！

如您使用命令行编译，按照`SenhaixFreqWriter.csproj`中的注释选择合适的框架后，可以参考`.github/workflows/build.yml`进行编译。

## 开发指引

+ 您可以自行实现跨平台版本写频软件的蓝牙功能，只需实现Utils/BLE/Interfaces/IBluetooth.cs中的方法即可。

+ 您可以自行修改SETTINGS.cs中的内容，手动启用不稳定的功能：

  ```csharp
  // 禁止在调试读写频时输出传输的数据
  public static bool DISABLE_DEBUG_CHAN_DATA_OUTPUT = true;
  // 卫星数据默认目录
  public static string DATA_DIR = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
      ? $"/Users/{Environment.UserName}/Library/Containers/com.sydneyowl/Data"
      : ".";
  // RPC插件默认开启的服务地址
  public const string RPC_URL = "http://127.0.0.1:8563/";
  // 各操作系统版本的插件名称
  public static string WINDOWS_BLE_PLUGIN_NAME = "BLEPlugin_windows_x64.exe";
  
  public static string OSX_BLE_PLUGIN_NAME = "BLEPlugin_macos_x64";
  
  public static string LINUX_BLE_PLUGIN_NAME = "BLEPlugin_linux_x64";
  
  // RPC服务端的启动参数，可用--help查看；输出仅在开启调试功能时能看到！
  public static string RPC_SERVER_PROCESS_ARGS = "--verbose --no-color";
  ```

+ 蓝牙插件若有**调试需要**，需要手动编译，或勾选了“手动控制RPC”，编译具体操作如下：

  1. 编译仓库中ble_plugin文件夹内的go项目，即`go mod tidy && go build`
     + 在windows上编译，需要gcc编译器
     + 在macOS上编译，需要安装xcode
     + 在linux上编译，需要bluez
  2. 直接双击打开编译产物，或者使用命令行指定参数：

    ```bash
  BLE RPC Server - Connect shx8x00 and c#
  
  Usage:
    BLE RPC Server [flags]
  
  Flags:
        --address string     RPC Server address (default "127.0.0.1")
    -h, --help               help for BLE
        --no-color           No color output in console
        --port int           RPC Server port (default 8563)
        --verbose            Print Debug Level logs
        --vverbose           Print Debug/Trace Level logs
  
    ```

  3. 运行写频软件，在写频方式->蓝牙中勾选`RPC`方式以及”手动控制“，点击搜索并连接；
  4. 正常读写频即可

## FAQ

+ linux平台上写频需要`sudo`！

## 其他

> [!WARNING]  
> 软件还在开发中，尚不稳定，欢迎提出 issues 和 pr!

卫星频率数据来源于[amateur-satellite-database](https://github.com/palewire/amateur-satellite-database)
，参考了[业余无线电 FM 卫星频率表](https://forum.hamcq.cn/d/351)
进行了多普勒修正。可以在此处查看更多:https://forum.hamcq.cn/d/351

shx8x00软件原理:见 [ble-connector](https://github.com/SydneyOwl/shx8800-ble-connector)
以及 [config-editor](https://github.com/SydneyOwl/shx8800-config-editor)

## 免责声明

- 本软件仅供技术交流和个人学习使用。任何个人或组织在使用本软件时必须**遵守中华人民共和国相关法律法规及无线电管理条例**。
- 如因使用本软件造成数据损失，**作者不承担任何法律责任**。数据无价，提醒您注意备份！

## 版本日志

`v0.1.a` 加入了蓝牙写频和便捷的信道更改。

`v0.1.0` 修复了即使蓝牙已连接也可能提示串口未连接的问题；修正了删除或清空信道时单元格未清空的问题。

`v0.1.1` 修复了 DataGridViewX 报错问题以及集成（实验性）开机画面修改。

`v0.1.2` 增加操作指引以及蓝牙连接状态指示。

`v0.1.3` 修复了信道操作后，会自动跳回 0 信道，还得拖动滚动条下去继续操作的问题。

`v0.1.4` 优化 UI，加入 GT12写频软件。

`v0.2.0` 修复蓝牙写频结束设备断开后，重新连接设备时搜索不到设备的问题/升级dotnet版本到4.6.2 LTS/更新所用依赖版本

`v0.2.1` 加入打星助手，更换了“关于”窗体

`v0.2.2` 重写官方winform写频软件，实现跨平台

`v0.3.0` 加入对GT12的支持,修复了8800写频软件中亚音读取错误的问题,加入开机画面生成及修改功能，加入打星助手，通过rpc实现了跨平台的蓝牙写频功能支持

`v0.3.1`优化使用插件的写频速度（linux和macOS），目前接近写频线速度

## Thanks...

+ `SenHaiX`的原版写频软件
+ `Avalonia` 的跨平台UI方案
+ `tinygo`的ble方案
+ `HIDSharp`的HID交互方案
+ [@rockliuxn](https://github.com/rockliuxn) 提供的图标，以及测试阶段的支持！
+ and more.....

## 许可证

本项目使用`The Unlicense`进行许可。

```markdown
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <https://unlicense.org>
```
