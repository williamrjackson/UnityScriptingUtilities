using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ShowHideGroup : MonoBehaviour
{
    [SerializeField]
    private float fadeTime = 1f;
    [SerializeField]
    private bool stateOnAwake;
    private CanvasGroup _group;
    private bool _isVisible;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _isVisible = stateOnAwake;
        _group.alpha = (stateOnAwake) ? 1f : 0f;
    }

    private void Start()
    {
        Off();
    }
    public void On()
    {
        if (_isVisible) return;
        _isVisible = true;
        Wrj.Utils.MapToCurve.Linear.ManipulateFloat(f =>  _group.alpha = f, _group.alpha, 1f, fadeTime);
    }
    public void Off()
    {
        if (!_isVisible) return;
        _isVisible = false;
        Wrj.Utils.MapToCurve.Linear.ManipulateFloat(f => _group.alpha = f, _group.alpha, 0f, fadeTime);
    }
    public void Toggle()
    {
        if (_isVisible)
        {
            Off();
        }
        else
        {
            On();
        }
    }
}
