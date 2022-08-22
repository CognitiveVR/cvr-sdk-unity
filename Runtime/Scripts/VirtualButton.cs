using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//SetPointerFocus called from ControllerPointer, or SetGazeFocus from HMDPointer
//fills an image over time
//activates OnFill action after pointing at this button for a duration

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
        //limits the button to certain types of pointers
        public ActivationType ActivationType;
        [UnityEngine.Serialization.FormerlySerializedAs("OnFill")]
        public UnityEngine.Events.UnityEvent OnConfirm;

        protected float FillAmount;
        protected bool focusThisFrame = false;
        protected bool canActivate = true;
        protected Color fillStartingColor;

        public MonoBehaviour MonoBehaviour { get { return this; } }

        //save the fill starting color
        protected virtual void Start()
        {
            if (fillImage != null)
                fillStartingColor = fillImage.color;
        }
        
        //this is called from update in the ControllerPointer script
        public virtual void SetPointerFocus()
        {
            if (ActivationType == ActivationType.Gaze) { return; }
            if (canActivate == false)
            {
                return;
            }

            focusThisFrame = true;
        }

        //this is called from update in the HMDPointer script
        public virtual void SetGazeFocus()
        {
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

        //used by ControllerPointer to draw a line to this button
        public virtual Vector3 GetPosition()
        {
            return transform.position;
        }

        //increase the fill amount if this image was focused this frame. calls OnConfirm if past threshold
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

            if (fillImage != null)
                fillImage.fillAmount = FillAmount / FillDuration;
            focusThisFrame = false;

            if (FillAmount > FillDuration && canActivate)
            {
                canActivate = false;
                StartCoroutine(FilledEvent());
                OnConfirm.Invoke();
            }
        }

        //when filled, change the color of the fill ring to black for half a second before returning to starting color
        protected virtual IEnumerator FilledEvent()
        {
            float t = 0;
            float duration = 0.5f;
            while (t < 1)
            {
                yield return null;
                t += Time.deltaTime / duration;
                if (fillImage != null)
                    fillImage.color = Color.black;
            }
            FillAmount = 0;
            yield return null;
            if (fillImage != null)
                fillImage.color = fillStartingColor;
        }
    }
}