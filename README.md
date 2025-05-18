<div align="center">

# LiveCaptions Translator

### *Real-time audio/speech translation tool based on Windows LiveCaptions*

[![Master Build](https://github.com/SakiRinn/LiveCaptions-Translator/actions/workflows/dotnet-build.yml/badge.svg?branch=master)](https://github.com/SakiRinn/LiveCaptions-Translator/actions/workflows/dotnet-build.yml)
[![GitHub Release](https://img.shields.io/github/v/release/SakiRinn/LiveCaptions-Translator?label=Latest)](https://github.com/SakiRinn/LiveCaptions-Translator/releases/latest)
[![Wiki](https://img.shields.io/badge/Wiki-GitHub-blue)](https://github.com/SakiRinn/LiveCaptions-Translator/wiki)
[![GitHub License](https://img.shields.io/github/license/SakiRinn/LiveCaptions-Translator)](https://github.com/SakiRinn/LiveCaptions-Translator/blob/master/LICENSE.txt)
[![GitHub Stars](https://img.shields.io/github/stars/SakiRinn/LiveCaptions-Translator)](https://github.com/SakiRinn/LiveCaptions-Translator/stargazers)

**English** | [中文](README_zh-CN.md)

</div>

## Overview

**✨ LiveCaptions Translator = Windows LiveCaptions + Translate API ✨**

This is a lightweight tool that seamlessly integrates translation APIs with Windows Live Captions, enabling real-time speech translation without requiring a Copilot+ PC.

Windows' built-in LiveCaptions is easy to use, uses few resources, and has extremely high recognition accuracy. If empowering it with the awesome translation capabilities of LLMs, you will get... possibly the best real-time translator available to date!

**🚀 Quick Start:** Download from [Releases](https://github.com/SakiRinn/LiveCaptions-Translator/releases) and start with a single click!

<div align="center">
  <img src="images/preview.png" alt="Preview of LiveCaptions Translator" width="90%" />
  <br>
  <em style="font-size:80%">Preview of LiveCaptions Translator</em>
  <br>
</div>

## Features

- **🔄 Seamless Integration** \
  Automatically invokes Windows LiveCaptions without opening separate windows. Provides a unified experience for real-time audio/speech translation.
  After your first use, Windows LiveCaptions will be hidden by default. You can show it again in the settings.

  <div align="center">
  <img src="images/show_livecaptions.png" alt="LiveCaptions Show/Hide button" width="90%" />
  <br>
  <em style="font-size:80%">LiveCaptions Show/Hide button</em>
  <br>
  </div>

  By enabling the ***Include microphone audio option*** in the setting of Windows LiveCaptions, you can achieve real-time speech translation!
  > ⚠️ **IMPORTANT:** You must change the source language in Windows LiveCaptions!

- **🎨 Modern Interface** \
  Easy-to-use and clean Fluent UI aligned with modern Windows aesthetics. It can automatically switches between light and dark themes 🌓 based on the system setting.

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

  It's strongly recommended using **LLM-based** translation engines, as LLMs excel at handling incomplete sentences and are adept at understanding context.

- **🪟 Overlay Window** \
  Open a borderless, transparent overlay window to display subtitles, providing the most immersive experience. This is very useful for scenarios like gaming, videos, and live streams!
  You can even make it completely embedded into the screen, becoming part of it. This means it won't affect any of your operations at all! This is perfect for gamers.

  <div align="center">
  <img src="images/overlay_window.png" alt="Overlay Window" width="80%" />
  <br>
  <em style="font-size:80%">Overlay window</em>
  <br>
  </div>

  You can open the Overlay Window on the taskbar. You can adjust its parameters such as the window background and subtitle color, font size, and transparency. Extremely high configurability allows it to completely match your preferences!
  You can adjust the number of sentences displayed simultaneously in the *Overlay Sentences* section of the setting page.

- **⚙️ Flexible Controls** \
  Supports Always-on-top window and convenient translation pause/resume, and you can copy text with a single click for quick share or saving.

- **📒 History Management** \
  Records original and translated text, perfect for meetings, lectures, and important discussions.
  You can export all records as a CSV file.

  <div align="center">
  <img src="images/history.png" alt="Translation history" width="90%" />
  <br>
  <em style="font-size:80%">Translation history</em>
  <br>
  </div>

- **🎞️ Log Cards** \
  Recent transcription records can be displayed as Log Cards, which helps you better grasp the context.
  You can enable it on the taskbar of the main page and change the number of cards in the *Log Cards* section of the setting page.

  <div align="center">
  <img src="images/log_cards.png" alt="Log cards" width="90%" />
  <br>
  <em style="font-size:80%">Log Cards</em>
  <br>
  </div>


## Prerequisites

<div align="center">

| Requirement                                                                                                           | Details                                     |
| --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| <img src="https://img.shields.io/badge/Windows-11%20(22H2+)-0078D6?style=for-the-badge&logo=windows&logoColor=white"> | With LiveCaptions support.                  |
| <img src="https://img.shields.io/badge/.NET-8.0+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">             | Recommended. Not test in previous versions. |

</div>

This tool is based on Windows LiveCaptions, which is available since Windows 11 22H2.

We suggest you have .NET runtime 8.0 or higher installed. If you are not available to install one, you can download the ***with runtime*** version but its size is bigger.

<div align="center">
  <p align="center">
    <a href="https://github.com/SakiRinn/LiveCaptions-Translator/wiki">
      <img src="https://img.shields.io/badge/📚_Check_our_Wiki_for_detailed_information-2ea44f?style=for-the-badge" alt="Check our Wiki">
    </a>
  </p>
</div>

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
  <img src="images/speech_recognition.png" alt="Items under speech recognition" width="80%" />
  <br>
  <em style="font-size:80%">Required speech recognition downloads</em>
  <br>
</div>

After configuration, close Windows LiveCaptions and launch LiveCaptions Translator to start using it! 🎉

## Project Stats

### Activity

<div align="center">
  <img src="https://img.shields.io/github/issues/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Issues&color=yellow" alt="GitHub Issues">
  <img src="https://img.shields.io/github/issues-pr/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Pull%20Requests&color=blue" alt="GitHub Pull Requests">
  <img src="https://img.shields.io/github/discussions/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Discussions&color=orange" alt="GitHub Discussions">
  <img src="https://img.shields.io/github/last-commit/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Last%20Commit&color=purple" alt="GitHub Last Commit">
</div>

### Contributors

<div align="center">
  <img src="https://img.shields.io/github/contributors/SakiRinn/LiveCaptions-Translator?style=for-the-badge&label=Contributors&color=success" alt="GitHub Contributors">
  <br>
  <a href="https://github.com/SakiRinn/LiveCaptions-Translator/graphs/contributors">
    <img src="https://contrib.rocks/image?repo=SakiRinn/LiveCaptions-Translator" />
  </a>
</div>

### Star History

[![Stargazers over time](https://starchart.cc/SakiRinn/LiveCaptions-Translator.svg?variant=adaptive)](https://starchart.cc/SakiRinn/LiveCaptions-Translator)
