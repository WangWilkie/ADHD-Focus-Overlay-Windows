# FocusFrame

FocusFrame 是一个基于 C#、WPF 和 .NET 8 的 Windows 多屏聚焦遮罩工具。程序会为每块显示器创建独立遮罩窗口，保证所有活动屏幕都被完整覆盖，同时只在当前活动屏幕上保留一个清晰的聚焦区域。

## 功能概览

- 覆盖所有活动显示器的全屏遮罩
- 任意时刻只保留一个聚焦区域
- 每块显示器独立遮罩，降低混合分辨率、负坐标布局下的错位风险
- 遮罩颜色和透明度可调
- 无边框聚焦开窗，边缘和角支持缩放
- 支持切换屏幕和隐藏/恢复遮罩两组全局快捷键
- 设置通过 JSON 持久化
- 支持通过 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 开机自启

## 支持系统

- Windows 10 2004 及以上
- Windows 11

## 构建

1. 安装 .NET 8 SDK 或更高版本
2. 在项目根目录运行：

```bat
build.bat
```

发布输出位于：

```text
dist\ADHDFocusOverlay.exe
```

## 如何运行

- 双击运行 [dist/ADHDFocusOverlay.exe](/F:/ADHD/dist/ADHDFocusOverlay.exe)
- 程序启动后会常驻系统托盘
- 托盘菜单提供：
  - `设置`
  - `切换屏幕`
  - `隐藏/恢复遮罩`
  - `退出`

## 快捷键

- `切换屏幕`：默认是 `Alt + Shift + Q`
- `隐藏/恢复遮罩`：默认是 `Alt + Shift + S`
- 设置窗口支持直接录制这两组快捷键：点击输入框后，直接按下想要的组合即可

## 如何移动聚焦区域

- 在同一块屏幕内移动：
  按住 `Alt + Shift`，再用鼠标左键拖动聚焦区域内部
- 切换到另一块屏幕：
  按下你配置的“切换屏幕”快捷键

## 如何调整聚焦区域大小

- 把鼠标移动到聚焦区域的边缘或角落
- 使用鼠标左键拖动进行缩放
- 缩放命中区域不可见，但内部保留了 18px 热区

## 如何启用开机自启

1. 通过托盘图标打开“设置”
2. 勾选“登录 Windows 后自动启动”

## 配置文件

配置文件路径：

```text
%AppData%\ADHDFocusOverlay\settings.json
```

示例：

```json
{
  "TintColor": "#606060",
  "DimOpacity": 170,
  "DesaturationEnabled": false,
  "FocusX": 760,
  "FocusY": 290,
  "FocusWidth": 900,
  "FocusHeight": 620,
  "ActiveMonitorId": "\\\\.\\DISPLAY1",
  "MovePickModifiers": 5,
  "MovePickVirtualKey": 81,
  "ToggleOverlayModifiers": 5,
  "ToggleOverlayVirtualKey": 83,
  "LaunchAtStartup": false
}
```
