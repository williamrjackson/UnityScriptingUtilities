# UnityScriptingUtilities
Stuff I use a lot, and a simple AnimationCurve-based tweening interface. Provides values as plotted on an Animation Curve. Used a lot like Mathf.Lerp, except not linear. 

Includes a bunch of functions to manipulate transforms/audio over time. For example, the following will scale, move and rotate a transfrom into a position over 5 seconds. The target position is defined by a sibling transfrom. 

`WrjUtils.MapToCurve.Ease.MatchSibling(transform, targetTransform, 5);`

![Tweening](TweenExample.gif)    

Also includes a Random GameObject class and a Bezier Path Editor with a tween to follow paths.

![Path](PathFollowerExample.gif)    
