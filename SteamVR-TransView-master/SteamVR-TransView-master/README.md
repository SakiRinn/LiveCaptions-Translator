# SteamVR-TransView
VRChat VR 实时翻译悬浮工具 | 语音/文字翻译实时叠加显示

## 功能介绍
- 读取 LiveCaptions 翻译数据库，实时获取翻译内容
- 通过 OpenVR/SteamVR 悬浮层在 VR 画面中叠加显示翻译文字
- 解决 VRChat VR 模式下无法查看语音翻译字幕的问题
- 轻量运行，不影响游戏性能

## 前置依赖
1. 安装并运行 [LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator)
2. 确保生成 `translation_history.db` 数据库文件
3. Windows 10/11 64位
4. 已安装 SteamVR 并正常运行

## 使用步骤
1. 独立运行时：`SteamVR-TransView.exe --db-path "D:\path\to\translation_history.db"`
2. 已合并到 LiveCaptions Translator 时，直接点击主窗口标题栏的 `VR` 按钮，主程序会自动传入数据库路径
3. 先启动 SteamVR，再进入 VRChat VR 即可看到悬浮翻译

## 文件说明
- `SteamVR-TransView.exe`：已打包的单文件面板（内嵌 Python、openvr、字体和 DLL）
- `steampy_overlay.py`：源码，修改后可用 `pyinstaller steampy_overlay.spec` 重新打包
- `steampy_overlay.spec`：打包配置
- `font/`、`libopenvr_api_64.dll`：源码运行和重新打包所需资源

## 注意事项
- 数据库路径必须填写**绝对路径**，否则无法读取翻译
- 先启动 SteamVR，再运行本工具
