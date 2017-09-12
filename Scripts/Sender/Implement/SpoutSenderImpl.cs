using UnityEngine;
using System.Collections;
using System;

namespace Spout{
	public class SpoutSenderImpl : System.IDisposable {
		//according to dxgiformat.h :
		//tested with DXGI_FORMAT_R8G8B8A8_UNORM (ATI Card)
		public enum TextureFormat { 
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2, 
            DXGI_FORMAT_R10G10B10A2_UNORM = 24, 
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28, 
            DXGI_FORMAT_B8G8R8A8_UNORM = 87
        }

		// if there are problems you can increase this value 
		public const int STARTUP_FRAMES_DELAY = 3;
		public const int CREATE_ATTEMPTS = 5;

		public readonly string sharingName = "UnitySender";
		public TextureFormat textureFormat;
		public readonly Texture texture;

		private bool senderIsCreated;

		//make this public if you want
		//It's better you set this always to true!
		//There are problems with creating a sender at OnEnable at Editor/App Start time so we have a little delay hack that calls the CreateSender at Update()
		private  bool StartupDelay = true;

		private int _startUpFrameCount = 0;
		private int _attempts= 0;

		public SpoutSenderImpl(string sharingName, TextureFormat textureFormat, Texture texture) {
			this.sharingName = sharingName;
			this.textureFormat = textureFormat;
			this.texture = texture;
		}

		public void Update() {
			if (texture == null)
				return;

			if (senderIsCreated)
				Spout.instance.UpdateSender (sharingName, texture);
			else
				if (StartupDelay)
				if (STARTUP_FRAMES_DELAY <= ++_startUpFrameCount && _attempts <= CREATE_ATTEMPTS)
					_CreateSender ();
		}

		void _CreateSender(){
			if (texture == null) return;
			if(!Spout.isInit)return;
			if(!Spout.instance.enabled)return;

			if (!senderIsCreated) {
				Spout.Log("Sender is not created, creating one");
				senderIsCreated = Spout.instance.CreateSender(sharingName, texture,(int) textureFormat);
			}

			_attempts++;
			if(_attempts > CREATE_ATTEMPTS)
				Debug.LogWarning(String.Format("There are problems with creating the sender {0}. Please check your settings or restart Unity.",sharingName));

			Spout.instance.OnSenderStopped -= OnSenderStoppedDelegate;
			Spout.instance.OnSenderStopped += OnSenderStoppedDelegate;

			Spout.instance.OnAllSendersStopped-=OnAllSendersStoppedDelegate;
			Spout.instance.OnAllSendersStopped+=OnAllSendersStoppedDelegate;
		}

		void _CloseSender(){
			Spout.Log("SpoutSender._CloseSender:"+sharingName);
			if(senderIsCreated)
				Spout.CloseSender(sharingName);
			_CloseSenderCleanUpData();
		}

		void OnSenderStoppedDelegate(object sender, TextureShareEventArgs e){
			if(e.sharingName == sharingName)
				_CloseSenderCleanUpData();
		}
		void OnAllSendersStoppedDelegate(){
			_CloseSenderCleanUpData();
		}
		void _CloseSenderCleanUpData(){
			senderIsCreated = false;
		}

		#region IDisposable implementation
		public void Dispose () {
			//we can't call  Spout2.instance because on Disable is also called when scene is destroyed.  
			//so a nother instance of Spout could be generated in the moment when the Spout2 instance is destroyed!
			_CloseSender();
		}
		#endregion
	}
}
