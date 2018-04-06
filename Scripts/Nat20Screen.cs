using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Nat20Screen : MonoBehaviour
{
    private AudioSource _audio;
    private Transform _natural;
    private Transform _20;

    private void Awake()
    {
        _natural = transform.FindChild("Natural");
        _20 = transform.FindChild("20");
        _audio = GetComponent<AudioSource>();
    }

    void OnEnable ()
	{
	    GameManager.Instance.InputEnabled = false;
	    Sequence seq = DOTween.Sequence();
	    seq.Append(_natural.DOScale(0, .5f).SetEase(Ease.OutBack).From());
        seq.Append(_20.DOScale(0, .5f).SetEase(Ease.OutBack).From());
	    seq.Play();

	    StartCoroutine(Hide());
	}

    private IEnumerator Hide()
    {
        yield return new WaitForSeconds(5f);
        GetComponentInChildren<ParticleSystem>().Stop();
        yield return new WaitForSeconds(2f);
        _audio.DOFade(0, .5f);
        Sequence seq = DOTween.Sequence();
        seq.Append(_natural.DOScale(0, .5f).SetEase(Ease.InBack));
        seq.Insert(.25f, _20.DOScale(0, .5f).SetEase(Ease.InBack));
        seq.Play();
        yield return new WaitForSeconds(.75f);
        _20.localScale = new Vector3(.6f,.6f,.6f);
        _natural.localScale = new Vector3(.6f, .6f, .6f);
        _audio.volume = 1;
        gameObject.SetActive(false);
        GameManager.Instance.InputEnabled = true;
    }
}
