using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine.Scripting;

namespace Bpsim.Parts.Simulation
{
	[UpdateAfter(typeof(PartSimulationSystem))]
	[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
	internal class PartSimulationSystemGroup : ComponentSystemGroup
	{
		[Preserve]
		public PartSimulationSystemGroup()
		{
		}
	}
}
