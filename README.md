# streamdeck-smtc

StreamDeck 插件：在按键上实时显示 Windows 当前媒体播放信息（SMTC / System Media Transport Controls），
按下按键发送播放控制热键。

## 功能

- 按键图标区显示当前播放的媒体信息（标题、封面图）
- 监听媒体会话事件：会话打开/关闭、焦点切换、播放状态/媒体属性/时间线变化
- 按下按键发送 `Ctrl+Alt+Num2` 热键（可在 `KeyboardSimulator.cs` 中修改）
- 基于 [BarRaider 的 StreamDeck-Tools](https://github.com/BarRaider/StreamDeck-Tools) 模板开发

## 构建

1. 用 Visual Studio 打开 `SD_STMC/SD_STMC.sln`（或 com.drinbol.smtc.csproj）
2. 还原 NuGet 包（StreamDeck-Tools、WindowsMediaController）
3. 编译后将输出部署到 StreamDeck 插件目录
   （`%APPDATA%\Elgato\StreamDeck\Plugins\`）

## 项目结构

```
SD_STMC/
├── manifest.json            # 插件清单（UUID/动作/作者信息）
├── PluginAction.cs          # 核心逻辑：SMTC 监听 + 按键响应
├── KeyboardSimulator.cs     # 模拟全局热键（SendInput）
├── PropertyInspector/       # 属性检查器 UI
└── Images/                  # 插件图标
```

## 许可证

MIT（基于 BarRaider StreamDeck-Tools 模板，同样遵循其 MIT 许可）
