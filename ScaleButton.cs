using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleButton : MonoBehaviour
{
    public bool isOpen = false;
    public float openScaleTarget = 1;
	public float closeScaleTarget = 1;
	public float scaleDuration = 0.5f;

	private bool opened = true;
    private bool closed = false;


	private void Update()
    {
        if (isOpen && !opened)
        {
            transform.DOScale(openScaleTarget, scaleDuration);
			opened = true;
            closed = false;
		}
        if (!isOpen && !closed)
        {
			transform.DOScale(closeScaleTarget, scaleDuration);
			opened = false;
			closed = true;
		}
    }
}
