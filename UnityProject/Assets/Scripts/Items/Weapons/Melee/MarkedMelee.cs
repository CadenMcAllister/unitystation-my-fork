using System.Text;
using UnityEngine;
using Items;
using Systems.StatusesAndEffects.Implementations;

namespace Weapons
{
	/// <summary>
	/// Adding this to a weapon allows it to mark enemies and do bonus damage when they are marked. 
	/// </summary>
	public class MarkedMelee : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField]
		private Marked statusEffect;
		
		[SerializeField] private float markedHitBonus;
		[SerializeField] private float backstabBonus;
		[SerializeField] private float pushForce;
		
		[SerializeField] private bool reqWield = false;
		[SerializeField] private bool doPush = false;

		private bool isCooldown;
		
		private ItemAttributesV2 attribs;
		private Wieldable wieldable;
		
		void Awake()
		{
			attribs = gameObject.GetComponent<ItemAttributesV2>();
			wieldable = gameObject.GetComponent<Wieldable>();
		}
		
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return interaction.UsedObject == gameObject && interaction.Intent == Intent.Harm;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.Intent != Intent.Harm) return;
			
			GameObject target = interaction.TargetObject;
			GameObject performer = interaction.Performer;
			
			if (reqWield)
			{
				if (wieldable.IsWielded == false)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You need to be wielding this to attack");
				}
			}
			
			var originalHitDamage = attribs.ServerHitDamage;
			
			var targetPlayerScript = target.GetComponent<PlayerScript>();
			var mark = Instantiate(statusEffect);
			if (targetPlayerScript != null && targetPlayerScript.StatusEffectManager.HasStatus(mark))
			{
				var damageTotal = markedHitBonus + originalHitDamage;
				
				var targetDir = targetPlayerScript.PlayerDirectional.CurrentDirection;
				var performerDir = interaction.PerformerPlayerScript.PlayerDirectional.CurrentDirection;

				//Backstabbing
				if (targetDir.Equals(performerDir))
				{
					damageTotal += backstabBonus;
				}
				
				attribs.ServerHitDamage = damageTotal;
			}

			Vector2 dir = (target.transform.position - performer.transform.position).normalized;
			
			WeaponNetworkActions wna = performer.GetComponent<WeaponNetworkActions>();
			wna.ServerPerformMeleeAttack(target, dir, interaction.TargetBodyPart, LayerType.None, OnHit);
			
			attribs.ServerHitDamage = originalHitDamage;
			
			void OnHit()
			{
				if (doPush)
				{
					var objPhys = target.GetComponent<UniversalObjectPhysics>();
					
					if (objPhys != null)
					{
						objPhys.NewtonianPush(dir, pushForce, 1, 0);
					}
				}
				
				if (targetPlayerScript != null)
				{
					targetPlayerScript.StatusEffectManager.RemoveStatus(mark);
				}				
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			StringBuilder exam = new StringBuilder();
			exam.AppendLine($"Mark a creature with a destabilizing force using the projectile, then hit them with melee to do {attribs.ServerHitDamage + markedHitBonus}")
				.AppendLine($"Does {attribs.ServerHitDamage + markedHitBonus + backstabBonus} damage instead if the target is backstabbed.");
			return exam.ToString();
		}
	}
}