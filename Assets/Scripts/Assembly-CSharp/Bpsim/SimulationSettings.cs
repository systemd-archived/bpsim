using Unity.Physics;

namespace Bpsim
{
	public class SimulationSettings : SettingsBase
	{
		private SimulationType m_simulationType;

		private float m_fixedTimeStep;

		private int m_solverIterationCount;

		private float m_gravityX;

		private float m_gravityY;

		private bool m_createJoints;

		private bool m_infiniteConnectionStrength;

		private float m_connectionStrengthFactor;

		public SimulationType SimulationType
		{
			get
			{
				return m_simulationType;
			}
			set
			{
				SetProperty(ref m_simulationType, value, "SimulationType");
			}
		}

		public float FixedTimeStep
		{
			get
			{
				return m_fixedTimeStep;
			}
			set
			{
				if (float.IsFinite(value) && value >= 0f)
				{
					SetProperty(ref m_fixedTimeStep, value, "FixedTimeStep");
				}
			}
		}

		public int SolverIterationCount
		{
			get
			{
				return m_solverIterationCount;
			}
			set
			{
				if (value >= 1)
				{
					SetProperty(ref m_solverIterationCount, value, "SolverIterationCount");
				}
			}
		}

		public float GravityX
		{
			get
			{
				return m_gravityX;
			}
			set
			{
				if (float.IsFinite(value))
				{
					SetProperty(ref m_gravityX, value, "GravityX");
				}
			}
		}

		public float GravityY
		{
			get
			{
				return m_gravityY;
			}
			set
			{
				if (float.IsFinite(value))
				{
					SetProperty(ref m_gravityY, value, "GravityY");
				}
			}
		}

		public bool CreateJoints
		{
			get
			{
				return m_createJoints;
			}
			set
			{
				SetProperty(ref m_createJoints, value, "CreateJoints");
			}
		}

		public bool InfiniteConnectionStrength
		{
			get
			{
				return m_infiniteConnectionStrength;
			}
			set
			{
				SetProperty(ref m_infiniteConnectionStrength, value, "InfiniteConnectionStrength");
			}
		}

		public float ConnectionStrengthFactor
		{
			get
			{
				return m_connectionStrengthFactor;
			}
			set
			{
				if (float.IsFinite(value) && value >= 0f)
				{
					SetProperty(ref m_connectionStrengthFactor, value, "ConnectionStrengthFactor");
				}
			}
		}

		public SimulationSettings()
		{
			SimulationType = SimulationType.HavokPhysics;
			FixedTimeStep = 0.02f;
			SolverIterationCount = 8;
			GravityX = 0f;
			GravityY = -9.81f;
			CreateJoints = true;
			InfiniteConnectionStrength = false;
			ConnectionStrengthFactor = 4f;
		}

		public void Update(SimulationSettings settings)
		{
			SimulationType = settings.SimulationType;
			FixedTimeStep = settings.FixedTimeStep;
			SolverIterationCount = settings.SolverIterationCount;
			GravityX = settings.GravityX;
			GravityY = settings.GravityY;
			CreateJoints = settings.CreateJoints;
			InfiniteConnectionStrength = settings.InfiniteConnectionStrength;
			ConnectionStrengthFactor = settings.ConnectionStrengthFactor;
		}
	}
}
