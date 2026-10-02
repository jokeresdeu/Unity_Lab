using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.PixelFantasy.PixelHeroes.Common.Scripts.UI
{
    public class Dialog : MonoBehaviour
    {
        public CanvasGroup CanvasGroup;
        public Text Message;
        public GameObject CancelButton;

        private Action _onConfirm, _onCancel;

        public static Dialog Instance;

        public void Awake()
        {
            Instance = this;
        }

        public void Show(string message, Action onConfirm = null, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            Message.text = message;
            CancelButton.SetActive(onCancel != null);
        }

        public void Confirm()
        {
            _onConfirm?.Invoke();
            StartCoroutine(Hide());
        }

        public void Cancel()
        {
            _onCancel?.Invoke();
            StartCoroutine(Hide());
        }

        private IEnumerator Hide()
        {
            while (CanvasGroup.alpha > 0)
            {
                CanvasGroup.alpha -= 4 * Time.fixedDeltaTime;

                yield return null;
            }

            CanvasGroup.blocksRaycasts = false;
        }
    }
}