# Changelog

All notable changes to this project will be documented in this file.

## [0.5.0] - 2026-07-25

### Added

- Linear partition shade model with 7-region architecture and extended parameters
- Independent RimLight shader GUI section

### Changed

- Reorganized shader file structure for better modularity

## [0.2.0] - 2026-07-19

### Added

- Runtime Poisson/Vogel disk PCF/PCSS shadow filtering
- SMAA post-processing effect
- Stencil configuration switches and geometry outline options
- Region ID system for per-region material parameters
- Additive light mode for forward core lighting
- Attribute-driven config GUI framework
- Per-channel PBR metallic/roughness/occlusion map selection

### Changed

- Removed RenderGraph dependency; migrated to RTHandle + CommandBuffer direct execution
- Rebuilt post-processing framework: data-driven `PostProcessPass` with `VolumeConfig`-owned `Processor`s (replacing hardcoded `PostFXPass`)
- Unified `RenderPassBase` lifecycle with explicit ctor injection; folded `Initialize` into `SetupFrameData`
- Renamed Toon pass files / LightModes to render-domain `Forward*` naming
- Split shader Input layer into CBUFFER-free Library + per-shader inline CBUFFER + feature Interface files
- Rewrote shader includes to package-absolute paths
- Split `RenderResources` into domain-specific sub-containers
- Simplified namespaces from `ArcToon.Runtime.*` to `ArcToon.*`
- Isolated debug pipeline into `GeometryDebug` and `ScreenDebug` passes
- Unified PBR scalar to linear roughness via `GetRoughness` input interface
- Extracted `ShadowAtlasRenderer` base class and singleton `PerObjectShadowCasterManager`

### Fixed

- Screen-space coordinate errors in Forward+ tile bounds
- Penumbra width estimation for directional light
- Depth RTHandle format mismatch (use `D32_SFloat_S8_UInt` to match RenderGraph `DepthBits.Depth32` behavior)
- Profiling `BeginSample`/`EndSample` mismatch errors
- Post-process engine material null reference

## [0.1.0] - 2026-03-10

### Added

- Initial release as a Unity Package
- Custom toon shading pipeline with ramp-based lighting
- Per-object shadow casting system
- Stencil-based face, pupil, and fringe rendering passes
- Forward+ light culling
- Post-processing stack (FXAA)
- Geometry-based outline pass
- Transparent object rendering passes
- Custom ShaderGUI for material editing
- Camera and Light editor overrides
