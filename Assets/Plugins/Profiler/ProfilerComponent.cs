using UnityEngine;

// Note: Ensure this component runs *BEFORE* everything else in the game
public class ProfilerComponent : MonoBehaviour {
	private void Update() {
		RamjetProfiler.LogFrameResults ();
		RamjetProfiler.ClearFrameResults ();
	}
}