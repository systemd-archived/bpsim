using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Properties;

namespace Bpsim.Parts
{
	public readonly struct PartAspect : IAspect, IQueryTypeParameter, IAspectCreate<PartAspect>
	{
		public struct Lookup
		{
			private byte __IsReadOnly;

			private ComponentLookup<ContainedPart> PartAspect_m_containedPartCAc;

			private ComponentLookup<PartConnectedComponent> PartAspect_m_connectedComponentCAc;

			private ComponentLookup<PartContainerValue> PartAspect_m_partContainerCAc;

			[ReadOnly]
			private ComponentLookup<PartExtensionComponent> PartAspect_m_extensionComponentCAc;

			private ComponentLookup<PartTransform> PartAspect_m_partTransformCAc;

			[ReadOnly]
			private ComponentLookup<PartTypeValue> PartAspect_m_partTypeCAc;

			private bool _IsReadOnly
			{
				get
				{
					return __IsReadOnly == 1;
				}
				set
				{
					__IsReadOnly = (byte)(value ? 1 : 0);
				}
			}

			public PartAspect this[Entity entity] => new PartAspect(PartAspect_m_containedPartCAc.GetRefRW(entity, _IsReadOnly), PartAspect_m_connectedComponentCAc.GetRefRW(entity, _IsReadOnly), PartAspect_m_partContainerCAc.GetRefRW(entity, _IsReadOnly), PartAspect_m_extensionComponentCAc.GetRefRO(entity), PartAspect_m_partTransformCAc.GetRefRW(entity, _IsReadOnly), PartAspect_m_partTypeCAc.GetRefRO(entity));

			public Lookup(ref SystemState state, bool isReadOnly)
			{
				__IsReadOnly = (byte)(isReadOnly ? 1 : 0);
				PartAspect_m_containedPartCAc = state.GetComponentLookup<ContainedPart>(isReadOnly);
				PartAspect_m_connectedComponentCAc = state.GetComponentLookup<PartConnectedComponent>(isReadOnly);
				PartAspect_m_partContainerCAc = state.GetComponentLookup<PartContainerValue>(isReadOnly);
				PartAspect_m_extensionComponentCAc = state.GetComponentLookup<PartExtensionComponent>(isReadOnly: true);
				PartAspect_m_partTransformCAc = state.GetComponentLookup<PartTransform>(isReadOnly);
				PartAspect_m_partTypeCAc = state.GetComponentLookup<PartTypeValue>(isReadOnly: true);
			}

			public void Update(ref SystemState state)
			{
				PartAspect_m_containedPartCAc.Update(ref state);
				PartAspect_m_connectedComponentCAc.Update(ref state);
				PartAspect_m_partContainerCAc.Update(ref state);
				PartAspect_m_extensionComponentCAc.Update(ref state);
				PartAspect_m_partTransformCAc.Update(ref state);
				PartAspect_m_partTypeCAc.Update(ref state);
			}
		}

		public struct ResolvedChunk
		{
			public NativeArray<ContainedPart> PartAspect_m_containedPartNaC;

			public NativeArray<PartConnectedComponent> PartAspect_m_connectedComponentNaC;

			public NativeArray<PartContainerValue> PartAspect_m_partContainerNaC;

			public NativeArray<PartExtensionComponent> PartAspect_m_extensionComponentNaC;

			public NativeArray<PartTransform> PartAspect_m_partTransformNaC;

			public NativeArray<PartTypeValue> PartAspect_m_partTypeNaC;

			public int Length;

			public PartAspect this[int index] => new PartAspect(new RefRW<ContainedPart>(PartAspect_m_containedPartNaC, index), new RefRW<PartConnectedComponent>(PartAspect_m_connectedComponentNaC, index), new RefRW<PartContainerValue>(PartAspect_m_partContainerNaC, index), new RefRO<PartExtensionComponent>(PartAspect_m_extensionComponentNaC, index), new RefRW<PartTransform>(PartAspect_m_partTransformNaC, index), new RefRO<PartTypeValue>(PartAspect_m_partTypeNaC, index));
		}

		public struct TypeHandle
		{
			private ComponentTypeHandle<ContainedPart> PartAspect_m_containedPartCAc;

			private ComponentTypeHandle<PartConnectedComponent> PartAspect_m_connectedComponentCAc;

			private ComponentTypeHandle<PartContainerValue> PartAspect_m_partContainerCAc;

			[ReadOnly]
			private ComponentTypeHandle<PartExtensionComponent> PartAspect_m_extensionComponentCAc;

			private ComponentTypeHandle<PartTransform> PartAspect_m_partTransformCAc;

			[ReadOnly]
			private ComponentTypeHandle<PartTypeValue> PartAspect_m_partTypeCAc;

			public TypeHandle(ref SystemState state, bool isReadOnly)
			{
				PartAspect_m_containedPartCAc = state.GetComponentTypeHandle<ContainedPart>(isReadOnly);
				PartAspect_m_connectedComponentCAc = state.GetComponentTypeHandle<PartConnectedComponent>(isReadOnly);
				PartAspect_m_partContainerCAc = state.GetComponentTypeHandle<PartContainerValue>(isReadOnly);
				PartAspect_m_extensionComponentCAc = state.GetComponentTypeHandle<PartExtensionComponent>(isReadOnly: true);
				PartAspect_m_partTransformCAc = state.GetComponentTypeHandle<PartTransform>(isReadOnly);
				PartAspect_m_partTypeCAc = state.GetComponentTypeHandle<PartTypeValue>(isReadOnly: true);
			}

			public void Update(ref SystemState state)
			{
				PartAspect_m_containedPartCAc.Update(ref state);
				PartAspect_m_connectedComponentCAc.Update(ref state);
				PartAspect_m_partContainerCAc.Update(ref state);
				PartAspect_m_extensionComponentCAc.Update(ref state);
				PartAspect_m_partTransformCAc.Update(ref state);
				PartAspect_m_partTypeCAc.Update(ref state);
			}

			public ResolvedChunk Resolve(ArchetypeChunk chunk)
			{
				ResolvedChunk result = default(ResolvedChunk);
				result.PartAspect_m_containedPartNaC = chunk.GetNativeArray(ref PartAspect_m_containedPartCAc);
				result.PartAspect_m_connectedComponentNaC = chunk.GetNativeArray(ref PartAspect_m_connectedComponentCAc);
				result.PartAspect_m_partContainerNaC = chunk.GetNativeArray(ref PartAspect_m_partContainerCAc);
				result.PartAspect_m_extensionComponentNaC = chunk.GetNativeArray(ref PartAspect_m_extensionComponentCAc);
				result.PartAspect_m_partTransformNaC = chunk.GetNativeArray(ref PartAspect_m_partTransformCAc);
				result.PartAspect_m_partTypeNaC = chunk.GetNativeArray(ref PartAspect_m_partTypeCAc);
				result.Length = chunk.Count;
				return result;
			}
		}

		public struct Enumerator : IEnumerator<PartAspect>, IEnumerator, IDisposable, IEnumerable<PartAspect>, IEnumerable
		{
			private ResolvedChunk _Resolved;

			private EntityQueryEnumerator _QueryEnumerator;

			private TypeHandle _Handle;

			public PartAspect Current => _Resolved[_QueryEnumerator.IndexInChunk];

			object IEnumerator.Current
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			internal Enumerator(EntityQuery query, TypeHandle typeHandle)
			{
				_QueryEnumerator = new EntityQueryEnumerator(query);
				_Handle = typeHandle;
				_Resolved = default(ResolvedChunk);
			}

			public void Dispose()
			{
				_QueryEnumerator.Dispose();
			}

			public bool MoveNext()
			{
				if (_QueryEnumerator.MoveNextHotLoop())
				{
					return true;
				}
				return MoveNextCold();
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private bool MoveNextCold()
			{
				ArchetypeChunk chunk;
				bool num = _QueryEnumerator.MoveNextColdLoop(out chunk);
				if (num)
				{
					_Resolved = _Handle.Resolve(chunk);
				}
				return num;
			}

			public Enumerator GetEnumerator()
			{
				return this;
			}

			void IEnumerator.Reset()
			{
				throw new NotImplementedException();
			}

			IEnumerator<PartAspect> IEnumerable<PartAspect>.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}
		}

		private readonly RefRO<PartTypeValue> m_partType;

		private readonly RefRW<PartTransform> m_partTransform;

		private readonly RefRW<ContainedPart> m_containedPart;

		private readonly RefRW<PartContainerValue> m_partContainer;

		private readonly RefRW<PartConnectedComponent> m_connectedComponent;

		private readonly RefRO<PartExtensionComponent> m_extensionComponent;

		public PartType PartType => m_partType.ValueRO.Type;

		public int PartIndex => m_partType.ValueRO.Index;

		public PartTypeInfo TypeInfo => new PartTypeInfo(m_partType.ValueRO.Type, m_partType.ValueRO.Index);

		public ref readonly PartExtensionData ExtensionData => ref m_extensionComponent.ValueRO.Value;

		[CreateProperty]
		public int CoordX
		{
			get
			{
				return m_partTransform.ValueRO.X;
			}
			set
			{
				m_partTransform.ValueRW.X = value;
			}
		}

		[CreateProperty]
		public int CoordY
		{
			get
			{
				return m_partTransform.ValueRO.Y;
			}
			set
			{
				m_partTransform.ValueRW.Y = value;
			}
		}

		[CreateProperty]
		public int Rotation
		{
			get
			{
				return m_partTransform.ValueRO.Rotation;
			}
			set
			{
				m_partTransform.ValueRW.Rotation = value;
			}
		}

		[CreateProperty]
		public bool Flipped
		{
			get
			{
				return m_partTransform.ValueRO.Flipped;
			}
			set
			{
				m_partTransform.ValueRW.Flipped = value;
			}
		}

		[CreateProperty]
		public Entity ContainedPart
		{
			get
			{
				return m_containedPart.ValueRO.Value;
			}
			set
			{
				m_containedPart.ValueRW.Value = value;
			}
		}

		[CreateProperty]
		public Entity PartContainer
		{
			get
			{
				return m_partContainer.ValueRO.Value;
			}
			set
			{
				m_partContainer.ValueRW.Value = value;
			}
		}

		[CreateProperty]
		public int ConnectedComponent
		{
			get
			{
				return m_connectedComponent.ValueRO.Index;
			}
			set
			{
				m_connectedComponent.ValueRW.Index = value;
			}
		}

		public PartAspect(RefRW<ContainedPart> partaspect_m_containedpartRef, RefRW<PartConnectedComponent> partaspect_m_connectedcomponentRef, RefRW<PartContainerValue> partaspect_m_partcontainerRef, RefRO<PartExtensionComponent> partaspect_m_extensioncomponentRef, RefRW<PartTransform> partaspect_m_parttransformRef, RefRO<PartTypeValue> partaspect_m_parttypeRef)
		{
			m_containedPart = partaspect_m_containedpartRef;
			m_connectedComponent = partaspect_m_connectedcomponentRef;
			m_partContainer = partaspect_m_partcontainerRef;
			m_extensionComponent = partaspect_m_extensioncomponentRef;
			m_partTransform = partaspect_m_parttransformRef;
			m_partType = partaspect_m_parttypeRef;
		}

		public PartAspect CreateAspect(Entity entity, ref SystemState systemState, bool isReadOnly)
		{
			return new Lookup(ref systemState, isReadOnly)[entity];
		}

		public void AddComponentRequirementsTo(ref UnsafeList<ComponentType> all, ref UnsafeList<ComponentType> any, ref UnsafeList<ComponentType> none, ref UnsafeList<ComponentType> disabled, ref UnsafeList<ComponentType> absent, bool isReadOnly)
		{
			if (isReadOnly)
			{
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<ContainedPart>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartConnectedComponent>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartContainerValue>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartExtensionComponent>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartTransform>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartTypeValue>());
			}
			else
			{
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadWrite<ContainedPart>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadWrite<PartConnectedComponent>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadWrite<PartContainerValue>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartExtensionComponent>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadWrite<PartTransform>());
				InternalCompilerInterface.CombineComponentType(ref all, ComponentType.ReadOnly<PartTypeValue>());
			}
		}

		public static Enumerator Query(EntityQuery query, TypeHandle typeHandle)
		{
			return new Enumerator(query, typeHandle);
		}

		public static void CompleteDependencyBeforeRO(ref SystemState state)
		{
			state.EntityManager.CompleteDependencyBeforeRO<ContainedPart>();
			state.EntityManager.CompleteDependencyBeforeRO<PartConnectedComponent>();
			state.EntityManager.CompleteDependencyBeforeRO<PartContainerValue>();
			state.EntityManager.CompleteDependencyBeforeRO<PartExtensionComponent>();
			state.EntityManager.CompleteDependencyBeforeRO<PartTransform>();
			state.EntityManager.CompleteDependencyBeforeRO<PartTypeValue>();
		}

		public static void CompleteDependencyBeforeRW(ref SystemState state)
		{
			state.EntityManager.CompleteDependencyBeforeRW<ContainedPart>();
			state.EntityManager.CompleteDependencyBeforeRW<PartConnectedComponent>();
			state.EntityManager.CompleteDependencyBeforeRW<PartContainerValue>();
			state.EntityManager.CompleteDependencyBeforeRO<PartExtensionComponent>();
			state.EntityManager.CompleteDependencyBeforeRW<PartTransform>();
			state.EntityManager.CompleteDependencyBeforeRO<PartTypeValue>();
		}
	}
}
