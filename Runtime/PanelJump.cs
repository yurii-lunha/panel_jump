using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Lunha.Panels
{
    /// <summary>
    /// Controls the visible of a RectTransform or Transform on the screen.
    /// </summary>
    public sealed class PanelJump : MonoBehaviour
    {
        public enum DirectionToHide
        {
            None,
            Up,
            Down,
            Left,
            Right,
            Center
        }

        #region Events

        public event Action BeforeShow;
        public event Action Shown;
        public event Action BeforeHide;
        public event Action Hidden;

        public UnityEvent beforeShow;
        public UnityEvent shown;
        public UnityEvent beforeHide;
        public UnityEvent hidden;

        #endregion

        #region Inspector

        [SerializeField] private DirectionToHide directionToHide;
        [SerializeField] private bool quickHideOnStart;

        [SerializeField, Tooltip(
             "Size used to to hide|show Transform panel. Do not fill for None direction and RectTransform.")]
        private Vector2 size = Vector2.zero;

        [SerializeField] private Vector2 hideAddPercent;

        [SerializeField, Range(.3f, 2f), Tooltip("Duration of show")]
        private float showDuration = 0.8f;

        [SerializeField, Range(.3f, 2f), Tooltip("Duration of hide")]
        private float hideDuration = 0.5f;

        [SerializeField] private AnimationCurve showCurve;
        [SerializeField] private AnimationCurve hideCurve;

        [Space, SerializeField] private bool testMode;

        #endregion

        #region Close

        #region Var

        private Transform _panel;
        private RectTransform _rectPanel;

        private Vector3 _defLocalPosition = Vector3.zero;
        private Vector3 _defLocalPositionHide = Vector3.zero;

        private bool _isRect;

        private Coroutine _processingRoutine;

        public bool IsVisible { private set; get; }

        private Vector2 PanelPosition
        {
            get => _isRect ? _rectPanel.anchoredPosition : (Vector2)_panel.localPosition;
            set
            {
                if (_isRect)
                {
                    _rectPanel.anchoredPosition = value;
                }
                else
                {
                    _panel.localPosition = value;
                }
            }
        }

        #endregion

        #region Events Invokators

        private void OnBeforeShow()
        {
            BeforeShow?.Invoke();
            beforeShow?.Invoke();
        }

        private void OnShown()
        {
            Shown?.Invoke();
            shown?.Invoke();
        }

        private void OnBeforeHide()
        {
            BeforeHide?.Invoke();
            beforeHide?.Invoke();
        }

        private void OnHidden()
        {
            Hidden?.Invoke();
            hidden?.Invoke();
        }

        #endregion

        private void Reset()
        {
            if (showCurve == null || showCurve.length == 0)
            {
                showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (hideCurve == null || hideCurve.length == 0)
            {
                hideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }

        private void Awake()
        {
            _rectPanel = GetComponent<RectTransform>();
            _panel = transform;

            _isRect = _rectPanel;

            PrepareDefaultPositions();
        }

        private void Start()
        {
            if (quickHideOnStart)
            {
                Hide(quick: true);
            }

            Test();
        }

        private void Test()
        {
            if (!testMode) return;

            Invoke(nameof(Toggle), 2f);
            Invoke(nameof(Test), 5f);
        }

        private void PrepareDefaultPositions()
        {
            var rectSize = _isRect ? _rectPanel.rect.size : Vector2.zero;

            _defLocalPosition = PanelPosition;

            var addSize = size + rectSize;

            if (hideAddPercent.x > 0f)
            {
                addSize.x += (addSize.x / 100f) * hideAddPercent.x;
            }

            if (hideAddPercent.y > 0f)
            {
                addSize.y += (addSize.y / 100f) * hideAddPercent.y;
            }

            _defLocalPositionHide = directionToHide switch
            {
                DirectionToHide.Up => new Vector3(_defLocalPosition.x, _defLocalPosition.y + addSize.y),
                DirectionToHide.Down => new Vector3(_defLocalPosition.x, _defLocalPosition.y - addSize.y),
                DirectionToHide.Left => new Vector3(_defLocalPosition.x - addSize.x, _defLocalPosition.y),
                DirectionToHide.Right => new Vector3(_defLocalPosition.x + addSize.x, _defLocalPosition.y),
                DirectionToHide.None => _defLocalPosition,
                _ => _defLocalPositionHide
            };
        }

        private IEnumerator ChangeVisibleRoutine(bool visible, Action callback = null, bool quick = false)
        {
            IsVisible = visible;

            if (visible)
            {
                OnBeforeShow();
            }
            else
            {
                OnBeforeHide();
            }

            if (quick)
            {
                PanelPosition = visible ? _defLocalPosition : _defLocalPositionHide;

                callback?.Invoke();

                yield break;
            }

            var basePosition = PanelPosition;
            var targetPosition = visible ? _defLocalPosition : _defLocalPositionHide;
            var deltaTime = Time.deltaTime / (visible ? showDuration : hideDuration);
            var curve = visible ? showCurve : hideCurve;

            for (var t = 0f; t <= 1f; t += deltaTime)
            {
                yield return null;

                var x = curve.Evaluate(t);

                PanelPosition = Vector3.Lerp(basePosition, targetPosition, x);
            }

            PanelPosition = targetPosition;

            callback?.Invoke();

            if (visible)
            {
                OnShown();
            }
            else
            {
                OnHidden();
            }
        }

        private void ClearRoutines()
        {
            if (_processingRoutine != null)
            {
                StopCoroutine(_processingRoutine);
            }
        }

        #endregion

        #region Open

        /// <summary>
        /// Toggle visibility.
        /// </summary>
        [ContextMenu("Change visible")]
        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Show panel
        /// </summary>
        /// <param name="callback">On complete action</param>
        /// <param name="quick">Move instantly OR use duration & animation curve</param>
        public void Show(Action callback = null, bool quick = false)
        {
            ClearRoutines();

            _processingRoutine = StartCoroutine(ChangeVisibleRoutine(true, callback, quick));
        }

        /// <summary>
        /// Hide panel
        /// </summary>
        /// <param name="callback">On complete action</param>
        /// <param name="quick">Move instantly OR use duration & animation curve</param>
        public void Hide(Action callback = null, bool quick = false)
        {
            ClearRoutines();

            _processingRoutine = StartCoroutine(ChangeVisibleRoutine(false, callback, quick));
        }

        /// <summary>
        /// Use to reconfigure a component after a real time move.
        /// </summary>
        [ContextMenu("Update positions")]
        public void UpdatePositions()
        {
            PrepareDefaultPositions();
        }

        /// <summary>
        /// Return configured visible & hide positions.
        /// </summary>
        /// <returns></returns>
        public (Vector3 visiblePosition, Vector3 hidePosition) GetPositions() =>
            (_defLocalPosition, _defLocalPositionHide);

        /// <summary>
        /// Set new visible & hide positions [in real time].
        /// * Invoke <see cref="UpdatePositions"/> to reconfigure component.
        /// </summary>
        /// <param name="visiblePosition">Default visible position</param>
        /// <param name="hidePosition">Hidden position</param>
        public void SetDefaultPosition(Vector3 visiblePosition, Vector3 hidePosition)
        {
            _defLocalPosition = visiblePosition;
            _defLocalPositionHide = hidePosition;
        }

        /// <summary>
        /// Set new direction to hide [in real time].
        /// * Invoke <see cref="UpdatePositions"/> to reconfigure component.
        /// </summary>
        /// <param name="direction">Direction to hide</param>
        public void SetDirectionToHide(DirectionToHide direction)
        {
            directionToHide = direction;
        }

        #endregion
    }
}