using UnityEngine;
using System.Collections;

public class Button : MonoBehaviour
{
    public string Command;
    public bool InputEnabled = true;

    void OnMouseDown()
    {
        if (!InputEnabled)
            return;
        GameManager.Instance.ButtonPress(Command,gameObject);
    }
}
