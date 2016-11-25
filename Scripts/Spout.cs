/* 
 * Spout4Unity
* Copyright © 2014-2015 Benjamin Kuperberg
* Copyright © 2015 Stefan Schlupek
* All rights reserved
*/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace Spout {

	public class Spout : MonoBehaviour {

		public  event EventHandler<TextureShareEventArgs> OnSenderStopped;
		public  event Action OnEnabled;
		public  event Action OnAllSendersStopped;

		public delegate void TextureSharedDelegate(TextureInfo texInfo);
		public  TextureSharedDelegate texSharedDelegate;
		public delegate void SenderStoppedDelegate(TextureInfo texInfo);
		public  SenderStoppedDelegate senderStoppedDelegate;

		#region Debug
		public static bool DebugOutputEnabled;

		public static void Log(string log) {
			if (DebugOutputEnabled)
				Debug.Log (log);
		}
		public static void LogFormat(string format, params object[] args) {
			if (DebugOutputEnabled)
				Debug.LogFormat (format, args);
		}
		#endregion

		public  List<TextureInfo> activeSenders;
		public  List<TextureInfo> activeLocalSenders;
		public HashSet<string> localSenderNames;
		
		public static bool isInit {
			get{return _isInit;}
		}
		//You can use a fakeName of your choice .It's just to force an update in the Spout Receiver at start even if the 'offical' sharingName doesn't change.
		public static string fakeName = "SpoutIsSuperCoolAndMakesFun";

		private IntPtr intptr_senderUpdate_delegate;
		private IntPtr intptr_senderStarted_delegate;
		private IntPtr intptr_senderStopped_delegate;
		
		// Use GCHandle to hold the delegate object in memory.
		private GCHandle handleSenderUpdate;
		private GCHandle handleSenderStarted;
		private GCHandle handleSenderStopped;

		#pragma warning disable 414
		[SerializeField]
		private static bool _isInit;
		#pragma warning restore 414
		private static bool isReceiving;
		
		private  List<TextureInfo> newSenders;
		private  List<TextureInfo> stoppedSenders;

		[SerializeField]
		private static Spout _instance;

		[SerializeField]
		private static Texture2D _nullTexture ;
		public static Texture2D nullTexture{
			get {return _nullTexture;}
		}
		
		public static Spout instance {
			get {
				if(_instance == null){
					_instance = GameObject.FindObjectOfType<Spout>();
					if(_instance == null) {
						GameObject _go = new GameObject("Spout");			
						_instance = _go.AddComponent<Spout>();
					}
					DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
			private set{_instance = value;}
		}

		protected virtual void Awake() {
			Log("Spout.Awake");
		
			if(_instance != null && _instance != this ) {
				Destroy(this.gameObject);
				return;
			}

			newSenders = new List<TextureInfo>();
			stoppedSenders = new List<TextureInfo>();
			activeSenders = new List<TextureInfo>();
			activeLocalSenders = new List<TextureInfo>();
			localSenderNames = new HashSet<string>();

			_nullTexture = new Texture2D(32,32);
			_nullTexture.hideFlags = HideFlags.HideAndDontSave;

		}
		protected virtual void OnEnable(){
			if(_instance != null && _instance != this ) {
				Destroy(this.gameObject);
				return;
			}

			newSenders = new List<TextureInfo>();
			stoppedSenders = new List<TextureInfo>();
			activeSenders = new List<TextureInfo>();
			activeLocalSenders = new List<TextureInfo>();
			localSenderNames = new HashSet<string>();
			
			_Init();
			if(OnEnabled != null) OnEnabled();
		}
		protected virtual void Update() {
			if(isReceiving)
				checkReceivers();

			lock(this) {
				foreach (TextureInfo s in newSenders) {
					activeSenders.Add (s);
					if (texSharedDelegate != null)
						texSharedDelegate (s);
				}
				
				newSenders.Clear();
			
				foreach (TextureInfo s in stoppedSenders) {
					foreach (TextureInfo t in activeSenders) {
						if (s.name == t.name) {
							activeSenders.Remove (t);
							break;
						}
					}
					if (senderStoppedDelegate != null)
						senderStoppedDelegate (s);
				}
				stoppedSenders.Clear();
			}//lock
		}

		protected virtual void OnDisable() {
			if (_instance != this)
				return;

			StopAllLocalSenders ();
			//Force the Plugin to check. Otherwise we don't get a SenderStopped delegate call
			Update ();
		}
		
		protected virtual void OnDestroy() {
			if (_instance != this)
				return;
		
			if (_isInit)
				_CleanUpResources ();

			isReceiving = false;
			_isInit = false;
			newSenders = null;
			stoppedSenders = null;
			activeSenders = null;
			activeLocalSenders = null;
			localSenderNames = null;
			
			OnEnabled = null;
			OnSenderStopped = null;
			
			_instance = null;

			GC.Collect ();//??
		}

		private void _Init() {
			initNative();
			_startReceiving();
			_isInit = true;
		} 		
		private void _CleanUpResources(){
			clean();
			
			_instance.texSharedDelegate = null;
			_instance.senderStoppedDelegate = null;
			
			_instance.handleSenderUpdate.Free(); 
			_instance.handleSenderStarted.Free();
			_instance.handleSenderStopped.Free();

			intptr_senderUpdate_delegate = IntPtr.Zero;
			intptr_senderStarted_delegate = IntPtr.Zero;
			intptr_senderStopped_delegate = IntPtr.Zero;
		}

		
		public void addListener(TextureSharedDelegate sharedCallback, SenderStoppedDelegate stoppedCallback ) {
			if(_instance == null)return;
			_instance.texSharedDelegate += sharedCallback;
			_instance.senderStoppedDelegate += stoppedCallback;
		}
		
		public static void removeListener(TextureSharedDelegate sharedCallback, SenderStoppedDelegate stoppedCallback ) {
			if (_instance == null)
				return;
			_instance.texSharedDelegate -= sharedCallback;
			_instance.senderStoppedDelegate -= stoppedCallback;
		}

		public  bool CreateSender(string sharingName, Texture tex, int texFormat = 1) {
			if (!enabled)
				return false;
			if (!_isInit)
				return false;

			Log ("Spout.CreateSender:" + sharingName + "::" + tex.GetNativeTexturePtr ().ToInt32 ());
			bool result = createSenderNative (sharingName, tex.GetNativeTexturePtr (), texFormat);
			if (!result)
				Debug.LogWarning (String.Format ("Spout sender creation with name {0} failed !", sharingName));
			if (result)
				localSenderNames.Add (sharingName);
			return result;
		}
		
		public  bool UpdateSender(string sharingName, Texture tex) {
			if (enabled == false || gameObject.activeInHierarchy == false || _isInit == false)
				return false;
			return updateSenderNative (sharingName, tex.GetNativeTexturePtr ());
		}
		public  TextureInfo getTextureInfo (string sharingName) {
			if (activeSenders == null)
				return null;
			
			foreach (TextureInfo tex in activeSenders) {
				if (tex.name == sharingName)
					return tex;
			}
			
			if (sharingName != Spout.fakeName)
				Log (String.Format ("sharing name {0} not found", sharingName));
			
			return null;
		}
		
		//Imports
		[DllImport ("NativeSpoutPlugin", EntryPoint="init")]
		public  static extern bool initNative();
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="initDebugConsole")]
		private static extern void _initDebugConsole();
		
		[DllImport ("NativeSpoutPlugin")]
		private static extern void checkReceivers();
		
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="createSender")]
		private static extern bool createSenderNative (string sharingName, IntPtr texture, int texFormat);
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="updateSender")]
		private static extern bool updateSenderNative (string sharingName, IntPtr texture);
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="closeSender")]
		public static extern bool CloseSender (string sharingName);
		
		[DllImport ("NativeSpoutPlugin")]
		private static extern void clean();

		
		[DllImport ("NativeSpoutPlugin", EntryPoint="startReceiving")]
		private static extern bool startReceivingNative(IntPtr senderUpdateHandler,IntPtr senderStartedHandler,IntPtr senderStoppedHandler);
					
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SpoutSenderUpdateDelegate(int numSenders);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SpoutSenderStartedDelegate(string senderName, IntPtr resourceView,int textureWidth, int textureHeight);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SpoutSenderStoppedDelegate(string senderName);

		
		public void initDebugConsole() {
			//check if multiple inits?
			_initDebugConsole();
		}

		private  void _startReceiving() {
			if(isReceiving)return;
			//Debug.Log("Spout.startReceiving");
			SpoutSenderUpdateDelegate senderUpdate_delegate = new SpoutSenderUpdateDelegate(SenderUpdate);
			handleSenderUpdate = GCHandle.Alloc(senderUpdate_delegate);
			 intptr_senderUpdate_delegate = Marshal.GetFunctionPointerForDelegate (senderUpdate_delegate);
			
			SpoutSenderStartedDelegate senderStarted_delegate = new SpoutSenderStartedDelegate(SenderStarted);
			handleSenderStarted = GCHandle.Alloc(senderStarted_delegate);
			 intptr_senderStarted_delegate = Marshal.GetFunctionPointerForDelegate (senderStarted_delegate);
			
			SpoutSenderStoppedDelegate senderStopped_delegate = new SpoutSenderStoppedDelegate(SenderStopped);
			handleSenderStopped = GCHandle.Alloc(senderStopped_delegate);
			 intptr_senderStopped_delegate = Marshal.GetFunctionPointerForDelegate (senderStopped_delegate);
			
			isReceiving = startReceivingNative(intptr_senderUpdate_delegate, intptr_senderStarted_delegate, intptr_senderStopped_delegate);
		}
		
		private  void SenderUpdate(int numSenders) {
			//Debug.Log("Sender update, numSenders : "+numSenders);
		}
		
		private  void SenderStarted(string senderName, IntPtr resourceView,int textureWidth, int textureHeight) {
			Log("Spout. Sender started, sender name : "+senderName);
			if(_instance == null || _instance.activeLocalSenders == null || _instance.newSenders == null)return;
			lock(this){
				TextureInfo texInfo = new TextureInfo(senderName);
				Log("resourceView:"+resourceView.ToInt32());
				texInfo.setInfos(textureWidth,textureHeight,resourceView);
				_instance.newSenders.Add(texInfo);
				if(_instance.localSenderNames.Contains(texInfo.name)){
				_instance.activeLocalSenders.Add(texInfo);
				//Debug.Log("activeLocalSenders.count:"+_instance.activeLocalSenders.Count);
				}
				Log("Spout.SenderStarted.End");
			}//lock
		}
		private  void SenderStopped(string senderName) {
			Log("Sender stopped, sender name : "+senderName);
			if(_instance == null || _instance.activeLocalSenders == null || _instance.stoppedSenders == null)return;
			lock(this){
				TextureInfo texInfo = new TextureInfo(senderName);
				
				_instance.stoppedSenders.Add (texInfo);

				_instance.localSenderNames.Remove(texInfo.name);

				if(_instance.activeLocalSenders.Contains(texInfo)){
					_instance.activeLocalSenders.Remove(texInfo);
				}
			}//lock
		}

		private void StopAllLocalSenders() {
			Log ("Spout.StopAllLocalSenders()"); 
			if (_instance == null)
				return;
			foreach (TextureInfo t in _instance.activeLocalSenders) {	
				CloseSender (t.name);
				if (OnSenderStopped != null)
					OnSenderStopped (this, new TextureShareEventArgs (t.name));
			}
			if (OnAllSendersStopped != null)
				OnAllSendersStopped ();
		}
	}

	public class TextureShareEventArgs : EventArgs {
		public string sharingName {get; set; }
		
		public TextureShareEventArgs(string myString) {
			this.sharingName = myString;
		}
	}

}


