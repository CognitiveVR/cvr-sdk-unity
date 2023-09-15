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
        [UnityEngine.Serialization.FormerlySerializedAs("OnFill")]
        public UnityEngine.Events.UnityEvent OnConfirm;
        public bool dynamicallyResize;
        public bool isEnabled = true;

        public BoxCollider boxCollider;
        public RectTransform rectTransform;

    [HideInInspector]
        public bool isSelected;

        protected float FillAmount;
        protected bool focusThisFrame = false;
        protected bool canActivate = true;
        protected Color fillStartingColor;
        protected float triggerValue;
        protected bool isUsingRightHand;
        private ActivationType activationType;
        private readonly Color confirmColor = new Color(0.12f, 0.64f, 0.96f, 1f);
        private ExitPollHolder currentExitPollHolder;

        public MonoBehaviour MonoBehaviour { get { return this; } }

        //save the fill starting color
        protected virtual void Start()
        {
            currentExitPollHolder = FindObjectOfType<ExitPollHolder>();
            activationType = GetCurrentActivationType(currentExitPollHolder);
            if (fillImage != null)
            {
                fillStartingColor = fillImage.color;
            }

            // Rects Driven by layout don't have a value in Start()
            StartCoroutine(WaitOneFrame());
        }

        IEnumerator WaitOneFrame()
        {
            yield return new WaitForEndOfFrame();
            if (dynamicallyResize)
            {
                DynamicallyResize();
            }
        }

        //this is called from update in the ControllerPointer script
        public virtual void SetPointerFocus(bool isRightHand)
        {
            isUsingRightHand = isRightHand;
            if (canActivate == false)
            {
                return;
            }
            if (activationType == ActivationType.TriggerButton)
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
            if (activationType != ActivationType.PointerFallbackGaze 
                || (activationType == ActivationType.PointerFallbackGaze && Cognitive3D.GameplayReferences.DoesPointerExistInScene() == false))
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
            if (isEnabled && !isSelected)
            {
                if (!gameObject.activeInHierarchy) { return; }
                if (canActivate && focusThisFrame && activationType == ActivationType.TriggerButton)
                {
                    StartCoroutine(FilledEvent());
                    OnConfirm.Invoke();
                }
                if (!canActivate && FillAmount <= 0f && activationType != ActivationType.TriggerButton)
                {
                    canActivate = true;
                }
                if (!focusThisFrame && canActivate)
                {
                    FillAmount -= Time.deltaTime;
                    FillAmount = Mathf.Clamp(FillAmount, 0, FillDuration);
                }
                else if (focusThisFrame && canActivate && (activationType != ActivationType.TriggerButton))
                {
                    FillAmount += Time.deltaTime;
                }

                if (fillImage != null && (activationType != ActivationType.TriggerButton))
                    fillImage.fillAmount = FillAmount / FillDuration;
                focusThisFrame = false;

                if (FillAmount > FillDuration && canActivate && (activationType != ActivationType.TriggerButton))
                {
                    canActivate = false;
                    StartCoroutine(FilledEvent());
                    OnConfirm.Invoke();
                }
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
            isSelected = select;
            buttonImage.color = select ? selectedColor : defaultColor;
        }

        public void SetConfirmEnabled()
        {
            buttonImage.color = confirmColor;
            isEnabled = true;
        }

        private ActivationType GetCurrentActivationType(ExitPollHolder exitPollHolder)
        {

            if (exitPollHolder.Parameters.PointerType == ExitPoll.PointerType.HMDPointer)
            {
                return ActivationType.PointerFallbackGaze;
            }
            else
            {
                return ActivationType.TriggerButton;
            }
        }

        private void DynamicallyResize()
        {
            var rect = rectTransform.rect;
            boxCollider.size = rect.size;
        }
    }
}