using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// Puts <c>Category=E2E</c> on every test marked with <see cref="E2EFactAttribute"/>.
	/// </summary>
	/// <remarks>
	/// A <c>[Trait]</c> placed on a subclass of <c>FactAttribute</c> is not inherited by the test,
	/// because xunit collects traits only from attributes that implement
	/// <see cref="ITraitAttribute"/>. Without this discoverer the filter that keeps the end to end
	/// tests out of CI would silently match nothing, which is the worst possible failure mode: the
	/// tests would run on an agent that cannot host them.
	/// </remarks>
	public sealed class E2ETraitDiscoverer : ITraitDiscoverer
	{
		internal const string AssemblyName = "TwinCAT.ProductivityTools.E2E.Tests";

		internal const string TypeName =
			"TwinCAT.ProductivityTools.E2E.Tests.Infrastructure.E2ETraitDiscoverer";

		public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
		{
			yield return new KeyValuePair<string, string>("Category", "E2E");
		}
	}
}
