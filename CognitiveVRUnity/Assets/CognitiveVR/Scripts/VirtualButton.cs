using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//player calls 'set focus' from simple pointer
//can be extended to load scenes, do other actions, etc

namespace CognitiveVR
{
    public enum ActivationType
    {
        Pointer,
        PointerFallbackGaze,
        Gaze,
        Any
    }

    public class VirtualButton : MonoBehaviour, IPointerFocus, IGazeFocus
    {
        public Image fillImage;
        public float FillDuration = 1;
        public ActivationType ActivationType;
        public UnityEngine.Events.UnityEvent OnFill;

        protected float FillAmount;
        protected bool focusThisFrame = false;
        protected bool canActivate = true;
        protected Color fillStartingColor;

        public MonoBehaviour MonoBehaviour { get { return this; } }

        //save the fill starting color
        protected virtual void Start()
        {
            fillStartingColor = fillImage.color;
        }

        //this is called from update in the simplepointer script
        public virtual void SetPointerFocus()
        {
            if (ActivationType == ActivationType.Gaze) { return; }
            if (canActivate == false)
            {
                return;
            }

            focusThisFrame = true;
        }

        public virtual void SetGazeFocus()
        {
            //how to tell if pointers don't exist?

            if (ActivationType != ActivationType.PointerFallbackGaze || (ActivationType == ActivationType.PointerFallbackGaze && CognitiveVR.GameplayReferences.DoesPointerExistInScene() == false))
            {
                if (ActivationType == ActivationType.Pointer) { return; }
                if (canActivate == false)
                {
                    return;
                }

                focusThisFrame = true;
            }
        }

        //used to draw a line to this button
        public virtual Vector3 GetPosition()
        {
            return transform.position;
        }

        //increase the fill amount if this button was focused this frame
        protected virtual void LateUpdate()
        {
            if (!gameObject.activeInHierarchy) { return; }
            if (!canActivate && FillAmount <= 0f)
            {
                canActivate = true;
            }
            if (!focusThisFrame && canActivate)
            {
                FillAmount -= Time.deltaTime;
                FillAmount = Mathf.Clamp(FillAmount, 0, FillDuration);
            }
            else if (focusThisFrame && canActivate)
            {
                FillAmount += Time.deltaTime;
            }

            fillImage.fillAmount = FillAmount / FillDuration;
            focusThisFrame = false;

            if (FillAmount > FillDuration && canActivate)
            {
                canActivate = false;
                StartCoroutine(FilledEvent());
                OnFill.Invoke();
            }
        }

        //when filled, change the colour of the fill ring
        protected virtual IEnumerator FilledEvent()
        {
            float t = 0;
            float duration = 0.5f;
            while (t < 1)
            {
                yield return null;
                t += Time.deltaTime / duration;
                fillImage.color = Color.black;
            }
            FillAmount = 0;
            yield return null;
            fillImage.color = fillStartingColor;
        }
    }
}