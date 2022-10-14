using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetTextFromSlider : MonoBehaviour
{
    public TMP_Text text;
    public Slider slider;
    public bool showPercentage;
    public bool showFloats;

    private void Start()
    {
        text.text = showPercentage ? (((slider.value - slider.minValue) * 100) / (slider.maxValue - slider.minValue)).ToString(showFloats ? "0.00" : "0") + "%" : slider.value.ToString(showFloats ? "0.00" : "0");
    }

    public void SetSliderValue(Slider _slider)
    {
        text.text = showPercentage ? (((_slider.value - _slider.minValue) * 100) / (_slider.maxValue - _slider.minValue)).ToString(showFloats ? "0.00" : "0") + "%" : _slider.value.ToString(showFloats ? "0.00" : "0");
    }   
}
