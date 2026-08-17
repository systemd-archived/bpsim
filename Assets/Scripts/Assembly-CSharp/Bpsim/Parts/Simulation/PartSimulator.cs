using Bpsim.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Bpsim.Parts.Simulation
{
	public class PartSimulator
	{
		[BurstCompile]
		private struct InstantiatePartsJob : IJob
		{
			public EntityManager EntityManager;

			public Reference<PartSceneUnmanaged> Source;

			public Reference<PartSimulatorUnmanaged> Destination;

			public void Execute()
			{
				foreach (Entity part in Source.Value.Parts)
				{
					Entity value = EntityManager.Instantiate(part);
					Destination.Value.Parts.Add(in value);
				}
			}
		}

		[BurstCompile]
		private struct BuildPartsJob : IJob
		{
			public ComponentLookup<PartSceneID> PartSceneIDLookup;

			public PartAspect.Lookup PartAspectLookup;

			public Reference<PartSceneUnmanaged> Source;

			public Reference<PartSimulatorUnmanaged> Destination;

			public void Execute()
			{
				int length = Source.Value.Parts.Length;
				using NativeParallelHashMap<Entity, Entity> nativeParallelHashMap = new NativeParallelHashMap<Entity, Entity>(length, Allocator.Temp);
				for (int i = 0; i < length; i++)
				{
					Entity entity = Source.Value.Parts[i];
					PartAspect partAspect = PartAspectLookup[entity];
					int coordX = partAspect.CoordX;
					int coordY = partAspect.CoordY;
					Entity entity2 = Destination.Value.Parts[i];
					PartSceneIDLookup[entity2] = new PartSceneID
					{
						Value = -1
					};
					nativeParallelHashMap.Add(entity, entity2);
					Destination.Value.PartMap.TryGet(coordX, coordY, 0, out var part);
					if (BasePart.IsContainer(partAspect.TypeInfo))
					{
						part.PartContainer = entity2;
						Destination.Value.PartMap.Set(coordX, coordY, 0, part);
					}
					else
					{
						part.Part = entity2;
						Destination.Value.PartMap.Set(coordX, coordY, 0, part);
					}
				}
				for (int j = 0; j < length; j++)
				{
					PartAspect partAspect2 = PartAspectLookup[Source.Value.Parts[j]];
					PartAspect partAspect3 = PartAspectLookup[Destination.Value.Parts[j]];
					if (partAspect2.ContainedPart != Entity.Null)
					{
						partAspect3.ContainedPart = nativeParallelHashMap[partAspect2.ContainedPart];
					}
					if (partAspect2.PartContainer != Entity.Null)
					{
						partAspect3.PartContainer = nativeParallelHashMap[partAspect2.PartContainer];
					}
				}
			}
		}

		[BurstCompile]
		private struct BuildRigidbodiesJob : IJob
		{
			public EntityCommandBuffer CommandBuffer;

			[ReadOnly]
			public ComponentLookup<LocalTransform> LocalTransformLookup;

			[ReadOnly]
			public ComponentLookup<PhysicsCollider> PhysicsColliderLookup;

			[ReadOnly]
			public ComponentLookup<PartTypeValue> PartTypeLookup;

			public Reference<PartSimulatorUnmanaged> Simulator;

			[ReadOnly]
			public NativeParallelHashMap<PartTypeInfo, PartExtensionData> ExtensionMap;

			public void Execute()
			{
				foreach (Entity part in Simulator.Value.Parts)
				{
					if (PhysicsColliderLookup.HasComponent(part))
					{
						PartTypeInfo value = PartTypeLookup[part].Value;
						bool kinematic = IsKinematic(value);
						PartSimulatorUnmanaged.AddPhysicsComponents(CommandBuffer, LocalTransformLookup, PhysicsColliderLookup, part, kinematic, ExtensionMap[value].Mass, 0.2f, 0f);
					}
				}
			}
		}

		[BurstCompile]
		private struct BuildJointsAndComponentsJob : IJob
		{
			public EntityCommandBuffer CommandBuffer;

			[ReadOnly]
			public ComponentLookup<WorldTransform> WorldTransformLookup;

			public PartAspect.Lookup PartAspectLookup;

			public Reference<PartSimulatorUnmanaged> Simulator;

			public float ConnectionStrengthFactor;

			public void Execute()
			{
				NativeList<Entity> parts = Simulator.Value.Parts;
				using NativeParallelHashMap<Entity, int> nativeParallelHashMap = new NativeParallelHashMap<Entity, int>(parts.Length, Allocator.Temp);
				for (int i = 0; i < parts.Length; i++)
				{
					nativeParallelHashMap.Add(parts[i], i);
				}
				using NativeDisjointSet disjointSet = new NativeDisjointSet(parts.Length, Allocator.Temp);
				foreach (Entity part2 in Simulator.Value.Parts)
				{
					PartAspect sourceAspect = PartAspectLookup[part2];
					int coordX = sourceAspect.CoordX;
					int coordY = sourceAspect.CoordY;
					if (BasePart.IsContainer(sourceAspect.TypeInfo) && sourceAspect.ContainedPart != Entity.Null)
					{
						PartAspect partAspect = PartAspectLookup[sourceAspect.ContainedPart];
						float maxImpulse = ConnectionStrengthFactor * (sourceAspect.ExtensionData.ConnectionStrength + partAspect.ExtensionData.ConnectionStrength);
						PartSimulatorUnmanaged.AddFixedJoint(CommandBuffer, WorldTransformLookup, part2, sourceAspect.ContainedPart, maxImpulse, enableCollision: false);
						disjointSet.Union(nativeParallelHashMap[part2], nativeParallelHashMap[sourceAspect.ContainedPart]);
					}
					for (int j = 0; j < 4; j++)
					{
						BitDirection direction = BitDirectionExtensions.FromIndex(j);
						var (num, num2) = direction.ToVector();
						if (!Simulator.Value.PartMap.TryGet(coordX + num, coordY + num2, 0, out var part))
						{
							continue;
						}
						for (int k = 0; k < 2; k++)
						{
							Entity entity = ((k == 0) ? part.PartContainer : part.Part);
							if (!(entity == Entity.Null))
							{
								PartAspect targetAspect = PartAspectLookup[entity];
								if (CanConnectTo(part2, in sourceAspect, entity, in targetAspect, direction))
								{
									float maxImpulse2 = ConnectionStrengthFactor * (sourceAspect.ExtensionData.ConnectionStrength + targetAspect.ExtensionData.ConnectionStrength);
									PartSimulatorUnmanaged.AddFixedJoint(CommandBuffer, WorldTransformLookup, part2, entity, maxImpulse2, enableCollision: false);
									disjointSet.Union(nativeParallelHashMap[part2], nativeParallelHashMap[entity]);
								}
							}
						}
					}
				}
				PartSimulation.UpdateConnectedComponents(Simulator, PartAspectLookup, disjointSet);
			}
		}

		private Entity m_physicsStep;

		private EntityManager m_entityManager;

		private Reference<PartSimulatorUnmanaged> m_unmanaged;

		public ref PartSimulatorUnmanaged Unmanaged => ref m_unmanaged.Value;

		public Reference<PartSimulatorUnmanaged> UnmanagedRef => m_unmanaged;

		public void Run(Reference<PartSceneUnmanaged> partScene)
		{
			m_unmanaged = Reference<PartSimulatorUnmanaged>.Allocate(Allocator.Persistent);
			m_unmanaged.Value.Initialize(partScene);
			SimulationSettings simulationSettings = UserSettings.Instance.SimulationSettings;
			PartSceneSystem system = PartManager.Instance.System;
			EntityManager entityManager = (m_entityManager = partScene.Value.EntityManager);
			CreatePhysicsStep(entityManager);
			using NativeParallelHashMap<PartTypeInfo, PartExtensionData> extensionMap = PartManager.Instance.Factory.PartExtensionMap.ToNative(Allocator.TempJob);
			entityManager.CompleteAllTrackedJobs();
			IJobExtensions.Run(new InstantiatePartsJob
			{
				EntityManager = entityManager,
				Source = partScene,
				Destination = m_unmanaged
			});
			EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			IJobExtensions.Run(new BuildPartsJob
			{
				PartSceneIDLookup = system.GetComponentLookup<PartSceneID>(),
				PartAspectLookup = new PartAspect.Lookup(ref system.CheckedStateRef, isReadOnly: false),
				Source = partScene,
				Destination = m_unmanaged
			});
			IJobExtensions.Run(new BuildRigidbodiesJob
			{
				CommandBuffer = commandBuffer,
				LocalTransformLookup = system.GetComponentLookup<LocalTransform>(isReadOnly: true),
				PhysicsColliderLookup = system.GetComponentLookup<PhysicsCollider>(isReadOnly: true),
				PartTypeLookup = system.GetComponentLookup<PartTypeValue>(isReadOnly: true),
				Simulator = m_unmanaged,
				ExtensionMap = extensionMap
			});
			if (simulationSettings.CreateJoints)
			{
				float num = (simulationSettings.InfiniteConnectionStrength ? float.PositiveInfinity : simulationSettings.ConnectionStrengthFactor);
				IJobExtensions.Run(new BuildJointsAndComponentsJob
				{
					CommandBuffer = commandBuffer,
					WorldTransformLookup = system.GetComponentLookup<WorldTransform>(isReadOnly: true),
					PartAspectLookup = new PartAspect.Lookup(ref system.CheckedStateRef, isReadOnly: false),
					Simulator = m_unmanaged,
					ConnectionStrengthFactor = num * Time.fixedDeltaTime
				});
				system.World.GetOrCreateSystemManaged<FrameJointSystem>().SetDirty();
			}
			commandBuffer.Playback(entityManager);
			commandBuffer.Dispose();
		}

		private void CreatePhysicsStep(EntityManager entityManager)
		{
			SimulationSettings simulationSettings = UserSettings.Instance.SimulationSettings;
			Entity entity = entityManager.CreateEntity();
			PhysicsStep componentData = PhysicsStep.Default;
			componentData.SimulationType = simulationSettings.SimulationType;
			componentData.Gravity = new float3(simulationSettings.GravityX, simulationSettings.GravityY, 0f);
			componentData.SolverIterationCount = simulationSettings.SolverIterationCount;
			entityManager.AddComponentData(entity, componentData);
			m_physicsStep = entity;
		}

		public void Dispose()
		{
			m_entityManager.DestroyEntity(m_physicsStep);
			m_unmanaged.Value.Dispose(m_entityManager);
			Reference<PartSimulatorUnmanaged>.Free(m_unmanaged, Allocator.Persistent);
		}

		public static bool IsKinematic(PartTypeInfo info)
		{
			return info.PartType == PartType.WoodenFrame;
		}

		public static bool CanConnectTo(Entity source, in PartAspect sourceAspect, Entity target, in PartAspect targetAspect, BitDirection direction)
		{
			if (!sourceAspect.ExtensionData.IsConnectionSource)
			{
				return false;
			}
			if ((sourceAspect.ExtensionData.ConnectionDirection.Rotate(sourceAspect.Rotation) & direction) == 0)
			{
				return false;
			}
			if ((targetAspect.ExtensionData.ConnectionDirection.Rotate(targetAspect.Rotation) & direction.Reverse()) == 0)
			{
				return false;
			}
			if (!targetAspect.ExtensionData.IsConnectionSource)
			{
				return true;
			}
			return source.Index < target.Index;
		}
	}
}
