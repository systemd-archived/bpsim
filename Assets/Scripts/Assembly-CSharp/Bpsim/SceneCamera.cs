using System;
using Bpsim.Parts;
using Bpsim.UI;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Bpsim
{
	public class SceneCamera : UnitySingleton<SceneCamera>
	{
		[SerializeField]
		private float m_minSize;

		[SerializeField]
		private float m_maxSize;

		private Camera m_camera;

		private Vector2 m_targetPoint;

		private Vector2 m_movementVelocity;

		private float m_scalingSpeed;

		private float m_targetSize;

		private Vector2 m_originalCameraTargetPoint;

		public Camera Camera => m_camera;

		public Vector2 TargetPoint => m_targetPoint;

		public float TargetSize => m_targetSize;

		public new static SceneCamera Instance => UnitySingleton<SceneCamera>.Instance;

		public void MoveTo(Vector2 position)
		{
			m_targetPoint = position;
			m_movementVelocity = Vector2.zero;
			base.transform.position = m_targetPoint.WithZ(base.transform.position.z);
		}

		public void ScaleTo(float size)
		{
			m_targetSize = size;
			m_scalingSpeed = 0f;
			m_camera.orthographicSize = size;
		}

		protected override void Awake()
		{
			base.Awake();
			m_camera = GetComponent<Camera>();
			m_targetPoint = base.transform.position;
			m_targetSize = m_camera.orthographicSize;
		}

		private void Start()
		{
			PartManager.Instance.SceneSelected += OnSceneSelected;
		}

		private bool CanMove()
		{
			if (UserInterface.Instance.PartOperator.Mode != PartOperationMode.View)
			{
				return PartManager.Instance.IsSimulating;
			}
			return true;
		}

		private void OnSceneSelected(PartScene partScene)
		{
			MoveTo(partScene.State.CameraPosition);
			ScaleTo(partScene.State.CameraSize);
		}

		private void Update()
		{
			float deltaTime = Time.deltaTime;
			SceneScreen obj = (PartManager.Instance.IsSimulating ? SimulationInterface.Instance.SceneScreen : UserInterface.Instance.SceneScreen);
			SceneScreen.PointerData previousPointer = obj.PreviousPointer;
			SceneScreen.PointerData pointer = obj.Pointer;
			if (pointer.IsPressed && previousPointer.IsPressed && CanMove())
			{
				Vector2 vector2 = m_camera.ScreenToWorldPoint(pointer.Point) - m_camera.ScreenToWorldPoint(previousPointer.Point);
				m_targetPoint -= vector2;
				m_movementVelocity = 0.5f / deltaTime * vector2 + 0.5f * m_movementVelocity;
			}
			else
			{
				if (m_movementVelocity.magnitude > 0.1f)
				{
					m_movementVelocity *= MathF.Exp(-5f * deltaTime);
				}
				else
				{
					m_movementVelocity = Vector2.zero;
				}
				m_targetPoint -= m_movementVelocity * deltaTime;
			}
			float num = 0.1f * m_targetSize + 1f;
			m_targetSize = Math.Clamp(m_targetSize - num * pointer.Scroll, m_minSize, m_maxSize);
		}

		private void LateUpdate()
		{
			if (PartManager.Instance.IsSimulating)
			{
				ComponentLookup<PartTypeValue> componentLookup = PartManager.Instance.System.GetComponentLookup<PartTypeValue>(isReadOnly: true);
				Entity entity = PartManager.Instance.PartSimulator.Unmanaged.FindLastPart(componentLookup, PartType.Pig);
				if (entity != Entity.Null)
				{
					Vector2 vector2 = PartManager.Instance.System.EntityManager.GetComponentData<LocalTransform>(entity).Position.xy;
					if (float.IsNaN(m_originalCameraTargetPoint.x))
					{
						m_originalCameraTargetPoint = vector2;
					}
					base.transform.position = (m_targetPoint + vector2 - m_originalCameraTargetPoint).WithZ(base.transform.position.z);
				}
				else
				{
					m_originalCameraTargetPoint.x = float.NaN;
					base.transform.position = m_targetPoint.WithZ(base.transform.position.z);
				}
			}
			else
			{
				m_originalCameraTargetPoint.x = float.NaN;
				base.transform.position = m_targetPoint.WithZ(base.transform.position.z);
			}
			float deltaTime = Time.deltaTime;
			for (float num = 0f; num < deltaTime; num += 0.02f)
			{
				float num2 = Math.Min(0.02f, deltaTime - num);
				float orthographicSize = m_camera.orthographicSize;
				float num3 = 144f * (m_targetSize - orthographicSize) - 24f * m_scalingSpeed;
				m_scalingSpeed += num3 * num2;
				m_camera.orthographicSize = orthographicSize + m_scalingSpeed * num2;
			}
		}
	}
}
