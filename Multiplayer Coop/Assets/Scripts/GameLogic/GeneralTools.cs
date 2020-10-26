using System;

public class GeneralTools
{
    public static int GetFirstNullIndexFromArray(Array array) {
        for(int i = 0; i < array.Length; i++) {
            if ((UnityEngine.Object)array.GetValue(i) == null) {
                return i;
            }
        }
        return -1;
    }
}
