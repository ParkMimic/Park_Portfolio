using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public GameObject[] tutorialSteps;
    private void Awake()
    {        
        foreach (var step in tutorialSteps)
        {
            if (step == tutorialSteps[0]) step.SetActive(true);
            else step.SetActive(false);
        }
    }
}
