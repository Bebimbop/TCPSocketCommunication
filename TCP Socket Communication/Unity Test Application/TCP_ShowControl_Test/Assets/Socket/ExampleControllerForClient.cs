using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ExampleControllerForClient : MonoBehaviour {

	public Text output;
	public Image StatusGraphic;
	public ORTCPClient client;

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
		client.port = Port;
		if (Port == 0) output.text = "Err : Check Port number";
	}

	void Start ()
	{
		Observable
			.FromEvent<ORTCPClient.TCPServerMessageRecivedEvent,ORTCPEventParams>
			(h=>(par)=>h(par),
		     h=> client.OnTCPMessageRecived += h, 
			 h => client.OnTCPMessageRecived -= h)
			.Subscribe(x =>
			{
				OnTCPMessage(x)
					.Subscribe(DoCommand);
			})
			.AddTo(this.gameObject);
		
		Observable.EveryUpdate().Subscribe(_ =>
		{
			switch (client._state)
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
		output.text = msg +"  ["+DateTime.Now +"] "+"\n" + output.text;
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
		   case  "Reset":
			   SceneManager.LoadScene(SceneManager.GetActiveScene().name);
			   break;
		}
	}


}
