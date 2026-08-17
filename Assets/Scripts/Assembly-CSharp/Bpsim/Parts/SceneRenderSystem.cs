using System;
using Bpsim.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Bpsim.Parts
{
	[UpdateAfter(typeof(UpdatePresentationSystemGroup))]
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	internal class SceneRenderSystem : SystemBase
	{
		private readonly struct SortPriority : IComparable<SortPriority>
		{
			public readonly int Index;

			public readonly int Priority;

			public SortPriority(int index, int priority)
			{
				Index = index;
				Priority = priority;
			}

			int IComparable<SortPriority>.CompareTo(SortPriority other)
			{
				int num = Priority.CompareTo(other.Priority);
				if (num == 0)
				{
					return Index.CompareTo(other.Index);
				}
				return num;
			}
		}

		private readonly struct PropertyInfo
		{
			public readonly int ID;

			public readonly int Size;

			public PropertyInfo(int id, int size)
			{
				ID = id;
				Size = size;
			}
		}

		private readonly struct GpuBufferInfo
		{
			public readonly NativeArray<byte> Source;

			public readonly int Offset;

			public readonly int Count;

			public readonly int Stride;

			public GpuBufferInfo(NativeArray<byte> source, int offset, int count, int stride)
			{
				Source = source;
				Offset = offset;
				Count = count;
				Stride = stride;
			}
		}

		[BurstCompile]
		private struct CopyPropertiesJob : IJobChunk
		{
			[ReadOnly]
			public NativeArray<int> EntityIndices;

			public ComponentTypeHandle<PartRenderInfo> PartRenderInfoHandle;

			[ReadOnly]
			public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

			[ReadOnly]
			public ComponentTypeHandle<MaterialColor> MaterialColorHandle;

			[ReadOnly]
			public ComponentTypeHandle<MeshRect> MeshRectHandle;

			[ReadOnly]
			public ComponentTypeHandle<MeshUVRect> MeshUVRectHandle;

			[ReadOnly]
			public ComponentTypeHandle<BlendFactor> BlendFactorHandle;

			[NativeDisableContainerSafetyRestriction]
			[NativeDisableParallelForRestriction]
			public NativeArray<GpuBufferInfo> BufferInfo;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				int count = chunk.Count;
				int num = EntityIndices[unfilteredChunkIndex];
				NativeArray<PartRenderInfo> nativeArray = chunk.GetNativeArray(ref PartRenderInfoHandle);
				NativeArray<LocalToWorld> nativeArray2 = chunk.GetNativeArray(ref LocalToWorldHandle);
				NativeArray<MaterialColor> nativeArray3 = chunk.GetNativeArray(ref MaterialColorHandle);
				NativeArray<MeshRect> nativeArray4 = chunk.GetNativeArray(ref MeshRectHandle);
				NativeArray<MeshUVRect> nativeArray5 = chunk.GetNativeArray(ref MeshUVRectHandle);
				NativeArray<BlendFactor> nativeArray6 = chunk.GetNativeArray(ref BlendFactorHandle);
				for (int i = 0; i < count; i++)
				{
					nativeArray[i] = nativeArray[i].WithIndex(num + i);
					GpuBufferInfo bufferInfo = BufferInfo[0];
					int offset = num + i;
					LocalToWorld localToWorld = nativeArray2[i];
					CopyToBuffer(in bufferInfo, offset, PackMatrix(in localToWorld.Value));
					CopyToBuffer(BufferInfo[1], num + i, PackMatrix(math.inverse(nativeArray2[i].Value)));
				}
				CopyToBuffer(BufferInfo[2], num, nativeArray3, count);
				CopyToBuffer(BufferInfo[3], num, nativeArray4, count);
				CopyToBuffer(BufferInfo[4], num, nativeArray5, count);
				CopyToBuffer(BufferInfo[5], num, nativeArray6, count);
			}

			private unsafe void CopyToBuffer<T>(in GpuBufferInfo bufferInfo, int offset, T value) where T : struct
			{
				UnsafeUtility.WriteArrayElement(bufferInfo.Source.GetUnsafePtr(), offset, value);
			}

			private unsafe void CopyToBuffer<T>(in GpuBufferInfo bufferInfo, int offset, NativeArray<T> source, int count) where T : struct
			{
				void* unsafeReadOnlyPtr = source.GetUnsafeReadOnlyPtr();
				void* destination = (byte*)bufferInfo.Source.GetUnsafePtr() + offset * bufferInfo.Stride;
				UnsafeUtility.MemCpy(destination, unsafeReadOnlyPtr, count * bufferInfo.Stride);
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct CullingJob : IJobChunk
		{
			[ReadOnly]
			public UnityEngine.FrustumPlanes Planes;

			[ReadOnly]
			public ComponentTypeHandle<PartRenderInfo> PartRenderInfoHandle;

			[ReadOnly]
			public ComponentTypeHandle<WorldTransform> WorldTransformHandle;

			[WriteOnly]
			public NativeList<SortPriority>.ParallelWriter SortedIndices;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				int count = chunk.Count;
				NativeArray<PartRenderInfo> nativeArray = chunk.GetNativeArray(ref PartRenderInfoHandle);
				NativeList<SortPriority> list = new NativeList<SortPriority>(count, Allocator.Temp);
				for (int i = 0; i < count; i++)
				{
					PartRenderInfo partRenderInfo = nativeArray[i];
					float3 min = partRenderInfo.Bounds.Min;
					float3 max = partRenderInfo.Bounds.Max;
					if (max.x >= Planes.left && min.x <= Planes.right && max.y >= Planes.bottom && min.y <= Planes.top)
					{
						list.AddNoResize(new SortPriority(partRenderInfo.Index, partRenderInfo.Priority));
					}
				}
				SortedIndices.AddRangeNoResize(list);
				list.Dispose();
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct SortIndicesJob : IJob
		{
			public NativeList<SortPriority> SortedIndices;

			public void Execute()
			{
				SortedIndices.Sort();
			}
		}

		[BurstCompile]
		private struct EmitDrawCommandsJob : IJob
		{
			[ReadOnly]
			public BatchID BatchID;

			[ReadOnly]
			public BatchMeshID MeshID;

			[ReadOnly]
			public BatchMaterialID MaterialID;

			[ReadOnly]
			public NativeList<SortPriority> SortedIndices;

			[WriteOnly]
			public BatchCullingOutput CullingOutput;

			public unsafe void Execute()
			{
				int length = SortedIndices.Length;
				BatchCullingOutputDrawCommands* unsafePtr = (BatchCullingOutputDrawCommands*)CullingOutput.drawCommands.GetUnsafePtr();
				unsafePtr->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(), UnsafeUtility.AlignOf<BatchDrawCommand>(), Allocator.TempJob);
				unsafePtr->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), UnsafeUtility.AlignOf<BatchDrawRange>(), Allocator.TempJob);
				unsafePtr->visibleInstances = (int*)UnsafeUtility.Malloc(length * 4, UnsafeUtility.AlignOf<int>(), Allocator.TempJob);
				unsafePtr->drawCommandPickingInstanceIDs = null;
				unsafePtr->drawCommandCount = 1;
				unsafePtr->drawRangeCount = 1;
				unsafePtr->visibleInstanceCount = length;
				unsafePtr->instanceSortingPositions = null;
				unsafePtr->instanceSortingPositionFloatCount = 0;
				*unsafePtr->drawCommands = new BatchDrawCommand
				{
					visibleOffset = 0u,
					visibleCount = (uint)length,
					batchID = BatchID,
					materialID = MaterialID,
					meshID = MeshID,
					submeshIndex = 0,
					splitVisibilityMask = 255,
					flags = BatchDrawCommandFlags.None,
					sortingPosition = 0
				};
				*unsafePtr->drawRanges = new BatchDrawRange
				{
					drawCommandsBegin = 0u,
					drawCommandsCount = 1u,
					filterSettings = new BatchFilterSettings
					{
						renderingLayerMask = uint.MaxValue
					}
				};
				for (int i = 0; i < length; i++)
				{
					unsafePtr->visibleInstances[i] = SortedIndices[i].Index;
				}
			}
		}

		private bool m_enableDepthSorting;

		private int m_bufferSize;

		private int m_sizeOfInstance;

		private int m_instanceCount;

		private int m_instanceCapacity;

		private BatchID m_batchID;

		private BatchMeshID m_meshID;

		private BatchMaterialID m_materialID;

		private EntityQuery m_updateEntityQuery;

		private EntityQuery m_cullingEntityQuery;

		private JobHandle m_performCullingDependency;

		private NativeArray<PropertyInfo> m_properties;

		private BatchRendererGroup m_batchRendererGroup;

		private GraphicsBuffer m_buffer;

		private const int BufferBegin = 48;

		private const int InitialBufferSize = 1048576;

		private ComponentTypeHandle<PartRenderInfo> __Bpsim_Parts_PartRenderInfo_RW_ComponentTypeHandle;

		private ComponentTypeHandle<LocalToWorld> __Unity_Transforms_LocalToWorld_RO_ComponentTypeHandle;

		private ComponentTypeHandle<MaterialColor> __Unity_Rendering_MaterialColor_RO_ComponentTypeHandle;

		private ComponentTypeHandle<MeshRect> __Bpsim_Rendering_MeshRect_RO_ComponentTypeHandle;

		private ComponentTypeHandle<MeshUVRect> __Bpsim_Rendering_MeshUVRect_RO_ComponentTypeHandle;

		private ComponentTypeHandle<BlendFactor> __Bpsim_Rendering_BlendFactor_RO_ComponentTypeHandle;

		private EntityQuery __query_595537251_0;

		private EntityQuery __query_595537251_1;

		[Preserve]
		protected override void OnCreate()
		{
			m_enableDepthSorting = true;
			m_batchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
			m_updateEntityQuery = __query_595537251_0;
			m_cullingEntityQuery = __query_595537251_1;
			InitializeProperties();
		}

		[Preserve]
		protected override void OnStartRunning()
		{
			Mesh builtinResource = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
			Material material = CoreManager.Instance.Resources.LoadAsset<Material>("Part_Texture_0");
			m_meshID = m_batchRendererGroup.RegisterMesh(builtinResource);
			m_materialID = m_batchRendererGroup.RegisterMaterial(material);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			int num = m_updateEntityQuery.CalculateEntityCount();
			if (num == 0)
			{
				m_instanceCount = 0;
				return;
			}
			m_instanceCount = num;
			if (num > m_instanceCapacity)
			{
				AllocateBuffer(48 + m_sizeOfInstance * num);
				UpdateBatch();
			}
			PopulateBuffer();
		}

		private void InitializeProperties()
		{
			int num = UnsafeUtility.SizeOf<float3x4>();
			int num2 = UnsafeUtility.SizeOf<float4>();
			NativeArray<PropertyInfo> properties = new NativeArray<PropertyInfo>(6, Allocator.Persistent);
			properties[0] = new PropertyInfo(Shader.PropertyToID("unity_ObjectToWorld"), num);
			properties[1] = new PropertyInfo(Shader.PropertyToID("unity_WorldToObject"), num);
			properties[2] = new PropertyInfo(Shader.PropertyToID("_Color"), num2);
			properties[3] = new PropertyInfo(Shader.PropertyToID("_Rect"), num2);
			properties[4] = new PropertyInfo(Shader.PropertyToID("_UVRect"), num2);
			properties[5] = new PropertyInfo(Shader.PropertyToID("_BlendFactor"), num2);
			m_sizeOfInstance = 2 * num + 4 * num2;
			m_properties = properties;
		}

		private void AllocateBuffer(int size)
		{
			if (m_bufferSize < size)
			{
				int num;
				for (num = ((m_bufferSize == 0) ? 1048576 : m_bufferSize); num < size; num *= 2)
				{
				}
				if (m_buffer != null)
				{
					m_buffer.Dispose();
				}
				m_bufferSize = num;
				m_instanceCapacity = (num - 48) / m_sizeOfInstance;
				m_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.LockBufferForWrite, num / 4, 4);
			}
		}

		private void PopulateBuffer()
		{
			m_performCullingDependency.Complete();
			m_performCullingDependency = default(JobHandle);
			int instanceCount = m_instanceCount;
			int instanceCapacity = m_instanceCapacity;
			int num = 48;
			NativeArray<GpuBufferInfo> bufferInfo = new NativeArray<GpuBufferInfo>(m_properties.Length, Allocator.TempJob);
			for (int i = 0; i < m_properties.Length; i++)
			{
				PropertyInfo propertyInfo = m_properties[i];
				NativeArray<byte> source = new NativeArray<byte>(instanceCount * propertyInfo.Size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				bufferInfo[i] = new GpuBufferInfo(source, num, instanceCount, propertyInfo.Size);
				num += instanceCapacity * propertyInfo.Size;
			}
			NativeArray<int> entityIndices = m_updateEntityQuery.CalculateBaseEntityIndexArray(Allocator.TempJob);
			__Bpsim_Rendering_BlendFactor_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			__Bpsim_Rendering_MeshUVRect_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			__Bpsim_Rendering_MeshRect_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			__Unity_Rendering_MaterialColor_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			__Unity_Transforms_LocalToWorld_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			__Bpsim_Parts_PartRenderInfo_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			JobChunkExtensions.ScheduleParallel(new CopyPropertiesJob
			{
				EntityIndices = entityIndices,
				PartRenderInfoHandle = __Bpsim_Parts_PartRenderInfo_RW_ComponentTypeHandle,
				LocalToWorldHandle = __Unity_Transforms_LocalToWorld_RO_ComponentTypeHandle,
				MaterialColorHandle = __Unity_Rendering_MaterialColor_RO_ComponentTypeHandle,
				MeshRectHandle = __Bpsim_Rendering_MeshRect_RO_ComponentTypeHandle,
				MeshUVRectHandle = __Bpsim_Rendering_MeshUVRect_RO_ComponentTypeHandle,
				BlendFactorHandle = __Bpsim_Rendering_BlendFactor_RO_ComponentTypeHandle,
				BufferInfo = bufferInfo
			}, m_updateEntityQuery, base.Dependency).Complete();
			entityIndices.Dispose();
			GraphicsBuffer buffer = m_buffer;
			for (int j = 0; j < bufferInfo.Length; j++)
			{
				GpuBufferInfo gpuBufferInfo = bufferInfo[j];
				int num2 = gpuBufferInfo.Count * gpuBufferInfo.Stride;
				NativeArray<byte> dst = buffer.LockBufferForWrite<byte>(gpuBufferInfo.Offset, num2);
				NativeArray<byte>.Copy(gpuBufferInfo.Source, dst);
				buffer.UnlockBufferAfterWrite<byte>(num2);
			}
			for (int k = 0; k < bufferInfo.Length; k++)
			{
				bufferInfo[k].Source.Dispose();
			}
			bufferInfo.Dispose();
		}

		private void UpdateBatch()
		{
			int instanceCapacity = m_instanceCapacity;
			int num = 48;
			NativeArray<MetadataValue> batchMetadata = new NativeArray<MetadataValue>(m_properties.Length, Allocator.Temp);
			for (int i = 0; i < m_properties.Length; i++)
			{
				PropertyInfo propertyInfo = m_properties[i];
				batchMetadata[i] = new MetadataValue
				{
					NameID = propertyInfo.ID,
					Value = (uint)(int.MinValue | num)
				};
				num += instanceCapacity * propertyInfo.Size;
			}
			m_batchID = m_batchRendererGroup.AddBatch(batchMetadata, m_buffer.bufferHandle);
		}

		[Preserve]
		protected override void OnDestroy()
		{
			m_batchRendererGroup.Dispose();
			m_buffer?.Dispose();
		}

		public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
		{
			if (m_batchID == BatchID.Null || m_instanceCount == 0)
			{
				return m_performCullingDependency;
			}
			NativeList<SortPriority> sortedIndices = new NativeList<SortPriority>(m_instanceCount, Allocator.TempJob);
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CullingJob
			{
				Planes = cullingContext.cullingSplits[0].cullingMatrix.decomposeProjection,
				PartRenderInfoHandle = GetComponentTypeHandle<PartRenderInfo>(isReadOnly: true),
				WorldTransformHandle = GetComponentTypeHandle<WorldTransform>(isReadOnly: true),
				SortedIndices = sortedIndices.AsParallelWriter()
			}, m_cullingEntityQuery, m_performCullingDependency);
			JobHandle dependsOn = jobHandle;
			if (m_enableDepthSorting)
			{
				dependsOn = IJobExtensions.Schedule(new SortIndicesJob
				{
					SortedIndices = sortedIndices
				}, jobHandle);
			}
			JobHandle inputDeps = IJobExtensions.Schedule(new EmitDrawCommandsJob
			{
				BatchID = m_batchID,
				MeshID = m_meshID,
				MaterialID = m_materialID,
				SortedIndices = sortedIndices,
				CullingOutput = cullingOutput
			}, dependsOn);
			JobHandle performCullingDependency = sortedIndices.Dispose(inputDeps);
			m_performCullingDependency = performCullingDependency;
			return m_performCullingDependency;
		}

		private static float3x4 PackMatrix(in float4x4 matrix)
		{
			return new float3x4(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz);
		}

		private void __AssignHandles(ref SystemState state)
		{
			EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
			EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PartRenderInfo, LocalToWorld, MaterialColor, MeshRect, MeshUVRect, BlendFactor>();
			entityQueryBuilder2 = entityQueryBuilder2.WithNone<DisableRendering>();
			__query_595537251_0 = entityQueryBuilder2.Build(ref state);
			entityQueryBuilder.Reset();
			entityQueryBuilder2 = entityQueryBuilder.WithAll<PartRenderInfo>();
			entityQueryBuilder2 = entityQueryBuilder2.WithNone<DisableRendering>();
			__query_595537251_1 = entityQueryBuilder2.Build(ref state);
			entityQueryBuilder.Reset();
			entityQueryBuilder.Dispose();
			__Bpsim_Parts_PartRenderInfo_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PartRenderInfo>();
			__Unity_Transforms_LocalToWorld_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LocalToWorld>(isReadOnly: true);
			__Unity_Rendering_MaterialColor_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MaterialColor>(isReadOnly: true);
			__Bpsim_Rendering_MeshRect_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MeshRect>(isReadOnly: true);
			__Bpsim_Rendering_MeshUVRect_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MeshUVRect>(isReadOnly: true);
			__Bpsim_Rendering_BlendFactor_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BlendFactor>(isReadOnly: true);
		}

		protected override void OnCreateForCompiler()
		{
			base.OnCreateForCompiler();
			__AssignHandles(ref base.CheckedStateRef);
		}

		[Preserve]
		public SceneRenderSystem()
		{
		}
	}
}
