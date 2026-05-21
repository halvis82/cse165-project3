using UnityEngine;

public sealed class AgentAnimationDriver : MonoBehaviour
{
    [SerializeField] private AgentNavigator navigator;
    [SerializeField] private Animator animator;
    [SerializeField] private float movingAnimatorSpeed = 1f;

    public void Configure(AgentNavigator agentNavigator, Animator targetAnimator)
    {
        navigator = agentNavigator;
        animator = targetAnimator;
    }

    private void LateUpdate()
    {
        if (animator == null || navigator == null)
        {
            return;
        }

        animator.speed = navigator.IsMoving ? movingAnimatorSpeed : 0f;
    }
}
