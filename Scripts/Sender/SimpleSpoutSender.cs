/* 
 * Spout4Unity
* Copyright © 2014-2015 Benjamin Kuperberg
* Copyright © 2015 Stefan Schlupek
* All rights reserved
*/
using UnityEngine;
using System.Collections;
using System;

namespace Spout{
	public class SimpleSpoutSender : MonoBehaviour {
		public string sharingName = "UnitySender";
		public SpoutSenderImpl.TextureFormat textureFormat = SpoutSenderImpl.TextureFormat.DXGI_FORMAT_R8G8B8A8_UNORM;
		public Texture texture;
		public bool debugConsole = false;

		SpoutSenderImpl _impl;

		protected virtual void Awake() {
			Spout.instance.OnEnabled-= _OnSpoutEnabled;
			Spout.instance.OnEnabled+= _OnSpoutEnabled;			
		}
		protected virtual void OnEnable() {
			if(debugConsole)
				Spout.instance.initDebugConsole();
			_impl = new SpoutSenderImpl (sharingName, textureFormat, texture);
		}
		protected virtual void OnDisable(){
			Debug.Log("SpoutSender.OnDisable");
			if (_impl != null) {
				_impl.Dispose ();
				_impl = null;
			}
		}
		protected virtual void Update(){
			if (_impl != null)
				_impl.Update ();
		}

		void _OnSpoutEnabled(){
			if(enabled){
				//force a reconnection
				enabled = !enabled;
				enabled = !enabled;
			}
		}
	}
}
