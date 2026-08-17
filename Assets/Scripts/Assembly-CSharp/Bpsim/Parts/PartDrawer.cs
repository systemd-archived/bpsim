using System;
using Bpsim.ComponentModel;
using Bpsim.UI;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Bpsim.Parts
{
	public class PartDrawer
	{
		public enum Shape
		{
			None = 0,
			Circle = 1,
			Square = 2
		}

		[BurstCompile]
		private struct PlacePartsJob : IJob
		{
			public bool Overlay;

			public Segment Segment;

			public PartCreationRequest Request;

			public Reference<PartSceneUnmanaged> PartScene;

			public void Execute()
			{
				int4 bounds = Segment.GetBounds();
				int2 @int = new int2(bounds.x, bounds.y);
				int2 int2 = new int2(bounds.z, bounds.w);
				for (int i = @int.x; i <= int2.x; i++)
				{
					for (int j = @int.y; j <= int2.y; j++)
					{
						if (Segment.IsInside(new float2(i, j)))
						{
							PartScene.Value.PlacePartAt(Request.WithPosition(i, j), Overlay);
						}
					}
				}
				PartScene.Value.Update();
			}
		}

		[BurstCompile]
		private struct ErasePartsJob : IJob
		{
			public PartType PartType;

			public int PartIndex;

			public Segment Segment;

			public Reference<PartSceneUnmanaged> PartScene;

			public void Execute()
			{
				int4 bounds = Segment.GetBounds();
				int2 @int = new int2(bounds.x, bounds.y);
				int2 int2 = new int2(bounds.z, bounds.w);
				for (int i = @int.x; i <= int2.x; i++)
				{
					for (int j = @int.y; j <= int2.y; j++)
					{
						if (Segment.IsInside(new float2(i, j)))
						{
							PartScene.Value.RemovePartAt(i, j, PartType, PartIndex);
						}
					}
				}
				PartScene.Value.Update();
			}
		}

		public readonly struct Segment
		{
			public readonly float2 Point0;

			public readonly float2 Point1;

			public readonly Shape Shape;

			public readonly float2 Size;

			public Segment(float2 point0, float2 point1, Shape shape, float2 size)
			{
				Point0 = point0;
				Point1 = point1;
				Shape = shape;
				Size = size;
			}

			public int4 GetBounds()
			{
				int2 xy = (int2)math.ceil(math.min(Point0, Point1) - 0.5f * math.cmax(Size));
				int2 zw = (int2)math.floor(math.max(Point0, Point1) + 0.5f * math.cmax(Size));
				return new int4(xy, zw);
			}

			public bool IsInside(float2 value)
			{
				float2 y = value - Point0;
				float2 halfSize = 0.5f * Size;
				float num = math.distance(Point0, Point1);
				if (num < 1E-05f)
				{
					return Shape switch
					{
						Shape.None => false, 
						Shape.Circle => math.distancesq(value, Point0) < halfSize.x * halfSize.x, 
						Shape.Square => math.abs(y.x) < halfSize.x && math.abs(y.y) < halfSize.y, 
						_ => false, 
					};
				}
				var (num4, x) = vector.invrotate((Point1 - Point0) / num, y);
				switch (Shape)
				{
				case Shape.None:
				{
					float num5 = num4 / num;
					if (num5 > -0.1f && num5 < 1.1f)
					{
						return math.abs(x) < 0.5f * Size.x;
					}
					return false;
				}
				case Shape.Circle:
				{
					float num5 = num4 / num;
					if (num5 >= 0f)
					{
						if (num5 <= 1f)
						{
							return math.abs(x) < halfSize.x;
						}
						return math.distancesq(value, Point1) < halfSize.x * halfSize.x;
					}
					if (num5 < 0f)
					{
						return math.distancesq(value, Point0) < halfSize.x * halfSize.x;
					}
					return false;
				}
				case Shape.Square:
					return IntersectRect(value - Point0, value - Point1, halfSize);
				default:
					return false;
				}
			}

			private static bool IntersectRect(float2 point0, float2 point1, float2 halfSize)
			{
				float num = point1.x - point0.x;
				float num2 = point1.y - point0.y;
				if (math.abs(num) < 1E-05f)
				{
					if (math.abs(point0.x) < halfSize.x && math.min(point0.y, point1.y) < halfSize.y)
					{
						return math.max(point0.y, point1.y) > 0f - halfSize.y;
					}
					return false;
				}
				if (math.abs(num2) < 1E-05f)
				{
					if (math.abs(point0.y) < halfSize.y && math.min(point0.x, point1.x) < halfSize.x)
					{
						return math.max(point0.x, point1.x) > 0f - halfSize.x;
					}
					return false;
				}
				float num3 = math.sign(num) * halfSize.x;
				float num4 = math.sign(num2) * halfSize.y;
				float num5 = math.max(0f, math.max((0f - num3 - point0.x) / num, (0f - num4 - point0.y) / num2));
				float num6 = math.min(1f, math.min((num3 - point0.x) / num, (num4 - point0.y) / num2));
				return num5 < num6;
			}
		}

		private PartDrawingSettings m_settings;

		private bool m_selecting;

		private Vector2Int m_selectionStart;

		private DependencyProperty<RectInt> m_selection;

		public PartDrawingSettings Settings => m_settings;

		public DependencyProperty<RectInt> Selection => m_selection;

		public PartDrawer()
		{
			m_settings = new PartDrawingSettings();
			m_selection = new DependencyProperty<RectInt>();
			Bind(PartManager.Instance);
		}

		private void Bind(PartManager partManager)
		{
			m_selection.Bind(delegate
			{
				if (partManager.HasActiveScene())
				{
					partManager.ActiveScene.State.Selection = m_selection.Value;
				}
			});
			PartManager.Instance.SceneSelected += OnSceneSelected;
		}

		private void OnSceneSelected(PartScene partScene)
		{
			m_selection.Value = partScene.State.Selection;
		}

		public void Update(PartManager partManager)
		{
			if (partManager.HasActiveScene())
			{
				UserInterface instance = UserInterface.Instance;
				switch (instance.PartOperator.Mode)
				{
				case PartOperationMode.Place:
				case PartOperationMode.Erase:
					PlaceOrEraseParts(partManager, instance);
					break;
				case PartOperationMode.Select:
					UpdateSelectionState(instance);
					break;
				}
			}
		}

		private void UpdateSelectionState(UserInterface userInterface)
		{
			SceneScreen sceneScreen = userInterface.SceneScreen;
			if (sceneScreen.Pointer.IsPressed)
			{
				Vector2 vector = SceneCamera.Instance.Camera.ScreenToWorldPoint(sceneScreen.Pointer.Point);
				Vector2Int vector2Int = new Vector2Int((int)Math.Round(vector.x), (int)Math.Round(vector.y));
				if (!m_selecting)
				{
					m_selecting = true;
					m_selectionStart = vector2Int;
					m_selection.Value = default(RectInt);
				}
				else if ((float)(m_selectionStart - vector2Int).sqrMagnitude >= 1f)
				{
					int num = Math.Min(m_selectionStart.x, vector2Int.x);
					int num2 = Math.Min(m_selectionStart.y, vector2Int.y);
					int num3 = Math.Max(m_selectionStart.x, vector2Int.x);
					int num4 = Math.Max(m_selectionStart.y, vector2Int.y);
					m_selection.Value = new RectInt(num, num2, num3 - num + 1, num4 - num2 + 1);
				}
			}
			else
			{
				m_selecting = false;
			}
		}

		[RequiresSyncPoint]
		private void PlaceOrEraseParts(PartManager partManager, UserInterface userInterface)
		{
			PartOperationMode mode = userInterface.PartOperator.Mode;
			SceneScreen sceneScreen = userInterface.SceneScreen;
			if (sceneScreen.PreviousPointer.IsPressed && sceneScreen.Pointer.IsPressed)
			{
				PartScene activeScene = partManager.ActiveScene;
				PartSelector partSelector = userInterface.PartSelector;
				PartTypeInfo partTypeInfo = new PartTypeInfo(partSelector.PartType, partSelector.PartIndex);
				Vector2 vector = SceneCamera.Instance.Camera.ScreenToWorldPoint(sceneScreen.PreviousPointer.Point);
				Vector2 vector2 = SceneCamera.Instance.Camera.ScreenToWorldPoint(sceneScreen.Pointer.Point);
				Segment segment = new Segment(vector, vector2, m_settings.BrushShape, m_settings.BrushSize);
				activeScene.Unmanaged.EntityManager.CompleteAllTrackedJobs();
				if (mode == PartOperationMode.Place)
				{
					Entity entity = partManager.EntitySpawner.FindPrefab(partTypeInfo);
					PartCreationRequest request = new PartCreationRequest(entity, activeScene.SceneID, partTypeInfo, 0, 0, partSelector.Rotation, partSelector.Flipped);
					IJobExtensions.Run(new PlacePartsJob
					{
						Overlay = Settings.Overlay,
						Segment = segment,
						Request = request,
						PartScene = activeScene.UnmanagedRef
					});
				}
				else
				{
					IJobExtensions.Run(new ErasePartsJob
					{
						PartType = Settings.PartType,
						PartIndex = Settings.PartIndex,
						Segment = segment,
						PartScene = activeScene.UnmanagedRef
					});
				}
			}
		}
	}
}
