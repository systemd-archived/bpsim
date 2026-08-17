using System;
using System.Linq;
using Bpsim.Parts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bpsim.UI
{
	internal class PartOperator : MonoBehaviour
	{
		[SerializeField]
		private ToggleGroup m_toggleGroup;

		[SerializeField]
		private DoubleClickableButton m_viewButton;

		[SerializeField]
		private DoubleClickableButton m_placeButton;

		[SerializeField]
		private DoubleClickableButton m_eraseButton;

		[SerializeField]
		private DoubleClickableButton m_selectButton;

		[SerializeField]
		private PartPropertyPanel m_partPropertyPanelPrefab;

		[SerializeField]
		private PartDrawingPanel m_partDrawingPanelPrefab;

		[SerializeField]
		private PartSelectionPanel m_partSelectionPanelPrefab;

		private UserInterface m_root;

		private PartOperationMode m_mode;

		private PartPropertyPanel m_propertyPanel;

		private PartDrawingPanel m_drawingPanel;

		private PartSelectionPanel m_selectionPanel;

		private NativeArray<Schematics.Unit> m_copiedSchematics;

		public PartOperationMode Mode => m_mode;

		public NativeArray<Schematics.Unit> CopiedSchematics
		{
			get
			{
				return m_copiedSchematics;
			}
			set
			{
				if (m_copiedSchematics.IsCreated)
				{
					m_copiedSchematics.Dispose();
				}
				m_copiedSchematics = value;
			}
		}

		private void Awake()
		{
			m_root = UserInterface.Instance;
			m_placeButton.onDoubleClick.AddListener(OpenPartDrawingPanel);
			m_eraseButton.onDoubleClick.AddListener(OpenPartDrawingPanel);
			m_selectButton.onDoubleClick.AddListener(OpenPartSelectionPanel);
			m_root.SceneScreen.DoubleClicked += OnScreenDoubleClicked;
		}

		private void Update()
		{
			Toggle toggle = m_toggleGroup.ActiveToggles().FirstOrDefault();
			if (toggle != null)
			{
				m_mode = toggle.name switch
				{
					"ViewButton" => PartOperationMode.View, 
					"PlaceButton" => PartOperationMode.Place, 
					"EraseButton" => PartOperationMode.Erase, 
					"SelectButton" => PartOperationMode.Select, 
					_ => PartOperationMode.None, 
				};
			}
		}

		private void OnScreenDoubleClicked(PointerEventData eventData)
		{
			Vector3 vector = SceneCamera.Instance.Camera.ScreenToWorldPoint(eventData.position);
			int x = (int)MathF.Round(vector.x);
			int y = (int)MathF.Round(vector.y);
			PartManager instance = PartManager.Instance;
			if (instance.HasActiveScene() && instance.ActiveScene.Unmanaged.FindPartGridAt(x, y, out var grid) && (grid.HasPartContainer || grid.HasPart))
			{
				Entity part = (grid.HasPart ? grid.Part : grid.PartContainer);
				OpenPartPropertyPanel(instance.ActiveScene, part);
			}
		}

		private void OpenPartPropertyPanel(PartScene partScene, Entity part)
		{
			if (m_propertyPanel != null)
			{
				m_propertyPanel.Initialize(partScene, part);
			}
			else if (!UserInterface.Instance.RightSidebar.HasContent)
			{
				PartPropertyPanel partPropertyPanel = UnityEngine.Object.Instantiate(m_partPropertyPanelPrefab);
				partPropertyPanel.UpdateLayout(sidebar: true);
				partPropertyPanel.Initialize(partScene, part);
				m_propertyPanel = partPropertyPanel;
			}
		}

		private void OpenPartDrawingPanel()
		{
			if (!UserInterface.Instance.RightSidebar.HasContent)
			{
				PartDrawingPanel partDrawingPanel = UnityEngine.Object.Instantiate(m_partDrawingPanelPrefab);
				partDrawingPanel.UpdateLayout(sidebar: true);
				m_drawingPanel = partDrawingPanel;
			}
		}

		private void OpenPartSelectionPanel()
		{
			if (!UserInterface.Instance.RightSidebar.HasContent)
			{
				PartSelectionPanel partSelectionPanel = UnityEngine.Object.Instantiate(m_partSelectionPanelPrefab);
				partSelectionPanel.UpdateLayout(sidebar: true);
				m_selectionPanel = partSelectionPanel;
			}
		}
	}
}
