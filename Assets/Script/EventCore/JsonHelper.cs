using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Debug.Log("newJson = " + newJson);
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        Debug.Log(wrapper.array);
        return wrapper.array;
    }

    [System.Serializable]
    public class Wrapper<T>  
    {
        public T[] array;
    }
}