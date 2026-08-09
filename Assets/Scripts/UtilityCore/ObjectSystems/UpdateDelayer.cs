using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class UpdateDelayer : MonoBehaviour
{
    public static Dictionary<string, Channel> updateChannels = new();
    public static Dictionary<string, Channel> fixedUpdateChannels = new();
    public class Channel
    {
        public float maxUpdatesPerFrame = 4;
        public List<Action> updateQueue = new();

        public int updatesDone = 0;
        public void Update()
        {
            while (updateQueue.Count > 0 && updatesDone < maxUpdatesPerFrame)
            {
                updateQueue[0]?.Invoke();
                updateQueue.RemoveAt(0);
                updatesDone++;
            }
            updatesDone = 0;
        }
    }

    private void Update()
    {
        foreach (var item in updateChannels)
            item.Value.Update();
    }
    private void FixedUpdate()
    {
        foreach (var item in fixedUpdateChannels)
            item.Value.Update();
    }

    public static void RegisterChannel(string channelName, float maxUpdatesPerFrame = 4, bool isFixedUpdate = false)
    {
        var channels = isFixedUpdate ? fixedUpdateChannels : updateChannels;
        if (!channels.ContainsKey(channelName))
            channels[channelName] = new Channel { maxUpdatesPerFrame = maxUpdatesPerFrame };
    }

    public static void QueueUpdate(Action updateAction, string channelName, bool isFixedUpdate = false)
    {
        Dictionary<string, Channel> channels = isFixedUpdate ? fixedUpdateChannels : updateChannels;
        if (!channels.ContainsKey(channelName)) RegisterChannel(channelName, 4, isFixedUpdate);
        if (channels[channelName].updatesDone < channels[channelName].maxUpdatesPerFrame)
        {
            updateAction?.Invoke();
            channels[channelName].updatesDone++;
        }
        else channels[channelName].updateQueue.Add(updateAction);
    }

    private static UpdateDelayer instance;
    public static void Setup()
    {
        if (instance != null) return;
        GameObject go = new("--Update Delayer--");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UpdateDelayer>();
    }
}