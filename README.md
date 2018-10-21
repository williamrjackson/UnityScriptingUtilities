# UnityScriptingUtilities
![PathTrail](Smile.gif)   
Stuff I use a lot, and a simple AnimationCurve-based tweening interface. 


## MapToCurve
Provides values as plotted on an Animation Curve. Used a lot like Mathf.Lerp, except not linear. 

Includes a bunch of functions to manipulate transforms/audio over time. For example, the following will scale, move and rotate a transfrom into a position over 5 seconds. The target position is defined by a sibling transfrom.
`Wrj.Utils.MapToCurve.Ease.MatchSibling(transform, targetTransform, 5);`

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
![Tweening](TweenExample.gif)    

## WeightedGameObjects
Also includes a _Weighted Random_ GameObject class (demonstrated on the right in the gif above).

Each GameObject in an array gets an int representing its weight. Higher weights are more likely for selection When `WeightedGameObjects.GetRandom()` is called.

## Utility Functions
- `EnsureComponent<T>(GameObject)`
  - Returns a component of Type T by finding the one on the game object, or by adding one if not found.
- `Switcheroo<T>(ref T a, ref T b)`
  - Swap items
- `SetLayerRecursive(GameObject, DesiredLayerName)`
  - Set the layer of a transform and all of its children by name.
- `GetPositiveAngle(float OR Vector3)`
  - Ensure an angle in degrees is within 0 - 360
- `Remap(value, sourceMin, sourceMax, destMin, destMax)`
  - Linear remaps a value from a source range to a desired range
- `QuadraticBezierCurve(origin, influence, destination, pointCount)`
  - Get an array of points representing a quadrartic bezier curve. 
- `CubicBezierCurve(origin, influenceA, influnceB, destination, pointCount)`
  - Get an array of points representing a cubic bezier curve. 
- `FromFeet(feet)`
  - Convert feet into Unity Units. Becasue I'm a dumb American that can only visualise space in US standard Units.
- `FromInches(feet)`
  - Convert inches into Unity Units.    

## Bezier Path Editor 
Editor scripts to create paths. `MapToCurve` includs a tween to follow paths over a duration using a speed curve.

![Path](PathFollowerExample.gif)    
