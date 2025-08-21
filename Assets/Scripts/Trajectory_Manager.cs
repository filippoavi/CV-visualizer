using System;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using NUnit.Framework;
using UnityEngine.UIElements;
using System.IO;

public class Trajectory_Manager : MonoBehaviour
{
    public string traj_csv_path = Application.dataPath + "/CSV/";

    // Trajectory data is stored as a list of arrays
    // First number in each line is the frame, second is the joint name, 3rd, 4th and 5th are the joint positions (z is up in data), last number is the visibility of the joint
    [SerializeField]
    public System.Collections.Generic.List<string[]> trajectoryData = new System.Collections.Generic.List<string[]>();
    public bool debug = true;
    public GameObject joints_root;
    [SerializeField]
    public System.Collections.Generic.List<GameObject> joints = new System.Collections.Generic.List<GameObject>(); // List of game objects to hold the joints of the person
    public float scale = 1000f; // Scale factor from data to Unity coordinates
    public float animationFramerate = 2f;
    public UIManager uiManager;
    public int jointNumber = 18;
    public bool stopAnimation = false;
    public GameObject personPrefab;
    private string videoIndex = "";
    private static int minFrame;
    private static int maxFrame;
    private Vector3 headP;
    private Vector3 spineVectorBase = new Vector3(0f, 0.60283422f, -0.00340802129f);
    // Dictionary of starting Euler angles of the joints
    private System.Collections.Generic.Dictionary<string, Vector3> startingRotations = new System.Collections.Generic.Dictionary<string, Vector3>
    {
        { "Hips", new Vector3(0f, 0f, 0f) },
        { "RAnkle", new Vector3(297.479553f, 354.272217f, 186.450226f) },
        { "RKnee", new Vector3(2.80536222f, 359.983734f, 179.671234f) },
        { "LAnkle", new Vector3(297.478546f, 5.70809603f, 173.571609f) },
        { "LKnee", new Vector3(2.80462027f, 0.0164177921f, 180.338806f) },
        { "RHand", new Vector3(90f, 89.9997406f, 0f) },
        { "RElbow", new Vector3(90f, 89.999733f, 0f) },
        { "RShoulder", new Vector3(90f, 89.9997406f, 0f) },
        { "LHand", new Vector3(90f, 269.999725f, 0f) },
        { "LElbow", new Vector3(90f, 269.999756f, 0f) },
        { "LShoulder", new Vector3(90f, 269.999725f, 0f) },
        { "LHip", new Vector3(0.72649169f, -0.00448312704f, 179.659653f) },
        { "RHip", new Vector3(0.725743175f, 0.00427764142f, 180.350372f) },
        { "Head", new Vector3(0f, 0f, 0f)}
    };
    private System.Collections.Generic.Dictionary<string, Vector3> originalLocalOffsets = new System.Collections.Generic.Dictionary<string, Vector3>
    {
        { "Spine0", new Vector3(-8.5978554e-06f, 0.09923462f, -0.01227335f) },
        { "Spine1", new Vector3(-6.9202592e-21f, 0.117319785f, -1.9984014e-17f) },
        { "Spine2", new Vector3(-1.9314919e-13f, 0.13458836f,  6.2616576e-15f) },
        { "Neck",   new Vector3(-2.5481228e-07f, 0.150277615f, 0.008779068f) }
    };

    // Coroutines
    IEnumerator TrajectoryCoroutine()
    {
        if (debug)
        {
            Debug.Log("Starting trajectory coroutine, trajectoryData.Count/jointNumber = " + trajectoryData.Count / jointNumber);
        }
        print("Starting " + Time.time);
        for (int i = 2; i < Mathf.FloorToInt((trajectoryData.Count-1) / jointNumber) + 2; i++)
        {
            UpdateJointsFromTrajectory(i);
            uiManager.UpdateSlider(i);
            yield return new WaitForSeconds(1f / animationFramerate);
            if (stopAnimation)
            {
                Debug.Log("Animation stopped.");
                stopAnimation = false;
                yield break;
            }
        }
        print("Done " + Time.time);
        uiManager.StartAnimation();
        stopAnimation = false;
    }
    IEnumerator UpdateHandRotationsCoroutine()
    {
        // Wait for a short time to ensure the joints are updated before setting hand rotations
        yield return new WaitForSeconds(0.05f);
        UpdateHandRotations();
        if (debug)
        {
            Debug.Log("Hand rotations updated.");
        }
    }
    IEnumerator ShowPerson(GameObject person)
    {
        yield return new WaitForSeconds(0.000001f);
        person.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        person.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
    }

    // Function for loading trajectory data from a CSV file
    private void LoadTrajectoryData(string csvPath)
    {
        // Read the CSV file to a list of arrays
        try
        {
            var lines = System.IO.File.ReadAllLines(csvPath);
            foreach (var line in lines)
            {
                // Split by comma and clean up values
                var values = line.Split(',');
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = values[i].Replace(" ", "").Replace("[", "").Replace("]", "");
                }
                trajectoryData.Add(values);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to read CSV file: {ex.Message}");
        }

        // Get min and max frame number
        int.TryParse(trajectoryData[1][0], out minFrame);
        int.TryParse(trajectoryData[trajectoryData.Count - 1][0], out maxFrame);

        if (debug)
        {
            Debug.Log($"Trajectory data loaded from {csvPath}. Total rows: {trajectoryData.Count}");
            Debug.Log("Sample data: " + (trajectoryData.Count > 2 ? string.Join(", ", trajectoryData[2]) : "No data"));
        }
    }

    // Function to load all joints from a given person GameObject, including the root
    private void LoadAllJoints(GameObject root)
    {
        if (root == null) return;
        joints.Add(root);
        foreach (Transform child in root.transform)
        {
            LoadAllJoints(child.gameObject);
        }
    }

    // Function to update joint positions based on trajectoryData for a given frame
    public void UpdateJointsFromTrajectory(int frame)
    {
        // Destroy and reinstantiate the person prefab (to reset the IK rig)
        DestroyPerson();
        InstantiatePerson();

        if (debug)
        {
            Debug.Log("Appliying joint position from frame: " + frame);
        }

        Vector3? rHipPos = null;
        Vector3? lHipPos = null;
        GameObject hips = null;

        foreach (var data in trajectoryData)
        {
            // Check if the frame matches
            if (int.TryParse(data[0], out int dataFrame) && dataFrame == frame)
            {
                string jointName = data[1];
                // Find all joints with the matching name
                var matchingJoints = joints.FindAll(j => j.name == jointName);
                foreach (var joint in matchingJoints)
                {
                    // Ensure '.' is used as the decimal separator
                    string xStr = data[2].Replace(",", ".");
                    string yStr = data[3].Replace(",", ".");
                    string zStr = data[4].Replace(",", ".");

                    // Parse z-up coordinates from data
                    if (float.TryParse(xStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(yStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                        float.TryParse(zStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                    {
                        if (jointName == "Hips") hips = joint;
                        // Ignore joint positions that are exactly (0, 0, 0)
                        if (!(x == 0f && y == 0f && z == 0f))
                        {
                            // Convert z-up (data) to y-up (Unity): (x, y, z) -> (x, z, y)
                            Vector3 unityPos = new Vector3(x / scale, z / scale, y / scale);
                            joint.transform.localPosition = unityPos;

                            // Store RHip and LHip positions
                            if (jointName == "RHip") rHipPos = unityPos;
                            if (jointName == "LHip") lHipPos = unityPos;
                            if (jointName == "Head") headP = unityPos;
                        }
                    }
                }
            }
        }

        // Set Hips position as the middle point of RHip and LHip
        if (rHipPos.HasValue && lHipPos.HasValue && hips != null)
        {
            hips.transform.localPosition = (rHipPos.Value + lHipPos.Value) / 2f;
        }

        // If the position of the head is Vector3(0,1.787,0) (or close to it by 0.1), set it as Vector3(0,0.787,0) above the hips
        GameObject head = joints.Find(j => j.name == "Head");
        if (head != null)
        {
            Vector3 headPos = head.transform.localPosition;
            if (Mathf.Abs(headPos.x) < 0.1f && Mathf.Abs(headPos.y - 1.787f) < 0.1f && Mathf.Abs(headPos.z) < 0.1f)
            {
                if (hips != null)
                {
                    Debug.Log("Setting head position to be above hips.");
                    head.transform.localPosition = new Vector3(0f, 0.787f, 0f) + hips.transform.localPosition;
                }
            }
        }

        // Set the position of the neck as the head position
        GameObject neck = joints.Find(j => j.name == "Neck");
        if (neck != null && head != null)
        {
            neck.transform.localPosition = head.transform.localPosition;
        }

        // Align the spine and neck according to the provided head and hips position
        AlignSpine();

        // Update joint rotations based on the new positions
        UpdateJointRotations();
        StartCoroutine(UpdateHandRotationsCoroutine());
    }

    // Function to align the spine between the hips and the head
    public void AlignSpineOld()
    {
        Transform hips = joints.Find(j => j.name == "Hips").transform;
        Transform spine0 = joints.Find(j => j.name == "Spine0").transform;
        Transform spine1 = joints.Find(j => j.name == "Spine1").transform;
        Transform spine2 = joints.Find(j => j.name == "Spine2").transform;
        Transform neck = joints.Find(j => j.name == "Neck").transform;
        Vector3 head = headP;

        Debug.Log("Hips: " + hips.position + " - Head: " + head);

        // Compute new spine vector and compare to normal
        Vector3 spineVectorPose = head - hips.position;
        float elongation = spineVectorPose.magnitude / spineVectorBase.magnitude;
        Vector3 difference = spineVectorPose - spineVectorBase;

        Debug.Log("spineVectorPose: " + spineVectorPose + " - difference: " + difference);

        // Rotate spine0 so it points towards head
        float locZ = head.z - (hips.position.z - 0.01f);
        float locY = head.y - (hips.position.y + 0.1f);
        float locC1 = Mathf.Sqrt(Mathf.Pow(locY, 2) + Mathf.Pow(locZ, 2));
        float locX = head.x - hips.position.x;
        float locC2 = Mathf.Sqrt(Mathf.Pow(locY, 2) + Mathf.Pow(locX, 2));
        //Debug.Log("Head position in spine0 RF x: " + head.x + " - " + hips.position.x + " - " + spine0.position.x);

        spine0.eulerAngles = new Vector3(Mathf.Acos((Mathf.Pow(locY, 2) + Mathf.Pow(locC1, 2) - Mathf.Pow(locZ, 2)) / (2 * locY * locC1)) * 2 * Mathf.PI, spine0.eulerAngles.y, Mathf.Acos((Mathf.Pow(locY, 2) + Mathf.Pow(locC2, 2) - Mathf.Pow(locX, 2)) / (2 * locY * locC2)) * 2 * Mathf.PI);

        Debug.Log("Spine0 rotations - x: " + spine0.eulerAngles.x + " - z: " + spine0.eulerAngles.z);
    }

    // Function to align the spine joints so they form a line from the hips to the head
    private void AlignSpine()
    {
        Transform hips = joints.Find(j => j.name == "Hips").transform;
        Transform spine0 = joints.Find(j => j.name == "Spine0").transform;
        Transform spine1 = joints.Find(j => j.name == "Spine1").transform;
        Transform spine2 = joints.Find(j => j.name == "Spine2").transform;
        Transform neck = joints.Find(j => j.name == "Neck").transform;
        Vector3 head = headP;
        Vector3 startPoint = hips.position;
        Vector3 endPoint = head;

        float[] originalDistances = { 0.099f, 0.216f, 0.351f, 0.499f };
        float originalTotalLength = 0.6f;
        float newLength = Vector3.Distance(startPoint, endPoint);

        // Direction vector from start to end
        Vector3 direction = (endPoint - startPoint).normalized;
        Vector3[] positions = new Vector3[originalDistances.Length];

        for (int i = 0; i < originalDistances.Length; i++)
        {
            // Scale original distance
            float scaledDistance = (originalDistances[i] / originalTotalLength) * newLength;

            // Compute new joint positions
            positions[i] = startPoint + direction * scaledDistance;
        }

        spine0.position = positions[0];
        spine1.position = positions[1];
        spine2.position = positions[2];
        neck.position = positions[3];
    }

    // Function to update the joint rotations based on their relative positions
    private void UpdateJointRotations()
    {
        // Find RHip and LHip joints
        GameObject rHip = joints.Find(j => j.name == "RHip");
        GameObject lHip = joints.Find(j => j.name == "LHip");

        if (rHip == null || lHip == null)
        {
            Debug.LogWarning("RHip or LHip joint not found.");
            return;
        }

        // Compute direction vector from LHip to RHip
        Vector3 hipDir = rHip.transform.localPosition - lHip.transform.localPosition;
        if (hipDir == Vector3.zero)
        {
            Debug.LogWarning("RHip and LHip positions are identical.");
            return;
        }

        // Compute overall rotation (Y axis) from hip direction
        float overallYRot = -Mathf.Atan2(hipDir.z, hipDir.x) * Mathf.Rad2Deg;

        // Create a rotation quaternion around the Y axis
        Quaternion overallRotation = Quaternion.Euler(0, overallYRot, 0);

        // List of joints to rotate
        string[] jointsToRotate = { "RAnkle", "LAnkle", "Hips", "Head" };

        foreach (string jointName in jointsToRotate)
        {
            GameObject joint = joints.Find(j => j.name == jointName);
            // Combine starting rotation and overall rotation
            Quaternion baseRot = Quaternion.Euler(startingRotations[jointName]);
            joint.transform.localRotation = overallRotation * baseRot;
        }

        if (debug)
        {
            Debug.Log($"Applied overall Y rotation ({overallYRot} deg) to selected joints.");
        }
    }

    // Function to update hand rotations based on forearm rotations
    private void UpdateHandRotations()
    {
        // Set hands rotation to be the same as the forearms
        GameObject rForearm = joints.Find(j => j.name == "RForearm");
        GameObject lForearm = joints.Find(j => j.name == "LForearm");
        if (debug)
        {
            Debug.Log($"RForearm rotation: {rForearm.transform.localRotation.eulerAngles}, LForearm rotation: {lForearm.transform.localRotation.eulerAngles}");
        }
        GameObject rHand = joints.Find(j => j.name == "RHand");
        GameObject lHand = joints.Find(j => j.name == "LHand");
        Quaternion rHandRot = Quaternion.Euler(startingRotations["RHand"]);
        Quaternion lHandRot = Quaternion.Euler(startingRotations["LHand"]);
        rHand.transform.localRotation = rForearm.transform.rotation;// * rHandRot;
        lHand.transform.localRotation = lForearm.transform.rotation;// * lHandRot;
    }

    // Function to display the animated movement of the person based on the trajectory data
    public void DisplayTrajectory()
    {
        if (trajectoryData.Count == 0)
        {
            Debug.LogWarning("No trajectory data available to display.");
        }

        if (debug)
        {
            Debug.Log("TrajectoryManager: Playing animation.");
        }

        // Start TrajectoryCoroutine
        StartCoroutine(TrajectoryCoroutine());
    }

    // Function to instatate the person prefab
    private void InstantiatePerson()
    {
        if (personPrefab == null)
        {
            Debug.LogError("Person prefab is not assigned.");
            return;
        }
        GameObject person = Instantiate(personPrefab);
        person.name = "Person";
        joints_root = person;
        // Load all joints from the person GameObject hierarchy
        joints.Clear();
        LoadAllJoints(joints_root);

        StartCoroutine(ShowPerson(person));
    }

    // Function to destroy the instantiated person
    private void DestroyPerson()
    {
        if (joints_root != null)
        {
            Destroy(joints_root);
            joints_root = null;
        }
        if (joints != null)
        {
            joints.Clear();
        }
    }

    void Start()
    {
        videoIndex = DataHolder.videoIndex;
        // Load trajectory data from the specified CSV path
        LoadTrajectoryData(traj_csv_path + videoIndex);
        uiManager.SetSliderRange(minFrame, maxFrame);

        if (debug)
        {
            Debug.Log($"Loaded {joints.Count} joints from person hierarchy.");
        }

        // Show starting frame
        UpdateJointsFromTrajectory(2);
    }
}