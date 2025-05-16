<div align="center">

# LiveCaptions Translator

### *A Real-time audio/speech translation tool based on Windows LiveCaptions*

[![Master Build](https://github.com/SakiRinn/LiveCaptions-Translator/actions/workflows/dotnet-build.yml/badge.svg?branch=master)](https://github.com/SakiRinn/LiveCaptions-Translator/actions/workflows/dotnet-build.yml)
[![GitHub Release](https://img.shields.io/github/v/release/SakiRinn/LiveCaptions-Translator?label=Latest)](https://github.com/SakiRinn/LiveCaptions-Translator/releases/latest)
[![Wiki](https://img.shields.io/badge/Wiki-GitHub-blue)](https://github.com/SakiRinn/LiveCaptions-Translator/wiki)
[![GitHub License](https://img.shields.io/github/license/SakiRinn/LiveCaptions-Translator)](https://github.com/SakiRinn/LiveCaptions-Translator/blob/master/LICENSE.txt)
[![GitHub Stars](https://img.shields.io/github/stars/SakiRinn/LiveCaptions-Translator)](https://github.com/SakiRinn/LiveCaptions-Translator/stargazers)

</div>

## Overview

**✨ LiveCaptions Translator = Windows LiveCaptions + Translate API ✨**

A lightweight utility that seamlessly integrates translation APIs with Windows Live Captions, enabling real-time speech translation without requiring a Copilot+ PC.

**🚀 Quick Start:** Download from [Releases](https://github.com/SakiRinn/LiveCaptions-Translator/releases) and start with a single click!

<div align="center">
  <img src="images/preview.png" alt="Preview of LiveCaptions Translator" width="95%" />
  <br>
  <em>Preview of LiveCaptions Translator</em>
  <br>
</div>

## Features

- **🔄 Seamless Integration** \
  Automatically invoke Windows LiveCaptions without opening separate windows. Provides a unified experience for real-time audio/speech translation.
  > 💡 **Note:** Speech recognition and translation requires enabling *Include microphone audio* in Windows LiveCaptions.

- **🎨 Modern Interface** \
  Clean and intuitive Fluent UI design aligned with modern Windows aesthetics. Easy to use for all technical skill levels.

- **🌓 Adaptive Theme** \
  Automatically switches between light and dark themes based on system settings. Reduces eye strain and maintains visual coherence.

- **🌐 Multiple Translation Services** \
  Supports various translation engines, including 2 out-of-the-box Google Translate.
  Implemented translation engines are shown in the table below:

  <div align="center">

  | API                                 | Type        | Hosting     |
  | ----------------------------------- | ----------- | ----------- |
  | [Ollama](https://ollama.com)        | LLM-based   | Self-hosted |
  | OpenAI Compatible API               | LLM-based   | Online      |
  | [OpenRouter](https://openrouter.ai) | LLM-based   | Online      |
  | Google Translate                    | Traditional | Online      |
  | DeepL                               | Traditional | Online      |
  | Youdao                              | Traditional | Online      |
  | Baidu Translate                     | Traditional | Online      |
  | MTranServer                         | Traditional | Self-hosted |

  </div>

  We strongly recommend using **LLM-based** translation engines, as LLMs excel at handling incomplete sentences and are adept at understanding context.

- **⚙️ Flexible Controls** \
  Supports Always-on-top window and pause/resume translation flexibly.

- **📝 History Management** \
  Record translated content for future reference. Perfect for meetings, lectures, and important discussions.

- **📋 Easy Access** \
  Copy translated text with a single click for quick sharing or saving.


## Prerequisites

<div align="center">

| Requirement                                                                                                           | Details                                     |
| --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| <img src="https://img.shields.io/badge/Windows-11%20(22H2+)-0078D6?style=for-the-badge&logo=windows&logoColor=white"> | With LiveCaptions support.                  |
| <img src="https://img.shields.io/badge/.NET-8.0+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">             | Recommended. Not test in previous versions. |

If you can't install the .NET runtime separately, download the larger `with runtime` version.

</div>

<p align="center">
  <a href="https://github.com/SakiRinn/LiveCaptions-Translator/wiki">
    <img src="https://img.shields.io/badge/📚_Check_our_Wiki_for_detailed_information-2ea44f?style=for-the-badge" alt="Check our Wiki">
  </a>
</p>

## Getting Started

> ⚠️ **IMPORTANT:** You must complete the following steps before running LiveCaptions Translator for the first time.
>
> For detailed information, see Microsoft's guide on [Using live captions](https://support.microsoft.com/en-us/windows/use-live-captions-to-better-understand-audio-b52da59c-14b8-4031-aeeb-f6a47e6055df).

### Step 1: Verify Windows LiveCaptions Availability

Confirm LiveCaptions is available on your system using any of these methods:

- Toggle **Live captions** in the quick settings
- Press **Win + Ctrl + L**
- Access via **Quick settings** > **Accessibility** > **Live captions**
- Open **Start** > **All apps** > **Accessibility** > **Live captions**
- Navigate to **Settings** > **Accessibility** > **Captions** and enable **Live captions**

### Step 2: Setup and Configure LiveCaptions

When you start for the first time, live captions will ask for your consent to process voice data on your device and prompt you to download language files to be used by on-device speech recognition.

After launching Windows LiveCaptions, you can click the **⚙️Gear** icon to open the setting menu.

To enhance your experience with LiveCaptions Translator, we strongly recommend configuring the following settings:

- Select Position > Overlaid on screen. (Importantly)
- Click Caption language > Add a language to add some languages and download all items under Speech Recognition in ··· Language options.

<div align="center">
  <img src="images/speech_recognition.png" alt="Items under speech recognition" width="90%" />
  <br>
  <em>Required speech recognition downloads</em>
  <br>
</div>

After configuration, close Windows LiveCaptions and launch LiveCaptions Translator to start using it! 🎉

## Project Stats

### 📊 Activity

<div align="center">
  <img src="https://img.shields.io/github/issues/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Issues&color=yellow" alt="GitHub Issues">
  <img src="https://img.shields.io/github/issues-pr/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Pull%20Requests&color=blue" alt="GitHub Pull Requests">
  <img src="https://img.shields.io/github/discussions/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Discussions&color=orange" alt="GitHub Discussions">
  <img src="https://img.shields.io/github/last-commit/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Last%20Commit&color=purple" alt="GitHub Last Commit">
</div>

### 🤵 Contributors

<div align="center">
  <img src="https://img.shields.io/github/contributors/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Contributors&color=success" alt="GitHub Contributors">
  <br>
  <a href="https://github.com/SakiRinn/LiveCaptions-Translator/graphs/contributors">
    <img src="https://contrib.rocks/image?repo=SakiRinn/LiveCaptions-Translator" />
  </a>
</div>

### ⭐ Star History

[![Stargazers over time](https://starchart.cc/SakiRinn/LiveCaptions-Translator.svg?variant=adaptive)](https://starchart.cc/SakiRinn/LiveCaptions-Translator)
