# Unity Scripting Utilities
![PathTrail](Smile.gif)    
Stuff I use a lot, and a simple AnimationCurve-based tweening interface.   

Add to projects using:  
`git submodule add https://github.com/williamrjackson/UnityScriptingUtilities.git Assets\ScriptingUtils`


## MapToCurve
Provides values as plotted on an Animation Curve. Used a lot like Mathf.Lerp, except not linear. 

Includes a bunch of functions to manipulate transforms/audio over time. For example, the following will scale, move and rotate a transfrom into a position over 5 seconds. The target position is defined by a sibling transfrom.
```C#
   Wrj.Utils.MapToCurve.Ease.MatchSibling(transform, targetTransform, 5);
//                       ^        ^          ^            ^           ^
//                     curve   function   affected   destination   duration
```
#### Manipulation Functions
```
Scale
Move (local space)
MoveWorld (world space)
MoveAlongPath
Rotate
FadeAudio
CrossfadeAudio (smoothly transitions between two audio sources)
FadeAlpha
ChangeColor
MatchSibling
```
#### Arguments & Common Modifiers
```
tForm          - Transform to affect
to             - Target destination/rotation/volume/etc.
duration       - Length of time in seconds for the manipulation to complete. 
                 Applies to a single loop/pingPong iteration
mirrorCurve    - Swap the in/out curve shapes. 
                 Get EaseOut by combining the EaseIn curve  with this flag
loop           - Repeat the manipulation this many times
pingPong       - Repeat the manipulation forward, then backward, this many times
mirrorPingPong - pingPong, but inverts the curve when pefroming tha backward manipulations
useTimeScale   - Set this to false to ignore time scaling
shortestPath   - In the case of manipulations that include rotation, this will cause minimal
                 possible rotation to get to the target. Without this, a rotation difference > 360 
                 will result in a full positive rotation before reaching the target.
onDone         - Specify a function to call when the operation completes
```
![Tweening](TweenExample.gif)    
## Test Bed Example
```C#
// Black Cube
Wrj.Utils.MapToCurve.Linear.Move(linTransform, linTransform.localPosition + Vector3.up * 5 + Vector3.right * -1.5f, duration, pingPong: 10);
Wrj.Utils.MapToCurve.Linear.Rotate(linTransform, Vector3.up * 135, duration, shortestPath: false, pingPong: 10);
Wrj.Utils.MapToCurve.Ease.ChangeColor(linTransform, Color.black, duration, pingPong: 10);

// Red Cube
Wrj.Utils.MapToCurve.EaseIn.Move(easeInTransform, easeInTransform.localPosition + Vector3.up * 5 + Vector3.right * .5f, duration, mirrorCurve: false, pingPong: 10);
Wrj.Utils.MapToCurve.EaseIn.Rotate(easeInTransform, Vector3.up * -360, duration, shortestPath: false, pingPong: 10);
Wrj.Utils.MapToCurve.Ease.ChangeColor(easeInTransform, Color.red, duration, pingPong: 10);

// Blue Cube
Wrj.Utils.MapToCurve.EaseIn.Move(easeOutTransform, easeOutTransform.localPosition + Vector3.up * 5 + Vector3.right * -.5f, duration, mirrorCurve: true, pingPong: 10);
Wrj.Utils.MapToCurve.EaseIn.Rotate(easeOutTransform, Vector3.forward * -720, duration, shortestPath: false, mirrorPingPong: 10);
Wrj.Utils.MapToCurve.Ease.ChangeColor(easeOutTransform, Color.blue, duration, pingPong: 10);

// Purple Cube
Wrj.Utils.MapToCurve.Ease.MatchSibling(easeTransform, targetTransform, duration, pingPong: 10);
Wrj.Utils.MapToCurve.Ease.ChangeColor(easeTransform, Color.magenta, duration, pingPong: 10);

// Purple Cube's Target
Wrj.Utils.MapToCurve.Ease.FadeAlpha(targetTransform, 0, duration, pingPong: 10);
```
## WeightedGameObjects
Also includes a Weighted Random GameObject class (demonstrated on the right in the gif above).

Each GameObject in an array gets an int representing its weight. Higher weights are more likely for selection When `WeightedGameObjects.GetRandom()` is called.

## Utility Functions
- `EnsureComponent<T>(GameObject)`
  - Returns a component instance by finding the one on the game object, or by adding one if not found.
- `Switcheroo<T>(ref T a, ref T b)`
  - Swap items
- `SetLayerRecursive(GameObject, DesiredLayerName)`
  - Set the layer of a transform and all of its children by name.
- `GetPositiveAngle(float OR Vector3)`
  - Ensure an angle in degrees is within 0 - 360
- `Remap(value, sourceMin, sourceMax, destMin, destMax)`
  - Linear remaps a value from a source range to a desired range
- `QuadraticBezierCurve(origin, influence, destination, pointCount)`
  - Get an array of points representing a quadratic bezier curve. 
- `CubicBezierCurve(origin, influenceA, influnceB, destination, pointCount)`
  - Get an array of points representing a cubic bezier curve. 
- `FromFeet(feet)`
  - Convert feet into Unity Units. Because I'm a silly American that can only visualise space in US standard units.
- `FromInches(inches)`
  - Convert inches into Unity Units.    

## Bezier Path Editor 
Editor scripts to create paths. `MapToCurve` includs a tween to follow paths over a duration using a speed curve.

![Path](PathFollowerExample.gif)    
