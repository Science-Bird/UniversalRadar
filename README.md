# Universal Radar

### My solution to the problems with the vanilla radar and the incompatibilities it introduces with modded moons, all in one highly configurable package!

---

- **Automatically detects terrain meshes on any moon and generates a contour map**
	- This works for modded moons via LLL (assuming it can find something that looks like terrain), and supports both mesh terrain or Unity's built-in terrain.
	- These are also highly configurable, so if you aren't happy with how a certain moon's map is auto-generated, you can always adjust its parameters.

- **Generates radar objects for large meshes (and bodies of water) on modded moons**
	- So you can usually see buildings and other large obstacles on the radar as well

- **New handcrafted radar sprites for objects, buildings, and hazards on vanilla moons**

- **A few other tweaks to radar and camera things to make the rest of the mod work as intended**

![Radar comparisons](https://imgur.com/vebv48v.png)

---

## Image Gallery

<details>
<summary>Expand</summary>
<br />

![Radar examples 1](https://imgur.com/IIW4MOl.png)

![Radar examples 2](https://imgur.com/mYxoFZ8.png)

![Radar examples 3](https://imgur.com/iB7PG6g.png)

</details>

---

## Questions / Issues

### What does this do for modded moons and vanilla moons?

On modded moons, this will add a height-shaded terrain contour map (if applicable terrain can be found) and create translucent meshes for large objects which will appear on the radar screen. This should be enough to create a fully functioning radar even when the moon creator hasn't added a contour map or any radar sprites themselves.

On vanilla moons, a terrain contour map is still generated automatically, but instead of meshes generated at runtime, sprites are used to represent buildings and large objects (which is more in-line with the vanilla style).

All of this is configurable, though, so if you want one feature but not the other, or vanilla moons to use generated meshes instead, that's all possible.

### What about moons without terrain?

Even without the terrain contour map, the generated radar meshes for large objects should usually fill in more boxy or industrial environments.

### How does this affect load times or performance?

It certainly may add a bit to your load times, but it should only be really significant when landing on a moon that does not have mesh terrain (the standard vanilla type of terrain in Lethal Company) for the first time. This can take especially long depending on the size or number of terrain objects on the moon. However, meshes are remembered for the rest of the session after this.

*(if this becomes a serious problem or even times out players with load times, you can disable these types of maps from being generated in config)*

The one other area I'd be concerned about for performance (though I haven't tested this extensively) is the generation of radar objects. So, if you feel like this mod is lowering your FPS (especially when looking at the radar screen), try lowering the ``Radar Object Display Threshold`` in config, or use the master disable option to stop generating them entirely.

### The default generated maps for certain moons don't look right

Consult the user guide below if you want to better configure it, and if it's your moon, consult the moon creator guide for how you can improve compatibility/change those default config settings.

### What moons don't work with Universal Radar?

As far as I know, [Magic Wesley's Cosmocos](https://thunderstore.io/c/lethal-company/p/Magic_Wesley/Cosmocos/) is the only entirely incompatible moon, but some may have mixed results, especially if they have many dynamic elements. Let me know if there are any other completely dysfunctional moons.

### Is this compatible with RebalancedMoons or MapImprovements?

[RebalancedMoons](https://thunderstore.io/c/lethal-company/p/dopadream/RebalancedMoonsBeta/): sort-of. [MapImprovements](https://thunderstore.io/c/lethal-company/p/SpookyBuddy/MapImprovements/): mostly not.

My custom sprites for vanilla moons have the vanilla layout in mind, so they're disabled when RebalancedMoons is detected and replaced with the runtime-generated meshes you see on modded moons (these are still controlled by the vanilla radar sprite config).

MapImprovements does its additions after my mod in the load order, so even dynamically generated meshes would be inaccurate, therefore no radar objects will be shown at all on vanilla moons with MapImprovements installed.

Terrain contour maps should work fine with both as far as I know. I've thought about further compatibility for RebalancedMoons by making some additional custom sprites, but I'm not ready to commit to that at the moment. MapImprovements is trickier since I'd likely need to totally change when my mod runs, which presents some challenges.

### Does this work client-side?

Maybe? Theoretically everything it does is on the client, but it would naturally mean some players would take longer loading than others. Let me know if you try it.

### Some objects aren't appearing on the radar on modded moons

If the object is smaller (specifically it's bounding box, something diagonal will be "bigger" than the same thing horizontal), then you can try lowering the `Radar Object Display Threshold` in general config.

However, there are other reasons why objects may not appear, such as the components they have and the structure of their colliders and renderers. This is just down to the way the moon is made, but I hope to be able to further improve my algorithm to better detect these anomalies.

### What about things on a moon that move or appear sometime after it loads?

Since Universal Radar does its generation right after the level loads, it goes before most other things (including random spawned map objects and any mods/moon mechanics which might introduce objects into the scene). I might want to change this at some point to improve compatibility, but at the moment I don't want to risk breaking all my existing code for it.

Some moving things might have the radar meshes move with them depending on how they're structured, but don't count on it 100%.

### Why do radar sprites on vanilla moons appear on the screen no matter how high up you are?

The way contour maps and outside radar objects work in vanilla is that they're fixed on the screen at all times. My contour map and generated radar objects are different in that they will clip in and out of view depending on height. Although I think some objects should be visible from all heights, others might benefit from being more height specific, but that would require some modifications to the vanilla structure of things. I'm considering it, but that's why things are the way they are now.

### The contour lines sometimes have messy pointy bits and smudges or other artifacts

The lines themselves will sometimes be sharp because they're based on a mesh which can have sharp edges and jagged turns, but there are also some underlying issues with the shader used to generate these contour maps (especially noticeable with larger line thickness). I worked on this issue for a while, but was unable to come up with anything better than what I have now. I very much hope there is some way to further improve the shader to reduce these issues, but it might require switching to a different method of generating contours entirely.

---

## User Guide

<details>
<summary>Expand</summary>

### Configuration modes

After generating the config by starting/joining a lobby, each moon will have an entry in config (under `Moon Overrides`). Most of these will be set to `Auto`, which means the shading of the map will be calculated based on the mesh when the moon loads.

If you change the mode to `Manual`, then re-generate your config again, you'll have many more options to customize that moon more precisely.

If you want to leave a moon completely unchanged (if it's causing issues or has an existing contour map you prefer), change the mode to `Ignore`.

### Quick and easy moon changes

There are a few immediate options for tweaking contour maps for specific moons in `Moon Overrides` while they're still in `Auto` mode:

- **Show Radar Objects:** Generates meshes for large objects on the moon and display them on the radar screen (will usually make things like large obstacles, buildings, or foliage visible).

- **More Translucent Radar Objects** For moons with very dense sets of objects (often when a moon has a large interior section), the radar meshes will be more transparent and thus won't be as overly bright when many are layered on top of each other.

- **Broader Height Range**: This is the first thing to try toggling if the shading on a moon appears to get too bright too quickly. It will spread out the shading over a longer stretch of elevations.

- **Opacity Multiplier**: Increase or decrease this to multiply the overall opacity level of the map's shading (higher values make the shading lighter). Note that since this is multiplication, if the shading is already almost black, multiplying it won't do much since it's already so low.

- **Line and Shading Colour**: Change up the colour of your contour map by changing this colour hex code.

In `Automatic Settings` there are some global settings for how all moons (with mode set to `Auto`) will automatically generate their contour maps:

- **Line Spacing**: The vertical height that will separate each contour line (lower this to add more lines, increase it to make fewer lines).

- **Line Thickness**: How thick the contour lines should be (**be aware that setting this higher will make errors in contour lines much more noticeable, and also will have less and less impact on thickness with larger values**).

- **Max Opacity**: How light the very highest parts of a contour map should be (higher for lighter).

Other than `Broader Height Range`, the `Opacity Multiplier` and `Max Opacity` are the most useful for troubleshooting. Note that the `Max Opacity` will only stop the shading from getting any brighter, but if you multiply the shading enough, it might reach that maximum before the actual target height for the map. If you're setting the maximum opacity to something below 0.9, you might want to try setting the `Opacity Multiplier` to the same thing, which should keep the multiplier from getting too ahead of the maximum.

### Full manual customization

If you switch to the `Manual` mode in `Moon Overrides`, you can change all the above mentioned values for that moon specifically, as well as:

- **Minimum and Maximum Height**: The heights where shading starts and ends (from black at the minimum to fully light at the maximum).

These are usually automatically computed, so if you want to try changing them yourself, you'll probably want a reference for where they're usually calculated at. Enable the `Log Automatic Values` setting in `Automatic Settings` and land on the moon you're interested in customizing. Check the logs for the maximum and minimum values it generates, and use those as a starting point.

### Additional config

There are also a handful of general configs for what types of radar objects should show up and more fine-tuning, but they're mostly self-explanatory. So, just give the config a read over if you want to change something.

</details>

---

## Moon Creator Guide

<details>
<summary>Expand</summary>

### Existing contour maps

By default, Universal Radar will disable any existing contour maps when it introduces its own. Moons can be set to `Ignore` in config to leave all their assets untouched. If you want the default values changed, see that section down below.

### Contour map compatibility

If you want to ensure your moon's terrain will be selected and used adequately, here's some tips:

- **If possible, use [TerraMesh](https://github.com/v0xx-dev/TerraMesh) to convert your terrain to a mesh if you haven't already (you can find more info in the [Lethal Company Modding Discord](https://discord.com/channels/1168655651455639582/1303914349533990983)), as this will significantly reduce load times when first landing on the moon with this mod (and is probably a good idea regardless to make terrain look nice).**

- Make sure your intended terrain objects have "terrain" in their name (and are either a Unity terrain object or have a mesh collider), and if there's any terrain you don't want to be mapped, make sure either the object or one of its parents has "OutOfBounds" somewhere in its name (neither of these are case sensitive).

	- *I automatically detect terrain based on a bunch of factors, so it can work without doing this, but you might as well guarantee it.*

- Clean up your nav mesh so it isn't going excessively out of bounds. I use the nav mesh to refine down the height range for terrains, so if the nav mesh is going way too high up the height shading will be off, and if it goes too far out a lot of out of bounds terrains or objects might have meshes generated for them.

- Make sure your terrain is on the `Room` layer (which it already should be).

### Radar object compatibility

If you're having issues with non-terrain objects not showing up on your moon, here are some things to note:

- I check for objects of a certain size (based on the size threshold in config) on the `Room`, `Default`, and `Collider` layers (plus `Foliage` and `Terrain` if a specific config is enabled), so make sure you're objects aren't really small and are on the right layers.

	- *Specifically, it looks at the area of the bounding box of the object when seen from above.*

- Meshes are found based on colliders, so your mesh renderers shouldn't be too far from your colliders in the hierarchy. Specifically, the mesh renderer should be either a parent, a grandparent, or on the same object as the collider (if this is too inconvenient, you can just give the colliders mesh renderers and they'll act as an outline for the missing mesh).

- Certain key words are used to include certain meshes regardless of their size: "catwalk" (anywhere in the object's path), "bridge" (anywhere in the object's path), or "floor" (specifically in the name of the mesh renderer), all not case-sensitive.

- Like with terrain, "outofbounds" in the path of an object will also exclude it from being used.

- I avoid certain components on objects, such as animators, network objects, and skinned mesh renderers, so note that mesh renderers associated with those won't be used.

	- *If you really need a radar object to move with an animation, try making it a child object without an actual animator, that should work fine.*

- For water, I just look for materials with names containing "water" and a shader name also containing "water". As far as I know this works for both vanilla and modded water blocks.

### Changing default values

If contour maps or radar objects aren't generating nicely with default `Auto` settings, I have some LLL content tags in place to make some adjustments. Simply make a content tag and set the "Content Tag Name" to one of the following and add it to your extended level to change the default config values:

- **`UniversalRadarExtendHeight`**: Equivalent to the `Broden Height Range` config, will increase the maximum level for shading. This is a common fix for contour shading being too bright or transitioning too quickly.

- **`UniversalRadarHideObjects`**: Equivalent to (disabling) the `Show Radar Objects` config, use this if you want radar object meshes to not generate on your moon by default.

- **`UniversalRadarLowOpacityObjects`**: Equivalent to the `More Translucent Radar Objects` config, use this if you have lots of layered radar objects that make things look way too bright.

- **`UniversalRadarIgnore`**: Equivalent to setting the mode of a moon to `Ignore`. This will mean Universal Radar won't change this moon at all by default.

- **`UniversalRadarLineColor`**: Determines the colour of the contour lines (and radar objects if present) on your moon. Set this using the "Content Tag Color" field.

- **`UniversalRadarBaseColor`**: Determines the colour of the contour shading on your moon. Set this using the "Content Tag Color" field.

These all still keep the moon in `Auto` mode, though. So, if you really need more fine control, you can either ask me to change the default values for you or patch the return value of [this method](https://github.com/Science-Bird/UniversalRadar/blob/main/Patches/LLLConfigPatch.cs#L11) in my mod yourself.

### Making radar sprites

Specifically for vanilla moons, I've made a bunch of custom radar sprites to populate the radar while still maintaining the original feel. If you want to try using these for your own moon, all the sprite assets/colours I used are publicly available [here](https://cdn.discordapp.com/attachments/663270103185358848/1385006066315690105/Radar_Sprite_Templates.zip?ex=68547eea&is=68532d6a&hm=53e15ad8b601dbb0383f92e9fd8bd3e30047f6c230f26e87eace561b455bbd5b&). Just ask me if you need any tips or want to know how I made certain sprites.

</details>

---

## Credits

Thank you to **IAmBatby** and **xameryn** for some consulting at various stages of the project.

Thanks to [v0xx's TerraMesh](https://thunderstore.io/c/lethal-company/p/v0xx/TerraMesh/) for making it possible to convert Unity terrains to meshes at runtime! Otherwise that would've been one hell of a compatibility challenge.

## Contact

Let me know about any suggestions or issues on the [GitHub](https://github.com/Science-Bird/UniversalRadar) or the Discord forum thread (I'm "sciencebird" on Discord).

---
I'm ScienceBird, I've made some other Lethal Company mods, I also do Twitter art sometimes, and I'm part of the Minecraft modding team Rasa Novum. Check us out on [CurseForge](https://www.curseforge.com/members/rasanovum/projects) or [Modrinth](https://modrinth.com/user/RasaNovum/mods) if you're interested in highly polished, balanced, yet simple Minecraft mods.