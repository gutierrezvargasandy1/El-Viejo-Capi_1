using UnityEngine;

public class SonidoCaminar : StateMachineBehaviour
{
    AudioSource fuente;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo info, int capa)
    {
        if (fuente == null) fuente = animator.GetComponent<AudioSource>();
        if (fuente != null && !fuente.isPlaying) fuente.Play();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo info, int capa)
    {
        if (fuente != null) fuente.Stop();
    }
}