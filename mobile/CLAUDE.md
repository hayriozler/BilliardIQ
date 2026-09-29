# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BilliardIQ is a smart 3-cushion billiards assistant built with .NET MAUI 10. It captures table images, tracks player stats/games, and will provide AI-powered shot recommendations with 3D visualization — all offline.

## Build Commands

```bash
# Build (defaults to Android on non-Windows, Windows on Windows)
dotnet build BilliardIQ.Mobile/BilliardIQ.Mobile.csproj

# Target a specific platform
dotnet build BilliardIQ.Mobile/BilliardIQ.Mobile.csproj -f net10.0-android
dotnet build BilliardIQ.Mobile/BilliardIQ.Mobile.csproj -f net10.0-windows10.0.19041.0

# Release build
dotnet build BilliardIQ.Mobile/BilliardIQ.Mobile.csproj -c Release -f net10.0-android

# Clean
dotnet clean BilliardIQ.Mobile/BilliardIQ.Mobile.csproj
```

No test project exists yet. There is no lint step beyond the C# compiler.

## Architecture

The app follows Clean Architecture with MVVM, structured inside `BilliardIQ.Mobile/`:

| Layer | Directory | Role |
|---|---|---|
| Presentation | `Pages/` | XAML views inheriting `BasePage` |
| Application | `PageModels/` | ViewModels inheriting `BasePageModel` (`ObservableValidator`) |
| Domain | `Models/` | Plain C# domain objects (Player, Game, Ball, shot analysis types) |
| Infrastructure | `Data/`, `Services/` | SQLite repositories, database service, error handling |

### MVVM Pattern

- **PageModels** use CommunityToolkit.Mvvm: `[ObservableProperty]`, `[RelayCommand]`, `[NotifyDataErrorInfo]` with DataAnnotations for validation.
- **Pages** receive their PageModel via constructor injection; `BasePage` sets `BindingContext = viewModel`.
- All Pages and PageModels are registered as **Singletons** in `MauiProgram.cs`, except shell-routable pages which use `AddTransientWithShellRoute<TPage, TPageModel>("route")`.

### Data Access

- `DatabaseService` manages SQLite connections (static utility, path from `Constants`).
- `BaseRepo` provides abstract CRUD; `PlayerRepository` and `GameRepository` inherit it.
- `GameRepository.DeleteGame` uses a transaction to cascade-delete `GameStats` and `GamePhotos` before deleting the `Games` row — SQLite foreign keys are not relied upon here.
- Database is initialized synchronously at startup: `DatabaseService.InitializeDatabaseAsync().GetAwaiter().GetResult()` in `MauiProgram.cs`.

### Navigation

Shell-based with three tab routes: `ondemand` (camera/analyzer), `gamelist`, `profile`. The `newgame` route is registered as a transient shell route and navigated to with query parameters (game ID).

### Error Handling

`ModalErrorHandler` (implements `IErrorHandler`) shows alerts via `Application.Current.MainPage.DisplayAlert`. A semaphore prevents concurrent dialogs. Async fire-and-forget is handled through `TaskUtilities.FireAndForgetSafeAsync(IErrorHandler)`.

## Key Conventions

- `GlobalUsing.cs` contains project-wide `global using` statements — add new common namespaces here rather than per-file.
- XAML is compiled via source generation (`<MauiXamlInflator>SourceGen</MauiXamlInflator>`), so XAML binding errors surface at compile time.
- Nullable reference types are enabled; unsafe blocks are allowed for future native interop.
- Toast notifications are disabled on Windows (guarded by `#if !WINDOWS`).

## Planned (Not Yet Implemented)

- Image processing (OpenCV/equivalent) for ball and table boundary detection
- ONNX Runtime or TensorFlow Lite for offline AI shot recommendations
- 3D table reconstruction and shot trajectory visualization
- `ThreeCushionShotAnalysis` and `ThreeCushionShotSuggestion` models exist in `Models/` but have no service layer yet