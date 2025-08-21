using UnityEngine;
/* using UnityEditor.Scripting.Python;
using UnityEditor;
using System.IO; */ // Not working
using TMPro;
using System.Collections;

// To run programs
using System;
using System.Diagnostics;
using System.Security;
using System.ComponentModel;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public GameObject CSVButton;
    public string pythonPath = "D:/Documenti/Universita/MS/Anno 2/Computer Vision/Progetto/CV_HPE";
    public string videoPath;
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

    // Coroutines
    /* IEnumerator PythonCSVCoroutine()
    {
        print("Starting " + Time.time);
        waitScreen.SetActive(true);

        string video = dropdown.options[dropdown.value].text.Replace("Video ", "");
        UnityEngine.Debug.Log($"Selected video: {video}");

        yield return new WaitForSeconds(1f);

        // Run the estimation script to compute the CSV
        string scriptPath = $"{pythonPath}/tracking_YOLO_eval.py";
        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" {video} {false} {false} {true}",
            WorkingDirectory = System.IO.Path.GetDirectoryName(scriptPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                UnityEngine.Debug.Log($"Python output:\n{output}");
                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.LogError($"Python error:\n{error}");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to launch Python: {ex.Message}");
        }
        waitScreen.SetActive(false);
        print("Done " + Time.time);
        // Save an int of the video index to PlayerPrefs
        PlayerPrefs.SetInt("VideoIndex", int.Parse(video));
        PlayerPrefs.Save();
    } */

    IEnumerator PythonStatsCoroutine()
    {
        print("Starting " + Time.time);
        waitScreen.SetActive(true);

        string video = dropdown.options[dropdown.value].text.Replace("Video ", "");
        UnityEngine.Debug.Log($"Selected video: {video}");

        yield return new WaitForSeconds(1f);

        // Run the estimation script to show stats
        string scriptPath = $"{pythonPath}/tracking_YOLO_eval.py";
        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" {video} {true} {true} {true}",
            WorkingDirectory = System.IO.Path.GetDirectoryName(scriptPath),
            UseShellExecute = true,
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                UnityEngine.Debug.Log($"Python output:\n{output}");
                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.LogError($"Python error:\n{error}");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to launch Python: {ex.Message}");
        }
        waitScreen.SetActive(false);
        print("Done " + Time.time);
        PlayerPrefs.SetInt("VideoIndex", int.Parse(video));
        PlayerPrefs.Save();
    }

    public void OpenVisualizer()
    {
        // Get selecetd video and choose correct CSV file
        string selectedVideo = CSVnames[dropdown.options[dropdown.value].text];
        // Save CSV to be used in PlayerPrefs
        PlayerPrefs.SetString("CSV", selectedVideo);
        PlayerPrefs.Save();
        // Open the visualizer scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Visualizer");
    }

/*     public void RunPthonEstimationCSV()
    {
        StartCoroutine(PythonCSVCoroutine());
    } */

    public void RunPthonEstimationPlots()
    {
        StartCoroutine(PythonStatsCoroutine());
    }
}