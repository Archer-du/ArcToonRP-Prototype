using UnityEngine;

namespace ArcToon.Scripts.Character
{
    public class CharacterAnimationController : MonoBehaviour
    {
        private Animator animator;
        private bool isPaused = false;
        
        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                TogglePause();
            }
        }
        
        private void TogglePause()
        {
            isPaused = !isPaused;
            
            if (animator != null)
            {
                animator.speed = isPaused ? 0f : 1f;
            }
        }
    }
}