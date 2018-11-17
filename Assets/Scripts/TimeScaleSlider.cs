using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

public class TimeScaleSlider : MonoBehaviour
{
	private Slider slider;
	private Text text;

	private void Awake()
	{
		slider = GetComponent<Slider>();
		text = GetComponentInChildren<Text>();
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		Time.timeScale = slider.value/100;
		text.text = "TimeScale x" + Time.timeScale;
	}
}
