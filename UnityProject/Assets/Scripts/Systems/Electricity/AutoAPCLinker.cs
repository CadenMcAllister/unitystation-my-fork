﻿using System;
using Logs;
using NaughtyAttributes;
using Objects.Engineering;
using SecureStuff;
using Systems.Electricity;
using UnityEngine;

namespace Systems.Scenes.Electricity
{
	public class AutoAPCLinker : MonoBehaviour
	{
		[SerializeField] private APCPoweredDevice targetDevice;
		[SerializeField] private float scanRadius;
		[SerializeField] private bool verboseDebugging;

		private void Awake()
		{
			if (CustomNetworkManager.IsServer == false) return;
			EventManager.AddHandler(Event.RoundStarted, UpdateAPCStatus);
		}

		private void OnDestroy()
		{
			EventManager.RemoveHandler(Event.RoundStarted, UpdateAPCStatus);
		}

		[Button()]
		private void UpdateAPCStatus()
		{
			if (targetDevice.RelatedAPC != null)
			{
				if (verboseDebugging) Loggy.Warning("[AutoAPCLinker] - " +
				                                        "Device already has a related APC. Skipping..", Category.Electrical);
				return;
			}
			var result = Physics2D.CircleCastAll(targetDevice.gameObject.AssumedWorldPosServer(), scanRadius, Vector2.zero);
			foreach (var hit in result)
			{
				if (hit.transform.gameObject.TryGetComponent<APC>(out var apc) == false) continue;
				apc.AddDevice(targetDevice);
				if (verboseDebugging)
				{
					Loggy.Info($"[AutoAPCLinker] - Found APC for {targetDevice.gameObject.ExpensiveName()} " +
					           $"at {apc.gameObject.AssumedWorldPosServer()}", Category.Electrical);
				}
				break;
			}
		}
	}
}