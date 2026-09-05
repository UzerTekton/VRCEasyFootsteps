# VRC Easy Footsteps

- General purpose footsteps system suitable for all VRChat world styles.

- For all players. You can hear other people!
  
- Dynamic and fun!
  - Scales with avatar height. You can hear the size of someone approaching!
  - Reactive to player movement. Hear the difference in how people walk.
  
- No setup needed, just drag the prefab into your scene and it will work.

- Very optimized.
  - No `Update()` Loops.
  - Zero networking.
  - Minimalistic architecture.

## How to setup

Just drag the prefab into your scene.

It should work immediately.

> [!IMPORTANT]
> Add an in-game attribution: `VRC Easy Footsteps 1.0.0 by Uzer Tekton`

## Technical notes

- Created and tested in SDK `3.10.5` and Unity `2022.3.22f1`.

- The GameObject will need these components:
  - VRC Player Object
  - AudioSource (and optionally VRC Spatial Audio Source)
    - Prefab default settings:
      - `clip` = *FootstepGeneric1* (This is where you change the sound file)
      - `spatialize` = `true` (Because VRC Spatial Audio Source won't stop complaining)
      - `playOnAwake` = `false`
      - `loop` = `false`
      - `priority` = `192` (Footsteps are a decorative background sound)
      - `volume` = `1`
      - `spatialBlend` = `1` (Full 3D)
      - `dopplerLevel` = `0` (Footsteps are instantaneous and should not smear across a distance)
      - `Spread` = `0`
      - `rolloffMode` = `AudioRolloffMode.Linear`
      - `minDistance` = `0`
      - `maxDistance` = `25`
    - Prefab default VRC Spatial Audio Source settings:
      - Gain = 10
      - Far, Near = Does not matter
      - Volumetric Radius = 0
      - Enable Spatialization = True (otherwise it won't be 3D or directional)
      - Use Audio Source Volume = True (allows better control with the rolloff curve, otherwise it could be too quiet)
    
  - VRC Easy Footsteps
    - Prefab defaults: `minVolumeScale`, `maxVolumeScale`: Every footstep is automatically adjusted within this range. Adjust to suit your world. These are multiplicative to the `volume` on the AudioSource. Defaults are `0.125` and `1`.
    
- High level process:
  1. Each player has their own copy of this object via VRC Player Object.
  2. VRC Easy Footsteps checks periodically whether it should play the sound.
  3. When it does, it moves the object to the player position, so the sound will come from around the foot position.
  4. It then tells the AudioSource to play the sound, with some adjustments.
  5. The checking loop continues indefinitely, or until the player leaves the instance causing the Player Object getting destroyed.

- Main checking loops are run at 24 Hz for performance reasons, with some fuzzing (slight randomization) to prevent spike patterns from emerging. The organic timing also makes it more natural.

- Footstep checking are based on real world data of normal step distances of a person walking or running at various speeds. The script also reacts correctly to impulse changes such as jumping, and shuffling your feet when turning in place, and correctly detects when you go prone and go quiet.

- The algorithm scales the parameters and the sound to fit the avatar height.
  - Bigger avatars will sound louder, deeper and have a stronger presence. They also take slower but greater steps.
  - Small avatars will sound fast and cheerful and proportion to its size.
  - Medium avatars will sound correct and feel more alive.

- The default sound file is made by myself in Audacity. MIT License. It is designed to be a basic, nondescript all-purpose footstep sound, is suitable for pitch adjustment, and believable for most floor surfaces.

> [!TIP]
> For advanced customization, see constants and comments in the code.

## Version history

VRC Easy Footsteps 1.0.0

2026-09-05

- Initial release.

## Contact

Discord: https://discord.gg/yG4HnBM8Du

## License

MIT License
