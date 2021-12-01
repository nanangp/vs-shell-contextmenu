# Visual Studio Shell Context Menu Extension

## Download
- Search for "shell context menu" in Visual Studio's built-in extensions manager.
- From the [VS Marketplace](https://marketplace.visualstudio.com/items?itemName=NanangP.ShellContextMenu).
- From GitHub's [Releases](../../releases/latest) page.

## Overview
This extension will add an extra menu item to the Visual Studio code window 
(and solution explorer) to show the *shell context menu* of the selected file.

![Preview](img/Site-Preview.png)

It was born out of frustration on having to do "Open containing folder" to 
perform any file operation, then eventually closing the explorer window 
when finished. Well, no more of that!

You can trigger the menu item from:
- Solution Explorer (supports multiple selection)
- Any editor window
- Document tabs

## Notes
- You will see the 64-bit context menu on VS2022,
  but only the 32-bit menu on earlier VS versions.

## What's New

See [changelog](src/VsShellContext/Changelog.md).
