using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveFileVisual : MonoBehaviour
{
    public int ID = 1;
    public TMPro.TextMeshProUGUI timeText;

    private void Awake() => UpdateFile();

    public void PlayFile() => Gameplay.BeginMainMenu(ID);

    public void DeleteFile()
    {
        GlobalState.DeleteSaveFile(ID);
        UpdateFile();
    }

    private void UpdateFile()
    {
        JToken J = new JObject().LoadJsonFromFile(GlobalState.SaveFilePath, $"SaveFile{ID}");
        if(J != null)
        {
            var TS = System.TimeSpan.FromSeconds(J["Time"].As<double>());
            timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";
        }
        else { timeText.text = "Empty"; }
    }
}
