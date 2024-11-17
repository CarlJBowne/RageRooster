using UnityEngine;

public class TestSingleton : Singleton<TestSingleton>
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot() => SetInfo(InitSavedPrefab, true, true);



}