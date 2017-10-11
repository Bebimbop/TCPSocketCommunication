using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ExampleControllerForClient : MonoBehaviour {

	public Text Output;
	public Text Servermsg;
	public Image StatusGraphic;
	public ORTCPClient Client;

	private List<string> myComandList = new List<string>();
	public int Port;
	
	private void Awake()
	{
		var path = "";
#if UNITY_EDITOR
		path = Application.dataPath;
#else
		path = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\"));
#endif
		Port = GetPort(path);
		Client.port = Port;
		if (Port == 0) Output.text = "Err : Check Port number";
	}

	void Start ()
	{
		Observable
			.FromEvent<ORTCPClient.TCPServerMessageRecivedEvent,ORTCPEventParams>
			(h=>(par)=>h(par),
		     h=> Client.OnTCPMessageRecived += h, 
			 h => Client.OnTCPMessageRecived -= h)
			.Subscribe(x =>
			{
				OnTCPMessage(x)
					.Subscribe(DoCommand);
			})
			.AddTo(this.gameObject);
		
		Observable.EveryUpdate().Subscribe(_ =>
		{
			switch (Client._state)
			{
				case ORTCPClientState.Connected:
					StatusGraphic.color = Color.green;
					break;
				case ORTCPClientState.Disconnected:
					StatusGraphic.color = Color.red;
					break;
			}
		}).AddTo(gameObject);
	}


	private UniRx.IObservable<string> OnTCPMessage (ORTCPEventParams e)
	{
		var msg = e.message;
		myComandList.Add(msg);
		Debug.Log(msg);
		if(msg.Contains("[TCPServer]")) Servermsg.text = msg +"  ["+DateTime.Now +"] "+"\n" + Servermsg.text;
			else Output.text = msg +"  ["+DateTime.Now +"] "+"\n" + Output.text;
		return myComandList.ToObservable();
	}

	private int GetPort(string path)
	{
		StreamReader reader = new StreamReader(path + "/Port.txt");
		var _port = 0; 
		int.TryParse(reader.ReadLine(),out _port);
		reader.Close();
		return _port;
	}

	//todo add more commands
	private void DoCommand(string msg)
	{
		switch (msg)
		{
		   case  "Reset-App":
			   SceneManager.LoadScene(SceneManager.GetActiveScene().name);
			   break;
		}
	}


}


