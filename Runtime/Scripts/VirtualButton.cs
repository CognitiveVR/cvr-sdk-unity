using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

//SetPointerFocus called from ControllerPointer, or SetGazeFocus from HMDPointer
//fills an buttonImage over time
//activates OnFill action after pointing at this button for a duration

namespace Cognitive3D
{
    enum ActivationType
    {
        PointerFallbackGaze,
        TriggerButton
    }

    [AddComponentMenu("Cognitive3D/Internal/Virtual Button")]
    public class VirtualButton : MonoBehaviour, IPointerFocus, IGazeFocus
    {
        public Image fillImage;
        public Image buttonImage;
        public float FillDuration = 1;
        public Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color selectedColor = new Color(0, 1, 0.05f, 1);
        //limits the button to certain types of pointers
        private ActivationType ActivationType;
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
            {
                fillStartingColor = fillImage.color;
            }

            if (FindObjectOfType<ExitPollHolder>().Parameters.PointerType == ExitPoll.PointerType.HMDPointer)
            {
                ActivationType = ActivationType.PointerFallbackGaze;
            }
            else
            {
                ActivationType = ActivationType.TriggerButton;
            }
        }
        
        //this is called from update in the ControllerPointer script
        public virtual void SetPointerFocus(bool isRightHand)
        {
            float triggerValue;
            if (canActivate == false)
            {
                return;
            }
            if (ActivationType == ActivationType.TriggerButton)
            {
                if (isRightHand)
                {
                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
                }
                else
                {
                    InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
                }

                focusThisFrame = triggerValue > 0.5;
            }
            else
            {
                focusThisFrame = true;
            }
        }

        //this is called from update in the HMDPointer script
        public virtual void SetGazeFocus()
        {
            if (ActivationType != ActivationType.PointerFallbackGaze || (ActivationType == ActivationType.PointerFallbackGaze && Cognitive3D.GameplayReferences.DoesPointerExistInScene() == false))
            {
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

        //increase the fill amount if this buttonImage was focused this frame. calls OnConfirm if past threshold
        protected virtual void LateUpdate()
        {

            Debug.Log("@@ The Activation Type is " + ActivationType);

            if (!gameObject.activeInHierarchy) { return; }
            if (canActivate && focusThisFrame && ActivationType == ActivationType.TriggerButton)
            {
                StartCoroutine(FilledEvent());
                OnConfirm.Invoke();
            }
            if (!canActivate && FillAmount <= 0f && ActivationType != ActivationType.TriggerButton)
            {
                canActivate = true;
            }
            if (!focusThisFrame && canActivate)
            {
                FillAmount -= Time.deltaTime;
                FillAmount = Mathf.Clamp(FillAmount, 0, FillDuration);
            }
            else if (focusThisFrame && canActivate && (ActivationType != ActivationType.TriggerButton))
            {
                FillAmount += Time.deltaTime;
            }

            if (fillImage != null && (ActivationType != ActivationType.TriggerButton))
                fillImage.fillAmount = FillAmount / FillDuration;
            focusThisFrame = false;

            if (FillAmount > FillDuration && canActivate && (ActivationType != ActivationType.TriggerButton))
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

        public void SetSelect(bool select)
        {
            buttonImage.color = select ? selectedColor : defaultColor;
        }

        public void ToggleButtonEnable(bool enabled)
        {

        }
    }
}