// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Diagnostics;
using Object = UnityEngine.Object;

[MetaCodeSample("SpiritSling")]
public static class Log
{
    public static string COLOR_DEBUG = "#47A0DA";
    public static string COLOR_INFO = "#F2F2F2";
    public static string COLOR_WARNING = "#F3D13C";
    public static string COLOR_ERROR = "#BE5154";

    [Conditional("DEBUG")]
    public static void Debug(object text, Object context = null)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log($"<color={COLOR_DEBUG}>[DEBUG] {text}</color>", context);
#else
        UnityEngine.Debug.Log($"[{DateTime.Now:HH:mm:ss}][DEBUG] {text}");
#endif
    }

    public static void Info(object text, Object context = null)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[{DateTime.Now:HH:mm:ss}]<color={COLOR_INFO}>[INFO] {text}</color>", context);
#else
        UnityEngine.Debug.Log($"[{DateTime.Now:HH:mm:ss}][INFO] {text}");
#endif
    }

    public static void Warning(object text, Object context = null)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning($"<color={COLOR_WARNING}>[WARN] {text}</color>", context);
#else
        UnityEngine.Debug.LogWarning($"[{DateTime.Now:HH:mm:ss}] {text}");
#endif
    }

    public static void Error(object text, Exception e = null, Object context = null)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.LogError($"<color={COLOR_ERROR}>[ERROR] {text}</color>", context);
#else
        UnityEngine.Debug.LogError($"[{DateTime.Now:HH:mm:ss}] {text}");
#endif
        if (e != null) UnityEngine.Debug.LogException(e);
    }
}
