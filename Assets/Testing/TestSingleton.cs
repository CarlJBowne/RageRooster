using UnityEngine;

public class TestSingleton : Singleton<TestSingleton>
{
    static void Data() => SetData(InitCreate, true, true);


}