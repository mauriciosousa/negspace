using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiConsole : MonoBehaviour {

    public int lineHeight;
    private List<string> _lines;
    private int _numberOfLines = 0;

    public bool Active = false;

    void Start () {
        _lines = new List<string>();

	}
    
	void Update () {
        _numberOfLines = Screen.height / lineHeight;

        if (Input.GetKeyDown(KeyCode.D))
            Active = !Active;
    }

    public void write(string line)
    {
        _lines.Add(line);
        if (_lines.Count == _numberOfLines) _lines.RemoveAt(0);
    }

    void OnGUI()
    {
        if (Active)
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            int left = 10;
            int top = 10;

            foreach (string line in _lines)
            {
                GUI.Label(new Rect(left, top, Screen.width, lineHeight), ">>> " + line);
                top += lineHeight;
            }
        }
    }
}
