using UnityEngine;
using System.Collections;

namespace Spout {
    public class MultiCameraSpoutSender : ProceduralSpoutSenderBase {
        public Camera[] targetCameras;

		#region implemented abstract members of ProceduralSpoutSenderBase
		protected override void NotifyOnUpdateTexture (RenderTexture tex) {
            foreach (var c in targetCameras)
                c.targetTexture = tex;
		}
		#endregion
	}
}
