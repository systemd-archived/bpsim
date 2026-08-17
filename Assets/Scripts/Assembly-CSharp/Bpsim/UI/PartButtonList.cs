using System;
using System.Collections.Generic;
using Bpsim.Collections;
using Bpsim.Parts;
using Bpsim.Parts.Simulation;
using Bpsim.Rendering;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Bpsim.UI
{
	public class PartButtonList : MonoBehaviour
	{
		private class ButtonComparer : IComparer<PartButton>
		{
			public int Compare(PartButton x, PartButton y)
			{
				int componentRank = x.Info.ComponentRank;
				int componentRank2 = y.Info.ComponentRank;
				if (componentRank < componentRank2)
				{
					return -1;
				}
				if (componentRank > componentRank2)
				{
					return 1;
				}
				if (!Settings.HighPartTypePriority)
				{
					Vector2 averagePosition = x.AveragePosition;
					Vector2 averagePosition2 = y.AveragePosition;
					if (averagePosition.x < averagePosition2.x)
					{
						return -1;
					}
					if (averagePosition.x > averagePosition2.x)
					{
						return 1;
					}
				}
				PartType sortedPartType = x.SortedPartType;
				PartType sortedPartType2 = y.SortedPartType;
				if (sortedPartType < sortedPartType2)
				{
					return -1;
				}
				if (sortedPartType > sortedPartType2)
				{
					return 1;
				}
				int partIndex = x.Info.PartIndex;
				int partIndex2 = y.Info.PartIndex;
				if (partIndex < partIndex2)
				{
					return -1;
				}
				if (partIndex > partIndex2)
				{
					return 1;
				}
				int buttonIndex = x.Info.ButtonIndex;
				int buttonIndex2 = y.Info.ButtonIndex;
				if (buttonIndex < buttonIndex2)
				{
					return -1;
				}
				if (buttonIndex > buttonIndex2)
				{
					return 1;
				}
				return 0;
			}
		}

		private struct ButtonSpriteInfo
		{
			public Texture Texture;

			public Rect UVRect;

			public Vector2 Scale;

			public Quaternion Rotation;

			public ButtonSpriteInfo(Texture texture, Rect uvRect, Vector2 scale, Quaternion rotation)
			{
				Texture = texture;
				UVRect = uvRect;
				Scale = scale;
				Rotation = rotation;
			}
		}

		private struct ButtonState
		{
			public int Index;

			public PartButton Button;

			public ButtonState(int index, PartButton button)
			{
				Index = index;
				Button = button;
			}
		}

		[SerializeField]
		private Canvas m_canvas;

		[SerializeField]
		private ScrollRect m_scrollView;

		[SerializeField]
		private GameObject m_triggerButtonPrefab;

		[SerializeField]
		private GameObject m_sliderButtonPrefab;

		private bool m_needsUpdate;

		private ButtonComparer m_comparer;

		private PartButton m_selectedButton;

		private List<PartButton> m_currentButtons;

		private List<PartTriggerButton> m_triggerButtonPool;

		private List<PartSliderButton> m_sliderButtonPool;

		private Heap<(int, int)> m_componentHeap;

		private Dictionary<PartButtonInfo, int> m_buttonInfoMap;

		private Dictionary<PartButtonInfo, ButtonSpriteInfo> m_spriteInfoMap;

		private Dictionary<(Entity, int), ButtonState> m_buttonStateMap;

		public bool NeedsUpdate
		{
			get
			{
				return m_needsUpdate;
			}
			set
			{
				m_needsUpdate = value;
			}
		}

		public PartButton SelectedButton => m_selectedButton;

		public List<PartButton> Buttons => m_currentButtons;

		public GameObject TriggerButtonPrefab => m_triggerButtonPrefab;

		public GameObject SliderButtonPrefab => m_sliderButtonPrefab;

		public static bool Enabled
		{
			get
			{
				if (Instance != null)
				{
					return Instance.gameObject.activeSelf;
				}
				return false;
			}
		}

		public static PartButtonList Instance { get; private set; }

		public static ButtonSettings Settings => UserSettings.Instance.ButtonSettings;

		private void Awake()
		{
			Instance = this;
			m_comparer = new ButtonComparer();
			m_currentButtons = new List<PartButton>();
			m_triggerButtonPool = new List<PartTriggerButton>();
			m_sliderButtonPool = new List<PartSliderButton>();
			m_componentHeap = new Heap<(int, int)>();
			m_buttonInfoMap = new Dictionary<PartButtonInfo, int>();
			m_buttonStateMap = new Dictionary<(Entity, int), ButtonState>();
		}

		public void Initialize()
		{
			CreateSpriteInfoMap();
			UpdateButtons();
		}

		private void CreateSpriteInfoMap()
		{
			m_spriteInfoMap = new Dictionary<PartButtonInfo, ButtonSpriteInfo>();
			Texture2D texture = CoreManager.Instance.Resources.LoadAsset<Texture2D>("PartButton_Texture");
			for (int i = 0; i < 4; i++)
			{
				PartButtonInfo key = GetButtonInfo(PartType.Fan, i);
				ButtonSpriteInfo spriteInfo = GetSpriteInfo(texture, "PartButton_Fan", Quaternion.AngleAxis(90 * i, Vector3.forward));
				m_spriteInfoMap.Add(key, spriteInfo);
			}
			for (int j = 0; j < 4; j++)
			{
				PartButtonInfo key2 = GetButtonInfo(PartType.Propeller, j);
				ButtonSpriteInfo spriteInfo2 = GetSpriteInfo(texture, "PartButton_Propeller", Quaternion.AngleAxis(90 * j, Vector3.forward));
				m_spriteInfoMap.Add(key2, spriteInfo2);
			}
			for (int k = 0; k < 4; k++)
			{
				PartButtonInfo key3 = GetButtonInfo(PartType.Rotor, k);
				ButtonSpriteInfo spriteInfo3 = GetSpriteInfo(texture, "PartButton_Rotor", Quaternion.AngleAxis(90 * k, Vector3.forward));
				m_spriteInfoMap.Add(key3, spriteInfo3);
			}
			static PartButtonInfo GetButtonInfo(PartType partType, int partIndex)
			{
				return new PartButtonInfo(PartButtonType.Trigger, 0, partType, partIndex, -1);
			}
		}

		private ButtonSpriteInfo GetSpriteInfo(Texture texture, string name)
		{
			return GetSpriteInfo(texture, name, Quaternion.identity);
		}

		private ButtonSpriteInfo GetSpriteInfo(Texture texture, string name, Quaternion rotation)
		{
			int iD = SpriteManager.Instance.GetID(name);
			SpriteRect sprite = SpriteManager.Instance.GetSprite(iD);
			return new ButtonSpriteInfo(texture, new Rect(sprite.U, sprite.V, sprite.W, sprite.H), new Vector2(sprite.VertexW, sprite.VertexH), rotation);
		}

		private bool FindSpriteInfo(PartType partType, out PartButtonInfo buttonInfo, out ButtonSpriteInfo spriteInfo)
		{
			foreach (KeyValuePair<PartButtonInfo, ButtonSpriteInfo> item in m_spriteInfoMap)
			{
				if (item.Key.PartType == partType)
				{
					buttonInfo = item.Key;
					spriteInfo = item.Value;
					return true;
				}
			}
			buttonInfo = default(PartButtonInfo);
			spriteInfo = default(ButtonSpriteInfo);
			return false;
		}

		private bool FindSpriteInfo(PartButtonInfo buttonInfo, out ButtonSpriteInfo spriteInfo)
		{
			buttonInfo.PartType = GetBasePartType(buttonInfo.PartType);
			buttonInfo.ComponentRank = -1;
			if (buttonInfo.PartType == PartType.JetEngine)
			{
				buttonInfo.PartIndex = -1;
			}
			return m_spriteInfoMap.TryGetValue(buttonInfo, out spriteInfo);
		}

		private void Update()
		{
			if (m_needsUpdate)
			{
				UpdateButtons();
				m_needsUpdate = false;
			}
		}

		private void UpdateButtons()
		{
			SaveButtonStates();
			CreateButtons();
			RenderButtons();
		}

		private void CreateButtons()
		{
			Reference<PartSimulatorUnmanaged> unmanagedRef = PartManager.Instance.PartSimulator.UnmanagedRef;
			int length = unmanagedRef.Value.ConnectedComponents.Length;
			int num = Math.Min(Settings.MaxSeparationCount, length);
			m_componentHeap.Clear();
			m_componentHeap.PushRange(GetComponentInfo(unmanagedRef));
			int[] array = new int[length];
			Array.Fill(array, num);
			for (int i = 0; i < num; i++)
			{
				array[m_componentHeap.Pop().Item2] = i;
			}
			m_buttonInfoMap.Clear();
			bool[] array2 = new bool[m_currentButtons.Count];
			List<PartButton> list = new List<PartButton>(m_currentButtons.Count);
			foreach (Entity part in unmanagedRef.Value.Parts)
			{
				PartAspect aspect = PartManager.Instance.System.EntityManager.GetAspect<PartAspect>(part);
				if (!BasePart.IsTriggerable(aspect.TypeInfo))
				{
					continue;
				}
				foreach (PartButtonInfo item in GetAllButtonInfo(aspect))
				{
					PartButtonInfo current2 = item;
					int componentRank = array[current2.ComponentIndex];
					current2.ComponentRank = componentRank;
					if (!m_buttonInfoMap.TryGetValue(current2, out var value))
					{
						value = list.Count;
						m_buttonInfoMap.Add(current2, value);
						list.Add(null);
					}
					m_buttonStateMap.TryGetValue((part, current2.ButtonIndex), out var value2);
					if (list[value] == null && value2.Button != null && !array2[value2.Index])
					{
						list[value] = value2.Button;
						array2[value2.Index] = true;
					}
				}
			}
			for (int j = 0; j < m_currentButtons.Count; j++)
			{
				PartButton partButton = m_currentButtons[j];
				if (!array2[j])
				{
					FreeButton(partButton);
				}
				else
				{
					partButton.Parts.Clear();
				}
			}
			array2 = new bool[list.Count];
			foreach (Entity part2 in unmanagedRef.Value.Parts)
			{
				PartAspect aspect2 = PartManager.Instance.System.EntityManager.GetAspect<PartAspect>(part2);
				if (!BasePart.IsTriggerable(aspect2.TypeInfo))
				{
					continue;
				}
				foreach (PartTriggerButtonInfo item2 in BasePart.GetTriggerButtonInfo(aspect2))
				{
					PartButtonInfo value3 = item2.Value;
					int componentRank2 = array[value3.ComponentIndex];
					value3.ComponentRank = componentRank2;
					int num2 = m_buttonInfoMap[value3];
					PartTriggerButton partTriggerButton = list[num2] as PartTriggerButton;
					if (partTriggerButton == null)
					{
						partTriggerButton = (PartTriggerButton)(list[num2] = AllocateButton<PartTriggerButton>());
					}
					if (array2[num2])
					{
						partTriggerButton.Parts.Add(part2);
						continue;
					}
					array2[num2] = true;
					partTriggerButton.Parts.Add(part2);
					partTriggerButton.SetInfo(value3);
					partTriggerButton.SetConsistent(item2.Consistent);
					partTriggerButton.SetMultiple(item2.Multiple);
					SetButtonSprite(value3, partTriggerButton);
				}
				foreach (PartSliderButtonInfo item3 in BasePart.GetSliderButtonInfo(aspect2))
				{
					PartButtonInfo value4 = item3.Value;
					int componentRank3 = array[value4.ComponentIndex];
					value4.ComponentRank = componentRank3;
					int num3 = m_buttonInfoMap[value4];
					PartSliderButton partSliderButton = list[num3] as PartSliderButton;
					if (partSliderButton == null)
					{
						partSliderButton = (PartSliderButton)(list[num3] = AllocateButton<PartSliderButton>());
					}
					if (array2[num3])
					{
						partSliderButton.Parts.Add(part2);
						continue;
					}
					array2[num3] = true;
					partSliderButton.Parts.Add(part2);
					partSliderButton.SetInfo(value4);
					partSliderButton.SetRange(item3.Range);
					SetButtonSprite(value4, partSliderButton);
				}
			}
			m_currentButtons = list;
			static IEnumerable<PartButtonInfo> GetAllButtonInfo(PartAspect partAspect)
			{
				foreach (PartTriggerButtonInfo item4 in BasePart.GetTriggerButtonInfo(partAspect))
				{
					yield return item4.Value;
				}
				foreach (PartSliderButtonInfo item5 in BasePart.GetSliderButtonInfo(partAspect))
				{
					yield return item5.Value;
				}
			}
			static IEnumerable<(int, int)> GetComponentInfo(Reference<PartSimulatorUnmanaged> simulator)
			{
				int componentCount = simulator.Value.ConnectedComponents.Length;
				for (int k = 0; k < componentCount; k++)
				{
					yield return (-simulator.Value.ConnectedComponents[k].PartCount, k);
				}
			}
		}

		private void SetButtonSprite(PartButtonInfo buttonInfo, PartButton button)
		{
			buttonInfo.ComponentRank = -1;
			buttonInfo.PartType = GetBasePartType(buttonInfo.PartType);
			if (FindSpriteInfo(buttonInfo, out var spriteInfo))
			{
				button.SetSprite(enabled: true, spriteInfo.Texture, spriteInfo.UVRect, spriteInfo.Scale / 0.7f, spriteInfo.Rotation);
			}
			else
			{
				button.SetSprite(enabled: false, null, Rect.zero, Vector2.zero, Quaternion.identity);
			}
		}

		public void RenderButtons()
		{
			if (m_currentButtons.Count == 0)
			{
				return;
			}
			foreach (PartButton currentButton in m_currentButtons)
			{
				currentButton.Initialize();
			}
			m_currentButtons.Sort(m_comparer);
			bool displayButtonIndex = Settings.DisplayButtonIndex;
			for (int i = 0; i < m_currentButtons.Count; i++)
			{
				char c = (char)(65 + i % 26);
				char c2 = (char)(48 + i / 26);
				string text = (displayButtonIndex ? ((i < 26) ? c.ToString() : (c.ToString() + c2)) : string.Empty);
				m_currentButtons[i].DisplayIndexText(text);
			}
			int num = 0;
			foreach (PartButton currentButton2 in m_currentButtons)
			{
				num += 1 + currentButton2.SubButtonCount;
			}
			float num2 = 1029.375f * ((float)Screen.width / (float)Screen.height);
			int j;
			for (j = 1; num > j * (int)(num2 / GetPadding(j + 1)); j++)
			{
			}
			float num3 = GetPadding(j);
			float min = GetPadding(j + 1);
			int num4 = num / j + ((num % j != 0) ? 1 : 0);
			if (Settings.LayoutMode == 1 && j > 1)
			{
				num4 = Math.Max(num4, (int)(num2 / num3));
			}
			float num5 = Math.Clamp(num2 / (float)num4, min, num3);
			float num6 = num5 / 240f;
			Vector3 localScale = new Vector3(num6, num6, 1f);
			int num7 = 0;
			float num8 = 10f;
			RectTransform content = m_scrollView.content;
			RectTransform rectTransform = (RectTransform)m_scrollView.transform;
			content.sizeDelta = new Vector2(content.sizeDelta.x, (float)j * num5 + num8);
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, Math.Min((float)j * num5 + num8, 360f * Settings.ScrollViewHeightScale));
			foreach (PartButton currentButton3 in m_currentButtons)
			{
				float num9 = (float)(-(num4 - 1)) / 2f + (float)(num7 % num4);
				float num10 = (float)(-(j - 1)) / 2f + (float)(num7 / num4);
				Vector2 vector = new Vector2(num5 * num9, (0f - num5) * num10 + num8);
				RectTransform obj = (RectTransform)currentButton3.transform;
				obj.localScale = localScale;
				obj.anchoredPosition = vector;
				num7++;
				if (currentButton3.SubButtonCount == 0)
				{
					continue;
				}
				foreach (PartButton subButton in currentButton3.SubButtons)
				{
					num9 = (float)(-(num4 - 1)) / 2f + (float)(num7 % num4);
					num10 = (float)(-(j - 1)) / 2f + (float)(num7 / num4);
					Vector2 vector2 = new Vector2(num5 * num9, (0f - num5) * num10 + num8);
					((RectTransform)subButton.transform).anchoredPosition = (vector2 - vector) / num6;
					num7++;
				}
			}
			static float GetPadding(int n)
			{
				return (float)Math.Max(200 - 20 * n, 140) * Settings.ButtonScale;
			}
		}

		private void SaveButtonStates()
		{
			m_buttonStateMap.Clear();
			for (int i = 0; i < m_currentButtons.Count; i++)
			{
				PartButton partButton = m_currentButtons[i];
				foreach (Entity part in partButton.Parts)
				{
					m_buttonStateMap.Add((part, partButton.Info.ButtonIndex), new ButtonState(i, partButton));
				}
			}
		}

		private void FreeButtons()
		{
			foreach (PartButton currentButton in m_currentButtons)
			{
				if (currentButton != null)
				{
					FreeButton(currentButton);
				}
			}
			m_currentButtons.Clear();
		}

		private T AllocateButton<T>() where T : PartButton
		{
			if (typeof(T) == typeof(PartTriggerButton))
			{
				List<PartTriggerButton> triggerButtonPool = m_triggerButtonPool;
				if (triggerButtonPool.Count == 0)
				{
					return CreateButton<T>();
				}
				T val = (T)(PartButton)triggerButtonPool[triggerButtonPool.Count - 1];
				triggerButtonPool.RemoveAt(triggerButtonPool.Count - 1);
				val.gameObject.SetActive(value: true);
				return val;
			}
			if (typeof(T) == typeof(PartSliderButton))
			{
				List<PartSliderButton> sliderButtonPool = m_sliderButtonPool;
				if (sliderButtonPool.Count == 0)
				{
					return CreateButton<T>();
				}
				T val2 = (T)(PartButton)sliderButtonPool[sliderButtonPool.Count - 1];
				sliderButtonPool.RemoveAt(sliderButtonPool.Count - 1);
				val2.gameObject.SetActive(value: true);
				return val2;
			}
			return null;
		}

		private T CreateButton<T>() where T : PartButton
		{
			GameObject original = null;
			if (typeof(T) == typeof(PartTriggerButton))
			{
				original = m_triggerButtonPrefab;
			}
			else if (typeof(T) == typeof(PartSliderButton))
			{
				original = m_sliderButtonPrefab;
			}
			T component = UnityEngine.Object.Instantiate(original).GetComponent<T>();
			component.transform.SetParent(m_scrollView.content, worldPositionStays: false);
			component.gameObject.SetActive(value: true);
			return component;
		}

		private void FreeButton(PartButton button)
		{
			button.Reset();
			button.gameObject.SetActive(value: false);
			if (button is PartTriggerButton item)
			{
				m_triggerButtonPool.Add(item);
			}
			else if (button is PartSliderButton item2)
			{
				m_sliderButtonPool.Add(item2);
			}
		}

		private static PartType GetBasePartType(PartType partType)
		{
			if (partType == PartType.EngineSmall || partType == PartType.EngineBig)
			{
				partType = PartType.Engine;
			}
			return partType;
		}
	}
}
