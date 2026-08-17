using Bpsim.Parts;
using Bpsim.Parts.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Bpsim.UI
{
	internal class SimulationInterface : UnitySingleton<SimulationInterface>
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

		[SerializeField]
		private GameObject m_panel;

		[SerializeField]
		private SceneScreen m_sceneScreen;

		[SerializeField]
		private GameObject m_expander;

		[SerializeField]
		private Text m_text;

		[SerializeField]
		private Button m_backButton;

		[SerializeField]
		private Button m_expandButton;

		[SerializeField]
		private PartButtonList m_partButtonListPrefab;

		private bool m_expanding;

		private UserInterface.FPSCounter m_fpsCounter;

		private PartButtonList m_partButtonList;

		public SceneScreen SceneScreen => m_sceneScreen;

		public new static SimulationInterface Instance => UnitySingleton<SimulationInterface>.Instance;

		protected override void Awake()
		{
			base.Awake();
			m_backButton.onClick.AddListener(EndSimulation);
			m_expandButton.onClick.AddListener(Expand);
			m_expanding = false;
			m_fpsCounter = new UserInterface.FPSCounter(0.5f);
			m_fpsCounter.Start();
		}

		private void Update()
		{
			m_panel.gameObject.SetActive(PartManager.Instance.IsSimulating);
			m_fpsCounter.Tick();
			if (m_expanding && PartManager.Instance.IsSimulating)
			{
				Reference<PartSimulatorUnmanaged> unmanagedRef = PartManager.Instance.PartSimulator.UnmanagedRef;
				m_text.text = $"Fps: {m_fpsCounter.FPS}\nParts: {unmanagedRef.Value.Parts.Length}\nJoints: {unmanagedRef.Value.Joints.Length}\n";
			}
			if (PartManager.Instance.IsSimulating)
			{
				if (m_partButtonList == null)
				{
					m_partButtonList = Object.Instantiate(m_partButtonListPrefab);
					m_partButtonList.transform.SetParent(m_panel.transform, worldPositionStays: false);
					m_partButtonList.Initialize();
				}
				ProcessTouch();
			}
		}

		private void EndSimulation()
		{
			PartManager.Instance.EndSimulation();
			Object.Destroy(m_partButtonList.gameObject);
		}

		private void Expand()
		{
			m_expanding = !m_expanding;
			m_expander.SetActive(m_expanding);
		}

		public void ProcessTouch()
		{
			PointerData pointer = GetPointer();
			if (pointer.IsPressed)
			{
				PartSimulation.ProcessTouch(PartManager.Instance.PartSimulator.UnmanagedRef, pointer.Point);
			}
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
				if (EventSystem.current.IsPointerOverUIObject(touch.screenPosition))
				{
					return PointerData.Empty;
				}
				float scroll = 0f;
				if (activeTouches.Count >= 2)
				{
					UnityEngine.InputSystem.EnhancedTouch.Touch touch2 = activeTouches[1];
					Vector2 vector = touch.screenPosition - touch2.screenPosition;
					Vector2 vector2 = vector - (touch.delta - touch2.delta);
					scroll = vector.magnitude - vector2.magnitude;
				}
				return new PointerData(pressed: true, touch.screenPosition, touch.delta, scroll, (float)touch.time);
			}
			if (Mouse.current != null)
			{
				Mouse current = Mouse.current;
				Vector2 vector3 = current.position.ReadValue();
				if (EventSystem.current.IsPointerOverUIObject(vector3))
				{
					return PointerData.Empty;
				}
				Vector2 delta = current.delta.ReadValue();
				float scroll2 = current.scroll.ReadValue().y / 120f;
				return new PointerData(current.leftButton.isPressed, vector3, delta, scroll2, Time.unscaledTime);
			}
			return PointerData.Empty;
		}
	}
}
