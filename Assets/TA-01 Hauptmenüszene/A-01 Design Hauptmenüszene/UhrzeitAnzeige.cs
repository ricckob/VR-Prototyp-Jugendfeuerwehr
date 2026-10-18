using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class UhrzeitAnzeige : MonoBehaviour
{
    private TMP_Text textfeld;

    void Start()
    {
        textfeld = GetComponent<TMP_Text>();
    }

    void Update()
    {
        textfeld.text = DateTime.Now.ToString("dddd dd.MM.yyyy\nHH:mm:ss", new CultureInfo("de-DE"));
    }
}