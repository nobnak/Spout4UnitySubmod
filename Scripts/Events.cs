using UnityEngine;
using System.Collections;

namespace Spout {
	
	[System.Serializable]
	public class TextureEvent : UnityEngine.Events.UnityEvent<Texture> {}

	[System.Serializable]
	public class RenderTextureEvent : UnityEngine.Events.UnityEvent<RenderTexture> {}

}
