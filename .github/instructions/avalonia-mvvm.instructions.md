---
description: "Use when changing Avalonia XAML views, desktop ViewModels, navigation, or desktop dialogs in Tibia Hunt Analyzer."
name: "Avalonia and MVVM"
applyTo:
  - "src/TibiaHuntAnalyzer.Desktop/**/*.cs"
  - "src/TibiaHuntAnalyzer.Desktop/**/*.axaml"
---

# Avalonia and MVVM

- Follow the existing `MainViewModel`, screen ViewModels, and `ViewModelBase` patterns. Use CommunityToolkit.Mvvm commands and observable properties where they fit; do not add a second MVVM framework or navigation abstraction.
- Keep ViewModels independent of Avalonia types. Put platform UI work such as file pickers in the window/view layer and connect it through the existing request events and ViewModel methods.
- Keep XAML bindings strongly typed with `x:DataType` where the view has a ViewModel, and follow the existing layout, focus, accessibility, and status-message conventions.
- Treat screen-level Exit/Back as navigation to the appropriate previous screen. Request application shutdown only through the application-level exit flow. Preserve existing unsaved-input confirmation behavior.
- Match the language and wording style of the screen being changed. Do not translate or redesign unrelated screens as part of a focused change.
- Inspect the relevant ViewModel and view together before changing a UI behavior, and validate with a desktop build plus focused tests where the behavior is testable.
