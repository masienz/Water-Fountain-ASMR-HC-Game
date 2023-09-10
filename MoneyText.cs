using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoneyText : MonoBehaviour
{
    public float colorDur = 0.3f;
	public Ease colorEase = Ease.OutBack;

    [Space]
	public float moveDuration = 0.6f;
    public float moveY = 3;
    public Ease moveEase = Ease.OutBack;

	void Start()
    {
        transform.DOMoveY(transform.position.y + moveY, moveDuration).SetEase(moveEase);
        Invoke("Color", moveDuration);
		Destroy(gameObject, moveDuration + colorDur);
    }

    private void Color()
    {
		Color has = GetComponent<TextMeshPro>().color;
		GetComponent<TextMeshPro>().DOColor(new Color(has.r, has.g, has.b, 0), colorDur).SetEase(colorEase);
	}
}
