# Magazine Check Interrupt
Seamlessly transition from a magazine check to a reload!

---

**Mag Check Interrupt** allows you to interrupt a magazine check mid-animation and transition into a reload.

### Features
- Continue directly into a reload, no need to wait for the magazine check animation to finish
- Finish the magazine check animation if no action is taken
- Slow down animation during the reload window, to give you ample time to decide if you should reload (configurable). Animations vary per magazine and some are fast
- Configuration is available client side

![Showcase gif](Assets/showcase.gif)

### Installation
- Extract the contents of the .zip archive into your SPT folder.
<details>
  <summary>Demonstration</summary>

![Installation](https://i.imgur.com/3N6gTe2.gif)
Thank you [DrakiaXYZ](https://forge.sp-tarkov.com/user/27605/drakiaxyz) for the gif
</details>

### Configuration
In the BepInEx configuration manager (<kbd>F12</kbd>)
- `Enable Slow Animation` - Slow down the check magazine animation for a certain time. Default is `true`

Advanced Configuration
- `Reload Window Start` - How early you can reload during the check magazine animation, in normalized time
- `Reload Window End` - How late you can reload during the check magazine animation, in normalized time
- `Slow Percentage` - Multiplier for the check magazine animation speed when Slow Animation is enabled
- `Slow Animation Start` - When to start slowing down the check magazine animation, in normalized time
- `Slow Animation End` - When to restore speed of the check magazine animation, in normalized time
- `Smoothing Max Delta` - Max delta for the smoothing of the slow animation. A higher value slows/restores the animation faster

### Compatibility
- [UI Fixes](https://forge.sp-tarkov.com/mod/1342/ui-fixes) - Partially incompatible with UI Fixes' Reload in Place feature

### Support
<details>
  <summary>Known Issues</summary>

- Animation events involving sounds may still fire on reload, resulting in duplicated sounds.
- Timings may vary slightly depending on the weapon/magazine animation

</details>

<details>
  <summary>Support</summary>

If you find bugs, or have feature suggestions, feel free to post them on the comments section, or open an issue on GitHub, or most preferably through the SPT Discord, ozen
</details>

### Credits
<details>
  <summary>Credits</summary>

- Thanks to [Tyfon](https://github.com/tyfon7) for lending me his Fika config sync code!
</details>
