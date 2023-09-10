using UnityEngine;
using DG.Tweening;

public class MoveIcon : MonoBehaviour
{
    public float animDuration = 1;
    public Vector3 targetVec;
    Vector3 startVec;
    Tween tween;
    bool isTarget = true;

    void Start()
    {
        startVec = transform.position;
        tween = transform.DOMove(isTarget ? startVec : startVec + targetVec, animDuration);
        isTarget = isTarget ? false : true;
    }

    void Update()
    {
        if (!tween.IsPlaying())
        {
            tween = transform.DOMove(isTarget ? startVec : startVec + targetVec, animDuration);
            isTarget = isTarget ? false : true;
        }
    }
}
