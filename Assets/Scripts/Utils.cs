using System;
using UnityEngine;

public class Utils
{
    private Utils()
    {
    }

    public static void DrawLabel(Transform theTransform, string text, Vector3 offset = default(Vector3),
        Vector2 size = default(Vector2))
    {
        if (size.Equals(Vector2.zero))
            size = new Vector2(80, 40);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(theTransform.position);
        Vector2 guiPosition = new Vector2(screenPos.x - size.x / 2, Screen.height - screenPos.y + size.y / 2) +
                              (Vector2) offset;
        GUI.Label(new Rect(guiPosition, size), text);
    }

    /// <summary>
    /// Author: https://answers.unity.com/users/6612/bunny83.html
    /// https://answers.unity.com/questions/169442/split-a-string-every-n-characters.html
    /// </summary>
    public static string[] SegmentateString(string text, int charCount)
    {
        if (text.Length == 0)
            return new string[0];
        var arrayLength = (text.Length - 1) / charCount + 1;
        var result = new string[arrayLength];
        for (var i = 0; i < arrayLength; i++)
        {
            var tmp = "";
            for (var n = 0; n < charCount; n++)
            {
                var index = i * charCount + n;
                if (index >= text.Length) //important if last "part" is smaller
                    break;
                tmp += text[index];
            }

            result[i] = tmp;
        }

        return result;
    }


    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject myLine = new GameObject("DrawLine() generated object");
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    public static T[] SliceArray<T>(T[] arr, uint indexFrom, uint indexTo)
    {
        if (indexFrom > indexTo)
        {
            throw new ArgumentOutOfRangeException("indexFrom is bigger than indexTo!");
        }

        uint length = indexTo - indexFrom;
        T[] result = new T[length];
        Array.Copy(arr, indexFrom, result, 0, length);

        return result;
    }
}