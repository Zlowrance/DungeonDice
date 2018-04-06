using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CritFailScreen : MonoBehaviour
{
    private AudioSource _audio;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        GameManager.Instance.InputEnabled = false;
        StartCoroutine(Hide());
    }

    private IEnumerator Hide()
    {
        yield return new WaitForSeconds(5f);
        _audio.DOFade(0, .5f);
       
        yield return new WaitForSeconds(.75f);
        _audio.volume = 1;
        gameObject.SetActive(false);
        GameManager.Instance.InputEnabled = true;
    }
}