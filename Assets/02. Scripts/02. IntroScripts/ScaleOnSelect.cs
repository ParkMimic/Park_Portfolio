using UnityEngine;
using UnityEngine.EventSystems;

public class ScaleOnSelect : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 targetScale;

    void Awake()
    {
        targetScale = Vector3.one * normalScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        targetScale = Vector3.one * selectedScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetScale = Vector3.one * normalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);
    }
}
