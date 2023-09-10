using UnityEngine;
using DG.Tweening;

public class ScaleIcon : MonoBehaviour
{
    public float animDuration = 1;
    public float startScale = 1;
    public float targetScale = 1.5f;
    Vector3 startScaleVec;
    Vector3 targetScaleVec;
    Tween tween;
    bool isTarget = true;

    void Start()
    {
        startScaleVec = transform.localScale;
        tween = transform.DOScale(startScaleVec * (isTarget ? startScale : targetScale), animDuration);
        isTarget = isTarget ? false : true;
    }

    void Update()
    {
        if (!tween.IsPlaying())
		{
            tween = transform.DOScale(startScaleVec * (isTarget ? startScale : targetScale), animDuration);
            isTarget = isTarget ? false : true;
        }
    }
}
