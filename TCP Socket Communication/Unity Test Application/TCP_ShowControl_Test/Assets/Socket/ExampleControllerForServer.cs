using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExampleControllerForServer : MonoBehaviour {

	public Text output;

	void Start () {
	//	ORTCPMultiServer.Instance.OnTCPMessageRecived += OnTCPMessage;
		//Note: ORTCPMultiServer is going to say a lot of shit in the console, in the class file, turn 'verbose' to false to prevent this.
	}
	
	private void OnTCPMessage (ORTCPEventParams e) 
	{
		print ("===========================================");
		print ("message recived from client: " + e.message);
		output.text += e.message+"\n";
		// just as a reminder, this will be a JSON in format of {hand:"right", number:"22", team_id:"0", path:"F/..."}. 
		//let me know if I left something out or you want a different format.

	}


	public void OnButtonClick()
	{
		ORTCPMultiServer.Instance.SendAllClientsMessage ("This message is sent to all clients. for example, send {state:0} to reset the tablet app");// you can reset the app at the end or after timeout
	}
}
