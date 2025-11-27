# EnhancedMovement

A BepInEx mod for MycoPunk that provides comprehensive movement system enhancements and refinements.

## Description

This client-side mod represents the culmination of movement system optimization for MycoPunk, patching core Player movement methods to deliver enhanced responsive and fluid character motion. By extensively modifying fundamental movement calculations including UpdateMovement, HandleJump, SetMoveInput, ApplyGravity, and related physics interactions, the mod achieves superior movement characteristics that feel more precise and dynamic.

Perfect for players who demand the most refined movement experience available, whether for competitive gameplay, complex maneuvers, or simply enjoying the most polished feel possible from the MycoPunk movement system.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Option 1: Via Thunderstore (Recommended)**
1. Download and install using the Thunderstore Mod Manager
2. Search for "EnhancedMovement" under MycoPunk community
3. Install and enable the mod

**Option 2: Manual Installation**
1. Ensure BepInEx is installed for MycoPunk
2. Copy `EnhancedMovement.dll` from the build folder
3. Place it in `<MycoPunk Game Directory>/BepInEx/plugins/`
4. Launch the game

### Executing program

Once installed, the mod works automatically and enhances all movement systems:

**Movement Enhancements:**
- **Refined Movement Calculations:** Optimized UpdateMovement processing for smoother motion
- **Enhanced Jump Mechanics:** Improved HandleJump implementation for better air control
- **Precise Input Processing:** Fine-tuned SetMoveInput for more responsive controls
- **Optimized Physics:** Refined ApplyGravity for more natural falling and momentum
- **Comprehensive Motion:** Multiple patched methods work together for cohesive movement experience

**Behavioral Improvements:**
- More responsive ground movement and airborne control
- Enhanced maneuverability in all game scenarios
- Improved physics interactions with environment
- Smoother transitions between movement states
- Better overall movement fluidity and precision

**Compatibility Features:**
- Works across all game levels and environments
- Compatible with various weapon and upgrade combinations
- Maintains stability during complex movement sequences
- Preserves game balance while providing enhanced feel

## Help

* **Movement feels different?** Mod extensively refines core movement calculations - more responsive feel is intended
* **Performance issues?** Monitor FPS - if significant drops occur, disable temporarily to test
* **Conflicts with other mods?** Highly likely to conflict with other movement mods - use as standalone
* **Not working?** Mod patches many core Player methods - ensure BepInEx logs show successful patches
* **Too different?** This mod provides comprehensive movement overhaul - consider if lesser modifications suit
* **Platforming harder/easier?** Jump and gravity modifications may change difficulty - adjust expectations
* **Disable temporarily?** Easy to remove .dll if you want to test without mod
* **Advanced configuration?** Mod works automatically - no config options to prevent conflicting setups

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
