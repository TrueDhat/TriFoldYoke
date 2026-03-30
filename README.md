# TriFold (NET MAUI)

TriFold is a cross-platform personal performance tracker built with .NET MAUI and SQLite.  
It organizes progress across three focus areas:

- **Body**: workout templates, planner/calendar, completion tracking, and template progress charts
- **Mind**: reusable task templates, subtask goal times, and completed-time logging
- **Spirit**: reusable task templates, subtask goal times, and completed-time logging

## What I Built and Why

I built TriFold to combine fitness and personal discipline tracking in a single workflow:

- plan workouts and tasks in advance
- log completion with minimal friction
- track trend/progression over time by category and template

Instead of treating all habits as generic checkboxes, this app keeps **Body**, **Mind**, and **Spirit** separate while still surfacing a unified daily summary in the Tri-Fold view.

## Feature Highlights

- **Template-Driven UX**
  - Mind/Spirit: standard task names reused from dropdown + "Other Task"
  - Body: workout templates with multi-exercise definitions
- **Goal System**
  - subtask goal times per template (Mind/Spirit)
  - category totals auto-calculated from subtask goals
- **Body Progression**
  - month calendar planner for template scheduling
  - toggle planned workout complete/incomplete to add/remove real workout entries
  - template chart with date axis, weighted totals, and editable datapoints
- **Data Durability**
  - SQLite persistence with migration-safe schema evolution
  - uniqueness guards to prevent plan duplication

## Engineering Decisions

- **Architecture**: MVVM + DI for clear separation of UI and state logic
- **Persistence**: repository abstraction with SQLite and in-memory implementations
- **Concurrency safety**: semaphore guards on async load/mutation paths to prevent duplicate UI data on init/reload
- **Incremental migrations**: `ALTER TABLE` + index creation for backward-compatible updates
- **UX consistency**: standardized template-first workflows across tabs

## Tech Stack

- **.NET 9 / C#**
- **.NET MAUI**
- **SQLite** (`sqlite-net-pcl`)
- **XAML + MVVM**

## Screenshots

Add these images under `docs/screenshots/` and GitHub will render them here.

### Tri-Fold Dashboard

![Tri-Fold dashboard](docs/screenshots/trifold-overview.png)

### Body Planner (Month Calendar)

![Body planner month calendar](docs/screenshots/body-calendar.png)

### Body Template Progress

![Body template progress chart](docs/screenshots/body-progress-chart.png)

### Mind Goals and Time Logging

![Mind goals and time logging](docs/screenshots/mind-goals.png)

## Project Structure

- `TriFoldApp/Models` - domain and persistence models
- `TriFoldApp/ViewModels` - app logic and state
- `TriFoldApp/Views` - XAML pages and UI
- `TriFoldApp/Services` - repository interfaces and storage implementations

## Run Locally (Windows)

From repo root:

```powershell
cd "TriFoldApp"
dotnet build
dotnet run -f net9.0-windows10.0.19041.0
```

If workloads are missing:

```powershell
dotnet workload install maui
```

## Roadmap

- richer analytics and trend breakdowns
- planner quality-of-life improvements
- export/share options for progress history

