You are a senior .NET 10, C#, WinUI 3, Windows App SDK, MVVM, performance, and desktop application engineer.

Your task is to turn the existing project into a modern, functional BitTorrent downloader for Windows.

The project already exists and is based on:

- .NET 10
- C#
- WinUI 3
- Windows App SDK
- WinUI MVVM template
- Windows desktop
- x64
- VS Code is used as the primary coding environment
- Visual Studio is available when needed for WinUI-specific tooling/debugging

The application will eventually be distributed through the Microsoft Store.

The application name is currently:

MyWindowsApp

Do not replace the technology stack.

DO NOT migrate the application to:

- WPF
- WinForms
- .NET MAUI
- Electron
- Tauri
- Wails
- React
- Avalonia
- Flutter
- another UI framework

Keep the application as:

C# + .NET 10 + WinUI 3 + Windows App SDK + XAML + MVVM


==================================================
1. PRIMARY OBJECTIVE
==================================================

Build a FUNCTIONAL BitTorrent downloader for Windows.

The priority order is:

1. Correct and functional torrent downloading
2. Stability and reliability
3. Memory management
4. Performance
5. Clean architecture
6. Error handling
7. Good UX
8. Modern visual design
9. Microsoft Store readiness

Do NOT build only a visual mockup.

The main torrent workflow must actually work.

The MVP should be capable of:

- adding .torrent files
- adding magnet links
- obtaining torrent metadata
- selecting a download directory
- starting downloads
- pausing downloads
- resuming downloads
- removing torrents
- tracking progress
- showing download speed
- showing upload speed
- showing ETA
- showing peer/seed information when available
- detecting completion
- persisting torrent state
- resuming torrents after application restart


==================================================
2. FIRST STEP: INSPECT THE EXISTING PROJECT
==================================================

Before modifying anything, inspect the entire project.

Inspect at minimum:

- MyWindowsApp.csproj
- App.xaml
- App.xaml.cs
- MainWindow.xaml
- MainWindow.xaml.cs
- MainPage.xaml
- MainPage.xaml.cs
- ViewModels/
- Assets/
- Properties/
- Package.appxmanifest
- app.manifest
- existing NuGet dependencies
- existing build configuration

Understand the existing architecture before changing it.

Do not blindly overwrite existing files.

Do not remove working WinUI configuration.

Preserve existing functionality unless there is a clear reason to change it.

First establish a clean baseline.

Run:

dotnet restore

dotnet build

If the baseline does not build, fix the baseline issue before implementing major features.


==================================================
3. BITTORRENT ENGINE
==================================================

DO NOT implement the BitTorrent protocol from scratch.

Do not manually implement:

- DHT protocol
- tracker protocol
- peer protocol
- piece exchange protocol
- torrent metadata parsing
- hashing implementation
- peer connection management
- choking/unchoking
- NAT traversal

Use an established .NET BitTorrent library.

Before choosing the library:

1. Inspect the current .NET 10 compatibility.
2. Check API compatibility.
3. Check package stability.
4. Check whether it supports the required features.
5. Check whether it is actively maintained.
6. Prefer stable packages over experimental packages.
7. Avoid unnecessary dependencies.

Candidate libraries may include:

- PeerSharp
- MonoTorrent

Do not blindly choose one.

Select the most appropriate library based on the actual project environment.

After selecting the library, create an abstraction layer so that the UI does not directly depend on the third-party BitTorrent API.


==================================================
4. TORRENT SERVICE ABSTRACTION
==================================================

The UI and ViewModels must NOT directly depend on PeerSharp, MonoTorrent, or another torrent library.

Use an abstraction such as:

ITorrentService

The architecture should resemble:

View
  ↓
ViewModel
  ↓
ITorrentService
  ↓
TorrentService
  ↓
BitTorrent Library


Create application-owned models where appropriate.

For example:

- TorrentItem
- TorrentStatus
- TorrentStatistics
- TorrentFile
- TorrentSettings

Do not expose third-party torrent library objects throughout the application.

The purpose is to make the BitTorrent engine replaceable later without rewriting the UI.


==================================================
5. MVP FEATURES
==================================================

Implement the following features as REAL FUNCTIONAL FEATURES.


------------------------------------------
5.1 Add .torrent
------------------------------------------

Allow the user to select a .torrent file using the Windows file picker.

Workflow:

Add Torrent
    ↓
Open .torrent
    ↓
Validate
    ↓
Read metadata
    ↓
Show torrent information
    ↓
Select download directory
    ↓
Start or queue download


------------------------------------------
5.2 Magnet links
------------------------------------------

Support magnet URIs such as:

magnet:?xt=urn:btih:...

Allow the user to paste a magnet URI.

Validate the input.

If invalid, show a friendly error.

When metadata is being retrieved, show a loading state.

Do not freeze the UI.


------------------------------------------
5.3 Torrent list
------------------------------------------

Create a main torrent list.

Each torrent should display:

- Name
- Status
- Progress
- Download speed
- Upload speed
- ETA
- Downloaded amount
- Total size
- Peers
- Seeds
- Availability when available

Example:

Ubuntu.iso

██████████████░░░░░░ 72%

↓ 12.4 MB/s
↑ 1.2 MB/s

ETA 04:32


------------------------------------------
5.4 Torrent states
------------------------------------------

Support at minimum:

- Queued
- Checking
- Downloading
- Paused
- Completed
- Error


------------------------------------------
5.5 Start
------------------------------------------

Start must actually start torrent activity.

Do not only change the UI status.


------------------------------------------
5.6 Pause
------------------------------------------

Pause must actually stop or suspend download activity.

Do not merely change the displayed status.


------------------------------------------
5.7 Resume
------------------------------------------

Resume must continue the existing torrent state.

Do not restart the download from zero.

Use the BitTorrent library's resume/state support when available.


------------------------------------------
5.8 Remove
------------------------------------------

Allow:

Remove torrent

and if practical:

Remove torrent only
Remove torrent + downloaded files

Always ask for confirmation before deleting downloaded files.

Never delete user data silently.


------------------------------------------
5.9 Open folder
------------------------------------------

Allow the user to open the torrent's download folder using Windows Explorer.


==================================================
6. DOWNLOAD LOCATION
==================================================

Provide a configurable default download directory.

Suggested default:

Downloads/Torrents

Allow the user to change the location.

Validate:

- path exists or can be created
- path is writable
- enough disk space when practical

Do not hardcode development-specific paths.

Do not require administrator privileges.


==================================================
7. PERSISTENCE
==================================================

Torrent state must survive application restarts.

When the user closes the application:

- active torrent information must be persisted
- download state must be persisted
- settings must be persisted
- resume information must be preserved

When the application starts again:

- restore previously added torrents
- restore their state
- allow incomplete torrents to resume
- completed torrents should remain visible in history

Use the BitTorrent library's resume mechanism when available.

Use SQLite only if persistence requirements justify it.

Do not create an unnecessarily complicated database architecture.


==================================================
8. DOWNLOAD MONITORING
==================================================

Display:

- progress
- download speed
- upload speed
- ETA
- downloaded bytes
- uploaded bytes
- total size
- peers
- seeds
- ratio where available

Use human-readable formatting:

KB/s
MB/s
GB/s

Do not display excessive decimal precision.


==================================================
9. GLOBAL SPEED LIMIT
==================================================

Implement settings for:

Download limit
Upload limit

Options:

- Unlimited
- 100 KB/s
- 500 KB/s
- 1 MB/s
- 5 MB/s
- 10 MB/s
- Custom

Use the BitTorrent engine's native rate limiting if available.

Do not implement inefficient custom throttling if the library already provides it.


==================================================
10. TORRENT DETAILS
==================================================

When the user selects a torrent, provide a detail view.

Sections:

General:
- Name
- Status
- Progress
- Total size
- Downloaded
- Uploaded
- Ratio
- ETA

Network:
- Download speed
- Upload speed
- Peers
- Seeds
- Connections

Files:
- list torrent files
- file size
- progress when available

Selective file downloading is NOT required for the first MVP unless the selected BitTorrent library makes it straightforward and reliable.

Do not delay the core MVP for advanced file selection.


==================================================
11. UI / UX
==================================================

The UI must feel like a modern Windows desktop application.

Design goals:

- clean
- modern
- simple
- intuitive
- easy to navigate
- accessible
- visually consistent
- not cluttered

Use WinUI 3 / Fluent Design patterns.

Use:

- proper spacing
- typography hierarchy
- clear visual grouping
- subtle rounded corners
- consistent controls
- appropriate hover states
- selected states
- loading states
- error states
- empty states
- accessible tooltips


==================================================
12. APPLICATION LAYOUT
==================================================

Use a simple navigation structure.

Suggested:

Sidebar:

Home
Downloads
Completed
Settings

Alternatively, Downloads may contain filters:

All
Downloading
Paused
Completed
Error

Choose whichever structure provides the clearest UX.

Do not create unnecessary navigation levels.


==================================================
13. MAIN DOWNLOAD SCREEN
==================================================

The main screen should prioritize the torrent list.

Suggested structure:

Sidebar
  ↓
Main content
  ↓
Header
  ↓
Add Torrent button
  ↓
Torrent list


Each torrent item should clearly communicate:

Name
Progress
Status
Speed
ETA
Primary actions

Do not overwhelm each row with 10+ buttons.

Use:

- primary action button
- overflow menu
- context menu

for secondary actions.


==================================================
14. ICONS
==================================================

Icons must be clear and consistent.

Use icons for:

- Add
- Download
- Upload
- Pause
- Resume
- Remove
- Folder
- Settings
- Search
- More
- Completed
- Error
- Refresh

Do NOT use emoji as the primary application icon system.

Do not mix multiple unrelated icon styles.

Prefer native WinUI/Fluent-compatible icons or one consistent icon library.

Use tooltips for icons where the meaning may not be immediately obvious.


==================================================
15. EMPTY STATE
==================================================

When no torrents exist:

No downloads yet

Add a torrent file or paste a magnet link to get started.

[ + Add Torrent ]


The empty state should look intentional, not like an error.


==================================================
16. LOADING STATE
==================================================

When retrieving magnet metadata:

Getting torrent information...

Please wait.


Disable inappropriate actions while metadata is loading.

Do not block the UI thread.


==================================================
17. ERROR HANDLING
==================================================

Never expose raw exception messages to normal users.

Bad:

System.InvalidOperationException...

Good:

Unable to start download

The torrent could not be started.
Please check the download location and try again.


However:

- log the actual exception
- preserve diagnostic information
- include useful context in logs


==================================================
18. MVVM ARCHITECTURE
==================================================

Use MVVM consistently.

Preferred structure:

Views/
ViewModels/
Models/
Services/
Data/
Infrastructure/
Controls/
Resources/

Recommended flow:

View
 ↓
ViewModel
 ↓
Service
 ↓
Data / Torrent Engine


ViewModels should not contain low-level torrent engine implementation.

Views should not contain business logic.

Avoid putting application logic inside code-behind unless it is genuinely UI-specific behavior.


==================================================
19. DEPENDENCY INJECTION
==================================================

Use dependency injection for services where appropriate.

Example conceptual architecture:

ITorrentService
ISettingsService
IPersistenceService
ILoggingService

Implementations:

TorrentService
SettingsService
PersistenceService


Do not create global static service managers.

Do not create unnecessary singletons.

Choose service lifetimes intentionally.


==================================================
20. MEMORY MANAGEMENT
==================================================

Memory management is a FIRST-CLASS REQUIREMENT.

The application must avoid memory leaks and uncontrolled memory growth.

Pay special attention to:

- torrent objects
- peer objects
- network streams
- sockets
- file streams
- buffers
- timers
- CancellationTokenSource
- event subscriptions
- ViewModels
- Pages
- Windows
- background tasks
- caches
- collections


Avoid:

- unbounded collections
- unbounded caches
- static collections that grow forever
- unnecessary object creation in hot loops
- unnecessary string allocations
- loading large files entirely into memory
- loading entire torrent payloads into memory
- retaining removed torrent objects
- retaining completed ViewModels unnecessarily
- event subscriptions that are never removed
- timers that are never disposed
- CancellationTokenSource objects that are never disposed
- fire-and-forget tasks without lifecycle management
- unmanaged resources without deterministic cleanup


Prefer:

- streaming I/O
- bounded buffers
- asynchronous I/O
- cancellation
- deterministic disposal
- bounded caches
- object reuse where appropriate
- incremental collection updates
- UI virtualization
- efficient data structures
- clear ownership/lifetime rules


A 10 GB torrent must NOT require multiple gigabytes of RAM merely because it is being downloaded.


==================================================
21. DISPOSABLE RESOURCE MANAGEMENT
==================================================

Any object that owns IDisposable or IAsyncDisposable resources must have a clear lifetime.

Pay particular attention to:

- FileStream
- Stream
- Socket
- Timer
- CancellationTokenSource
- IDisposable
- IAsyncDisposable
- network resources
- torrent engine resources

Dispose resources when ownership ends.

Do not dispose resources owned by another component.

Do not blindly wrap every object in using.

Understand ownership before disposing.


==================================================
22. ASYNC / THREADING
==================================================

Do not block the UI thread.

Never use blocking operations such as:

.Result
.Wait()
Thread.Sleep()

for normal asynchronous work.

Prefer:

async/await
CancellationToken
Task
IAsyncEnumerable when appropriate


Do not create unnecessary threads manually.

Prefer .NET Task-based asynchronous programming and the torrent library's own asynchronous APIs.


==================================================
23. BACKGROUND TASK LIFECYCLE
==================================================

Do not create unmanaged fire-and-forget tasks.

Avoid:

_ = SomeAsyncMethod();

unless the detached task is intentional and has:

- exception handling
- cancellation
- lifecycle ownership


During application shutdown:

1. stop accepting new work
2. cancel background operations
3. stop torrent engine operations
4. persist state
5. flush necessary data
6. dispose resources
7. exit cleanly


==================================================
24. HIGH-FREQUENCY EVENTS
==================================================

Torrent engines may generate a large number of events.

Do NOT directly push every engine event into the UI.

For statistics such as:

- speed
- progress
- peer count
- ETA

aggregate or throttle updates.

Target a reasonable UI update frequency such as:

250-1000 ms

depending on the metric.

The torrent engine's event frequency must remain independent from UI refresh frequency.


==================================================
25. UI PERFORMANCE
==================================================

Keep the WinUI UI responsive.

Avoid:

- rebuilding the entire torrent list for every update
- recreating all ViewModels repeatedly
- huge visual trees
- excessive nested layouts
- unnecessary converters in hot paths
- excessive animations
- thousands of UI elements
- unnecessary data binding churn


Use:

- virtualization
- incremental collection updates
- property-level updates
- efficient observable collections
- throttled statistics updates


If one torrent changes speed, update only that torrent.

Do NOT rebuild the entire list.


==================================================
26. LARGE FILE HANDLING
==================================================

Never load large downloaded files entirely into RAM.

Use:

- FileStream
- asynchronous file operations
- buffered I/O
- bounded buffers
- random access where appropriate

Memory usage should remain reasonably stable as torrent size increases.


==================================================
27. DATABASE / PERSISTENCE PERFORMANCE
==================================================

If SQLite is used:

- use parameterized queries
- avoid unnecessary queries
- avoid loading the entire database into memory
- add indexes where appropriate
- use async APIs where available
- avoid blocking UI operations
- keep database lifetime controlled

Do not introduce Entity Framework merely for the sake of using it.

Use EF Core only if it meaningfully improves maintainability for this project.


==================================================
28. C# TYPE SAFETY
==================================================

C# compiler correctness is mandatory.

Use nullable reference types.

Check whether the project already has:

<Nullable>enable</Nullable>

If not, enable it if doing so is compatible with the existing project.

Do NOT silence nullable issues with:

!
#pragma warning disable
dynamic
empty catch blocks

unless there is a documented technical reason.

Do not hide compiler or analyzer errors.


==================================================
29. CODE ANALYZERS
==================================================

Use modern .NET/Roslyn analyzers where appropriate.

Do not globally disable analyzers to make the build pass.

Fix the underlying issue.

Only suppress a warning when:

- there is a real reason
- the suppression is narrowly scoped
- the reason is documented


==================================================
30. FORMATTING
==================================================

Use standard .NET formatting.

Run:

dotnet format

before considering a feature complete.

If a solution file exists, use:

dotnet format <solution>.sln

Do not manually fight the formatter.

Keep formatting consistent.


==================================================
31. TESTING
==================================================

Create tests for meaningful business logic.

Prioritize:

- torrent state transitions
- settings validation
- speed formatting
- ETA calculation
- path validation
- persistence logic
- service behavior
- error handling

Tests should be deterministic.

Do not depend on random external torrent peers for unit tests.

Use mocks/fakes for unit tests where appropriate.

Use integration tests only where they provide real value.


==================================================
32. VALIDATION LOOP
==================================================

After every meaningful implementation milestone:

Run:

dotnet restore

dotnet format

dotnet build

dotnet test


If any command fails:

1. Read the actual error.
2. Identify the root cause.
3. Fix the root cause.
4. Run the failed command again.
5. Re-run the complete validation when appropriate.

Do NOT stop after the first successful build.

Do NOT declare completion while:

- build errors exist
- test failures exist
- formatting fails
- critical analyzer issues remain
- known runtime errors remain


Do not assume the code works simply because it compiles.


==================================================
33. RUNTIME SMOKE TEST
==================================================

After successful compilation:

Run the application.

Verify:

- application launches
- navigation works
- Add Torrent works
- file picker works
- magnet input works
- loading state works
- torrent can start
- torrent can pause
- torrent can resume
- torrent progress updates
- download speed updates
- upload speed updates
- completion is detected
- remove confirmation works
- settings work
- application closes cleanly


Do not claim runtime functionality has been tested if it has not actually been tested.


==================================================
34. LOGGING
==================================================

Implement structured logging.

Log important events:

- application startup
- torrent added
- torrent started
- torrent paused
- torrent resumed
- torrent completed
- torrent removed
- torrent error
- metadata retrieval error
- file system error
- network error
- application shutdown


Do not log sensitive information unnecessarily.


==================================================
35. SETTINGS
==================================================

Provide a Settings page.

At minimum:

- download directory
- download speed limit
- upload speed limit
- theme:
  - System
  - Light
  - Dark

Persist settings locally.


==================================================
36. THEME
==================================================

Support:

System
Light
Dark

Default:

System

Use centralized resources for:

- colors
- typography
- spacing
- control styles
- corner radius
- cards
- buttons
- navigation

Do not hardcode visual values everywhere.

Create a consistent design system.


==================================================
37. ACCESSIBILITY
==================================================

Consider accessibility from the beginning.

Ensure:

- controls have meaningful labels
- icons have tooltips
- keyboard navigation works
- focus states are visible
- text contrast is readable
- important actions are not icon-only without accessible names


==================================================
38. SECURITY
==================================================

Do not implement:

- DRM bypass
- unauthorized access
- credential theft
- malicious persistence
- security bypass mechanisms
- stealth behavior

The torrent client should operate as a normal user-level desktop application.

Do not require administrator privileges unless absolutely necessary.


==================================================
39. MICROSOFT STORE READINESS
==================================================

The application will eventually target Microsoft Store distribution.

Keep Store readiness in mind:

- use MSIX packaging correctly
- do not hardcode development paths
- avoid unnecessary capabilities
- do not require administrator privileges
- use official Windows APIs
- keep permissions minimal
- keep dependencies reasonable
- ensure application behaves correctly as a packaged app


Do not attempt Store submission yet.

First make the application stable and functional.


==================================================
40. PERFORMANCE PHILOSOPHY
==================================================

Do not prematurely optimize everything.

But also do not introduce obvious inefficient patterns.

Use:

Measure
 ↓
Identify bottleneck
 ↓
Optimize
 ↓
Measure again


Pay special attention to:

- memory
- allocations
- CPU usage
- UI responsiveness
- disk I/O
- network I/O
- torrent statistics updates


Do not make claims such as "this is faster" without evidence.


==================================================
41. AVOID OVER-ENGINEERING
==================================================

This is an MVP.

Do NOT implement:

- microservices
- cloud synchronization
- user accounts
- remote control server
- plugin system
- analytics backend
- telemetry infrastructure
- automatic torrent search engine
- complicated event sourcing
- CQRS without a real need
- unnecessary generic repositories
- unnecessary abstraction layers
- custom BitTorrent protocol implementation


Keep the architecture clean but pragmatic.


==================================================
42. IMPLEMENTATION ORDER
==================================================

Follow this order.

PHASE 1 — FOUNDATION

- inspect project
- baseline build
- inspect dependencies
- choose BitTorrent library
- setup service abstraction
- setup DI if needed
- setup logging
- setup resources/theme


PHASE 2 — UI SHELL

- MainWindow
- navigation
- sidebar
- Downloads page
- Completed page
- Settings page
- empty state
- theme


PHASE 3 — TORRENT ENGINE

- .torrent file
- magnet link
- metadata
- add torrent
- start
- pause
- resume
- remove


PHASE 4 — MONITORING

- progress
- download speed
- upload speed
- ETA
- peers
- seeds
- status


PHASE 5 — PERSISTENCE

- torrent state
- resume data
- settings
- download location
- application restart recovery


PHASE 6 — POLISH

- error handling
- loading states
- tooltips
- context menus
- keyboard navigation
- animations
- performance
- memory review


==================================================
43. DEVELOPMENT RULE
==================================================

Do not make hundreds of unrelated changes in one step.

Work incrementally.

For each feature:

1. Inspect.
2. Plan.
3. Implement.
4. Format.
5. Build.
6. Test.
7. Run if applicable.
8. Fix errors.
9. Review memory/performance implications.
10. Continue.


If a feature becomes too large, break it into smaller milestones.


==================================================
44. FINAL SELF REVIEW
==================================================

Before declaring the implementation complete, perform a senior-engineer review.

CORRECTNESS

- Does the feature actually work?
- Are edge cases handled?
- Are errors handled?
- Are cancellation paths handled?
- Are restart scenarios handled?


MEMORY

- Can removed torrents still be referenced?
- Can ViewModels remain referenced after navigation?
- Are events unsubscribed?
- Are timers disposed?
- Are CancellationTokenSource instances disposed?
- Can collections grow indefinitely?
- Are caches bounded?
- Are large files streamed?
- Are background tasks terminated?


PERFORMANCE

- Are there unnecessary allocations?
- Are high-frequency events throttled?
- Is UI update frequency controlled?
- Is UI virtualization used where appropriate?
- Is the UI thread free of blocking work?
- Is the torrent list updated incrementally?


ARCHITECTURE

- Is UI independent from the BitTorrent engine?
- Is business logic outside Views?
- Are ViewModels reasonably thin?
- Are services responsible for their own concerns?
- Are dependencies injected appropriately?
- Are abstractions justified?


MAINTAINABILITY

- Are names clear?
- Are methods reasonably small?
- Is the code understandable?
- Are comments used where they provide real value?
- Is there unnecessary complexity?


QUALITY

Run:

dotnet format

dotnet build

dotnet test

Fix all relevant failures.

Do not declare success without actual validation.


==================================================
45. DEFINITION OF DONE
==================================================

The MVP is considered complete only when:

[ ] Application starts successfully
[ ] Main navigation works
[ ] Modern UI is implemented
[ ] Icons are clear and consistent
[ ] Empty state exists
[ ] Loading state exists
[ ] Error state exists
[ ] User can add .torrent
[ ] User can paste magnet link
[ ] Torrent metadata can be retrieved
[ ] Download location can be selected
[ ] Torrent can start
[ ] Torrent can pause
[ ] Torrent can resume
[ ] Torrent can be removed
[ ] Download actually works
[ ] Progress updates
[ ] Download speed updates
[ ] Upload speed updates
[ ] ETA updates
[ ] Peer/seed information works when available
[ ] Completion is detected
[ ] Torrent state persists
[ ] Restart recovery works
[ ] Settings persist
[ ] Theme switching works
[ ] UI remains responsive
[ ] Large files do not cause excessive memory usage
[ ] No obvious memory leaks are introduced
[ ] Background tasks have clear lifetimes
[ ] Disposable resources are correctly managed
[ ] Errors are logged
[ ] User-facing errors are friendly
[ ] dotnet format succeeds
[ ] dotnet build succeeds
[ ] dotnet test succeeds where tests exist
[ ] No known critical runtime issue remains


==================================================
46. FINAL REPORT
==================================================

When finished, provide a concise final report containing:

1. Features implemented
2. BitTorrent library selected
3. Why that library was selected
4. NuGet packages added
5. Project structure
6. Important architecture decisions
7. Memory/performance considerations
8. Tests performed
9. Commands used for validation
10. Known limitations
11. Remaining work for production
12. Whether the application was actually runtime-tested

Do not claim that something was tested if it was not actually tested.

Do not hide unresolved errors.

The goal is not merely to generate code.

The goal is to produce a stable, maintainable, efficient, memory-conscious, modern Windows torrent downloader that can evolve toward a production Microsoft Store application.