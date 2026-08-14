using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Plc;
using Xunit;

namespace TwinCAT.ProductivityTools.Integration.Tests.Commands
{
	/// <summary>
	/// Covers the walk over the PLC tree that "Remove all comments" and "Remove all regions" share.
	/// </summary>
	/// <remarks>
	/// The tree consists of COM objects of the TwinCAT automation interface. They are substituted
	/// here, which keeps the test honest about what the production code may rely on: a tree item is
	/// enumerated with <c>foreach</c>, and an item that carries code implements the declaration or
	/// implementation interface in addition to the tree item interface.
	/// </remarks>
	public class PlcTextRewriterTests
	{
		[Fact]
		public void RewritesTheDeclarationAndTheImplementationOfAnItem()
		{
			ITcSmTreeItem pou = Code("declaration", "implementation");

			PlcRewriteResult result = new PlcTextRewriter(text => text.ToUpperInvariant()).Apply(
				pou
			);

			((ITcPlcDeclaration)pou).Received().DeclarationText = "DECLARATION";
			((ITcPlcImplementation)pou).Received().ImplementationText = "IMPLEMENTATION";
			result.Visited.Should().Be(2);
			result.Changed.Should().Be(2);
		}

		[Fact]
		public void ReachesTheMethodsAndPropertiesOfAPou()
		{
			// A method, an action, a property getter and a transition are children of the POU and
			// carry their own code. Rewriting only the POU itself would leave most of a real
			// project untouched, which is the reason this walk exists.
			ITcSmTreeItem method = Code("method declaration", "method body");
			ITcSmTreeItem getter = Code(null, "getter body");
			ITcSmTreeItem pou = Code("pou declaration", "pou body", method, getter);

			PlcRewriteResult result = new PlcTextRewriter(
				text => text?.Replace("body", "BODY")
			).Apply(pou);

			((ITcPlcImplementation)method).Received().ImplementationText = "method BODY";
			((ITcPlcImplementation)getter).Received().ImplementationText = "getter BODY";
			((ITcPlcImplementation)pou).Received().ImplementationText = "pou BODY";
			result.Changed.Should().Be(3);
		}

		[Fact]
		public void WalksAFolderOfAProjectDownToItsCode()
		{
			ITcSmTreeItem pou = Code("declaration", "body");
			ITcSmTreeItem folder = Container(pou);
			ITcSmTreeItem project = Container(folder);

			PlcRewriteResult result = new PlcTextRewriter(text => text + "!").Apply(project);

			((ITcPlcImplementation)pou).Received().ImplementationText = "body!";
			result.Visited.Should().Be(2);
		}

		[Fact]
		public void LeavesUnchangedTextAlone()
		{
			// Writing the text back marks the object dirty and makes the project system rewrite the
			// file, which turns a command that changed nothing into a source control diff.
			ITcSmTreeItem pou = Code("declaration", "body");

			PlcRewriteResult result = new PlcTextRewriter(text => text).Apply(pou);

			result.Visited.Should().Be(2);
			result.Changed.Should().Be(0);
			((ITcPlcDeclaration)pou).DidNotReceive().DeclarationText = Arg.Any<string>();
			((ITcPlcImplementation)pou).DidNotReceive().ImplementationText = Arg.Any<string>();
		}

		[Fact]
		public void IgnoresEmptyText()
		{
			ITcSmTreeItem pou = Code(string.Empty, null);

			PlcRewriteResult result = new PlcTextRewriter(text => "replacement").Apply(pou);

			result.Changed.Should().Be(0);
			((ITcPlcDeclaration)pou).DidNotReceive().DeclarationText = Arg.Any<string>();
			((ITcPlcImplementation)pou).DidNotReceive().ImplementationText = Arg.Any<string>();
		}

		[Fact]
		public void SkipsOnlyTheSubtreeOfAnItemThatRefusesToEnumerate()
		{
			// References and other virtual folders throw when enumerated. Losing the whole run
			// because of one of them is what made the command unusable on real projects.
			ITcSmTreeItem sibling = Code("declaration", "body");
			ITcSmTreeItem hostile = Substitute.For<ITcSmTreeItem>();
			hostile.ChildCount.Throws(new COMException("This item cannot be enumerated."));

			ITcSmTreeItem project = Container(hostile, sibling);

			PlcRewriteResult result = new PlcTextRewriter(text => text + "!").Apply(project);

			((ITcPlcImplementation)sibling).Received().ImplementationText = "body!";
			result.Changed.Should().Be(2);
		}

		[Fact]
		public void StopsAtTheDepthLimit()
		{
			// A tree that reports itself as its own child would otherwise take the IDE down with a
			// stack overflow. The rewriter visits the root plus 64 levels, and each level has a
			// declaration and an implementation.
			ITcSmTreeItem recursive = Code("declaration", "body");
			recursive.ChildCount.Returns(1);
			recursive
				.GetEnumerator()
				.Returns(_ => new List<ITcSmTreeItem> { recursive }.GetEnumerator());

			PlcRewriteResult result = new PlcTextRewriter(text => text).Apply(recursive);

			result.Visited.Should().Be(2 * 65);
		}

		[Fact]
		public void ToleratesAMissingItem()
		{
			PlcRewriteResult result = new PlcTextRewriter(text => text).Apply(null);

			result.IsEmpty.Should().BeTrue();
		}

		[Fact]
		public void RejectsAMissingRewrite()
		{
			Action creating = () => new PlcTextRewriter(null);

			creating.Should().Throw<ArgumentNullException>();
		}

		/// <summary>
		/// A tree item that carries code, for example a POU, a method or a property accessor.
		/// </summary>
		private static ITcSmTreeItem Code(
			string declaration,
			string implementation,
			params ITcSmTreeItem[] children
		)
		{
			ITcSmTreeItem item = Substitute.For<
				ITcSmTreeItem,
				ITcPlcDeclaration,
				ITcPlcImplementation
			>();

			((ITcPlcDeclaration)item).DeclarationText.Returns(declaration);
			((ITcPlcImplementation)item).ImplementationText.Returns(implementation);

			WithChildren(item, children);

			return item;
		}

		/// <summary>
		/// A tree item without code of its own, for example a project or a folder.
		/// </summary>
		private static ITcSmTreeItem Container(params ITcSmTreeItem[] children)
		{
			ITcSmTreeItem item = Substitute.For<ITcSmTreeItem>();

			WithChildren(item, children);

			return item;
		}

		private static void WithChildren(ITcSmTreeItem item, ITcSmTreeItem[] children)
		{
			item.ChildCount.Returns(children.Length);

			// A fresh enumerator per call, because the rewriter enumerates an item once per visit
			// and a shared enumerator would already be exhausted the second time.
			item.GetEnumerator()
				.Returns(_ => ((IEnumerable)new List<ITcSmTreeItem>(children)).GetEnumerator());
		}
	}
}
