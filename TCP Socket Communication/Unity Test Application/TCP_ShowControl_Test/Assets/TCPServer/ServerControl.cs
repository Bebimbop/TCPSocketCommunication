using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ServerControl : MonoBehaviour
{
    private string defaultSaveToPath = "";
    private bool resetCall = false;

    private char[] delimiterChars = {' ', ',', '{', '}', '"', ':' };

    // Use this for initialization
    void Start ()
	{
	    ORTCPMultiServer.Instance.OnTCPMessageReceived += OnTCPMessage;

	    resetCall = false;
        
	    //The default path is primarily for testing, but is also used as a backup.
	    defaultSaveToPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SuitUpSnapshots";

	    //If the default directory doesn't exist, create it.
	    if (!Directory.Exists(defaultSaveToPath))
	        Directory.CreateDirectory(defaultSaveToPath);
    }

    private void OnTCPMessage(ORTCPEventParams e)
    {

    }
}
