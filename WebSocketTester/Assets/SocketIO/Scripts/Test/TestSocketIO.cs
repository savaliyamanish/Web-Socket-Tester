

using System.Collections;
using UnityEngine;
using SocketIO;
using TMPro;
using UnityEngine.UI;
using SFB;
using System.IO;
using System.Collections.Generic;


public class TestSocketIO : MonoBehaviour
{
	public TextMeshProUGUI projectPath;

	public SocketIOComponent _prefab;
	SocketIOComponent socketIo=null;
	public TMP_InputField socketURL,eventName,eventData;
	public TextMeshProUGUI logText;
	public Image connectBtn;
	public Sprite connectIcon,disconnectIcon;
	public Button emitButton,emitSaveButton;
	public Scrollbar logScroll;
	public Toggle isReturn;
	public TMP_Dropdown eventDropDown;
	bool socketError;
	Project currentPoject;
	System.Text.StringBuilder sb;
	public void Start()
	{
		sb = new System.Text.StringBuilder ();
	
		Application.logMessageReceived += (string condition, string stackTrace, LogType type) => {

		//	System.IO.File.AppendAllText (path, "\n" + System.DateTime.Now.ToLongTimeString () + "-" + System.DateTime.Now.Millisecond + " \n" + condition + "\n\n" + stackTrace);
			myLog(condition);
		};
		ClearLog ();
		currentPoject = new Project (Application.temporaryCachePath+"/"+"temp.ms");
		currentPoject.socketUrl = PlayerPrefs.GetString ("SocketURL", socketURL.text);
		LoadProjectToTester ();
		InvokeRepeating ("UpdateLog", .1f,0.1f);
	}
	public void myLog(string s)
	{
		sb.AppendLine ("");
		sb.AppendLine ("");
		sb.AppendLine (s);
	}
	public void ClearLog()
	{
		sb = new System.Text.StringBuilder ();
		logScroll.value=0;
	}
	public void UpdateScroll()
	{
		logScroll.value=0;
		
	}
	void UpdateLog()
	{
		if (!sb.ToString ().Equals (logText.text)) {
			logText.SetText (sb);

		}
	}
	public void OnGOBtn()
	{
		if(!socketURL.text.Trim().Equals("") || !socketURL.text.Contains("ws://"))
		{
			
			PlayerPrefs.SetString ("SocketURL", socketURL.text	);
			currentPoject.socketUrl=socketURL.text;
			currentPoject.SavePorject ();

			if (socketIo !=null) 
			{
				StopSocket ();
				Destroy (socketIo.gameObject);
			}
			else
			{
				socketIo = Instantiate<SocketIOComponent> (_prefab);
				Invoke ("StartSocket", 0.2f);
			}
		}
		else
		{
			Debug.Log ("Enter Valid URL");
		}
	}
	public void StartSocket ()
	{
		
		socketIo.On ("connect", OnConnect);
		socketIo.On ("disconnect", OnDisconnect);
		socketIo.On ("error", OnError);	
		socketURL.interactable = false;
		connectBtn.sprite=disconnectIcon;
		socketIo.myDebugLog = myLog;
		socketIo.Connect ();

	}

	public void StopSocket ()
	{
		socketIo.AllOff ();
		socketError = false;
		socketIo.Close ();
		connectBtn.sprite=connectIcon;
		socketURL.interactable = true;

	}

	public	void OnConnect (SocketIOEvent evnt)
	{
		myLog ("Socket Connected " + evnt.data);
		socketError = false;
		emitButton.interactable = true;
		emitSaveButton.interactable = true;

	}
	public void OnDisconnect (SocketIOEvent evnt)
	{
		emitButton.interactable = false;
		emitSaveButton.interactable = false;
		myLog ("Socket DisConnected " + evnt.data);
	}

	public	void OnError (SocketIOEvent evnt)
	{
		emitButton.interactable = false;
		emitSaveButton.interactable = false;
		myLog ("Socket Error " + evnt.data);
	}

	public void OnEmit()
	{
		if (!eventName.text.Trim ().Equals ("")) {
			if(new JSONObject(eventData.text).type == JSONObject.Type.NULL && !eventData.text.Trim ().Equals ("") )
			{
				Debug.Log ("Enter Valid Event JSON Data ");
			}
			else
			{
				
				if (eventData.text.Trim ().Equals (""))
					eventData.text = "{}";
				if (isReturn.isOn) {
				
					socketIo.Emit(eventName.text,new JSONObject(eventData.text),(obj)=>{
						myLog(obj.ToString(true));
					});
				}
				else
				{
					socketIo.Emit(eventName.text,new JSONObject(eventData.text));
				}



			}
		}
		else
		{
			Debug.Log ("Enter Valid Event Detail .... ");
		}
	}

	public void OnEmitSave()
	{
		if (!eventName.text.Trim ().Equals ("")) {
			if(new JSONObject(eventData.text).type == JSONObject.Type.NULL && !eventData.text.Trim ().Equals ("") )
			{
				Debug.Log ("Enter Valid Event JSON Data ");
			}
			else
			{

				if (eventData.text.Trim ().Equals (""))
					eventData.text = "{}";
				if (isReturn.isOn) {
					socketIo.Emit(eventName.text,new JSONObject(eventData.text),(obj)=>{
						myLog(obj.ToString(true));
					});
				}
				else
				{
					socketIo.Emit(eventName.text,new JSONObject(eventData.text));
				}
				currentPoject.AddEvent (new EventInfo(eventName.text,eventData.text,isReturn.isOn));
				UpdateOptionList ();

			}
		}
		else
		{
			Debug.Log ("Enter Valid Event Detail .... ");
		}
	}
	public void NewProject()
	{ 
		string path= StandaloneFileBrowser.SaveFilePanel ("New Project",PlayerPrefs.GetString("LastSave",Application.dataPath),"MyPoject","ms");
		if(path!=null && !path.Equals(""))
		{
			if (socketIo !=null) 
			{
				OnGOBtn ();
			}
			PlayerPrefs.SetString ("LastSave", path);
			currentPoject = new Project (path);
			projectPath.text = path;
		}
	
	}
	public void OpenProject()
	{
		string[] path= StandaloneFileBrowser.OpenFilePanel ("Open Project",PlayerPrefs.GetString("LastSave",Application.dataPath),"ms",false);
		if(path!=null&&path.Length>0 && !path[0].Equals("") && File.Exists(path[0]))
		{
			if (socketIo !=null) 
			{
				OnGOBtn ();
			}
			PlayerPrefs.SetString ("LastSave", path[0]);
			currentPoject = new Project (path [0]);
			projectPath.text = path [0];
			LoadProjectToTester ();
		}
	}
	void LoadProjectToTester()
	{
		projectPath.text = currentPoject.path;
		socketURL.text = currentPoject.socketUrl;
		UpdateOptionList ();

	}
	public void UpdateOptionList()
	{
		eventDropDown.options = new List<TMP_Dropdown.OptionData> ();
		currentPoject.eventList.ForEach ((obj) => {
			eventDropDown.options.Add(new TMP_Dropdown.OptionData(obj.eventName));
		});
		eventDropDown.RefreshShownValue ();
	}
	public void OnOptionSelect(int index)
	{
		eventName.text = currentPoject.eventList [index].eventName;
		eventData.text = currentPoject.eventList [index].eventData;
		isReturn.isOn = currentPoject.eventList [index].isReturnValue;
	}
}
[System.Serializable]
public class Project
{
	public string path;
	public string socketUrl;
	public List<EventInfo> eventList;
	public Project(string p)
	{
		path= p;
		if(File.Exists(path))
		{
			LoadProject ();
		}
		else
		{
			eventList = new List<EventInfo> ();
			eventList.Add (new EventInfo ());

			SavePorject ();
		}
	}
	void LoadProject()
	{
		if(path!=null &&!path.Equals(""))
			JsonUtility.FromJsonOverwrite(File.ReadAllText (path),this);
	}
	public void SavePorject()
	{
		if(path!=null &&!path.Equals(""))
			File.WriteAllText (path, JsonUtility.ToJson (this));
		
	}
	public void AddEvent(EventInfo temp)
	{
		if(eventList.Exists(((obj) => obj.eventName.Equals(temp.eventName))))
		{
			eventList.Find (((obj) => obj.eventName.Equals (temp.eventName))).eventData = temp.eventData;
			eventList.Find (((obj) => obj.eventName.Equals (temp.eventName))).isReturnValue = temp.isReturnValue;
		}
		else
		{
			eventList.Add (temp);
		}
		SavePorject ();
	}
}
[System.Serializable]
public class EventInfo 
{
	public string eventName;
	public string eventData;
	public bool isReturnValue;
	public EventInfo()
	{
		eventName="";
		eventData="{}";
		isReturnValue=true;
	}
	public EventInfo(string nm,string dt,bool b)
	{
		eventName=nm;
		eventData=dt;
		isReturnValue=b;
	}
}
