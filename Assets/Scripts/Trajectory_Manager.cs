using System;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class Trajectory_Manager : MonoBehaviour
{
    public string traj_csv_path = @"D:\Documenti\Universita\MS\Anno 2\Computer Vision\Progetto\CV_HPE\csv_trajectories\trajectories.csv";

    // Trajectory data is stored as a list of arrays
    // First number in each line is the frame, second is the joint name, 3rd, 4th and 5th are the joint positions (z is up in data), last number is the visibility of the joint
    [SerializeField]
    public static System.Collections.Generic.List<string[]> trajectoryData =
        new System.Collections.Generic.List<string[]>();
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

    // Coroutines
    IEnumerator TrajectoryCoroutine()
    {
        if (debug)
        {
            Debug.Log("Starting trajectory coroutine, trajectoryData.Count/jointNumber = " + trajectoryData.Count / jointNumber);
        }
        print("Starting " + Time.time);
        for (int i = 2; i < (trajectoryData.Count / jointNumber) + 2; i++)
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
        yield return new WaitForSeconds(0.01f);
        UpdateHandRotations();
        if (debug)
        {
            Debug.Log("Hand rotations updated.");
        }
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

        // Subtract spine2Pos position from the shoulders (0f, 0.349291f, 0f) from the hips
        GameObject rShoulder = joints.Find(j => j.name == "RShoulder");
        GameObject lShoulder = joints.Find(j => j.name == "LShoulder");
        if (rShoulder != null && lShoulder != null && hips != null)
        {
            Vector3 spine2Pos = hips.transform.localPosition + new Vector3(0f, 0.349291f, 0f);
            rShoulder.transform.localPosition -= spine2Pos;
            lShoulder.transform.localPosition -= spine2Pos;
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

        // Fix stationary joints
        //FixStaticJoints(frame);

        // Update joint rotations based on the new positions
        UpdateJointRotations();
        StartCoroutine(UpdateHandRotationsCoroutine());
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
        string[] jointsToRotate = { "RAnkle", "LAnkle", "Hips", "Head"};

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

    // Function that checks if the joints were not moved in this frame and translate it by the hips movement
    private void FixStaticJoints(int currentFrame)
    {
        if (currentFrame <= 2) return;

        foreach (var jointName in startingRotations.Keys)
        {
            // Find joint GameObject
            GameObject joint = joints.Find(j => j.name == jointName);
            if (joint == null) continue;

            // Get positions from trajectoryData for current and previous frame
            Vector3? prevPos = null;
            Vector3? currPos = null;
            foreach (var data in trajectoryData)
            {
                if (data[1] == jointName)
                {
                    if (int.TryParse(data[0], out int frame))
                    {
                        string xStr = data[2].Replace(",", ".");
                        string yStr = data[3].Replace(",", ".");
                        string zStr = data[4].Replace(",", ".");
                        if (float.TryParse(xStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(yStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                            float.TryParse(zStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                        {
                            Vector3 unityPos = new Vector3(x / scale, z / scale, y / scale);
                            if (frame == currentFrame - 1) prevPos = unityPos;
                            if (frame == currentFrame) currPos = unityPos;
                        }
                    }
                }
            }

            // If position hasn't changed, translate by hips movement
            if (prevPos.HasValue && currPos.HasValue && Vector3.Distance(prevPos.Value, currPos.Value) < 0.0001f)
            {
                // Get hips movement
                Vector3? prevHips = null;
                Vector3? currHips = null;
                foreach (var data in trajectoryData)
                {
                    if (data[1] == "Hips")
                    {
                        if (int.TryParse(data[0], out int frame))
                        {
                            string xStr = data[2].Replace(",", ".");
                            string yStr = data[3].Replace(",", ".");
                            string zStr = data[4].Replace(",", ".");
                            if (float.TryParse(xStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                                float.TryParse(yStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                                float.TryParse(zStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                            {
                                Vector3 unityPos = new Vector3(x / scale, z / scale, y / scale);
                                if (frame == currentFrame - 1) prevHips = unityPos;
                                if (frame == currentFrame) currHips = unityPos;
                            }
                        }
                    }
                }
                if (prevHips.HasValue && currHips.HasValue)
                {
                    Vector3 hipsDelta = currHips.Value - prevHips.Value;
                    joint.transform.localPosition += hipsDelta;
                }
            }
        }
    }

    void Start()
    {
        // Load trajectory data from the specified CSV path
        LoadTrajectoryData(traj_csv_path);

        if (debug)
        {
            Debug.Log($"Loaded {joints.Count} joints from person hierarchy.");
        }

        // Show starting frame
        UpdateJointsFromTrajectory(2);
    }
}