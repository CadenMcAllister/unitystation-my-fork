using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Communications;

namespace Items.Weapons
{
	/// <summary>
	/// Interaction script for explosives that can go on walls, objects, players, etc.
	/// </summary>
	public class AttachableExplosive : ExplosiveBase,
		ICheckedInteractable<PositionalHandApply>, IRightClickable, IInteractable<InventoryApply>
	{
		[SyncVar] private bool isOnObject = false;
		private GameObject attachedToObject;

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateBombPosition);
		}

		[Server]
		private void AttachExplosive(GameObject target, Vector2 targetPostion)
		{
			if (target.TryGetComponent<PushPull>(out var handler))
			{
				Inventory.ServerDrop(pickupable.ItemSlot, targetPostion);
				attachedToObject = target;
				UpdateManager.Add(UpdateBombPosition, 0.1f);
				scaleSync.SetScale(new Vector3(0.6f, 0.6f, 0.6f));
				return;
			}

			Inventory.ServerDrop(pickupable.ItemSlot, targetPostion);
			//Visual feedback to indicate that it's been attached and not just dropped.
			scaleSync.SetScale(new Vector3(0.6f, 0.6f, 0.6f));
		}

		private void UpdateBombPosition()
		{
			if(attachedToObject == null) return;
			if(attachedToObject.WorldPosServer() == gameObject.WorldPosServer()) return;
			registerItem.customNetTransform.SetPosition(attachedToObject.WorldPosServer());
		}

		[Command(requiresAuthority = false)]
		private void CmdTellServerToDeattachExplosive()
		{
			DeAttachExplosive();
		}

		[Server]
		private void DeAttachExplosive()
		{
			isOnObject = false;
			pickupable.ServerSetCanPickup(true);
			objectBehaviour.ServerSetPushable(true);
			scaleSync.SetScale(new Vector3(1f, 1f, 1f));
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateBombPosition);
			attachedToObject = null;
		}

		/// <summary>
		/// checks to see if we can attach the explosive to an object.
		/// </summary>
		private bool CanAttatchToTarget(Matrix matrix, RegisterTile tile)
		{
			return matrix.Get<RegisterDoor>(tile.WorldPositionServer, true).Any() || matrix.Get<Pickupable>(tile.WorldPositionServer, true).Any();
		}


		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false
			    || isArmed == true || pickupable.ItemSlot == null && isOnObject == false) return false;
			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			void Perform()
			{
				if (interaction.TargetObject?.OrNull().RegisterTile()?.OrNull().Matrix?.OrNull() != null)
				{
					var matrix = interaction.TargetObject.RegisterTile().Matrix;
					var tiles = matrix.GetRegisterTile(interaction.TargetObject.TileLocalPosition().To3Int(), true);
					//Check to see if we're trying to attach the object to things we're not supposed to
					//because we don't want stuff like this happening :
					//https://youtu.be/0Yu8hEBMRwc
					foreach (var registerTile in tiles)
					{
						if (CanAttatchToTarget(registerTile.Matrix, registerTile) == false) continue;
						Chat.AddExamineMsg(interaction.Performer,
							$"The {interaction.TargetObject.ExpensiveName()} isn't a good spot to arm the explosive on..");
						return;
					}
				}

				AttachExplosive(interaction.TargetObject, interaction.TargetVector);
				isOnObject = true;
				pickupable.ServerSetCanPickup(false);
				objectBehaviour.ServerSetPushable(false);
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You attach the {gameObject.ExpensiveName()} to {interaction.TargetObject.ExpensiveName()}",
					$"{interaction.PerformerPlayerScript.visibleName} attaches a {gameObject.ExpensiveName()} to {interaction.TargetObject.ExpensiveName()}!");
			}

			//incase we forgot to pair while the C4 is on the wall
			if (interaction.UsedObject != null && interaction.UsedObject.TryGetComponent<SignalEmitter>(out var emitter))
			{
				Emitter = emitter;
				Frequency = emitter.Frequency;
				Chat.AddExamineMsg(interaction.Performer, "You successfully pair the remote signal to the device.");
				return;
			}

			//For interacting with the explosive while it's on a wall.
			if (isOnObject || interaction.IsAltClick)
			{
				explosiveGUI.ServerPerformInteraction(interaction);
				return;
			}

			//The progress bar that triggers Preform()
			//Must not be interrupted for it to work.
			var bar = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Perform);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), progressTime, interaction.Performer);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.TargetSlot.IsEmpty == false)
			{
				if (interaction.TargetSlot.ItemObject.TryGetComponent<SignalEmitter>(out var emitter))
				{
					Emitter = emitter;
					Frequency = emitter.Frequency;
					Chat.AddExamineMsg(interaction.Performer, "You successfully pair the remote signal to the device.");
				}
			}
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = new RightClickableResult();
			if (isOnObject == false) return result;
			if (CustomNetworkManager.IsServer)
			{
				result.AddElement("Deattach", DeAttachExplosive);
			}
			else
			{
				result.AddElement("Deattach", CmdTellServerToDeattachExplosive);
			}
			return result;
		}
	}
}