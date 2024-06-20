using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

//SetPointerFocus called from ControllerPointer, or SetGazeFocus from HMDPointer
//fills an buttonImage over time
//activates OnFill action after pointing at this button for a duration

namespace Cognitive3D
{
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
        protected bool slowFill = true;
        protected Color fillStartingColor;
        protected float triggerValue;
        protected bool isUsingRightHand;
        private readonly Color confirmColor = new Color(0.12f, 0.64f, 0.96f, 1f);
        private ExitPollHolder currentExitPollHolder;

        public MonoBehaviour MonoBehaviour { get { return this; } }

        //save the fill starting color
        protected virtual void Start()
        {
            currentExitPollHolder = FindObjectOfType<ExitPollHolder>();
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
        public virtual void SetPointerFocus(bool isRightHand, bool activation, bool fill)
        {
            if (canActivate == false)
            {
                return;
            }
            isUsingRightHand = isRightHand;
            focusThisFrame = activation;
            slowFill = fill;
        }

        //this is called from update in the HMDPointer script
        public virtual void SetGazeFocus()
        {
            if (canActivate == false)
            {
                return;
            }
            if (slowFill)
            {
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

                // Button interactable and focused
                if (canActivate && focusThisFrame)
                {
                    // Immediately "click": usually used by controller
                    if (!slowFill)
                    {
                        StartCoroutine(FilledEvent());
                        OnConfirm.Invoke();
                    }
                    else // Increment the gradual fill: usually used by hand and hmd gaze
                    {
                        FillAmount += Time.deltaTime;
                        // Fill complete, thus "click"
                        if (FillAmount > FillDuration)
                        {
                            canActivate = false;
                            StartCoroutine(FilledEvent());
                            OnConfirm.Invoke();
                        }
                    }
                }

                // Make it interactable again
                if (!canActivate && FillAmount <= 0f && slowFill)
                {
                    canActivate = true; 
                }


                // Button interactable and not focused: unfill the fill
                if (!focusThisFrame && canActivate)
                {
                    FillAmount -= Time.deltaTime;
                    FillAmount = Mathf.Clamp(FillAmount, 0, FillDuration);
                }

                // Update the button image
                if (fillImage != null && slowFill)
                {
                    fillImage.fillAmount = FillAmount / FillDuration;
                }

                focusThisFrame = false;
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

        private void DynamicallyResize()
        {
            var rect = rectTransform.rect;
            boxCollider.size = rect.size;
        }
    }
}