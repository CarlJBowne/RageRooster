using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateBase : StateMachineBehaviour
{
	protected PlayerStateMachine machine;
	protected GameObject gameObject;
	protected Transform transform;
	protected Animator anim;
	protected bool initialized;
	[SerializeField] protected float gravity;


	public void Initialize(PlayerStateMachine machine)
	{
		this.machine = machine;
		gameObject = machine.gameObject;
		transform = machine.transform;
		anim = machine.animator;
		OnInitialize();
	}

	public virtual void OnInitialize() { }

}
