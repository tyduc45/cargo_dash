using System.Collections;
using UnityEngine;

public abstract class SceneTransition : MonoBehaviour
{
    public abstract IEnumerator AnimatorTransitionIn();
    public abstract IEnumerator AnimatorTransitionOut();
    
}
