using UnityEngine;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using DG.Tweening;
using TMPro;

public class Dice : MonoBehaviour
{
    public Vector3 TestDirection;
    public bool InputEnabled;
    public DiceStates DiceState = DiceStates.BUTTON;
    
    public enum DiceStates
    {
        BUTTON,
        SPAWNED,
        ROLLED,
        FROZEN,
        SELECTED
    }

    private Rigidbody _rigidbody;
    private string _finalValue;
    private bool _isCrit = false;

	void Start ()
	{
	    _rigidbody = GetComponent<Rigidbody>();
	}
	
	void Update ()
	{
	    if (_rigidbody.isKinematic)
	        return;
	    
        switch (DiceState)
        {

            case DiceStates.ROLLED:
                if (_rigidbody.IsSleeping())
                {
                    enabled = false;
                    InputEnabled = true;
                    DiceState = DiceStates.FROZEN;
                    GameManager.Instance.DiceFrozen();
                    ChooseNumber();
                }
                break;
        }
    }

    private void OnMouseOver()
    {
        if (!InputEnabled || !GameManager.Instance.InputEnabled)
            return;
        switch (DiceState)
        {
            case DiceStates.FROZEN:
                GameManager.Instance.DragDice(transform);
                break;
        }
    }

    private void OnMouseUpAsButton()
    {
        if (!InputEnabled || !GameManager.Instance.InputEnabled)
            return;
        switch (DiceState)
        {
            case DiceStates.BUTTON:
                break;
            case DiceStates.SPAWNED:
                GameManager.Instance.RemoveDice(gameObject);
                break;
            case DiceStates.FROZEN:
                ResolveDice();
                break;
        }
    }

    private void OnMouseDown()
    {
        if (!InputEnabled || !GameManager.Instance.InputEnabled)
            return;
        switch (DiceState)
        {
            case DiceStates.BUTTON:
                if (GameManager.Instance.AddDice(gameObject))
                {
                    transform.DOKill(true);
                    transform.DOPunchScale(new Vector3(.2f, .2f, 0), .5f);
                }
                break;
            case DiceStates.SPAWNED:
                break;
            case DiceStates.ROLLED:
                break;
        }
    }

    public int GetValue()
    {
        return int.Parse(_finalValue);
    }

    public void ResolveDice()
    {
        GameObject parts = ((GameObject)Instantiate(_isCrit? GameManager.Instance.DiceDespawnCritParticle : GameManager.Instance.DiceDespawnParticle, transform.position,
                            Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)))));
        Transform text = Instantiate(GameManager.Instance.ValuePrefab).transform;
        text.position = transform.position;
        TextMeshPro textMesh = text.GetComponent<TextMeshPro>();
        textMesh.text = _finalValue;
        if (gameObject.name.Contains("D20"))
        {
            switch (_finalValue)
            {
                case "20":
                    foreach (var part in parts.GetComponentsInChildren<ParticleSystem>())
                    {
                        part.startColor = Color.green;
                    }
                    textMesh.fontMaterial.SetColor("_UnderlayColor",Color.green);
                    break;
                case "1":
                    foreach (var part in parts.GetComponentsInChildren<ParticleSystem>())
                    {
                        part.startColor = Color.red;
                    }
                    textMesh.fontMaterial.SetColor("_UnderlayColor", Color.red);
                    break;
            }
        }

       
        Sequence seq = DOTween.Sequence();
        seq.Append(text.DOScale(0, .25f).SetEase(Ease.OutBack).From());
        seq.Append(text.DOBlendableLocalMoveBy(Vector3.up * 6, 2f).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(text.gameObject));
        seq.Play();
        transform.parent = null;
        Destroy(gameObject);
        GameManager.Instance.DiceResolved(transform);
    }

    private void ChooseNumber()
    {
        _finalValue =
            transform.GetComponentsInChildren<Transform>()
                .Where(t => t.parent == transform.FindChild("Numbers"))
                .OrderBy(t => Vector3.Angle(TestDirection, t.up))
                .First()
                .name;
        Debug.Log(">> " + gameObject.name + " landed on: " + _finalValue);
        Material mat = GetComponent<Renderer>().material;

        
        _rigidbody.isKinematic = true;
        bool handled = false;
        
        if (gameObject.name.Contains("D20"))
        {
            switch (_finalValue)
            {
                case "20":
                    _isCrit = true;
                    /*GameManager.Instance.DoSpecialScreen("Nat20");
                    mat.DOColor(Color.green, .5f)
                        .SetEase(Ease.InOutQuad);
                    handled = true;*/
                    break;
                case "1":
                    _isCrit = true;
                    /*GameManager.Instance.DoSpecialScreen("CritFail");
                    mat.DOColor(Color.red, .5f)
                        .SetEase(Ease.InOutQuad);
                    handled = true;*/
                    break;
            }
        }
        if (!handled)
        {
            mat.DOColor(new Color(.3f,.3f,.3f), .5f)
               .SetEase(Ease.InOutQuad);
        }
    }
}
