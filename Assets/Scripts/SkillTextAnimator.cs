using TMPro;
using UnityEngine;

public class SkillTextAnimator : MonoBehaviour
{
    public TextMeshPro Text;

    public Animator m_animator;

    // destroy when animation ends
    public void Update()
    {
        if (m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1) Destroy(gameObject);
    }

    public void Play(string s, Vector3 position)
    {
        Text.SetText(s);
        transform.position = position;
        m_animator.SetTrigger("Play");
    }
}