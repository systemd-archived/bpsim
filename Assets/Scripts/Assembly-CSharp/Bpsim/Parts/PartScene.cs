using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bpsim.Parts
{
	public class PartScene : MonoBehaviour
	{
		[SerializeField]
		private GameObject m_grid;

		[SerializeField]
		private GameObject m_largeGrid;

		private bool m_gridEnabled;

		private int m_id;

		private string m_name;

		private World m_world;

		private PartSceneState m_state;

		private Reference<PartSceneUnmanaged> m_unmanaged;

		public int SceneID => m_id;

		public PartSceneState State => m_state;

		public ref PartSceneUnmanaged Unmanaged => ref m_unmanaged.Value;

		public Reference<PartSceneUnmanaged> UnmanagedRef => m_unmanaged;

		internal PartSceneSystem System => m_world.GetOrCreateSystemManaged<PartSceneSystem>();

		public string SceneName
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
			}
		}

		public bool GridEnabled
		{
			get
			{
				return m_gridEnabled;
			}
			set
			{
				m_gridEnabled = value;
				SetGridEnabled();
			}
		}

		public void Initialize(int id, World world)
		{
			m_id = id;
			m_world = world;
			m_state = new PartSceneState();
			m_unmanaged = Reference<PartSceneUnmanaged>.Allocate(Allocator.Persistent);
			m_unmanaged.Value.Initialize(id, world.Unmanaged);
		}

		private void Awake()
		{
			m_gridEnabled = true;
		}

		private void Update()
		{
		}

		private void LateUpdate()
		{
			Vector3 position = SceneCamera.Instance.transform.position;
			m_grid.transform.position = new Vector3(Round(position.x, 24.5f, 50f), Round(position.y, 24.5f, 50f), m_grid.transform.position.z);
			m_largeGrid.transform.position = new Vector3(Round(position.x, -0.5f, 50f), Round(position.y, -0.5f, 50f), m_largeGrid.transform.position.z);
			SetGridEnabled();
			Transform transform = base.transform.Find("Selection");
			transform.gameObject.SetActive(m_state.HasSelection);
			if (m_state.HasSelection)
			{
				RectInt selection = m_state.Selection;
				transform.position = new Vector3((float)selection.xMin + 0.5f * ((float)selection.width - 1f), (float)selection.yMin + 0.5f * ((float)selection.height - 1f), transform.position.z);
				transform.localScale = new Vector3(selection.width, selection.height, 1f);
			}
		}

		private void OnDestroy()
		{
			if (m_world.IsCreated)
			{
				m_unmanaged.Value.OnDestroy();
			}
			m_unmanaged.Value.Dispose();
			Reference<PartSceneUnmanaged>.Free(m_unmanaged, Allocator.Persistent);
		}

		private void SetGridEnabled()
		{
			float orthographicSize = SceneCamera.Instance.Camera.orthographicSize;
			m_grid.SetActive(m_gridEnabled && orthographicSize < 50f);
			m_largeGrid.SetActive(m_gridEnabled);
		}

		private static float Round(float value, float offset, float scale)
		{
			return MathF.Round((value - offset) / scale) * scale + offset;
		}
	}
}
