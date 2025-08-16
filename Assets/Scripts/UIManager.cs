using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public Trajectory_Manager trajectoryManager;
    public GameObject slider;
    public GameObject frameNumber;
    public GameObject animationButton;
    public bool isAnimationPlaying = false;
    public GameObject videoPanel;
    private int videoIndex = 3;

    public void SelectFrame()
    {
        trajectoryManager.UpdateJointsFromTrajectory((int)slider.GetComponent<UnityEngine.UI.Slider>().value);
    }

    public void UpdateFrameNumber()
    {
        frameNumber.GetComponent<TextMeshProUGUI>().text = "Frame: " + slider.GetComponent<UnityEngine.UI.Slider>().value;
    }

    public void StartAnimation()
    {
        if (!isAnimationPlaying)
        {
            Debug.Log("UIManager: Starting animation.");
            trajectoryManager.DisplayTrajectory();
            animationButton.GetComponent<TextMeshProUGUI>().text = "Stop animation";
            isAnimationPlaying = true;
        }
        else
        {
            Debug.Log("UIManager: Stopping animation.");
            trajectoryManager.stopAnimation = true;
            animationButton.GetComponent<TextMeshProUGUI>().text = "Start animation";
            isAnimationPlaying = false;
        }

    }

    public void UpdateSlider(int frame)
    {
        if (trajectoryManager != null && trajectoryManager.joints.Count > 0)
        {
            slider.GetComponent<UnityEngine.UI.Slider>().value = frame;
            UpdateFrameNumber();
        }
    }

    public void OpenMenu()
    {
        // Open the main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void Start()
    {
        // Load the video index from PlayerPrefs
        if (PlayerPrefs.HasKey("VideoIndex"))
        {
            videoIndex = PlayerPrefs.GetInt("VideoIndex", 0);
        }
        videoPanel.GetComponent<TextMeshProUGUI>().text = "Current Video: " + videoIndex;        
    }
}