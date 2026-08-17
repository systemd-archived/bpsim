using System;
using Bpsim.Parts.Simulation;
using Bpsim.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Rendering.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Bpsim.Parts
{
	internal static class PartEntityFactory
	{
		public class Context
		{
			public EntityManager EntityManager { get; private set; }

			public RenderMeshArray RenderMeshArray { get; private set; }

			public Context(EntityManager entityManager, UnityEngine.Material material, Mesh mesh)
			{
				EntityManager = entityManager;
				RenderMeshArray = new RenderMeshArray(new UnityEngine.Material[1] { material }, new Mesh[1] { mesh });
			}
		}

		public static Entity Create(Context context, GameObject gameObject, bool prefab)
		{
			Entity entity = context.EntityManager.CreateEntity();
			context.EntityManager.SetName(entity, gameObject.name);
			CreateComponents(context, gameObject.GetComponents<Component>(), entity);
			if (prefab)
			{
				context.EntityManager.AddComponent<Prefab>(entity);
			}
			context.EntityManager.AddBuffer<LinkedEntityGroup>(entity).Add(entity);
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				CreateChild(context, gameObject.transform.GetChild(i).gameObject, entity, entity, prefab);
			}
			BuildCompoundCollider(context, entity);
			return entity;
		}

		private static void BuildCompoundCollider(Context context, Entity entity)
		{
			DynamicBuffer<Entity> dynamicBuffer = context.EntityManager.GetBuffer<LinkedEntityGroup>(entity).Reinterpret<Entity>();
			NativeList<CompoundCollider.ColliderBlobInstance> nativeList = new NativeList<CompoundCollider.ColliderBlobInstance>(4, Allocator.TempJob);
			try
			{
				for (int i = 1; i < dynamicBuffer.Length; i++)
				{
					Entity entity2 = dynamicBuffer[i];
					if (context.EntityManager.HasComponent<PhysicsCollider>(entity2))
					{
						PhysicsCollider componentData = context.EntityManager.GetComponentData<PhysicsCollider>(entity2);
						CompoundCollider.ColliderBlobInstance value = new CompoundCollider.ColliderBlobInstance
						{
							CompoundFromChild = RigidTransform.identity,
							Collider = componentData.Value,
							Entity = entity2
						};
						nativeList.Add(in value);
					}
				}
				if (nativeList.Length > 0)
				{
					if (context.EntityManager.HasComponent<PhysicsCollider>(entity))
					{
						PhysicsCollider componentData2 = context.EntityManager.GetComponentData<PhysicsCollider>(entity);
						CompoundCollider.ColliderBlobInstance value = new CompoundCollider.ColliderBlobInstance
						{
							CompoundFromChild = RigidTransform.identity,
							Collider = componentData2.Value,
							Entity = entity
						};
						nativeList.Add(in value);
						BlobAssetReference<Unity.Physics.Collider> value2 = CompoundCollider.Create(nativeList.AsArray());
						context.EntityManager.SetComponentData(entity, new PhysicsCollider
						{
							Value = value2
						});
					}
					else
					{
						BlobAssetReference<Unity.Physics.Collider> value3 = CompoundCollider.Create(nativeList.AsArray());
						context.EntityManager.AddComponentData(entity, new PhysicsCollider
						{
							Value = value3
						});
					}
				}
			}
			finally
			{
				((IDisposable)nativeList/*cast due to .constrained prefix*/).Dispose();
			}
		}

		public static Entity CreateChild(Context context, GameObject gameObject, Entity rootEntity, Entity parentEntity, bool prefab)
		{
			if (gameObject.name == "Foreground")
			{
				return Entity.Null;
			}
			Entity entity = context.EntityManager.CreateEntity();
			context.EntityManager.SetName(entity, gameObject.name);
			context.EntityManager.AddComponentData(entity, new Parent
			{
				Value = parentEntity
			});
			context.EntityManager.GetBuffer<LinkedEntityGroup>(rootEntity).Add(entity);
			CreateComponents(context, gameObject.GetComponents<Component>(), entity);
			if (prefab)
			{
				context.EntityManager.AddComponent<Prefab>(entity);
			}
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				CreateChild(context, gameObject.transform.GetChild(i).gameObject, rootEntity, entity, prefab);
			}
			return entity;
		}

		public static void CreateComponents(Context context, Component[] components, Entity entity)
		{
			foreach (Component component in components)
			{
				CreateComponent(context, component, entity);
			}
		}

		public static void CreateComponent(Context context, Component component, Entity entity)
		{
			if (!(component is Transform component2))
			{
				if (!(component is Renderer component3))
				{
					if (!(component is SpriteBase component4))
					{
						if (!(component is UnityEngine.Collider component5))
						{
							if (component is ManagedPart component6)
							{
								CreatePart(context, component6, entity);
							}
						}
						else
						{
							CreateColllider(context, component5, entity);
						}
					}
					else
					{
						CreateSprite(context, component4, entity);
					}
				}
				else
				{
					CreateRenderer(context, component3, entity);
				}
			}
			else
			{
				CreateTransform(context, component2, entity);
			}
		}

		public static void CreateTransform(Context context, Transform component, Entity entity)
		{
			Vector3 localScale = component.localScale;
			bool flag = System.Math.Abs(localScale.x - localScale.y) < 1E-05f && System.Math.Abs(localScale.y - localScale.z) < 1E-05f && System.Math.Abs(localScale.z - localScale.x) < 1E-05f;
			LocalTransform componentData = LocalTransform.FromPositionRotationScale(component.localPosition, component.localRotation, flag ? localScale.x : 1f);
			context.EntityManager.AddComponentData(entity, componentData);
			context.EntityManager.AddComponentData(entity, new LocalToWorld
			{
				Value = float4x4.identity
			});
			if (!flag)
			{
				context.EntityManager.AddComponentData(entity, new PostTransformScale
				{
					Value = float3x3.Scale(localScale)
				});
			}
		}

		public static void CreateRenderer(Context context, Renderer component, Entity entity)
		{
			Color color = component.sharedMaterial.color;
			Unity.Rendering.Authoring.MaterialColor component2 = component.GetComponent<Unity.Rendering.Authoring.MaterialColor>();
			if (component2 != null)
			{
				color = component2.color;
			}
			context.EntityManager.AddComponentData(entity, new Unity.Rendering.MaterialColor
			{
				Value = new float4(color.r, color.g, color.b, color.a)
			});
			context.EntityManager.AddComponent<PartRenderInfo>(entity);
			if (!component.enabled)
			{
				context.EntityManager.AddComponent<DisableRendering>(entity);
			}
		}

		public static void CreateSprite(Context context, SpriteBase component, Entity entity)
		{
			if (SpriteManager.Instance.TryGetID(component.SpriteName, out var id))
			{
				context.EntityManager.AddComponentData(entity, new SpriteID
				{
					Value = id
				});
				context.EntityManager.AddComponentData(entity, new MeshRect
				{
					Value = new float4(0f, 0f, 1f, 1f)
				});
				context.EntityManager.AddComponentData(entity, new MeshUVRect
				{
					Value = new float4(0f, 0f, 1f, 1f)
				});
				context.EntityManager.AddComponentData(entity, new BlendFactor
				{
					Value = new float4(1f, 0f, 0f, 0f)
				});
			}
		}

		public static void CreateColllider(Context context, UnityEngine.Collider component, Entity entity)
		{
			BlobAssetReference<Unity.Physics.Collider> value;
			if (!(component is UnityEngine.BoxCollider boxCollider))
			{
				if (!(component is UnityEngine.SphereCollider sphereCollider))
				{
					if (!(component is UnityEngine.CapsuleCollider capsuleCollider))
					{
						throw new InvalidCastException();
					}
					float3 @float = new float3(0f, 0.5f * capsuleCollider.height - capsuleCollider.radius, 0f);
					value = PartPhysics.CreateCapsule((float3)capsuleCollider.center + @float, (float3)capsuleCollider.center - @float, capsuleCollider.radius);
				}
				else
				{
					value = PartPhysics.CreateSphere(sphereCollider.center, sphereCollider.radius);
				}
			}
			else
			{
				value = PartPhysics.CreateBox(boxCollider.center, boxCollider.size);
			}
			context.EntityManager.AddComponentData(entity, new PhysicsCollider
			{
				Value = value
			});
		}

		public static void CreatePart(Context context, ManagedPart component, Entity entity)
		{
			PartTypeInfo partTypeInfo = new PartTypeInfo(component.PartType, component.PartIndex);
			context.EntityManager.AddComponent(entity, GetPartComponentTypes(partTypeInfo));
			context.EntityManager.SetComponentData(entity, new PartTypeValue
			{
				Type = partTypeInfo.PartType,
				Index = partTypeInfo.PartIndex
			});
			context.EntityManager.SetComponentData(entity, new PartExtensionComponent
			{
				Value = PartManager.Instance.Factory.PartExtensionMap[partTypeInfo]
			});
			if (partTypeInfo.BelongsTo(BasePart.ColoredFrames))
			{
				context.EntityManager.SetComponentData(entity, new ColoredFrame
				{
					IsTransparent = false,
					HasChanged = true,
					Color = ColoredFrame.GetColor(partTypeInfo)
				});
			}
			else if (partTypeInfo.BelongsTo(BasePart.TransparentFrames))
			{
				context.EntityManager.SetComponentData(entity, new ColoredFrame
				{
					IsTransparent = true,
					HasChanged = true,
					Color = ColoredFrame.GetColor(partTypeInfo)
				});
			}
		}

		private static ComponentTypeSet GetPartComponentTypes(PartTypeInfo typeInfo)
		{
			FixedList128Bytes<ComponentType> types = new FixedList128Bytes<ComponentType>
			{
				ComponentType.ReadOnly<PartTag>(),
				ComponentType.ReadOnly<PartTypeValue>(),
				ComponentType.ReadWrite<PartTransform>(),
				ComponentType.ReadWrite<ContainedPart>(),
				ComponentType.ReadWrite<PartContainerValue>(),
				ComponentType.ReadWrite<PartConnectedComponent>(),
				ComponentType.ReadWrite<PartExtensionComponent>()
			};
			if (typeInfo.BelongsTo(BasePart.ColoredFrames))
			{
				types.Add(ComponentType.ReadWrite<ColoredFrame>());
			}
			else if (typeInfo.BelongsTo(BasePart.TransparentFrames))
			{
				types.Add(ComponentType.ReadWrite<ColoredFrame>());
				types.Add(ComponentType.ReadWrite<TransparentFrame>());
			}
			else
			{
				PartType partType = typeInfo.PartType;
				if (partType == PartType.Fan || partType == PartType.Propeller || partType == PartType.Rotor)
				{
					types.Add(ComponentType.ReadWrite<FanPropeller>());
				}
				else
				{
					partType = typeInfo.PartType;
					if (partType == PartType.CokeBottle || partType == PartType.SodaBottle || partType == PartType.Rocket || partType == PartType.RedRocket)
					{
						types.Add(ComponentType.ReadWrite<Rocket>());
					}
					else if (typeInfo.PartType == PartType.GrapplingHook)
					{
						types.Add(ComponentType.ReadWrite<GrapplingHook>());
					}
					else if (typeInfo.PartType == PartType.Kicker)
					{
						types.Add(ComponentType.ReadWrite<Separator>());
					}
				}
			}
			return new ComponentTypeSet(in types);
		}
	}
}
