namespace Source1
{
	public struct ViewVectors
	{
		public Vector3 ViewPosition { get; set; }

		public Vector3 HullMin { get; set; }
		public Vector3 HullMax { get; set; }

		public Vector3 DuckHullMin { get; set; }
		public Vector3 DuckHullMax { get; set; }

		public Vector3 ObserverHullMax { get; set; }
		public Vector3 ObserverHullMin { get; set; }

		public Vector3 ObserverDeadViewPosition { get; set; }
	}
}
