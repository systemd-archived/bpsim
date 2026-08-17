using System;
using Bpsim.Parts;
using Bpsim.Physics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Bpsim.UI
{
	internal class PartPropertyPanel : SwitchableInterface
	{
		[SerializeField]
		private Button m_switchToButton;

		[SerializeField]
		private Button m_switchBackButton;

		[SerializeField]
		private Text m_partType;

		[SerializeField]
		private Text m_partIndex;

		[SerializeField]
		private Text m_coordX;

		[SerializeField]
		private Text m_coordY;

		[SerializeField]
		private Text m_rotation;

		[SerializeField]
		private Text m_quaternion;

		[SerializeField]
		private Button m_incrementButton;

		[SerializeField]
		private Button m_eraseButton;

		[SerializeField]
		private Button m_closeButton;

		private PartScene m_scene;

		private Entity m_part;

		public void Initialize(PartScene partScene, Entity part)
		{
			m_scene = partScene;
			m_part = part;
		}

		public void UpdateLayout(bool sidebar)
		{
			UserInterface instance = UserInterface.Instance;
			Transform parent = (sidebar ? instance.RightSidebar.Content.transform : instance.SubCanvas.transform);
			base.transform.SetParent(parent, worldPositionStays: false);
			UpdateLayout(sidebar, instance.RightSidebar.Size);
		}

		protected override void Awake()
		{
			base.Awake();
			m_spacing = 20f;
			m_padding = new Vector2(30f, 0f);
		}

		private void Start()
		{
			m_closeButton.onClick.AddListener(base.Close);
			m_incrementButton.onClick.AddListener(IncrementRotation);
			m_switchToButton.onClick.AddListener(delegate
			{
				UpdateLayout(sidebar: false);
			});
			m_switchBackButton.onClick.AddListener(delegate
			{
				UpdateLayout(sidebar: true);
			});
		}

		private void Update()
		{
			m_switchToButton.gameObject.SetActive(m_inSidebar);
			m_switchBackButton.gameObject.SetActive(!m_inSidebar);
			if (m_scene == null || !m_scene.System.EntityManager.Exists(m_part))
			{
				Clear();
				return;
			}
			PartAspect aspect = m_scene.System.EntityManager.GetAspect<PartAspect>(m_part);
			float3 @float = 180f / MathF.PI * m_scene.System.EntityManager.GetComponentData<WorldTransform>(m_part).Rotation.ToEulerAngles();
			m_partType.text = aspect.PartType.GetAliasName();
			m_partIndex.text = aspect.PartIndex.ToString();
			m_coordX.text = aspect.CoordX.ToString();
			m_coordY.text = aspect.CoordY.ToString();
			m_rotation.text = aspect.Rotation.ToString();
			m_quaternion.text = $"({@float.y}, {@float.z}, {@float.x})";
		}

		private void Clear()
		{
			m_scene = null;
			m_part = Entity.Null;
			m_partType.text = string.Empty;
			m_partIndex.text = string.Empty;
			m_coordX.text = string.Empty;
			m_coordY.text = string.Empty;
			m_rotation.text = string.Empty;
			m_quaternion.text = string.Empty;
		}

		private void IncrementRotation()
		{
			if (!(m_scene == null) && !(m_part == Entity.Null))
			{
				PartAspect partAspect = m_scene.System.EntityManager.GetAspect<PartAspect>(m_part);
				partAspect.Rotation++;
				m_scene.Unmanaged.UpdateNeighbours(in partAspect);
			}
		}
	}
}
