using UnityEngine;
using System.Collections;

public class ORTimer : MonoBehaviour {
	
	// Static
	
	public static ORTimer Execute(GameObject target, float duration, string message) {
		
		GameObject go = new GameObject("ORTimer");
		ORTimer timer = go.AddComponent<ORTimer>();
		timer.target = target;
		timer.duration = duration;
		timer.message = message;
		
		go.transform.parent = target.transform;
		
		return timer;

	}
	
	// MonoBehaviour
	
	public GameObject target;
	public float duration;
	public string message;
	
	private float _startTime;
	
	private void OnEnable() {
		_startTime = Time.time;
	}
	
	private void Update () {
		
		if (Time.time - _startTime >= duration) {
			
			if (target != null && target.gameObject != null)
				target.SendMessage(message, this, SendMessageOptions.DontRequireReceiver);
			
			Destroy(gameObject);
			
		}
	
	}
	
}
