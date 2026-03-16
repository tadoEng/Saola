# Saola

Grasshopper components for ETABS structural modeling, built on [EtabSharp](https://github.com/tadodev/EtabSharp).

Named after Vietnam's rarest animal — the Asian unicorn.

---

## Prerequisites

- Rhino 8 (uses .NET 8)
- ETABS 21+ installed and licensed
- EtabSharp 0.3.2-beta on NuGet

## Setup

1. Copy `ETABSv1.dll` from your ETABS installation into `refs/`
   - Default: `C:\Program Files\Computers and Structures\ETABS 21\ETABSv1.dll`

2. Update Rhino/Grasshopper assembly paths in both `.csproj` files if Rhino 8
   is not installed at the default location.

3. Build the solution:
   ```
   dotnet build Saola.sln
   ```

4. Copy `Saola.gha` from `src/Saola.Plugin/bin/Debug/net8.0/` into your
   Grasshopper Libraries folder:
   - `%APPDATA%\Grasshopper\Libraries\`

5. Open Rhino 8, launch Grasshopper, and find components under the **Saola** tab.

---

## Components

### Connection
| Component | Nickname | Description |
|-----------|----------|-------------|
| Initialize | Init | Connect to a running ETABS instance |

### Frames
| Component | Nickname | Description |
|-----------|----------|-------------|
| Draw Column | Column | Draw a column from a line + section name |
| Draw Beam | Beam | Draw a beam from a line + section name |

### Areas
| Component | Nickname | Description |
|-----------|----------|-------------|
| Draw Wall | Wall | Draw a wall from a closed point list + section name |
| Draw Slab | Slab | Draw a slab from a closed point list + section name |

---

## Usage pattern

All components pass the `Model` connection through as the first output,
so you can chain them left to right on the canvas:

```
[Initialize] → Model → [Draw Column] → Model → [Draw Wall] → ...
```

Wall and Slab components accept a **closed** point list — the last point
should repeat the first (standard Grasshopper closed polyline convention).
The closing point is stripped automatically before passing to ETABS.

---

## Project structure

```
Saola/
├── Saola.sln
├── refs/                        ← place ETABSv1.dll here (not in source control)
└── src/
    ├── Saola.Core/              ← no GH dep, unit-testable
    │   ├── Core/
    │   │   ├── ETABSSession.cs  ← COM singleton
    │   │   └── GH_ETABSModel.cs ← Goo wrapper
    │   └── Parameters/
    │       └── ETABSModelParameter.cs
    └── Saola.Plugin/            ← GH components + plugin registration
        ├── SaolaInfo.cs
        └── Components/
            ├── Connection/InitializeComponent.cs
            ├── Frames/DrawColumnComponent.cs
            ├── Frames/DrawBeamComponent.cs
            ├── Areas/DrawWallComponent.cs
            └── Areas/DrawSlabComponent.cs
```
