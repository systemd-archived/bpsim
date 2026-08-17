using Unity.Entities;

namespace Bpsim.Parts
{
	public readonly struct PartCreationRequest
	{
		public readonly Entity Entity;

		public readonly int SceneID;

		public readonly PartTypeInfo TypeInfo;

		public readonly int CoordX;

		public readonly int CoordY;

		public readonly int Rotation;

		public readonly bool Flipped;

		public PartCreationRequest(Entity entity, int sceneID, PartTypeInfo typeInfo, int coordX, int coordY, int rotation, bool flipped)
		{
			Entity = entity;
			SceneID = sceneID;
			TypeInfo = typeInfo;
			CoordX = coordX;
			CoordY = coordY;
			Rotation = rotation;
			Flipped = flipped;
		}

		public PartCreationRequest(Entity entity, int sceneID, Schematics.Unit unit)
		{
			Entity = entity;
			SceneID = sceneID;
			TypeInfo = new PartTypeInfo((PartType)unit.Type, unit.Index);
			CoordX = unit.X;
			CoordY = unit.Y;
			Rotation = unit.Rotation;
			Flipped = unit.Flipped;
		}

		public PartCreationRequest WithPosition(int x, int y)
		{
			return new PartCreationRequest(Entity, SceneID, TypeInfo, x, y, Rotation, Flipped);
		}

		public PartCreationRequest WithRotation(int rotation, bool flipped)
		{
			return new PartCreationRequest(Entity, SceneID, TypeInfo, CoordX, CoordY, rotation, flipped);
		}

		public Entity Submit(in PartSceneUnmanaged partScene)
		{
			Entity entity = partScene.EntityManager.Instantiate(Entity);
			partScene.EntityManager.AddComponent(entity, ComponentType.ReadOnly<PartSceneID>());
			partScene.EntityManager.SetComponentData(entity, new PartTransform
			{
				X = CoordX,
				Y = CoordY,
				Rotation = Rotation,
				Flipped = Flipped
			});
			partScene.EntityManager.SetComponentData(entity, new PartSceneID
			{
				Value = SceneID
			});
			return entity;
		}
	}
}
