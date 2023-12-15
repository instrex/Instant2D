![logo](https://github.com/instrex/Instant2D/blob/master/Instant2D/Resources/icon.png?raw=true)

**Instant2D** is a code-first game framework built for convenience and ease of use. It is currently **work in progress** (as this page and repository lol), so things constantly change!
Most of the vital stuff is up and running now, and multiple unique concepts have been implemented.

## 

### **Timescales**  
Every entity may have its own **timescale** value, which will influence how fast its **update logic** gets triggered.

### **Transform Interpolation**
Entity position, rotation and scales are interpolated according to **actual time flow**. Combined with the **Timescale** feature, this makes for amazingly **smooth slow-mo effects** and support for **monitors with high refresh rate** rarely seen in 2D games.  

You can easily adjust the timestep logic to fit your project the best!

### **Assets**
Instant2D features an asset pipeline which is easy to use, understand and extend. You can easily define your own loaders, apply **hot reloading** logic to them, load assets **on-demand** or **immediately**.
```cs 
using Instant2D.Assets;

class ExampleLoader : IAssetLoader {
    public IEnumerable<Asset> Load(AssetManager assets) {
        using var stream = assets.OpenStream("hello_world.txt");
        using var reader = new StreamReader(stream);

        yield return new Asset<string>("example/hello_world", reader.ReadToEnd());
    }
}
```


Repositories allow to read assets from a **packed source** (such as a **.zip file**), folder or groups of packed files. You can extend it to support **any file system**!  

By default, it will read assets directly from the `assets` folder (configurable).

### **Sprite Manifests**
A powerful **json-based manifest format** which allows you to define properties of sprite assets. Supports **frame-based animation**, which features **custom keyframe events**, **sprite points** and alot more!

You can setup a sprite to be split into multiple different sprites or define rectangles of an image manually and adjust sprite's origin (absolute or normalized).

Using the **hot reload system**, you can fine-tune manifest files and sprites themselves, and **all the changes will be reflected in real-time**!  

You don't have to define everything in manifests, as entries for all sprite assets (not currently present in defined manifests) will be automatically generated using default values!

```jsonc
{
    "sprite_name": {
        // absolute origin
        "origin": [12, 12],

        // or relative origin (should be decimal!)
        "origin": [0.5, 0.5],

        // split the spritesheet by evenly sized cells
        "split": [32, 32],

        // or split by frame count
        "split": 12,

        // or split by manual rects
        "split": {
            "sub_1": [0, 0, 24, 24],

            "sub_2": {
                // origin of child sprites may also be overriden
                "region": [24, 0, 24, 24],
                "origin": [9, 24]
            }
        },

        // optional animation definition
        "animation": {
            "fps": 12,

            // customizable keyframe events you can define handles for
            "events": [
                // format is: [frame_index, event_name, [args]]
                [0, "play_sound", "sfx/example"],

                // 'point' event is built-in and allows to define moving or static points on the sprite
                // an API to get them with transformations applied is present in all SpriteRenderers!
                [1, "point", [14, 16]]
            ]
        }
    },

    "another_sprite": {
        // copy all properties from "sprite_name"!
        "inherit": "sprite_name",

        // you can override some if you want
        "origin": [0.5, 0]
    }
}
```
