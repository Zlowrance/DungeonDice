using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public bool InputEnabled;
	public Transform DiceParent;
	public Transform SpawnedDice;
	public Transform CurrentDiceSpot;
	public GameObject DiceDespawnParticle;
	public GameObject DiceDespawnCritParticle;
	public GameObject ValuePrefab;
	public Transform SpecialScreens;
	public Transform RightArrow;
	public Transform LeftArrow;
	public Transform RollButton;
	public Transform DiceControls;
	public Transform ClearButton;
	public Transform Lines;
	public ParticleSystem SelectedParticle;
	public Material SelectedMat;

	public Transform[] DiceArray;

	private int _diceIndex;
	private Sequence _diceChangeSequence;
	private bool _dragActive = false;
	private List<Transform> _dragDice = new List<Transform>();
	private List<int> _currentCombo = new List<int>();
	private float _lastComboCheck = 0;

	private static readonly int[] cJohnCenaCombo = {1,1,1,-1,-1,-1,1,-1,1,-1,1,-1};
	private const float cComboInterval = .5f;

	void Start()
	{
		Instance = this;
		InputEnabled = true;
		ClearButton.GetComponent<Button>().InputEnabled = false;
		_diceIndex = 0;
		DiceArray[0].position = CurrentDiceSpot.position;
		DiceArray[0].parent = CurrentDiceSpot;
		CurrentDiceSpot.DOBlendableLocalRotateBy(new Vector3(15f, 10f, 0), 1f)
			 .SetEase(Ease.Linear)
			 .SetLoops(-1, LoopType.Incremental);
		for (int i = 1; i < DiceArray.Length; i++)
		{
			Transform curr = DiceArray[i];
			curr.position = CurrentDiceSpot.position;
			curr.localScale = Vector3.zero;
		}
		for (int i = 0; i < SpecialScreens.childCount; i++)
		{
			SpecialScreens.GetChild(i).gameObject.SetActive(false);
		}
		ButtonPress("None", gameObject);
		StartCoroutine(DoLightFlicker());
	}

	public void ButtonPress(string command, GameObject go)
	{
		if (!InputEnabled)
			return;
		go.transform.DOKill(true);
		switch (command)
		{
			case "RightArrow":
				ChangeDice(1);
				go.transform.DOPunchPosition(new Vector3(.1f, 0, 0), .5f);
				break;
			case "LeftArrow":
				ChangeDice(-1);
				go.transform.DOPunchPosition(new Vector3(-.1f, 0, 0), .5f);
				break;
			case "Drop":
				for (int i = 0; i < SpawnedDice.childCount; i++)
				{
					Transform curr = SpawnedDice.GetChild(i);
					Rigidbody r = curr.GetComponent<Rigidbody>();
					r.constraints = RigidbodyConstraints.None;
					r.useGravity = true;
					Vector3 posToUse = curr.localPosition.magnitude < 1 ? new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), Random.Range(-3, 3)) : curr.localPosition;
					Vector3 torque = posToUse * 10;
					r.maxAngularVelocity = Mathf.Infinity;
					r.AddRelativeTorque(torque);
					r.AddForce(posToUse * 500);
					curr.GetComponent<Dice>().DiceState = Dice.DiceStates.ROLLED;
				}
				RollButton.DOLocalMoveX(-2.4f, .5f).SetEase(Ease.InBack);
				DiceControls.DOMoveZ(-3.2f, .5f).SetEase(Ease.InBack);
				InputEnabled = false;
				break;
			case "Clear":
				StartCoroutine(ResolveDice());
				go.transform.DOPunchScale(new Vector3(.1f, .1f, 0), .5f);
				break;
		}
		//EnableButton(LeftArrow, _diceIndex > 0);
		//EnableButton(RightArrow, _diceIndex < DiceArray.Length - 1);
	}

	public void DragDice(Transform which)
	{
		if (_dragDice.Contains(which))
			return;
		bool mouseDown = true;
#if UNITY_EDITOR
		mouseDown = Input.GetMouseButton(0);
#endif
		if (mouseDown)
		{
			_dragDice.Add(which);
			Transform part = Instantiate(SelectedParticle).transform;
			part.parent = which;
			part.localPosition = Vector3.zero;
			which.GetComponent<Dice>().DiceState = Dice.DiceStates.SELECTED;
			_dragActive = true;
		}
	}

	private void Update()
	{
		if (_dragActive)
		{
#if UNITY_EDITOR
			_dragActive = Input.GetMouseButton(0);
#elif UNITY_ANDROID
            _dragActive = Input.touchCount > 0;
#endif
			Debug.Log(">>Drag active = " + _dragActive);
			if (!_dragActive)
			{
				if (_dragDice.Count > 1)
					StartCoroutine(DragFinished());
				else
				{
					_dragDice[0].GetComponent<Dice>().ResolveDice();
					_dragDice.Clear();
				}
			}
		}
		LineRenderer line = Lines.GetComponent<LineRenderer>();
		if (_dragDice.Count > 0)
		{
			line.SetVertexCount(_dragDice.Count + 1);
			for (int i = 0; i < _dragDice.Count; i++)
			{
				if (_dragDice[i] != null)
					line.SetPosition(i, _dragDice[i].position);
			}
			if (_dragActive)
			{
				line.SetPosition(_dragDice.Count, Camera.main.ScreenToWorldPoint(Input.mousePosition +
																	  new Vector3(0, 0,
																			Camera.main.transform.position.y - SpawnedDice.position.y)));
			}
			else
			{
				if (_dragDice.Count > 2)
					line.SetPosition(_dragDice.Count, _dragDice[0].position);
				else
				{
					line.SetVertexCount(_dragDice.Count);
				}
			}

		}
		else
		{
			line.SetVertexCount(0);
		}
	}

	private IEnumerator DragFinished()
	{
		Debug.Log("Drag finished!!");
		InputEnabled = false;
		_dragActive = false;

		int numChillens = _dragDice.Count;
		int finalValue = 0;
		float angleSeparation = 360f / numChillens;
		for (int i = 0; i < numChillens; i++)
		{
			Transform currDice = _dragDice[i];
			Vector3 pos = currDice.localPosition;
			currDice.localPosition = new Vector3(0, 1, -1.25f);
			currDice.RotateAround(SpawnedDice.position, Vector3.up, angleSeparation * i);
			currDice.DOLocalMove(pos, .5f).SetEase(Ease.OutQuad).From();
			if (currDice.localScale.x > 1)
				currDice.DOScale(1, .5f);
			currDice.GetComponent<MeshCollider>().enabled = false;
			finalValue += currDice.GetComponent<Dice>().GetValue();
			currDice.DOLocalRotate(new Vector3(1080, 2100, 0), 1.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InCirc);
			currDice.GetComponent<Renderer>().material.DOColor(new Color(1, .3f, 0), 1f).SetEase(Ease.InCirc);
			yield return new WaitForSeconds(.1f);
		}
		yield return new WaitForSeconds(.5f);
		Vector3 finalPos = SpawnedDice.position + new Vector3(0, 1, 0);
		foreach (var dragDie in _dragDice)
		{
			dragDie.DOMove(finalPos, .5f).SetEase(Ease.InBack);
		}
		yield return new WaitForSeconds(.5f);
		for (int i = _dragDice.Count - 1; i > -1; i--)
		{
			Transform curr = _dragDice[i];
			curr.parent = null;
			DiceResolved(curr);
			Destroy(curr.gameObject);
		}
		Instantiate(DiceDespawnCritParticle, finalPos, Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360))));
		Transform text = Instantiate(ValuePrefab).transform;
		text.position = finalPos;
		TextMeshPro textMesh = text.GetComponent<TextMeshPro>();
		textMesh.text = "" + finalValue;
		Sequence seq = DOTween.Sequence();
		seq.Append(text.DOScale(0, .25f).SetEase(Ease.OutBack).From());
		seq.Append(text.DOBlendableLocalMoveBy(Vector3.up * 6, 2f).SetEase(Ease.InQuad));
		seq.OnComplete(() => Destroy(text.gameObject));
		seq.Play();
		LineRenderer line = Lines.GetComponent<LineRenderer>();
		line.SetVertexCount(0);
		yield return new WaitForSeconds(.5f);
		InputEnabled = true;
	}

	private IEnumerator ResolveDice()
	{
		InputEnabled = false;
		foreach (var dice in SpawnedDice.GetComponentsInChildren<Dice>())
		{
			dice.ResolveDice();
			yield return new WaitForSeconds(.2f);
		}
	}

	public void DoSpecialScreen(string which)
	{
		InputEnabled = false;
		Transform trans = SpecialScreens.FindChild(which);
		trans.gameObject.SetActive(true);
	}

	public bool AddDice(GameObject dice)
	{
		if (SpawnedDice.childCount >= 6)
			return false;
		if (SpawnedDice.childCount == 0)
			RollButton.DOLocalMoveX(-1.2f, .5f).SetEase(Ease.OutBounce);
		Transform curr = Instantiate(dice).transform;
		curr.position = dice.transform.position;
		curr.parent = SpawnedDice;
		curr.GetComponent<Dice>().DiceState = Dice.DiceStates.SPAWNED;
		Rigidbody r = curr.GetComponent<Rigidbody>();
		r.isKinematic = false;
		/*r.AddRelativeTorque(new Vector3(Random.Range(0, 2) == 0 ? -1 : 1 * Random.Range(.5f,2), 
												  Random.Range(0, 2) == 0 ? -1 : 1 * Random.Range(.5f, 2), 
												  Random.Range(0, 2) == 0 ? -1 : 1 * Random.Range(.5f, 2)));*/
		ReflowDice();
		return true;
	}

	public void RemoveDice(GameObject dice)
	{
		Instantiate(DiceDespawnParticle, dice.transform.position,
				Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360))));
		dice.transform.parent = null;
		Destroy(dice);
		ReflowDice();
		if (SpawnedDice.childCount == 0)
		{
			Button button = RollButton.GetComponent<Button>();
			button.InputEnabled = false;
			RollButton.DOLocalMoveX(-2.4f, .5f).SetEase(Ease.InBack).OnComplete(() => button.InputEnabled = true);
		}
	}

	public void DiceFrozen()
	{
		if (SpawnedDice.GetComponentsInChildren<Dice>().All(d => d.DiceState == Dice.DiceStates.FROZEN))
		{
			ClearButton.DOMoveZ(-1.65f, .5f).SetEase(Ease.OutBack);
			ClearButton.GetComponent<Button>().InputEnabled = true;
			InputEnabled = true;
		}
	}

	public void DiceResolved(Transform dice)
	{
		_dragDice.Remove(dice);
		if (SpawnedDice.childCount == 0)
		{
			Sequence seq = DOTween.Sequence();
			seq.Append(ClearButton.DOMoveZ(-2.5f, .5f).SetEase(Ease.InBack));
			seq.Append(DiceControls.DOMoveZ(-1.66f, .5f).SetEase(Ease.OutBack));
			seq.OnComplete(() =>
			{
				InputEnabled = true;
				ClearButton.GetComponent<Button>().InputEnabled = false;
			});
			seq.Play();
		}
	}

	public void ReflowDice()
	{
		int numChillens = SpawnedDice.childCount;
		switch (numChillens)
		{
			case 1:
				Transform curr = SpawnedDice.GetChild(0);
				curr.DOLocalMove(Vector3.zero, .5f).SetEase(Ease.OutBack);
				curr.DOScale(1.5f, .5f);
				break;
			default:        //should be used for > 1
				float angleSeparation = 360f / numChillens;
				int angleIndex = 0;
				for (int i = numChillens - 1; i > -1; i--)
				{
					Transform currDice = SpawnedDice.GetChild(i);
					Vector3 pos = currDice.localPosition;
					currDice.localPosition = Quaternion.Euler(0, angleSeparation * angleIndex, 0) * (Vector3.back * 1.25f);
					currDice.DOLocalMove(pos, .5f).SetEase(Ease.OutBack).From();
					angleIndex++;
					if (currDice.localScale.x > 1)
						currDice.DOScale(1, .5f);
				}
				break;
		}
	}

	private IEnumerator DoLightFlicker()
	{
		Transform bgLight = GameObject.Find("BGLight").transform;
		while (true)
		{
			yield return
				 bgLight.DOScaleY(Random.Range(-1.5f, 1.5f), Random.Range(.02f, .1f))
					  .SetEase(Ease.Linear)
					  .SetLoops(2, LoopType.Yoyo)
					  .SetRelative()
					  .WaitForCompletion();
		}
	}

	private void EnableButton(Transform button, bool enable)
	{
		button.GetComponent<SpriteRenderer>().DOKill(true);
		button.GetComponent<Button>().InputEnabled = enable;
		button.GetComponent<SpriteRenderer>().DOColor(enable ? Color.white : Color.gray, .25f);
	}

	private void ChangeDice(int dir)
	{
		int newIndex = _diceIndex + dir;
		if (newIndex < 0)
			newIndex = DiceArray.Length - 1;
		if (newIndex >= DiceArray.Length)
			newIndex = 0;
		Transform curr = DiceArray[newIndex];
		Transform last = DiceArray[_diceIndex];
		curr.DOKill(true);
		last.DOKill(true);
		last.parent = DiceParent;
		curr.parent = CurrentDiceSpot;
		if (_diceChangeSequence != null)
			_diceChangeSequence.Kill(true);
		_diceChangeSequence = DOTween.Sequence();
		_diceChangeSequence.Append(last.DOScale(0, .25f).SetEase(Ease.InBack));
		_diceChangeSequence.Append(curr.DOScale(1, .25f).SetEase(Ease.OutBack));

		_diceChangeSequence.Play();
		_diceIndex = newIndex;

		//for our nice bug reported ;)
		AddAndCheckCombo(dir);
	}

	private void AddAndCheckCombo(int dir)
	{
		if (Time.time - _lastComboCheck > cComboInterval)
			ClearCombo();
      _currentCombo.Add(dir);
		_lastComboCheck = Time.time;

		if (_currentCombo.SequenceEqual(cJohnCenaCombo))
		{
			//the bastard really did it!
			DoSpecialScreen("Nat20");
			ClearCombo();
		}
		else
		{
			//w/e we dont care
		}
	}

	private void ClearCombo()
	{
		_currentCombo.Clear();
	}

	public void OnApplicationPause(bool isPaused)
	{
		if (!isPaused)
		{
			InputEnabled = true;
		}
	}
}
