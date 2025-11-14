#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Collections;

[InitializeOnLoad]
static class EnableNativeLeakDetection
{
 static EnableNativeLeakDetection()
 {
 try
 {
 // Enable leak detection with stack traces for NativeCollections. This helps
 // identify where Allocator.Temp/TempJob allocations are coming from.
 NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
 Debug.Log("NativeLeakDetection.Mode set to EnabledWithStackTrace");
 }
 catch (System.Exception ex)
 {
 Debug.LogWarning($"Failed to enable NativeLeakDetection: {ex.Message}");
 }
 }
}
#endif
