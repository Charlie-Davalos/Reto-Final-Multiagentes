using UnityEngine;
using TMPro;

//contador de steps
public class StepCounterTMP : MonoBehaviour
{
    public TextMeshProUGUI stepText;
    private int stepCount;

    void Start()
    {
        stepCount = 0;
        UpdateStepText();
    }

    public void IncrementStep()
    {
        stepCount++;
        UpdateStepText();
    }

    void UpdateStepText()
    {
        stepText.text = "Steps: " + stepCount.ToString();
    }
}
