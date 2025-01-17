using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using Logs;
using MapSaver;
using Newtonsoft.Json;
using SecureStuff;
using TileManagement;
using Util;
using Object = UnityEngine.Object;

public class FileSelectorWindow : EditorWindow
{
	private string folderPath = "";
	private string[] fileNames;

	[MenuItem("Mapping/𓃡𓃡 Map Loader Saver Selector 𓃡𓃡")]
	public static void ShowWindow()
	{
		// Create and show the editor window
		GetWindow<FileSelectorWindow>("𓃡𓃡 Map Loader Saver Selector 𓃡𓃡");
	}

// Key to store the selected file name in EditorPrefs
	private const string SelectedMap = "SelectedMap";

	private static bool DeleteMapAfterSave = false;
	private void OnEnable()
	{
		// Retrieve the selected file name from EditorPrefs (if it exists)
		if (EditorPrefs.HasKey(SelectedMap))
		{
			SubSceneManager.AdminForcedMainStation = EditorPrefs.GetString(SelectedMap);
		}

		// Set the default folder path to "Assets/StreamingAssets/Maps"
		folderPath = Path.Combine(Application.dataPath, "StreamingAssets/Maps");

		// Check if the default folder exists, if not, create it
		if (Directory.Exists(folderPath) == false)
		{
			Directory.CreateDirectory(folderPath);
		}

		// Get all file names from the default folder
		LoadFilesFromFolder();
	}

	private Vector2 scrollPosition = Vector2.zero; // Scroll position variable
	private Color separatorColor = Color.gray; // Define the separator color



	private void OnGUI()
	{
		GUILayout.Label("Delete Map After Save ", EditorStyles.boldLabel);
		DeleteMapAfterSave = GUILayout.Toggle(DeleteMapAfterSave, "", GUILayout.Width(20)); // Add a checkbox with a width of 20



		// Display the selected folder path
		if (!string.IsNullOrEmpty(folderPath))
		{
			GUILayout.Space(5);

			// Display the files in the selected folder
			if (fileNames != null && fileNames.Length > 0)
			{
				GUILayout.Label("Files in Folder:", EditorStyles.boldLabel);

				// Add a scroll view to handle many files
				scrollPosition =
					EditorGUILayout.BeginScrollView(scrollPosition,
						GUILayout.Height(500)); // Adjust the height as needed

				foreach (string fileName in fileNames)
				{
					GUILayout.BeginHorizontal();

					var RelativePath = GetRelativePath(folderPath, fileName);

					// Create a checkbox that is checked if the fileName matches the selectedFileName
					bool isSelected = (RelativePath == EditorPrefs.GetString(SelectedMap, ""));
					bool newIsSelected =
						GUILayout.Toggle(isSelected, "", GUILayout.Width(20)); // Add a checkbox with a width of 20

					// If the checkbox state changed, update the static variable
					if (newIsSelected != isSelected)
					{
						if (newIsSelected)
						{
							EditorPrefs.SetString(SelectedMap, RelativePath); // Save the selected file name
						}
						else
						{
							EditorPrefs.DeleteKey(SelectedMap); // Remove the saved file name when unselected
						}
					}

					// Display the file name label
					GUILayout.Label(RelativePath, GUILayout.Width(380));

					if (GUILayout.Button("Save", GUILayout.Width(50)))
					{
						// Start a coroutine to perform the save function
						Save(fileName);
						if (DeleteMapAfterSave)
						{
							MiscFunctions_RRT.DeleteAllRootGameObjects();
						}
					}

					if (GUILayout.Button("Load", GUILayout.Width(50)))
					{
						// Start a coroutine to perform the load function
						Load(fileName);
					}

					if (GUILayout.Button("Copy Path", GUILayout.Width(80)))
					{
						// Copy the relative file path to the clipboard
						string relativePath = GetRelativePath(folderPath, fileName);
						EditorGUIUtility.systemCopyBuffer = relativePath;
						Debug.Log("Copied relative path to clipboard: " + relativePath);
					}

					GUILayout.EndHorizontal();

					// Add a colored line separator between each file entry
					Rect rect = GUILayoutUtility.GetRect(1, 1); // Get a rect for the line
					EditorGUI.DrawRect(rect, separatorColor); // Draw the colored separator line
				}

				EditorGUILayout.EndScrollView();
			}
			else
			{
				GUILayout.Label("No files found in the selected folder.", EditorStyles.wordWrappedLabel);
			}
		}
	}


	private string GetRelativePath(string basePath, string fullPath)
	{
		// Ensure both paths use forward slashes
		basePath = basePath.Replace("\\", "/");
		fullPath = fullPath.Replace("\\", "/");

		if (fullPath.StartsWith(basePath))
		{
			// Remove the base path from the full path to get the relative path
			return fullPath.Substring(basePath.Length + 1); // +1 to remove the leading slash
		}

		return fullPath; // If not, return the full path as a fallback
	}

	private void LoadFilesFromFolder()
	{
		if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
		{
			// Get all files from the folder and its subfolders
			fileNames = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
				.Where(x => x.Contains(".meta") == false).ToArray();
		}
		else
		{
			fileNames = new string[0]; // No files found
		}
	}


	private void Load(string filePath)
	{
		MapSaver.MapSaver.CodeClass.ThisCodeClass.Reset();
		MapSaver.MapSaver.MapData mapData =
			JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(AccessFile.Load(filePath, FolderType.Maps));
		var Imnum = MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero, mapData);
		List<IEnumerator> PreviousLevels = new List<IEnumerator>();
		bool Loop = true;
		while (Loop && PreviousLevels.Count == 0)
		{
			if (Imnum.Current is IEnumerator)
			{
				PreviousLevels.Add(Imnum);
				Imnum = (IEnumerator) Imnum.Current;
			}

			Loop = Imnum.MoveNext();
			if (Loop == false)
			{
				if (PreviousLevels.Count > 0)
				{
					Imnum = PreviousLevels[PreviousLevels.Count - 1];
					PreviousLevels.RemoveAt(PreviousLevels.Count - 1);
					Loop = Imnum.MoveNext();
				}
			}
		}
	}


	public List<MetaTileMap> SortObjectsByChildIndex(List<MetaTileMap> objects)
	{
		// Sort the objects based on their sibling index
		objects.Sort((x, y) => y.transform.parent.GetSiblingIndex().CompareTo(x.transform.parent.GetSiblingIndex()));

		// Return the sorted list
		return objects;
	}

	private void Save(string filePath)
	{
		try
		{
			var MapMatrices = Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None).ToList();

			// Sort objects by their recursive child index path
			MapMatrices = SortObjectsByChildIndex(MapMatrices);

			if (MapMatrices.Count == 0)
			{
				Loggy.Error($"No maps found for Save {filePath}");
				return;
			}

			MapMatrices.Reverse();

			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore, // Ignore null values
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
				Formatting = Formatting.Indented
			};
			var Map = MapSaver.MapSaver.SaveMap(MapMatrices, false, MapMatrices[0].name);
			AccessFile.Save(filePath, JsonConvert.SerializeObject(Map, settings), FolderType.Maps);
			EditorUtility.DisplayDialog("Save Complete", $"Map saved successfully to {filePath}.", "OK");
		}
		catch (Exception e)
		{
			Loggy.Error(e.ToString());
		}
	}
}