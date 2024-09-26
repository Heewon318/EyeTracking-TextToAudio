using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FixationData
{
    public Vector2 Position { get; set; }
    public float Duration { get; set; }

    public FixationData(Vector2 position, float duration)
    {
        Position = position;
        Duration = duration;
    }
}

public class SaliencyMap
{
    public Dictionary<int, float> WordFixationDurations { get; private set; }

    public SaliencyMap()
    {
        WordFixationDurations = new Dictionary<int, float>();
    }

    public void AddFixation(int wordIndex, float duration)
    {
        if (WordFixationDurations.ContainsKey(wordIndex))
        {
            WordFixationDurations[wordIndex] += duration;
        }
        else
        {
            WordFixationDurations[wordIndex] = duration;
        }
    }
}

public class ScanPath
{
    //public List<int> WordIndices { get; private set; }
    public List<Vector2> Positions { get; private set; }

    public ScanPath()
    {
        Positions = new List<Vector2>();
    }

    public void AddPosition(Vector2 position)
    {
        if (Positions.Count > 100) 
        {
            Positions.Remove(Positions[0]);
        }
        Positions.Add(position);
    }

    public int Count { get { return Positions.Count; } }

    public Vector2 this[int index] { get { return Positions[index]; } }

    public void SaveScanPathToFile()
    {
        string path = Application.dataPath + "/scan_path_data.csv";
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("x,y");
            foreach (var position in Positions)
            {
                writer.WriteLine($"{position.x},{position.y}");
            }
        }
        Debug.Log("Scan path data saved to scan_path_data.csv");
    }
}