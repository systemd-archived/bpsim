using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Bpsim.Parts
{
	public struct PartSceneUnmanaged : IDisposable
	{
		private bool m_dirty;

		private int m_id;

		private NativeList<Entity> m_parts;

		private NativeParallelHashSet<Entity> m_deletedParts;

		private PartGridMap<PartGridInfo> m_partMap;

		private EntityManager m_entityManager;

		public int SceneID => m_id;

		public EntityManager EntityManager => m_entityManager;

		public NativeList<Entity> Parts => m_parts;

		public PartGridMap<PartGridInfo> PartMap => m_partMap;

		public void Initialize(int id, WorldUnmanaged world)
		{
			m_id = id;
			m_parts = new NativeList<Entity>(256, Allocator.Persistent);
			m_deletedParts = new NativeParallelHashSet<Entity>(256, Allocator.Persistent);
			m_partMap = new PartGridMap<PartGridInfo>(256, Allocator.Persistent);
			m_entityManager = world.EntityManager;
		}

		public void SetDirty()
		{
			m_dirty = true;
		}

		public void Update()
		{
			if (!m_dirty)
			{
				return;
			}
			m_dirty = false;
			int length = m_parts.Length;
			int i = 0;
			int length2 = 0;
			while (i < length)
			{
				for (; i < length && m_deletedParts.Contains(m_parts[i]); i++)
				{
				}
				if (i < length)
				{
					m_parts[length2++] = m_parts[i++];
				}
			}
			m_parts.Resize(length2, NativeArrayOptions.UninitializedMemory);
			foreach (Entity deletedPart in m_deletedParts)
			{
				EntityManager.DestroyEntity(deletedPart);
			}
			m_deletedParts.Clear();
		}

		public void OnDestroy()
		{
			foreach (Entity part in m_parts)
			{
				EntityManager.DestroyEntity(part);
			}
		}

		public void Dispose()
		{
			m_parts.Dispose();
			m_partMap.Dispose();
			m_deletedParts.Dispose();
		}

		public Entity FindFirstPart(PartType partType, int partIndex = -1)
		{
			for (int i = 0; i < m_parts.Length; i++)
			{
				Entity entity = m_parts[i];
				PartTypeValue componentData = EntityManager.GetComponentData<PartTypeValue>(entity);
				if (componentData.Type == partType && (partIndex == -1 || componentData.Index == partIndex))
				{
					return entity;
				}
			}
			return Entity.Null;
		}

		public Entity FindLastPart(PartType partType, int partIndex = -1)
		{
			for (int num = m_parts.Length - 1; num >= 0; num--)
			{
				Entity entity = m_parts[num];
				PartTypeValue componentData = EntityManager.GetComponentData<PartTypeValue>(entity);
				if (componentData.Type == partType && (partIndex == -1 || componentData.Index == partIndex))
				{
					return entity;
				}
			}
			return Entity.Null;
		}

		public bool FindPartAt(int x, int y, out Entity part)
		{
			if (m_partMap.TryGet(x, y, 0, out var part2))
			{
				if (part2.HasPartContainer)
				{
					part = part2.PartContainer;
					return true;
				}
				if (part2.HasPart)
				{
					part = part2.Part;
					return true;
				}
			}
			part = Entity.Null;
			return false;
		}

		public bool FindPartGridAt(int x, int y, out PartGridInfo grid)
		{
			return m_partMap.TryGet(x, y, 0, out grid);
		}

		public bool PlacePartAt(in PartCreationRequest request, bool overlay)
		{
			int coordX = request.CoordX;
			int coordY = request.CoordY;
			m_partMap.TryGet(coordX, coordY, 0, out var part);
			PartTypeInfo typeInfo = request.TypeInfo;
			if (!CanPlacePartAt(coordX, coordY, typeInfo, part.Occupied))
			{
				return false;
			}
			bool flag = false;
			Entity part2 = Entity.Null;
			if (BasePart.IsContainer(typeInfo))
			{
				bool flag2 = !part.HasPart || BasePart.CanBeContained(EntityManager.GetComponentData<PartTypeValue>(part.Part).Value);
				if ((!part.HasPartContainer && flag2) || overlay)
				{
					if (part.HasPartContainer)
					{
						RemovePart(part.PartContainer);
					}
					if (part.HasPart && !flag2)
					{
						SetLargePartGridAt(coordX, coordY, -1, part.Part);
						RemovePart(part.Part);
						part.Part = Entity.Null;
					}
					flag = true;
					part2 = (part.PartContainer = request.Submit(in this));
				}
			}
			else if (BasePart.CanBeContained(typeInfo))
			{
				if (!part.HasPart || overlay)
				{
					if (part.HasPart)
					{
						SetLargePartGridAt(coordX, coordY, -1, part.Part);
						RemovePart(part.Part);
					}
					flag = true;
					part2 = (part.Part = request.Submit(in this));
				}
			}
			else if ((!part.HasPart && !part.HasPartContainer) || overlay)
			{
				if (part.HasPartContainer)
				{
					RemovePart(part.PartContainer);
					part.PartContainer = Entity.Null;
				}
				if (part.HasPart)
				{
					SetLargePartGridAt(coordX, coordY, -1, part.Part);
					RemovePart(part.Part);
				}
				flag = true;
				part2 = (part.Part = request.Submit(in this));
			}
			if (flag)
			{
				AddPart(part2);
				UpdateContainer(part);
				SetPartGridAt(coordX, coordY, part);
				SetLargePartGridAt(coordX, coordY, 1, part2);
				UpdateNeighbours(coordX, coordY);
			}
			return flag;
		}

		public bool RemovePartAt(int x, int y, PartType partType = PartType.All, int partIndex = -1)
		{
			if (!m_partMap.TryGet(x, y, 0, out var part))
			{
				return false;
			}
			bool flag = false;
			if (part.HasPartContainer && Match(in this, part.PartContainer, partType, partIndex))
			{
				flag = true;
				RemovePart(part.PartContainer);
				part.PartContainer = Entity.Null;
			}
			if (part.HasPart && Match(in this, part.Part, partType, partIndex))
			{
				flag = true;
				SetLargePartGridAt(x, y, -1, part.Part);
				RemovePart(part.Part);
				part.Part = Entity.Null;
			}
			if (flag)
			{
				UpdateContainer(part);
				SetPartGridAt(x, y, part);
				UpdateNeighbours(x, y);
			}
			return flag;
			static bool Match(in PartSceneUnmanaged scene, Entity entity, PartType partType2, int num)
			{
				PartTypeInfo value = scene.EntityManager.GetComponentData<PartTypeValue>(entity).Value;
				if (partType2 == PartType.All || partType2 == value.PartType)
				{
					if (num != -1)
					{
						return num == value.PartIndex;
					}
					return true;
				}
				return false;
			}
		}

		private void AddPart(Entity part)
		{
			m_parts.Add(in part);
		}

		private void RemovePart(Entity part)
		{
			SetDirty();
			m_deletedParts.Add(part);
		}

		private bool CanPlacePartAt(int x, int y, PartTypeInfo typeInfo, int occupied)
		{
			if (occupied > 0 && !BasePart.IsStructural(typeInfo))
			{
				return false;
			}
			if (BasePart.IsLarge(typeInfo))
			{
				RectInt gridRect = BasePart.GetGridRect(typeInfo);
				for (int i = gridRect.xMin; i < gridRect.xMax; i++)
				{
					for (int j = gridRect.yMin; j < gridRect.yMax; j++)
					{
						if ((i != 0 || j != 0) && m_partMap.TryGet(x + i, y + j, 0, out var part) && part.HasPart && !BasePart.IsStructural(EntityManager.GetComponentData<PartTypeValue>(part.Part).Value))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		private void SetPartGridAt(int x, int y, PartGridInfo grid)
		{
			if (grid.IsEmpty)
			{
				m_partMap.Remove(x, y, 0);
			}
			else
			{
				m_partMap.Set(x, y, 0, grid);
			}
		}

		private void SetLargePartGridAt(int x, int y, int delta, Entity part)
		{
			PartTypeInfo value = EntityManager.GetComponentData<PartTypeValue>(part).Value;
			if (!BasePart.IsLarge(value))
			{
				return;
			}
			RectInt gridRect = BasePart.GetGridRect(value);
			for (int i = gridRect.xMin; i < gridRect.xMax; i++)
			{
				for (int j = gridRect.yMin; j < gridRect.yMax; j++)
				{
					if (i != 0 || j != 0)
					{
						m_partMap.TryGet(x + i, y + j, 0, out var part2);
						part2.Occupied += delta;
						m_partMap.Set(x + i, y + j, 0, part2);
					}
				}
			}
		}

		private void UpdateContainer(PartGridInfo grid)
		{
			if (grid.HasPartContainer)
			{
				EntityManager.SetComponentData(grid.PartContainer, new ContainedPart
				{
					Value = grid.Part
				});
			}
			if (grid.HasPart)
			{
				EntityManager.SetComponentData(grid.Part, new PartContainerValue
				{
					Value = grid.PartContainer
				});
			}
		}

		public void UpdateNeighbours(in PartAspect partAspect)
		{
			UpdateNeighbours(partAspect.CoordX, partAspect.CoordY);
		}

		private void UpdateNeighbours(int x, int y)
		{
			UpdateNeighbour(x, y);
			UpdateNeighbour(x + 1, y);
			UpdateNeighbour(x, y + 1);
			UpdateNeighbour(x - 1, y);
			UpdateNeighbour(x, y - 1);
		}

		private void UpdateNeighbour(int x, int y)
		{
			m_partMap.TryGet(x, y, 0, out var part);
			if (!part.HasPart)
			{
				return;
			}
			int attachmentCount = BasePart.GetAttachmentCount(EntityManager.GetComponentData<PartTypeValue>(part.Part).Value);
			if (attachmentCount == 0)
			{
				return;
			}
			NativeArray<Entity> attachments = new NativeArray<Entity>(attachmentCount, Allocator.Temp);
			foreach (Entity item in EntityManager.GetBuffer<LinkedEntityGroup>(part.Part).Reinterpret<Entity>())
			{
				EntityManager.GetName(item, out var name);
				int num = (int)BasePart.FindAttachment(in name);
				if (num != -1)
				{
					attachments[num] = item;
				}
			}
			AlignAttachments(part.Part, in attachments);
		}

		private void AlignAttachments(Entity part, in NativeArray<Entity> attachments)
		{
			PartTransform componentData = EntityManager.GetComponentData<PartTransform>(part);
			PartContainerValue componentData2 = EntityManager.GetComponentData<PartContainerValue>(part);
			int x = componentData.X;
			int y = componentData.Y;
			int num = componentData.Rotation % 8;
			bool flag = componentData2.Value != Entity.Null;
			Entity part2;
			bool flag2 = FindPartAt(x + 1, y, out part2);
			Entity part3;
			bool flag3 = FindPartAt(x, y + 1, out part3);
			Entity part4;
			bool flag4 = FindPartAt(x - 1, y, out part4);
			Entity part5;
			bool flag5 = FindPartAt(x, y - 1, out part5);
			for (int i = 0; i < attachments.Length; i++)
			{
				if (attachments[i] == Entity.Null)
				{
					continue;
				}
				bool flag6 = false;
				if (!flag)
				{
					if (i >= 4 == num >= 4)
					{
						int num2 = (i + num) % 4;
						flag6 = num2 switch
						{
							0 => flag2, 
							1 => flag3, 
							2 => flag4, 
							3 => flag5, 
							_ => throw new SwitchExpressionException(num2), 
						};
					}
					if (i == 3 && !flag2 && !flag3 && !flag4 && !flag5)
					{
						flag6 = true;
					}
				}
				if (EntityManager.HasComponent<DisableRendering>(attachments[i]) == flag6)
				{
					if (flag6)
					{
						EntityManager.RemoveComponent<DisableRendering>(attachments[i]);
					}
					else
					{
						EntityManager.AddComponent<DisableRendering>(attachments[i]);
					}
				}
			}
		}
	}
}
