# ArcToon Render Pipeline

A custom toon render pipeline built on Unity's Scriptable Render Pipeline (SRP), featuring advanced toon shading techniques for stylized rendering.

## Features

- Custom toon shading with ramp-based lighting
- Per-object shadow casting and rendering
- Stencil-based face/pupil/fringe rendering for anime-style characters
- Forward+ light culling
- Post-processing effects (FXAA, etc.)
- Geometry-based outline pass
- Transparent object rendering with dedicated passes

## Requirements

- Unity 6000.0 (Unity 6) or later
- Burst package
- Mathematics package
- Render Pipelines Core package

## Installation

Add this package via Unity Package Manager using the Git URL:

```
https://github.com/Archer-du/ArcToonRP-Prototype.git#arctoon/0.1.0
```

1. Open Unity and go to **Window > Package Manager**
2. Click the **+** button in the top left corner
3. Select **Add package from git URL...**
4. Paste the URL above and click **Add**

## Branch Strategy

- `arctoon/0.1.0` — Stable release branch
- `arctoon/develop` — Main development branch

## Usage

1. Create an ArcToon Render Pipeline Asset via **Assets > Create > Rendering > ArcToon Render Pipeline Asset**
2. Assign the asset in **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings**
3. Use the provided shaders (`ArcToon/ToonBase`, `ArcToon/ToonFace`, etc.) on your materials

## License

See [LICENSE.md](LICENSE.md) for details.
