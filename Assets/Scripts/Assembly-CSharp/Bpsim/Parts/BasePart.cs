using System.Collections.Generic;
using Bpsim.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Bpsim.Parts
{
	public static class BasePart
	{
		public static PartTypeInfo WoodenBox => new PartTypeInfo(PartType.WoodenFrame, 10);

		public static PartTypeInfo SeparatedFrame => new PartTypeInfo(PartType.MetalFrame, 8);

		public static PartTypeInfo LightFrame => new PartTypeInfo(PartType.MetalFrame, 10);

		public static PartTypeInfo AlienMetalFrame => new PartTypeInfo(PartType.MetalFrame, 11);

		public static PartRangeInfo ColoredFrames => new PartRangeInfo(PartType.MetalFrame, 12, 129);

		public static PartTypeInfo MetalBox => new PartTypeInfo(PartType.MetalFrame, 130);

		public static PartTypeInfo BracketFrame => new PartTypeInfo(PartType.MetalFrame, 131);

		public static PartRangeInfo TransparentFrames => new PartRangeInfo(PartType.MetalFrame, 132, 133);

		public static PartTypeInfo OffRoadWheel => new PartTypeInfo(PartType.MotorWheel, 7);

		public static PartTypeInfo AvoidanceRocketA => new PartTypeInfo(PartType.Rocket, 1);

		public static PartTypeInfo AvoidanceRocketB => new PartTypeInfo(PartType.Rocket, 3);

		public static PartTypeInfo TrackingRocketA => new PartTypeInfo(PartType.RedRocket, 1);

		public static PartTypeInfo TrackingRocketB => new PartTypeInfo(PartType.RedRocket, 3);

		public static PartTypeInfo BlasterTNT => new PartTypeInfo(PartType.TNT, 6);

		public static PartRangeInfo HingePlates => new PartRangeInfo(PartType.Rope, 4, 7);

		public static PartTypeInfo AutoGun => new PartTypeInfo(PartType.GrapplingHook, 6);

		public static PartRangeInfo MultipartGenerators => new PartRangeInfo(PartType.GrapplingHook, 8, 10);

		public static PartTypeInfo AutoConnector => new PartTypeInfo(PartType.Kicker, 1);

		public static PartTypeInfo ElasticConnectorA => new PartTypeInfo(PartType.Kicker, 2);

		public static PartTypeInfo Marker => new PartTypeInfo(PartType.Kicker, 3);

		public static PartTypeInfo ElasticConnectorB => new PartTypeInfo(PartType.Kicker, 4);

		public static PartRangeInfo EntityLightsA => new PartRangeInfo(PartType.PointLight, 0, 4);

		public static PartTypeInfo DecelerationLight => new PartTypeInfo(PartType.PointLight, 5);

		public static PartTypeInfo AutoControlLight => new PartTypeInfo(PartType.PointLight, 6);

		public static PartRangeInfo EntityLightsB => new PartRangeInfo(PartType.SpotLight, 0, 3);

		public static bool IsContainer(in PartAspect part)
		{
			return IsContainer(part.TypeInfo);
		}

		public static bool IsContainer(PartTypeInfo info)
		{
			return info.PartType switch
			{
				PartType.WoodenFrame => info.PartIndex != WoodenBox.PartIndex, 
				PartType.MetalFrame => info.PartIndex != MetalBox.PartIndex, 
				_ => false, 
			};
		}

		public static bool CanBeContained(in PartAspect part)
		{
			return CanBeContained(part.TypeInfo);
		}

		public static bool CanBeContained(PartTypeInfo info)
		{
			return info.PartType switch
			{
				PartType.WoodenFrame => info.PartIndex == WoodenBox.PartIndex, 
				PartType.MetalFrame => info.PartIndex == MetalBox.PartIndex, 
				_ => true, 
			};
		}

		public static bool IsStructural(in PartAspect part)
		{
			return IsStructural(part.TypeInfo);
		}

		public static bool IsStructural(PartTypeInfo info)
		{
			switch (info.PartType)
			{
			case PartType.WoodenFrame:
				return info.PartIndex != WoodenBox.PartIndex;
			case PartType.MetalFrame:
				return info.PartIndex != MetalBox.PartIndex;
			case PartType.Spring:
			case PartType.Rope:
				return true;
			default:
				return false;
			}
		}

		public static bool IsLarge(in PartAspect part)
		{
			return IsLarge(part.TypeInfo);
		}

		public static bool IsLarge(PartTypeInfo info)
		{
			PartType partType = info.PartType;
			if (partType == PartType.KingPig || partType == PartType.GoldenPig)
			{
				return true;
			}
			return false;
		}

		public static bool IsEightWay(in PartAspect part)
		{
			return IsEightWay(part.TypeInfo);
		}

		public static bool IsEightWay(PartTypeInfo info)
		{
			PartType partType = info.PartType;
			if (partType == PartType.GrapplingHook || partType == PartType.SpotLight)
			{
				return true;
			}
			return false;
		}

		public static bool CanFlip(in PartAspect part)
		{
			return CanFlip(part.TypeInfo);
		}

		public static bool CanFlip(PartTypeInfo info)
		{
			switch (info.PartType)
			{
			case PartType.Wings:
			case PartType.Tailplane:
				return true;
			case PartType.MetalWing:
			case PartType.MetalTail:
				return true;
			default:
				return false;
			}
		}

		public static RectInt GetGridRect(in PartAspect part)
		{
			return GetGridRect(part.TypeInfo);
		}

		public static RectInt GetGridRect(PartTypeInfo info)
		{
			PartType partType = info.PartType;
			if (partType == PartType.KingPig || partType == PartType.GoldenPig)
			{
				return new RectInt(-1, 0, 3, 2);
			}
			return new RectInt(0, 0, 1, 1);
		}

		public static Vector3 MoveTo(in PartAspect part)
		{
			int coordX = part.CoordX;
			int coordY = part.CoordY;
			return new Vector3(coordX, coordY, -0.1f + GetZOffset(in part));
		}

		public static Quaternion RotateTo(ref PartAspect part)
		{
			int rotation = part.Rotation;
			bool flipped = part.Flipped;
			if (CanFlip(in part))
			{
				rotation = (part.Rotation = (rotation % 4 + 4) % 4);
				int num2 = ((flipped && (rotation == 0 || rotation == 2)) ? 180 : 0);
				int num3 = ((flipped && (rotation == 1 || rotation == 3)) ? 180 : 0);
				int num4 = 90 * rotation;
				return Quaternion.Euler(num2, num3, num4);
			}
			if (IsEightWay(in part))
			{
				rotation = (part.Rotation = (rotation % 8 + 8) % 8);
				int num6 = 90 * rotation + ((rotation >= 4) ? (-315) : 0);
				return Quaternion.Euler(0f, 0f, num6);
			}
			rotation = (part.Rotation = (rotation % 4 + 4) % 4);
			int num8 = 90 * rotation;
			return Quaternion.Euler(0f, 0f, num8);
		}

		public static float GetZOffset(in PartAspect part)
		{
			PartTypeInfo partTypeInfo = new PartTypeInfo(part.PartType, part.PartIndex);
			float result;
			switch (partTypeInfo.PartType)
			{
			case PartType.WoodenFrame:
			{
				int partIndex = partTypeInfo.PartIndex;
				return (partIndex >= 5 && partIndex <= 8) ? (-0.45f) : (partTypeInfo.BelongsTo(WoodenBox) ? 0.01f : 0f);
			}
			case PartType.MetalFrame:
				if (!partTypeInfo.BelongsTo(ColoredFrames, TransparentFrames))
				{
					int partIndex = partTypeInfo.PartIndex;
					if (partIndex != 5 && partIndex != 6 && partIndex != 8 && partIndex != 9 && partIndex != 11)
					{
						result = (partTypeInfo.BelongsTo(MetalBox) ? 0.01f : 0f);
						goto IL_00f1;
					}
				}
				result = -0.45f;
				goto IL_00f1;
			case PartType.Pig:
			case PartType.KingPig:
			case PartType.GoldenPig:
				return 0.02f;
			case PartType.Egg:
			case PartType.Pumpkin:
				return 0.02f;
			case PartType.EngineSmall:
			case PartType.Engine:
			case PartType.EngineBig:
				return -0.01f;
			case PartType.Gearbox:
				return -0.5f;
			case PartType.PointLight:
			case PartType.SpotLight:
				return -0.01f;
			default:
				{
					return 0.01f;
				}
				IL_00f1:
				return result;
			}
		}

		public static int GetRenderPriority(in PartTransform partTransform, in WorldTransform worldTransform)
		{
			int x = partTransform.X;
			int y = partTransform.Y;
			return -((int)math.round(1000f * worldTransform.Position.z) << 16) - (2 * x + 3 * y);
		}

		public static int GetRenderPriority(in PartTransform partTransform, in LocalToWorld localToWorld)
		{
			int x = partTransform.X;
			int y = partTransform.Y;
			return -((int)math.round(1000f * localToWorld.Position.z) << 16) - (2 * x + 3 * y);
		}

		public static int GetAttachmentCount(PartAspect part)
		{
			return GetAttachmentCount(part.TypeInfo);
		}

		public static int GetAttachmentCount(PartTypeInfo info)
		{
			switch (info.PartType)
			{
			case PartType.CokeBottle:
			case PartType.SodaBottle:
				return 4;
			case PartType.Rocket:
			case PartType.RedRocket:
				return 4;
			case PartType.TNT:
				return (info.PartIndex == 2) ? 4 : 0;
			case PartType.Wings:
			case PartType.MetalWing:
				return 4;
			case PartType.GrapplingHook:
				return 8;
			case PartType.SpotLight:
				return 8;
			case PartType.JetEngine:
				return 4;
			default:
				return 0;
			}
		}

		public static Direction FindAttachment(in FixedString64Bytes name)
		{
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"RightAttachment"))
			{
				return Direction.Right;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"TopAttachment") || name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"TopFrameSprite"))
			{
				return Direction.Up;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"LeftAttachment"))
			{
				return Direction.Left;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"BottomAttachment") || name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"BottomFrameSprite"))
			{
				return Direction.Down;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"TopRightAttachment"))
			{
				return Direction.UpRight;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"TopLeftAttachment"))
			{
				return Direction.UpLeft;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"BottomLeftAttachment"))
			{
				return Direction.DownLeft;
			}
			if (name == ILSpyHelper_AsRefReadOnly((FixedString32Bytes)"BottomRightAttachment"))
			{
				return Direction.DownRight;
			}
			return (Direction)(-1);
			static ref readonly T ILSpyHelper_AsRefReadOnly<T>(in T temp)
			{
				//ILSpy generated this function to help ensure overload resolution can pick the overload using 'in'
				return ref temp;
			}
		}

		public static bool IsTriggerable(PartTypeInfo typeInfo)
		{
			PartType partType = typeInfo.PartType;
			if ((uint)(partType - 12) <= 2u)
			{
				return true;
			}
			return false;
		}

		public static bool IsEnabled(Entity part, EntityManager entityManager)
		{
			if (entityManager.HasComponent<FanPropeller>(part))
			{
				return entityManager.GetComponentData<FanPropeller>(part).Enabled;
			}
			return false;
		}

		public static IEnumerable<PartTriggerButtonInfo> GetTriggerButtonInfo(PartAspect partAspect)
		{
			yield return new PartTriggerButtonInfo(PartButtonType.Trigger, 0, partAspect.PartType, partAspect.Rotation, 0);
		}

		public static IEnumerable<PartSliderButtonInfo> GetSliderButtonInfo(PartAspect partAspect)
		{
			yield break;
		}
	}
}
