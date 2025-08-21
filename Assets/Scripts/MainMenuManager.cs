using UnityEngine;
using TMPro;
using System.Collections;
using System;
using System.Diagnostics;
using System.Security;
using System.ComponentModel;
using System.IO;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public GameObject CSVButton;
    public GameObject waitScreen;

    // Dictionary from names in dropdown to CSV files to load
    private System.Collections.Generic.Dictionary<string, string> CSVnames = new System.Collections.Generic.Dictionary<string, string>
    {
        { "Annotations video 1", "csv_traj_ann_1.csv"},
        { "Annotations video 2", "csv_traj_ann_2.csv"},
        { "Annotations video 3", "csv_traj_ann_3.csv"},
        { "YOLO full videos", "csv_traj_YOLO_full.csv"},
        { "YOLO finetuned full videos", "csv_traj_YOLO_ft_full.csv"}
    };

    public void OpenVisualizer()
    {
        // Get selecetd video
        string selectedVideo = CSVnames[dropdown.options[dropdown.value].text];
        DataHolder.videoIndex = selectedVideo;
        // Open the visualizer scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Visualizer");
    }
}