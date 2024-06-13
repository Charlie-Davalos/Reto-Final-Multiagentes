using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class WebClient : MonoBehaviour
{
    public GameObject robotPrefab;
    public GameObject trashPrefab;
    public GameObject trashcanPrefab;
    public TextMeshProUGUI stepCounterText;

    private Dictionary<int, GameObject> robots = new Dictionary<int, GameObject>();
    private Dictionary<Vector2, GameObject> trash = new Dictionary<Vector2, GameObject>();
    private StepCounterTMP stepCounter;

    IEnumerator SendData(string data)
    {
        string url = "http://localhost:8585";
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, data))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Response: " + jsonResponse);

                // Imprimir los datos JSON recibidos
                Debug.Log("Received JSON: " + jsonResponse);

                // respuesta del servidor
                InitialConfig config = JsonUtility.FromJson<InitialConfig>(jsonResponse.Replace('\'', '\"'));
                Debug.Log("Configuration received, initializing scene.");
                InitializeScene(config);
            }
        }
    }

    IEnumerator GetRobotPositions()
    {
        string url = "http://localhost:8585";
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Response: " + jsonResponse);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    State state = JsonUtility.FromJson<State>(jsonResponse);

                    // Actualizar la posición de los robots
                    foreach (var robot in state.robots)
                    {
                        if (robots.ContainsKey(robot.id))
                        {
                            robots[robot.id].GetComponent<RobotController>().MoveTo(new Vector3(robot.x, robot.y, 0));
                            robots[robot.id].GetComponent<RobotController>().UpdateTrashCount(robot.collected_trash);
                        }
                    }

                    // Actualizar la cantidad de basura, eliminar basura recogida
                    List<Vector2> keysToRemove = new List<Vector2>();
                    foreach (var t in state.trash)
                    {
                        Vector2 position = new Vector2(t.x, t.y);
                        if (trash.ContainsKey(position))
                        {
                            if (t.amount <= 0)
                            {
                                Destroy(trash[position]);
                                keysToRemove.Add(position);
                            }
                            else
                            {
                                trash[position].name = t.amount.ToString();
                                trash[position].GetComponent<TrashController>().amount = t.amount;
                            }
                        }
                        else
                        {
                            var trashObject = Instantiate(trashPrefab, new Vector3(t.x, t.y, 0), Quaternion.identity);
                            trashObject.name = t.amount.ToString();
                            trashObject.GetComponent<TrashController>().amount = t.amount;
                            trash[position] = trashObject;
                        }
                    }

                    // Eliminar basura que ha sido recogida
                    foreach (var key in keysToRemove)
                    {
                        trash.Remove(key);
                    }

                    // contador de pasos
                    stepCounter.IncrementStep();
                }
                else
                {
                    Debug.LogWarning("Empty response received from server.");
                }
            }
        }
    }

    void InitializeScene(InitialConfig config)
    {
        Debug.Log("Initializing Trashcan");
        if (trashcanPrefab == null)
        {
            Debug.LogError("Trashcan prefab is not assigned!");
            return;
        }

        Debug.Log("Initializing Robots");
        if (robotPrefab == null)
        {
            Debug.LogError("Robot prefab is not assigned!");
            return;
        }

        Debug.Log("Initializing Trash");
        if (trashPrefab == null)
        {
            Debug.LogError("Trash prefab is not assigned!");
            return;
        }

        // Inicializar trashcan
        Debug.Log("Creating Trashcan at position: " + config.trashcan.x + ", " + config.trashcan.y);
        Instantiate(trashcanPrefab, new Vector3(config.trashcan.x, config.trashcan.y, 0), Quaternion.identity);

        // Inicializar robots
        foreach (var robot in config.robots)
        {
            Debug.Log("Creating Robot ID " + robot.id + " at position: " + robot.x + ", " + robot.y);
            var robotObject = Instantiate(robotPrefab, new Vector3(robot.x, robot.y, 0), Quaternion.identity);
            robots[robot.id] = robotObject;
        }

        // Inicializar basura
        foreach (var trashItem in config.trash)
        {
            Debug.Log("Creating Trash at position: " + trashItem.x + ", " + trashItem.y + " with amount: " + trashItem.amount);
            var trashObject = Instantiate(trashPrefab, new Vector3(trashItem.x, trashItem.y, 0), Quaternion.identity);
            trashObject.name = trashItem.amount.ToString();
            trashObject.GetComponent<TrashController>().amount = trashItem.amount;
            trash[new Vector2(trashItem.x, trashItem.y)] = trashObject;
        }
    }

    void Start()
    {
        Debug.Log("Start method called.");
        stepCounter = stepCounterText.GetComponent<StepCounterTMP>();
        Vector3 fakePos = new Vector3(3.44f, 0, 0);
        string json = JsonUtility.ToJson(fakePos);
        StartCoroutine(SendData(json));
        StartCoroutine(UpdateRobotPositions());
    }

    IEnumerator UpdateRobotPositions()
    {
        while (true)
        {
            yield return GetRobotPositions();
            yield return new WaitForSeconds(1f); 
        }
    }

    void Update()
    {
        
    }

    [System.Serializable]
    public class Position
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class Robot
    {
        public int id;
        public float x;
        public float y;
        public int collected_trash;
    }

    [System.Serializable]
    public class Trash
    {
        public float x;
        public float y;
        public int amount;
    }

    [System.Serializable]
    public class InitialConfig
    {
        public Position trashcan;
        public Robot[] robots;
        public Trash[] trash;
    }

    [System.Serializable]
    public class State
    {
        public Robot[] robots;
        public Trash[] trash;
    }
}
