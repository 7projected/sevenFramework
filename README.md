# sevenFramework

A custom 2D game framework built on MonoGame featuring tile-based rendering, SAT collision detection, chunk-based spatial partitioning, and a custom animation system.

## Features

### Core Engine
- Scene-based architecture
- Delta-time update system
- Texture dictionary loading system
- Keyboard input tracking (pressed/released)

### Tile Map System
- Tiled JSON map support
- Multi-layer tile rendering
- TileSet-based definitions
- Render target baking for performance
- Tile scaling system

### Collision System
- SAT-based polygon collision detection
- Broad-phase AABB optimization
- Narrow-phase collision resolution
- Collision layer system
- Chunk-based spatial partitioning

### Chunking System
- Automatic world chunk generation
- Polygon assignment per chunk
- Spatial collision optimization

### Entity System
- Basic physics-based movement system
- Player controller implementation
- Axis-separated collision resolution
- Velocity-based movement

### Camera System
- World-space transform matrix
- Zoom and rotation support
- Camera clamping to world bounds
- Render target rendering pipeline

### Animation System
- Keyframe-based animations
- Duration-based playback system
- Looping animation support
- Animation manager with named states

### Rendering
- SpriteBatch rendering pipeline
- Tile map baking system
- Pixel-perfect rendering support

### Math Utilities
- Custom Vector2i integer type
- Rotation system (degrees and radians)
- Transform system
- Polygon SAT utilities

### Debug Tools
- Debug rendering system
- Polygon visualization for collision debugging
- Bounding box visualization
- FPS counter

## Architecture Overview

The engine consists of:

- SceneManager (game loop and state handling)
- TileMap (world rendering and collision generation)
- CollisionMap (spatial partitioning and chunking)
- BasicEntity (movement and collision system)
- AnimationManager (animation state system)
- Camera (world-to-screen transformation)

## Requirements

- MonoGame Framework
- .NET 6 or higher

## Notes

This engine is in active development and is focused on custom 2D systems including collision, rendering, and animation.

## Future Plans

- Optional ECS architecture
- Improved animation blending
- Physics improvements (friction, gravity system)
- Lighting system
- Editor tooling support