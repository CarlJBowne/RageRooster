using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
	public Animator animator;
	private PlayerStateBase currentState;


	private void Awake()
	{
		animator = GetComponent<Animator>();
		PlayerStateBase[] animatorStates = animator.GetBehaviours<PlayerStateBase>();
		foreach (PlayerStateBase state in animatorStates) state.Initialize(this);
	}
}
