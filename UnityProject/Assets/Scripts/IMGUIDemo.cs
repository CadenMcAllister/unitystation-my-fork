using System;
using ImGuiNET;
using UImGui;
using UnityEngine;

public class IMGUIDemo : MonoBehaviour
{
	[SerializeField] private bool showDemo = false;
	private void Update()
	{
		bool IsHeadless = Application.isBatchMode;

#if UNITY_STANDALONE_LINUX_API
		IsHeadless = true;
#endif

		if (IsHeadless)
		{
			DestroyImmediate(this);
			DestroyImmediate(this.GetComponent<UImGui.UImGui>());
			return;
		}
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Insert))
		{
			showDemo = !showDemo;
			if (showDemo)
			{
				UImGuiUtility.Layout += OnLayout;
				UImGuiUtility.OnInitialize += OnInitialize;
				UImGuiUtility.OnDeinitialize += OnDeinitialize;
			}
			else
			{
				UImGuiUtility.Layout -= OnLayout;
				UImGuiUtility.OnInitialize -= OnInitialize;
				UImGuiUtility.OnDeinitialize -= OnDeinitialize;
			}
		}
	}

	private void Start()
	{
		bool IsHeadless = Application.isBatchMode;

#if UNITY_STANDALONE_LINUX_API
		IsHeadless = true;
#endif

		if (IsHeadless)
		{
			DestroyImmediate(this.GetComponent<UImGui.UImGui>());
			DestroyImmediate(this);
			return;
		}
		if (showDemo)
		{
			UImGuiUtility.Layout += OnLayout;
			UImGuiUtility.OnInitialize += OnInitialize;
			UImGuiUtility.OnDeinitialize += OnDeinitialize;
		}
	}

	private void OnInitialize(UImGui.UImGui obj)
	{
		// runs after UImGui.OnEnable();
	}

	private void OnDeinitialize(UImGui.UImGui obj)
	{
		// runs after UImGui.OnDisable();
	}

	private void OnDisable()
	{
		UImGuiUtility.Layout -= OnLayout;
		UImGuiUtility.OnInitialize -= OnInitialize;
		UImGuiUtility.OnDeinitialize -= OnDeinitialize;
	}

	private void Awake()
	{
		bool IsHeadless = Application.isBatchMode;

#if UNITY_STANDALONE_LINUX_API
		IsHeadless = true;
#endif

		if (IsHeadless)
		{
			DestroyImmediate(this.GetComponent<UImGui.UImGui>());
			DestroyImmediate(this);
			return;
		}
	}

	private void OnLayout(UImGui.UImGui obj)
	{
		bool IsHeadless = Application.isBatchMode;

#if UNITY_STANDALONE_LINUX_API
		IsHeadless = true;
#endif

		if (IsHeadless)
		{
			DestroyImmediate(this.GetComponent<UImGui.UImGui>());
			DestroyImmediate(this);
			return;
		}
		ImGui.ShowDemoWindow();
	}
}
