using Bpsim.Physics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Bpsim.Parts.Simulation
{
	public struct PartSimulatorUnmanaged
	{
		private NativeList<Entity> m_parts;

		private NativeList<Entity> m_joints;

		private PartGridMap<PartGridInfo> m_partMap;

		private NativeList<ConnectedComponentInfo> m_connectedComponents;

		public NativeList<Entity> Parts => m_parts;

		public NativeList<Entity> Joints => m_joints;

		public PartGridMap<PartGridInfo> PartMap => m_partMap;

		public NativeList<ConnectedComponentInfo> ConnectedComponents => m_connectedComponents;

		public void Initialize(Reference<PartSceneUnmanaged> partScene)
		{
			int num = math.max(partScene.Value.Parts.Length, 256);
			m_parts = new NativeList<Entity>(num, Allocator.Persistent);
			m_joints = new NativeList<Entity>(num, Allocator.Persistent);
			m_partMap = new PartGridMap<PartGridInfo>(num, Allocator.Persistent);
			m_connectedComponents = new NativeList<ConnectedComponentInfo>(64, Allocator.Persistent);
		}

		public Entity FindFirstPart(ComponentLookup<PartTypeValue> partTypeLookup, PartType partType, int partIndex = -1)
		{
			for (int i = 0; i < m_parts.Length; i++)
			{
				Entity entity = m_parts[i];
				PartTypeValue partTypeValue = partTypeLookup[entity];
				if (partTypeValue.Type == partType && (partIndex == -1 || partTypeValue.Index == partIndex))
				{
					return entity;
				}
			}
			return Entity.Null;
		}

		public Entity FindLastPart(ComponentLookup<PartTypeValue> partTypeLookup, PartType partType, int partIndex = -1)
		{
			for (int num = m_parts.Length - 1; num >= 0; num--)
			{
				Entity entity = m_parts[num];
				PartTypeValue partTypeValue = partTypeLookup[entity];
				if (partTypeValue.Type == partType && (partIndex == -1 || partTypeValue.Index == partIndex))
				{
					return entity;
				}
			}
			return Entity.Null;
		}

		public static void AddPhysicsComponents(EntityCommandBuffer commandBuffer, ComponentLookup<LocalTransform> localTransformLookup, ComponentLookup<PhysicsCollider> physicsColliderLookup, Entity entity, bool kinematic, float mass, float linearDamping, float angularDamping)
		{
			commandBuffer.AddSharedComponent(entity, new PhysicsWorldIndex(0u));
			MassProperties massProperties = physicsColliderLookup[entity].Value.Value.MassProperties;
			PartPhysics.AddRigidbody(commandBuffer, entity, kinematic, massProperties, mass, linearDamping, angularDamping);
			LocalTransform transform = localTransformLookup[entity];
			commandBuffer.AddComponent(entity, Constraint2D.CreateFromTransform(transform));
		}

		public static void AddFixedJoint(EntityCommandBuffer commandBuffer, ComponentLookup<WorldTransform> worldTransformLookup, Entity entityA, Entity entityB, float maxImpulse, bool enableCollision)
		{
			WorldTransform worldTransform = worldTransformLookup[entityA];
			WorldTransform worldTransform2 = worldTransformLookup[entityB];
			RigidTransform t = new RigidTransform(worldTransform.Rotation, worldTransform.Position);
			RigidTransform t2 = new RigidTransform(worldTransform2.Rotation, worldTransform2.Position);
			RigidTransform b = new RigidTransform
			{
				rot = quaternion.identity,
				pos = 0.5f * (worldTransform.Position + worldTransform2.Position)
			};
			RigidTransform transform = math.mul(math.inverse(t), b);
			PhysicsJoint component = PartPhysics.CreateFixedJoint(bodyFrameB: new BodyFrame(math.mul(math.inverse(t2), b)), bodyFrameA: new BodyFrame(transform));
			component.SetImpulseEventThresholdAllConstraints(maxImpulse, float.PositiveInfinity);
			Entity e = commandBuffer.CreateEntity();
			commandBuffer.SetName(e, (FixedString64Bytes)"JointEntity");
			commandBuffer.AddSharedComponent(e, new PhysicsWorldIndex(0u));
			commandBuffer.AddComponent(e, component);
			commandBuffer.AddComponent(e, new PhysicsConstrainedBodyPair(entityA, entityB, enableCollision));
			commandBuffer.AddComponent(e, new PartJointInfo
			{
				Type = PartJointType.Normal,
				State = 0
			});
		}

		public void AddJoint(Entity entityA, Entity entityB, Entity jointEntity)
		{
			m_joints.Add(in jointEntity);
		}

		public void Dispose(EntityManager entityManager)
		{
			foreach (Entity part in m_parts)
			{
				entityManager.DestroyEntity(part);
			}
			foreach (Entity joint in m_joints)
			{
				entityManager.DestroyEntity(joint);
			}
			m_parts.Dispose();
			m_joints.Dispose();
			m_partMap.Dispose();
			m_connectedComponents.Dispose();
		}
	}
}
