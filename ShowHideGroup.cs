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
        _group.alpha = stateOnAwake ? 1f : 0f;
    }

    public void On()
    {
        if (_isVisible) return;
        _isVisible = true;
        _group.interactable = true;
        _group.blocksRaycasts = true;
        Wrj.Utils.MapToCurve.Linear.FadeAlpha(_group, 1f, fadeTime);
    }
    public void Off()
    {
        if (!_isVisible) return;
        _isVisible = false;
        _group.interactable = false;
        _group.blocksRaycasts = false;
        Wrj.Utils.MapToCurve.Linear.FadeAlpha(_group, 0f, fadeTime);
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
