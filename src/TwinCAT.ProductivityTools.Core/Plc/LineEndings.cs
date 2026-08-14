namespace TwinCAT.ProductivityTools.Plc
{
	/// <summary>
	/// Keeps the line ending of a document stable across text transformations, so that rewriting a
	/// POU does not turn the whole file into a diff.
	/// </summary>
	public static class LineEndings
	{
		public const string Windows = "\r\n";
		public const string Unix = "\n";

		/// <summary>
		/// Returns the dominant line ending of <paramref name="text"/>, defaulting to the Windows
		/// one because that is what the TwinCAT editors produce.
		/// </summary>
		public static string Detect(string text) =>
			string.IsNullOrEmpty(text) || text.Contains(Windows) ? Windows : Unix;
	}
}
