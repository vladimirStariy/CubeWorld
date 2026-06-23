using UnityEngine;

public sealed class FrameProfilerDriver : MonoBehaviour
{
    private void LateUpdate()
    {
        RuntimeFrameProfiler.EndFrame(Time.unscaledDeltaTime * 1000f);
    }
}
