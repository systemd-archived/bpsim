using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;

namespace Bpsim.UI
{
	internal class SceneScreen : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public readonly struct PointerData
		{
			public readonly bool HasValue;

			public readonly bool IsPressed;

			public readonly Vector2 Point;

			public readonly Vector2 Delta;

			public readonly float Scroll;

			public readonly float Time;

			public static PointerData Empty => default(PointerData);

			public PointerData(bool pressed, Vector2 point, Vector2 delta, float scroll, float time)
			{
				HasValue = true;
				IsPressed = pressed;
				Point = point;
				Delta = delta;
				Scroll = scroll;
				Time = time;
			}
		}

		private PointerData m_previousPointer;

		private PointerData m_pointer;

		private ClickCounter m_counter;

		public PointerData PreviousPointer => m_previousPointer;

		public PointerData Pointer => m_pointer;

		public event Action<PointerEventData> DoubleClicked;

		private void Awake()
		{
			m_counter = new ClickCounter(0.3f);
			if (Touchscreen.current != null && !EnhancedTouchSupport.enabled)
			{
				EnhancedTouchSupport.Enable();
			}
		}

		private void Update()
		{
			m_previousPointer = m_pointer;
			PointerData pointer = GetPointer();
			m_pointer = pointer;
		}

		private PointerData GetPointer()
		{
			if (Touchscreen.current != null)
			{
				ReadOnlyArray<UnityEngine.InputSystem.EnhancedTouch.Touch> activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
				if (activeTouches.Count == 0)
				{
					return PointerData.Empty;
				}
				UnityEngine.InputSystem.EnhancedTouch.Touch touch = activeTouches[0];
				if (EventSystem.current.IsPointerOverUIObject(touch.screenPosition, out var raycastResult) && raycastResult.gameObject != base.gameObject)
				{
					return PointerData.Empty;
				}
				float scroll = 0f;
				if (activeTouches.Count >= 2)
				{
					UnityEngine.InputSystem.EnhancedTouch.Touch touch2 = activeTouches[1];
					Vector2 vector = touch.screenPosition - touch2.screenPosition;
					Vector2 vector2 = vector - (touch.delta - touch2.delta);
					scroll = Math.Clamp(10f * (vector.magnitude / (vector2.magnitude + 1f) - 1f), -1f, 1f);
				}
				return new PointerData(activeTouches.Count == 1, touch.screenPosition, touch.delta, scroll, (float)touch.time);
			}
			if (Mouse.current != null)
			{
				Mouse current = Mouse.current;
				Vector2 vector3 = current.position.ReadValue();
				if (EventSystem.current.IsPointerOverUIObject(vector3, out var raycastResult2) && raycastResult2.gameObject != base.gameObject)
				{
					return PointerData.Empty;
				}
				Vector2 delta = current.delta.ReadValue();
				float scroll2 = current.scroll.ReadValue().y / 120f;
				return new PointerData(current.leftButton.isPressed, vector3, delta, scroll2, Time.unscaledTime);
			}
			return PointerData.Empty;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			m_counter.Click();
			if (m_counter.ClickCount == 2)
			{
				this.DoubleClicked?.Invoke(eventData);
			}
		}
	}
}
