using System;
using FluentAssertions;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.E2E.Tests.Infrastructure;
using TwinCAT.ProductivityTools.Plc;
using Xunit;

namespace TwinCAT.ProductivityTools.E2E.Tests
{
	/// <summary>
	/// Runs the PLC source rewriting against a real PLC project.
	/// </summary>
	/// <remarks>
	/// The rewriting itself is covered by unit tests on the pure text level and by integration
	/// tests against a substituted automation interface. What only a real project can answer is
	/// whether TwinCAT accepts what the extension writes back: it validates the declaration and the
	/// implementation when they are assigned, and it rejects a body it cannot compile.
	/// </remarks>
	[Collection(TwinCatSolutionCollection.Name)]
	public class PlcSourceRewritingTests
	{
		private static readonly ICommentRemover Comments = new CommentRemover();

		private static readonly IRegionRemover Regions = new RegionRemover();

		private const string DeclarationWithComments =
			"PROGRAM MAIN\r\n"
			+ "VAR\r\n"
			+ "    counter : INT; // counts something\r\n"
			+ "    (* a block comment *)\r\n"
			+ "    flag : BOOL;\r\n"
			+ "END_VAR\r\n";

		private const string BodyWithComments =
			"// leading comment\r\n"
			+ "counter := counter + 1;\r\n"
			+ "(* trailing\r\n"
			+ "   block comment *)\r\n"
			+ "flag := counter > 0;\r\n";

		private readonly TwinCatSolutionFixture solution;

		public PlcSourceRewritingTests(TwinCatSolutionFixture solution)
		{
			this.solution = solution;
		}

		[E2EFact(RequiresPlcEngineering = true)]
		public void TwinCatAcceptsADeclarationWithoutComments()
		{
			solution.Run(() =>
			{
				ITcSmTreeItem pou = CreatePou("RewriteTarget");

				var declaration = (ITcPlcDeclaration)pou;

				declaration.DeclarationText = DeclarationWithComments;
				declaration.DeclarationText = Comments.Remove(declaration.DeclarationText);

				// Reading back is what makes this an end to end assertion: TwinCAT parses and
				// normalises the text on the way in, so a rewrite it dislikes never returns intact.
				declaration.DeclarationText.Should().NotContain("//").And.NotContain("(*");
				declaration.DeclarationText.Should().Contain("counter : INT;");
				declaration.DeclarationText.Should().Contain("flag : BOOL;");
			});
		}

		[E2EFact(RequiresPlcEngineering = true)]
		public void TwinCatAcceptsAnImplementationWithoutComments()
		{
			solution.Run(() =>
			{
				ITcSmTreeItem pou = CreatePou("BodyTarget");

				var implementation = (ITcPlcImplementation)pou;

				implementation.ImplementationText = BodyWithComments;
				implementation.ImplementationText = Comments.Remove(
					implementation.ImplementationText
				);

				implementation.ImplementationText.Should().NotContain("//").And.NotContain("(*");
				implementation.ImplementationText.Should().Contain("counter := counter + 1;");
			});
		}

		[E2EFact(RequiresPlcEngineering = true)]
		public void TwinCatAcceptsAnImplementationWithoutRegions()
		{
			solution.Run(() =>
			{
				ITcSmTreeItem pou = CreatePou("RegionTarget");

				var implementation = (ITcPlcImplementation)pou;

				implementation.ImplementationText =
					"{region 'counting'}\r\ncounter := counter + 1;\r\n{endregion}\r\n";

				implementation.ImplementationText = Regions.Remove(
					implementation.ImplementationText
				);

				implementation.ImplementationText.Should().NotContain("{region");
				implementation.ImplementationText.Should().NotContain("{endregion");
				implementation.ImplementationText.Should().Contain("counter := counter + 1;");
			});
		}

		/// <summary>
		/// Creates a program with a variable the tests can write to.
		/// </summary>
		private ITcSmTreeItem CreatePou(string name)
		{
			ITcSmTreeItem plcProject = solution.AddPlcProject(name + "Plc");

			ITcSmTreeItem pous = TwinCatSolutionFixture.Descendant(plcProject, "POUs");

			const int ProgramSubType = 604;

			ITcSmTreeItem pou = pous.CreateChild(name, ProgramSubType, string.Empty, null);

			var declaration = (ITcPlcDeclaration)pou;

			declaration.DeclarationText =
				$"PROGRAM {name}\r\nVAR\r\n    counter : INT;\r\n    flag : BOOL;\r\nEND_VAR\r\n";

			return pou;
		}
	}
}
