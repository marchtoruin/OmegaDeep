# UnityDiverGame

## Description

UnityDiverGame is a 2D underwater adventure game developed in Unity. Players control a diver exploring underwater environments, managing oxygen and health while interacting with marine life and collecting items. The game features tile-based environments created using **Unity's built-in Tilemap tools**.

## Features

*   **Player Control:** Diver movement (`DiverMovement`), aiming (`ArmAim`), shooting (`DiverShooter`), flashlight (`FlashlightController`).
*   **Resource Management:** Player health (`PlayerHealth`) and oxygen (`PlayerOxygen`) systems with UI bars (`PlayerHealthBar`, `PlayerOxygenBar`).
*   **Collectables:**
    *   Air Bubbles (`AirBubble`): Replenish oxygen, respawn after collection.
    *   Red Orbs (`RedOrbCollect`): Replenish health upon collection.
*   **Enemy AI & Systems:**
    *   Cone Snail: Simple patrol behavior (`ConeSnailPatrol`) and health (`ConeSnailHealth`).
    *   Bad Fish: Patrol, chase, and boss charge attack behavior (`BadFishAI`) with health system (`badFishHealth`).
    *   Jellyfish: Basic AI (`JellyfishAI`), health (`JellyfishHealth`), player damage (`JellyfishDamagePlayer`), charge pickup (`JellyChargePickup`).
    *   Generic Enemy Systems: Collision handling (`EnemyCollision`), damage (`EnemyDamage`), movement (`EnemyMovement`), spawning (`EnemySpawner`).
*   **Environment & Physics:**
    *   Tile-based maps created with Unity Tilemaps.
    *   Map collision handled via `Tilemap Collider 2D` and `Composite Collider 2D`.
    *   Map loading potentially handled by `LayerChunkLoader`.
    *   Physics interactions (e.g., bouncing collectables via `Rigidbody2D`).
*   **Camera & Effects:**
    *   Camera follows the player (`CameraFollow`).
    *   Parallax scrolling for backgrounds (`MidgroundParallax`).
    *   Visual effects like water waves (`WaterWave`), player damage effects, helmet bubbles (`HelmetBubbleEmitter`).
*   **Audio:** Integration with FMOD for sound events (`FMODPlayOnTrigger`), bus management (`FMODBusManager`), water surface sounds (`WaterSurfaceFMOD`), boost sounds (`AirboostAudio`), and music (`MusicManager`).
*   **Lighting:** 2D lighting effects using URP's Light 2D system.
*   **UI:** Health/Oxygen/Battery bars, Death screen (`DeathUIController`), Typewriter text effect (`TypewriterEffect`).
*   **Scene Management:** Transitions between scenes (`SceneTransition`), potential warping (`WarpingScripts` folder).

## Engine Version

*   Unity **2022.3.x LTS** (Ensure you have a compatible version installed)

## Setup

1.  Clone the repository or download the project files.
2.  Open Unity Hub.
3.  Click "Open" or "Add project from disk".
4.  Navigate to the `UnityDiverGame` project folder and select it.
5.  Ensure Unity Hub uses a **2022.3.x LTS** version to open the project.
6.  The main scene is likely located in `Assets/Scenes/`.

## Project Structure Overview

*   `Assets/Scenes/`: Contains the game scenes (levels, main menu, etc.).
*   `Assets/Prefabs/`: Contains reusable game objects like the Player, enemies, collectables.
*   `Assets/Scripts/`: Contains all C# game logic scripts, potentially organized into subfolders (Lighting, TitleScreen, WarpingScripts).
*   `Assets/Art/`: Contains visual assets like sprites, textures, animations.
*   `Assets/Audio/`: May contain raw audio files (though FMOD project might handle this).
*   `Assets/Materials/`: Contains Unity materials for rendering sprites and effects.
*   `Assets/Palettes/`: Likely contains Tile Palettes used for level design.
*   `Assets/Settings/`: May contain project-specific settings assets (e.g., Render Pipeline Assets).
*   `Assets/FMODProject/`, `Assets/FMODSessions/`: Contain the FMOD Studio project files and session data.
*   `Assets/Plugins/`: Contains third-party libraries or assets (e.g., FMOD integration).
*   `Assets/TextMesh Pro/`: Contains assets and resources for TextMesh Pro UI text.

## Key Systems & Scripts (Summary)

*(See Features section for more detail on specific scripts)*

*   **Player:** `PlayerHealth`, `PlayerOxygen`, `DiverMovement`, `DiverShooter`, `ArmAim`, `FlashlightController`
*   **Enemies:** `BadFishAI`, `badFishHealth`, `ConeSnailPatrol`, `ConeSnailHealth`, `JellyfishAI`, `JellyfishHealth`, `EnemyCollision`, `EnemySpawner`
*   **Collectables:** `AirBubble`, `RedOrbCollect`
*   **UI:** `PlayerHealthBar`, `PlayerOxygenBar`, `BatteryBarUI`, `DeathUIController`
*   **Core Systems:** `CameraFollow`, `LayerChunkLoader`, `SceneTransition`, various FMOD scripts.

## Third-Party Tools

*   **FMOD Studio:** Used for implementing advanced audio.

## Architectural Overview & Considerations

This project follows a standard Unity component-based architecture. Key systems are encapsulated in scripts attached to GameObjects.

*   **Player Entity:** The player's functionality is distributed across several components likely attached to the main Player GameObject or its children (e.g., `ArmPivot`):
    *   `PlayerHealth`: Manages health, damage, invulnerability, death, and respawn. Also currently handles `RedOrb` collection via `OnCollisionEnter2D`.
    *   `PlayerOxygen`: Manages the oxygen resource.
    *   `DiverMovement`: Handles player physics, movement input, and knockback states.
    *   `ArmAim`: Controls the aiming direction of the player's arm/weapon.
    *   `DiverShooter`: Manages weapon firing logic.
    *   `FlashlightController`: Controls the flashlight functionality.
    *   `HelmetBubbleEmitter`: Likely handles visual effects for breathing/oxygen.
*   **Enemy Entities:** Enemies generally follow a pattern of having separate AI and Health components:
    *   `BadFishAI` / `badFishHealth`: Handles complex enemy logic including patrol, chase, and the boss charge mechanic. `badFishHealth` manages health, boss status modifications, and UI.
    *   `ConeSnailPatrol` / `ConeSnailHealth`: Simpler patrol enemy.
    *   `JellyfishAI` / `JellyfishHealth`: Specific jellyfish logic.
    *   `EnemyCollision` / `EnemyDamage`: Appear to be potentially generic components for handling enemy damage dealing, possibly used by multiple enemy types.
*   **Collectable Entities:** Two distinct patterns are used:
    *   `AirBubble`: Uses its own `OnTriggerEnter2D` logic to handle collection and respawning. Relies on a trigger collider setup.
    *   `RedOrbCollect`: Currently acts as a data container (`healAmount`). Collection is handled by the `PlayerHealth` script's `OnCollisionEnter2D` based on tag ("airBubble") and physical collision.
*   **UI System:** UI elements like health, oxygen, and battery bars (`PlayerHealthBar`, `PlayerOxygenBar`, `BatteryBarUI`) are updated by corresponding player system scripts (`PlayerHealth`, `PlayerOxygen`, `FlashlightController` likely). A `DeathUIController` manages the death screen.
*   **Scene/World Management:**
    *   `LayerChunkLoader`: Suggests a system for loading/unloading parts of the map based on player position, likely for performance or large world management.
    *   `SceneTransition` / `WarpingScripts`: Indicate systems for moving between different scenes or areas within a scene.
*   **Physics:** The game utilizes `Rigidbody2D` for physics simulation. Collision interactions are managed via Layers in the Physics 2D settings, `Tilemap Collider 2D`, `Composite Collider 2D`, and specific component colliders (Box, Circle, Trigger/Non-Trigger). Physics Materials are used for bounce/friction control.
*   **Audio:** FMOD is integrated for audio playback, likely using `FMODPlayOnTrigger` for simple events and potentially `FMODBusManager`, `MusicManager`, and others for more complex control.
*   **Dependencies & Setup:**
    *   Scripts often find required components using `GetComponent`, `GetComponentInChildren`, `FindObjectOfType`, or direct Inspector assignment.
    *   Scripts like `SetupHealthConnections` and `EnsureConnections` exist, suggesting some complexity in ensuring component references are correctly linked at runtime, possibly automating some setup steps.

**Potential Areas for Review & Consideration:**

*   **Collection Consistency:** The differing collection methods for Air Bubbles (trigger script on bubble) vs. Red Orbs (collision script on player) might become harder to manage. Consider standardizing on one approach if possible (e.g., all collectables handled by player collision script, or all collectables handling their own collection via triggers/interfaces).
*   **Component Coupling:** How tightly coupled are systems like Player, UI, and Enemies? For example, `PlayerHealth` directly references `PlayerHealthBar`. Consider exploring patterns like events, delegates, or Scriptable Object events to decouple systems further, which can improve modularity and testability.
*   **Enemy System Design:** The presence of `EnemyCollision`, `EnemyDamage`, and `EnemyMovement` alongside specific AI/Health scripts (`BadFishAI`, `JellyfishAI`, etc.) suggests potential overlap or areas for simplification/refactoring into a more unified enemy base class or interface structure.
*   **Setup/Helper Scripts:** The role of `SetupHealthConnections` and `EnsureConnections` should be clearly understood. Do they mitigate complex scene setup requirements? Could dependency injection frameworks or Scriptable Object architecture simplify these connections?
*   **State Management:** Complex AI like `BadFishAI` uses an enum for state management. Evaluate if this approach remains clear and manageable as AI complexity grows, or if a more formal State Machine pattern might be beneficial.

*(This README provides a more detailed overview based on project exploration. Feel free to expand it further.)* 