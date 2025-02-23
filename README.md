﻿# LiveCaptions Translator

***LiveCaptions Translator = Windows LiveCaptions + Translate API***

A small and useful tool that integrate a translate API into Windows Live Captions to enable real-time speech translation.

Download it from the [Releases](https://github.com/SakiRinn/LiveCaptions-Translator/releases), and it's ready to use with just a click.

![Preview](images/preview.png "Preview of LiveCaptions Translator")

## Features

+ Automatically invoke Windows LiveCaptions without opening two windows.
+ Simple and beautiful Fluent UI.
+ Switch bright and dark theme automatically. (Follow system theme)
+ Currently supports Ollama, OpenAI API, OpenRouter, and Google Translate. You are welcomed to implement more apis.
+ Directly open the setting menu of Windows LiveCaptions.
+ Buttons to keep the window on top and pause translation, translation-only mode, transparent mode.
+ Record what have been translated though Live Captions.

## Prerequisite

This tool is based on Windows LiveCaptions, which is available in Windows 11, version 22H2 and later.

If your Windows version is proper, you can confirm whether Windows LiveCaptions is available by doing one of the following:

+ Turn on **Live captions** in the quick settings.
+ Turn on the **Live captions** toggle in the quick settings **Accessibility** flyout.
+ Press **Win + Ctrl + L**.
+ Select **Start** > **All apps** > **Accessibility** > **Live captions**.
+ Go to **Settings** > **Accessibility** > **Captions** and turn on the **Live captions** toggle.

When turned on the first time, live captions will ask for your consent to process voice data on your device and prompt you to download language files to be used by on-device speech recognition.

***Importantly:*** *You must complete the above step before running LiveCaption Translator*.

> For more information, see [Use live captions to better understand audio](https://support.microsoft.com/en-us/windows/use-live-captions-to-better-understand-audio-b52da59c-14b8-4031-aeeb-f6a47e6055df).


After launching Windows LiveCaptions, you can click the **⚙️gear** icon to open the setting menu.

To enhance your experience with LiveCaptions Translator, we strongly recommend configuring the following settings:

+ Select **Position** > **Overlaid on screen**. *(Importantly)*
+ Click **Caption language** > **Add a language** to add some languages and download all items under **Speech Recognition** in **··· Language options**.

![Preview](images/speech_recognition.png "Items under speech recognition")

Now, close Windows LiveCaptions and open LiveCaptions Translator to start using it!

## Known Issue

On Windows 24H2 or higher, you might fail to change the language of live captions. You can try to manually launch live caption and then run the python script at root to solve this.
