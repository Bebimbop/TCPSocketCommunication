using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ClientListener : MonoBehaviour
{
    public Text output;
    public ORTCPClient client;

	// Use this for initialization
	void Start ()
	{
        Debug.Log("Client is active.");
	    client.OnTCPMessageRecieved += OnTCPMessage;
	}
	
	//// Update is called once per frame
	//void Update ()
 //   {
		
	//}

    private void OnTCPMessage(ORTCPEventParams e)
    {
        output.text += e.message + "\n";
    }
}
