using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveFileVisual : MonoBehaviour
{
    public int ID = 1;
    public TMPro.TextMeshProUGUI timeText;
    private JsonFile File;

    private void Awake()
    {
        File = new(GlobalState.SaveFilePath, $"SaveFile{ID}");
        UpdateFile();
    }


    public void PlayFile() => Gameplay.BeginMainMenu(ID);

    public void DeleteFile()
    {
        GlobalState.DeleteSaveFile(ID);
        UpdateFile();
    }

    private void UpdateFile()
    {
        
        if(File.LoadFromFile() == JsonFile.LoadResult.Success)
        {
            var TS = System.TimeSpan.FromSeconds(File.Data["Time"].ToObject<double>());
            timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";
        }
        else { timeText.text = "Empty"; }
    }
}
