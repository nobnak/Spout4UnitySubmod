using UnityEngine;
using System.Collections;

namespace Spout {
	[RequireComponent(typeof(Camera))]
	public class CameraSpoutSender : ProceduralSpoutSenderBase {
		Camera _attachedCam;

		protected override void Awake () {
			base.Awake ();
			_attachedCam = GetComponent<Camera> ();
		}

		#region implemented abstract members of ProceduralSpoutSenderBase
		protected override void NotifyOnUpdateTexture (RenderTexture tex) {
			if (_attachedCam != null)
				_attachedCam.targetTexture = tex;
		}
		#endregion
	}
}
