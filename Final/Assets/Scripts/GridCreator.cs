using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

//creador del grid
public class GridCreator : MonoBehaviour
{
    public GameObject cellPrefab;

    private GameObject[,] gridArray;

    void Start()
    {
        StartCoroutine(GetOfficeLayoutFromServer());
    }

    IEnumerator GetOfficeLayoutFromServer()
    {
        string url = "http://localhost:8585";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;
            OfficeLayoutResponse response = JsonUtility.FromJson<OfficeLayoutResponse>(jsonResponse);
            CreateGrid(response.office_layout);
        }
    }

    void CreateGrid(string[] layout)
    {
        int rows = layout.Length;
        int columns = layout[0].Split(' ').Length;
        gridArray = new GameObject[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            string[] cells = layout[row].Split(' ');
            for (int col = 0; col < columns; col++)
            {
                Vector3 cellPosition = new Vector3(col, -row, 0);
                GameObject cell = Instantiate(cellPrefab, cellPosition, Quaternion.identity);
                cell.transform.SetParent(transform);
                cell.name = $"Cell_{row}_{col}";

                gridArray[row, col] = cell;
            }
        }
    }

    [System.Serializable]
    public class OfficeLayoutResponse
    {
        public string[] office_layout;
    }
}
