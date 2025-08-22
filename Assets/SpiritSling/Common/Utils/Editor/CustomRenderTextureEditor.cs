// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEditor;
using System.IO;

// [CustomEditor(typeof(CustomRenderTexture))]
[MetaCodeSample("SpiritSling")]
public class CustomRenderTextureEditorUtils
{
	// public override void OnInspectorGUI()
	// {
	// 	base.OnInspectorGUI();
	//
	// 	CustomRenderTexture customRenderTexture = (CustomRenderTexture)target;
	//
	// 	if (GUILayout.Button("Save as PNG"))
	// 	{
	// 		SaveTextureAsPNG(customRenderTexture, "CustomRenderTexture.png");
	// 	}
	// }
	
	[MenuItem("CONTEXT/RenderTexture/Save as PNG", false, 10)]
	public static void SaveTextureAsPNG(MenuCommand menuCommand)
	{
		RenderTexture rt = menuCommand.context as RenderTexture;
		SaveTextureAsPNG(rt, rt.name + "_Texture2D.png");
	}
	
	[MenuItem("CONTEXT/RenderTexture/Save as PNG", true, 10)]
	public static bool ValidateSaveTextureAsPNG(MenuCommand command)
	{
		RenderTexture rt = command.context as RenderTexture;
		// Validate if the action can be performed (e.g., check if renderTexture is not null)
		return rt != null;
	}

	[ContextMenu("Save as PNG")]
	public static void SaveTextureAsPNG(RenderTexture renderTexture, string fileName)
	{
		// Create a new RenderTexture
		RenderTexture temporaryRenderTexture = new RenderTexture(renderTexture.width, renderTexture.height, 0, renderTexture.format);

		// Copy the CustomRenderTexture to the new RenderTexture
		Graphics.Blit(renderTexture, temporaryRenderTexture);

		// Set the active RenderTexture to the temporary one
		RenderTexture previousRenderTexture = RenderTexture.active;
		RenderTexture.active = temporaryRenderTexture;

		// Create a new Texture2D
		Texture2D texture2D = new Texture2D(temporaryRenderTexture.width, temporaryRenderTexture.height, TextureFormat.RGBA32, false);
		texture2D.ReadPixels(new Rect(0, 0, temporaryRenderTexture.width, temporaryRenderTexture.height), 0, 0);
		texture2D.Apply();

		// Reset the active RenderTexture
		RenderTexture.active = previousRenderTexture;

		// Encode the texture to PNG
		byte[] bytes = texture2D.EncodeToPNG();

		// Save the PNG to the Application's persistent data path
		string fullPath = Path.Combine(Application.dataPath, fileName);
		File.WriteAllBytes(fullPath, bytes);

		// Clean up
		// RenderTexture.ReleaseTemporary(temporaryRenderTexture);
		Object.DestroyImmediate(texture2D);

		Debug.Log("Saved CustomRenderTexture as PNG: " + fullPath);
		AssetDatabase.Refresh();
	}
}


public class RenderTextureProjectContextMenu : AssetPostprocessor
{
	// Adds a menu item named "Do Something" for RenderTextures in the Project Window.
	[MenuItem("Assets/Save as PNG")]
	private static void DoSomething()
	{
		// Get the selected RenderTexture
		var rt = Selection.activeObject as RenderTexture;
		if (rt != null)
		{
			CustomRenderTextureEditorUtils.SaveTextureAsPNG(rt, rt.name + "_Texture2D.png");
		}
	}

	// Validates if the selected asset is a RenderTexture to enable the context menu
	[MenuItem("Assets/Save as PNG", true)]
	private static bool ValidateDoSomething()
	{
		// Return true if the selected object is a RenderTexture
		return Selection.activeObject is RenderTexture;
	}
}
