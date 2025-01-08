using UnityEngine;

public class TestSingleton : SingletonAdvanced<TestSingleton>
{
    static void Data() => SetData(InitCreate, true, true);


}