using System;
using Bpsim.Parts;
using Bpsim.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Bpsim.UI
{
	internal class PartSelector : MonoBehaviour
	{
		[SerializeField]
		private GameObject m_itemTemplate;

		[SerializeField]
		private CustomPartSelector m_customPartSelectorTemplate;

		[SerializeField]
		private GameObject m_content;

		[SerializeField]
		private ToggleGroup m_toggleGroup;

		private PartType m_partType;

		private int[] m_indexMap;

		private int[] m_rotationMap;

		private bool[] m_flippedMap;

		private GameObject[] m_itemMap;

		public PartType PartType => m_partType;

		public int PartIndex => m_indexMap[(int)m_partType];

		public int Rotation => m_rotationMap[(int)m_partType];

		public bool Flipped => m_flippedMap[(int)m_partType];

		private void Start()
		{
			m_indexMap = new int[49];
			m_rotationMap = new int[49];
			m_flippedMap = new bool[49];
			m_itemMap = new GameObject[49];
			PartCollection<ManagedPart> partCollection = PartManager.Instance.Factory.PartCollection;
			for (int i = 0; i < partCollection.Length; i++)
			{
				PartType partType = (PartType)i;
				if (partCollection.TryFindParts(partType, out var _))
				{
					m_itemMap[(int)partType] = CreateItem(partType);
				}
			}
		}

		private GameObject CreateItem(PartType partType)
		{
			GameObject obj = UnityEngine.Object.Instantiate(m_itemTemplate);
			obj.name = $"Item_{(int)partType}";
			obj.transform.SetParent(m_content.transform, worldPositionStays: false);
			obj.SetActive(value: true);
			obj.transform.Find("Name").GetComponent<Text>().text = partType.GetAliasName();
			Toggle component = obj.GetComponent<Toggle>();
			component.group = m_toggleGroup;
			component.onValueChanged.AddListener(delegate(bool value)
			{
				if (value)
				{
					m_partType = partType;
				}
			});
			obj.GetComponent<DoubleClickableButton>().onDoubleClick.AddListener(delegate
			{
				CreateCustomPartSelector(partType);
			});
			RectTransform rectTransform = (RectTransform)obj.transform.Find("PartIcon");
			SpriteBase component2 = rectTransform.GetComponent<SpriteBase>();
			component2.SpriteName = $"Part_{partType}_0_Rendered";
			if (component2.IsDirty)
			{
				component2.Apply();
				NormalizeScale(rectTransform);
			}
			return obj;
		}

		private void UpdateItem(PartType partType, int partIndex, int rotation, bool flipped)
		{
			m_rotationMap[(int)partType] = rotation;
			m_flippedMap[(int)partType] = flipped;
			if (m_indexMap[(int)partType] != partIndex)
			{
				m_indexMap[(int)partType] = partIndex;
				RectTransform rectTransform = (RectTransform)m_itemMap[(int)partType].transform.Find("PartIcon");
				SpriteBase component = rectTransform.GetComponent<SpriteBase>();
				component.SpriteName = $"Part_{partType}_{partIndex}_Rendered";
				if (component.IsDirty)
				{
					component.Apply();
					NormalizeScale(rectTransform);
				}
			}
		}

		private void CreateCustomPartSelector(PartType partType)
		{
			CustomPartSelector customPartSelector = UnityEngine.Object.Instantiate(m_customPartSelectorTemplate);
			customPartSelector.transform.SetParent(UserInterface.Instance.SubCanvas.transform, worldPositionStays: false);
			customPartSelector.Initialize(partType, m_indexMap[(int)partType], m_rotationMap[(int)partType], m_flippedMap[(int)partType]);
			customPartSelector.Completed += UpdateItem;
		}

		public static void NormalizeScale(RectTransform transform)
		{
			Vector2 sizeDelta = transform.sizeDelta;
			float num = Math.Max(sizeDelta.x, sizeDelta.y);
			transform.sizeDelta = Math.Min(75f / num, 0.625f) * sizeDelta;
		}
	}
}
