# Changelog

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
