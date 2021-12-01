# Changelog

## Version 2.2

New:
* Supports VS2017 to VS2022.
* Dropped support for VS2015/14.0 and below. Sorry :(

Notes:
* You will see the 64-bit context menu on VS2022,
  but only the 32-bit menu on earlier VS versions.

## Version 2.1

New:
* Extra places where the menu item is shown: Tabs, Folders, Solution items, JSON editors.

## Version 2.0

New:
* Now built from VS2019 (with vsixmanifest v2).
* Supports VS2012-2019.
* Supports code editors for XAML, HTML, CSS, and other web files.

Changes:
* Optimised resources to cut down size by 60%.

Notes:
* Since the VS shell is 32-bit, the context menu you see would be the
  32-bit context menu, not the 64-bit one.
* Because we are still supporting older VS shells (VS2012/11.0) we
  cannot take advantage of the new async loading of extension packages.
  Shouldn't be an issue, as the loading doesn't actually do much work.
