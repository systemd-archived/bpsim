using System;
using System.Collections.Generic;
using Bpsim.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace Bpsim.UI
{
	internal class UserSettingsPanel : InterfaceBase
	{
		[SerializeField]
		private GameObject m_content;

		[SerializeField]
		private GameObject m_buttonGroup;

		[SerializeField]
		private Button m_resetButton;

		[SerializeField]
		private Button m_closeButton;

		[SerializeField]
		private GameObject m_toggleTemplate;

		[SerializeField]
		private GameObject m_inputFieldTemplate;

		[SerializeField]
		private GameObject m_dropdownTemplate;

		private List<GameObject> m_settingItems;

		private DependencyProxy<SimulationSettings> m_simulationSettingsProxy;

		private void Start()
		{
			Bind();
		}

		private void Bind()
		{
			m_closeButton.onClick.AddListener(base.Close);
			m_resetButton.onClick.AddListener(Reset);
			m_simulationSettingsProxy = new DependencyProxy<SimulationSettings>(UserSettings.Instance.SimulationSettings);
			m_settingItems = new List<GameObject>();
			foreach (KeyValuePair<string, DependencyProperty> property2 in m_simulationSettingsProxy.Properties)
			{
				DependencyProperty value = property2.Value;
				GameObject gameObject;
				if (value is DependencyProperty<bool> property)
				{
					gameObject = UnityEngine.Object.Instantiate(m_toggleTemplate);
					gameObject.transform.Find("Value").GetComponent<ToggleSwitch>().Bind(property);
				}
				else if (value.PropertyType.IsEnum)
				{
					gameObject = UnityEngine.Object.Instantiate(m_dropdownTemplate);
					Dropdown component = gameObject.transform.Find("Value").GetComponent<Dropdown>();
					string[] names = Enum.GetNames(value.PropertyType);
					component.options = new List<Dropdown.OptionData>(names.Length);
					string[] array = names;
					foreach (string text in array)
					{
						component.options.Add(new Dropdown.OptionData(text));
					}
					typeof(Binding).GetMethod("Bind", new Type[2]
					{
						typeof(Dropdown),
						typeof(DependencyProperty<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
					}).MakeGenericMethod(value.PropertyType).Invoke(this, new object[2] { component, value });
				}
				else
				{
					gameObject = UnityEngine.Object.Instantiate(m_inputFieldTemplate);
					InputField component2 = gameObject.transform.Find("Value").GetComponent<InputField>();
					typeof(Binding).GetMethod("Bind", new Type[2]
					{
						typeof(InputField),
						typeof(DependencyProperty<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
					}).MakeGenericMethod(value.PropertyType).Invoke(this, new object[2] { component2, value });
				}
				gameObject.SetActive(value: true);
				gameObject.transform.SetParent(m_content.transform, worldPositionStays: false);
				gameObject.transform.Find("Name").GetComponent<Text>().text = property2.Key;
				m_settingItems.Add(gameObject);
			}
		}

		private void Reset()
		{
			UserSettings.Instance.Reset();
		}

		private void OnDestroy()
		{
			m_simulationSettingsProxy.Source.Unregister();
			m_simulationSettingsProxy.Unregister();
		}
	}
}
