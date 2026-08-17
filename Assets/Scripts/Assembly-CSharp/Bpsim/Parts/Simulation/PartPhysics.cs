using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Bpsim.Parts.Simulation
{
	public static class PartPhysics
	{
		public static BlobAssetReference<Collider> CreateBox(float3 center, float3 size)
		{
			return BoxCollider.Create(new BoxGeometry
			{
				Center = center,
				Orientation = quaternion.identity,
				Size = size,
				BevelRadius = math.min(ConvexHullGenerationParameters.Default.BevelRadius, 0.5f * math.cmin(size))
			});
		}

		public static BlobAssetReference<Collider> CreateSphere(float3 center, float radius)
		{
			return SphereCollider.Create(new SphereGeometry
			{
				Center = center,
				Radius = radius
			});
		}

		public static BlobAssetReference<Collider> CreateCapsule(float3 point0, float3 point1, float radius)
		{
			return CapsuleCollider.Create(new CapsuleGeometry
			{
				Vertex0 = point0,
				Vertex1 = point1,
				Radius = radius
			});
		}

		public static PhysicsJoint CreateFixedJoint(BodyFrame bodyFrameA, BodyFrame bodyFrameB)
		{
			return PhysicsJoint.CreateFixed(bodyFrameA, bodyFrameB);
		}

		public static void AddCollider(EntityManager entityManager, Entity entity, BlobAssetReference<Collider> collider)
		{
			entityManager.AddComponentData(entity, new PhysicsCollider
			{
				Value = collider
			});
		}

		public static void AddCollider(EntityCommandBuffer commandBuffer, Entity entity, BlobAssetReference<Collider> collider)
		{
			commandBuffer.AddComponent(entity, new PhysicsCollider
			{
				Value = collider
			});
		}

		public static void AddRigidbody(EntityManager entityManager, Entity entity, bool kinematic, MassProperties massProperties, float mass, float linearDamping, float angularDamping)
		{
			if (!kinematic)
			{
				entityManager.AddComponentData(entity, PhysicsMass.CreateDynamic(massProperties, mass));
			}
			else
			{
				entityManager.AddComponentData(entity, PhysicsMass.CreateKinematic(massProperties));
			}
			entityManager.AddComponentData(entity, PhysicsVelocity.Zero);
			entityManager.AddComponentData(entity, new PhysicsDamping
			{
				Linear = linearDamping,
				Angular = angularDamping
			});
		}

		public static void AddRigidbody(EntityCommandBuffer commandBuffer, Entity entity, bool kinematic, MassProperties massProperties, float mass, float linearDamping, float angularDamping)
		{
			if (!kinematic)
			{
				commandBuffer.AddComponent(entity, PhysicsMass.CreateDynamic(massProperties, mass));
			}
			else
			{
				commandBuffer.AddComponent(entity, PhysicsMass.CreateKinematic(massProperties));
			}
			commandBuffer.AddComponent(entity, PhysicsVelocity.Zero);
			commandBuffer.AddComponent(entity, new PhysicsDamping
			{
				Linear = linearDamping,
				Angular = angularDamping
			});
		}
	}
}
