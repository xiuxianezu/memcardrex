# MemcardRex
### 高级 PlayStation 1 记忆卡编辑器
![memcardrex](https://github.com/user-attachments/assets/82553694-5cd2-49e8-b900-524dc32ccade)

**快速下载：**
* [Windows 最新版（上游原版）](https://github.com/ShendoXT/memcardrex/releases/tag/9909f69)
* [macOS 最新版（上游原版）](https://github.com/ShendoXT/memcardrex/releases/tag/e85f601)
* Linux 版本处于测试阶段。[编译说明](https://github.com/ShendoXT/memcardrex/tree/master/MemcardRex.Linux)
* **简体中文语言包编译版（本仓库 Releases，Windows / Linux / macOS 三平台）**：见下方 [Releases](https://github.com/xiuxianezu/memcardrex/releases) 下载，界面已内置简体中文。

**关于支持与开发：**
<br>这个项目已经存在了近 20 年（首个版本发布于 2007 年）。<br>
我在 2014 年将其开源并放到 GitHub 上，因为当时留给它的时间越来越少。<br>
我把项目放到 GitHub，是希望社区能让它继续活下去。<br>
社区确实做出了一些贡献，例如 VMP 与 PSV 支持、PS3 记忆卡适配器支持等……<br><br>
但对更多功能的需求总是落在我身上，而（我希望）我做到了：<br>
你们让我做 macOS 版，我做了；让我做 Linux 版，我也做了。<br>
可反过来，每当我放出一个捐赠链接，周围就一片寂静。<br>
20 年里只有 **一位** 朋友捐赠过，我由衷感谢他；其他人一拿到他们要求的功能就转身离开了……<br>
<br>
所以对你来说一切都只关乎钱？是的。全世界只有我一个人需要靠钱活下去。<br>
但这不是钱的问题，而是支持的表示。你们没有给这株植物浇水，它已经枯死了。<br>
人们并不感激我抽出生命中的时间，为你们不断要求的额外功能付出……这连一杯咖啡都不值吗？是我不好。<br>
不过我也不想再在这上面花费任何时间了……<br>
<br>
让下一款更好的编辑器，从新程序员——或者 AI？——的指尖诞生吧。<br>


<br>**功能特性：**
* 标签页界面——可以同时打开多张记忆卡。
* 支持复制、删除、恢复、导出、导入和编辑存档。
* 支持撤销/重做，带有可回溯的历史记录列表。
* 支持第三方存档编辑器的插件系统。
* 支持与真实记忆卡通信的硬件接口。
* PocketStation 支持（读取序列号、导出 BIOS、写入 PC 时间）。

<br>**系统要求：**
* .NET 8。

<br>**支持的记忆卡格式：**
* ePSXe/PSEmu Pro 记忆卡（*.mcr）
* DexDrive 记忆卡（*.gme）
* pSX/AdriPSX 记忆卡（*.bin）
* Bleem! 记忆卡（*.mcd）
* VGS 记忆卡（*.mem、*.vgs）
* PSXGame Edit 记忆卡（*.mc）
* DataDeck 记忆卡（*.ddf）
* WinPSM 记忆卡（*.ps）
* Smart Link 记忆卡（*.psm）
* MCExplorer（*.mci）
* PCSX ReARMed/RetroArch（*.srm）
* PSP 虚拟记忆卡（*.VMP）
* PS3 虚拟记忆卡（*.VM1）
* PS Vita「MCX」PocketStation 记忆卡（*.BIN）
* POPStarter 虚拟记忆卡（*.VMC）
* MiSTer FPGA（PSX 核心）记忆卡（*.sav）

<br>**支持的单个存档格式：**
* PSXGame Edit 单存档（*.mcs）
* XP、AR、GS、Caetla 单存档（*.psx）
* Memory Juggler（*.ps1）
* Smart Link（*.mcb）
* Datel（*.mcx、*.pda）
* RAW 单存档
* PS3 虚拟存档（*.psv）

### 硬件接口
MemcardRex 支持通过外部设备与真实记忆卡通信。
<br>请确保在「选项 → 偏好设置」中选择正确的 COM 端口。

<details>
<summary>1. DexDrive</summary>
最早在记忆卡与 PC 之间传输数据的方式，虽然有点小毛病。
<br>如果遇到问题，请拔掉 DexDrive 的电源，从 COM 口拔下后重新连接。

建议给 DexDrive 接上电源线，否则部分记忆卡可能无法被识别。
<br>支持原生 COM 口或基于 USB 的转接器。
</details>

<details>
<summary>2. MemCARDuino</summary>
MemCARDuino 是一款面向多种 Arduino 开发板的开源记忆卡通信软件。
<br>https://github.com/ShendoXT/memcarduino
</details>

<details>
<summary>3. PS1CardLink</summary>
PS1CardLink 是用于实体 PlayStation 与 PSOne 主机的软件。
<br>它需要一条官方或自制的 TTL 串口线来与 PC 通信。

有了它，你的主机就变成一个类似 DexDrive 和 MemCARDuino 的记忆卡读取器。

MemcardRex 还可以通过串口桥接器（如 [esp-link](https://github.com/jeelabs/esp-link)）远程访问串口。
<br>它可以很方便地塞进一台没有外部硬件端口的 PSOne 里。
<br>https://github.com/ShendoXT/ps1cardlink
</details>

<details>
<summary>4. Unirom</summary>
Unirom 是 PlayStation 与 PSOne 主机的 Shell（引导工具）。
<br>它需要一条官方或自制的 TTL 串口线来与 PC 通信。
<br>https://unirom.github.io
</details>

<details>
<summary>5. PS3 记忆卡适配器（PS3 Memory Card Adaptor）</summary>
PS3 记忆卡适配器是索尼官方 USB 适配器，可以在 PlayStation 3 上读写 PS1 记忆卡。
<br>要在 Windows PC 上使用它，需要安装一个自定义 USB 驱动。

这个 USB 驱动可以借助 [Zadig](https://zadig.akeo.ie) 轻松创建并安装，步骤如下：
* 将 PS3 记忆卡适配器插入空闲 USB 口，然后启动 Zadig。
* Zadig 应将 PS3 MCA 显示为「Unknown Device」。确认 USB ID 匹配：054C 02EA。
* 勾选 Edit 复选框，将设备命名为「PS3 Memory Card Adaptor」。
* 在驱动选项列表中选择「WinUSB」，然后点击 Install Driver 按钮。
    - 如果需要 LibUSB 驱动支持，请将「libusb-1.0.dll」放入 MemcardRex 目录（仅限 2.0 RC1 及以后版本）。
* 约 30 秒后，Zadig 应显示驱动安装成功。

安装好 USB 驱动并插上 PS3 记忆卡适配器后，你就可以读写和格式化 PS1 记忆卡了。
</details>

### 致谢
**作者：**
<br>Alvaro Tanarro, bitrot-alpha, Damián Parrino, kevh182, KuromeSan, lmiori92, Nico de Poel, Robxnano, Shendo。

**Beta 测试人员：**
<br>Gamesoul Master, Xtreme2damax, Carmax91 和 NKO。

**感谢：**
<br>@ruantec, Cobalt, TheCloudOfSmoke, RedawgTS, Hard core Rikki, RainMotorsports, Zieg, Bobbi, OuTman, Kevstah2004, Kubusleonidas, Frédéric Brière, Mark James, Cor'e, DeadlySystem, Padraig Flood 和 Martin Korth (nocash)。

### 简体中文语言包（zh-CN）
Windows 版内置了简体中文语言包（`Languages\zh-CN.xml`）和轻量级本地化层（`Support\Localization.cs`）。
程序启动时通过精确匹配查找翻译所有用户可见界面文本；因此任何尚未收录的新英文串会暂时保持英文，直到加入语言包。

如何自定义翻译：
* 要覆盖内置翻译，请将修改后的 `Languages\zh-CN.xml` 放到可执行文件旁（`MemcardRex.exe\Languages\zh-CN.xml`）。外部文件优先于嵌入资源。
* 要将翻译编译进应用，请重新编译解决方案——XML 会自动作为嵌入资源（`EmbeddedResource Languages\zh-CN.xml`）。
* 新增条目格式为 `<string><source>英文原文</source><target>中文</target></string>`；换行请使用 `&#10;`，并保持 `<source>` 与界面显示的英文完全一致。

三个平台版本（Windows / Linux / macOS）均内置简体中文语言包：

* **Windows** —— `MemcardRex.Windows\Languages\zh-CN.xml` + `Support\Localization.cs`。Designer/代码中的界面字符串均用 `Localization.T("...")` 包裹；窗体加载后由 `ApplyToForm` 翻译。
* **Linux（GTK）** —— `MemcardRex.Linux\Languages\zh-CN.xml` + `Localization.cs`。嵌入的 `.ui` 模板在加载时由 `Localization.Builder(...)` 翻译（标签、标题、提示按精确匹配）；代码字符串用 `Localization.T("...")` 包裹。
* **macOS** —— `MemcardRex.macOS\Languages\zh-CN.xml` + `Localization.cs`。主菜单、工具栏和对话框视图在运行时由 `ApplyToMenus` / `ApplyToWindow` / `ApplyToView` 翻译；代码字符串用 `Localization.T("...")` 包裹。

同一个 `zh-CN.xml` 字典（236 条翻译）已嵌入到每个平台版本。按设计保留不译的内容：存档区域值（America/Europe/Japan）、硬件设备名（DexDrive、MemCARDuino 等）、文件格式名、表情符号工具栏图标、应用/版本标题 `MemcardRex 2.0 beta`。
