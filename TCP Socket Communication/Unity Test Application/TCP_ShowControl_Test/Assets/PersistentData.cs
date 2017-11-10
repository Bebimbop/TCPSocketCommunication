using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentData : MonoBehaviour
{

    public enum eAppStatus
    {
        Invalid = -1,
        Starting,
        Running,
        ShuttingDown,
    }

    public static eAppStatus appStatus = eAppStatus.Starting;

    public enum eSensorStatus
    {
        Invalid = -1,
        Available,
        Unavailable
    }

    public static eSensorStatus sensorStatus = eSensorStatus.Available;
}
