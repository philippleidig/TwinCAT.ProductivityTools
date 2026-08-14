using System;
using System.Collections.Generic;
using TCatSysManagerLib;

namespace TwinCAT.ProductivityTools.Plc
{
	/// <summary>
	/// Result of a rewrite run over the PLC tree.
	/// </summary>
	internal struct PlcRewriteResult
	{
		public int Visited { get; set; }

		public int Changed { get; set; }

		public bool IsEmpty => Visited == 0;
	}

	/// <summary>
	/// Applies a text transformation to every declaration and implementation below a tree item.
	/// </summary>
	/// <remarks>
	/// A POU keeps its methods, actions, properties and transitions as child tree items, and each
	/// of them carries its own declaration and implementation. Walking the children is therefore
	/// the only way to reach all of the code of a POU - and the same walk turns a PLC project or a
	/// folder into a valid selection for free.
	/// </remarks>
	internal sealed class PlcTextRewriter
	{
		private readonly Func<string, string> rewrite;

		public PlcTextRewriter(Func<string, string> rewrite)
		{
			this.rewrite = rewrite ?? throw new ArgumentNullException(nameof(rewrite));
		}

		public PlcRewriteResult Apply(ITcSmTreeItem treeItem)
		{
			PlcRewriteResult result = new PlcRewriteResult();

			Apply(treeItem, ref result, 0);

			return result;
		}

		private void Apply(ITcSmTreeItem treeItem, ref PlcRewriteResult result, int depth)
		{
			if (treeItem == null || depth > MaximumDepth)
			{
				return;
			}

			ApplyToItem(treeItem, ref result);

			foreach (ITcSmTreeItem child in Children(treeItem))
			{
				Apply(child, ref result, depth + 1);
			}
		}

		private void ApplyToItem(object treeItem, ref PlcRewriteResult result)
		{
			if (treeItem is ITcPlcDeclaration declaration)
			{
				result.Visited++;

				string text = declaration.DeclarationText;
				string rewritten = rewrite(text);

				if (HasChanged(text, rewritten))
				{
					declaration.DeclarationText = rewritten;
					result.Changed++;
				}
			}

			if (treeItem is ITcPlcImplementation implementation)
			{
				result.Visited++;

				string text = implementation.ImplementationText;
				string rewritten = rewrite(text);

				if (HasChanged(text, rewritten))
				{
					implementation.ImplementationText = rewritten;
					result.Changed++;
				}
			}
		}

		private static bool HasChanged(string text, string rewritten)
		{
			return !string.IsNullOrEmpty(text)
				&& !string.Equals(text, rewritten, StringComparison.Ordinal);
		}

		/// <summary>
		/// Enumerates the children eagerly so that a tree item that refuses to enumerate - which
		/// happens for references and other virtual folders - only skips its own subtree.
		/// </summary>
		private static IEnumerable<ITcSmTreeItem> Children(ITcSmTreeItem treeItem)
		{
			List<ITcSmTreeItem> children = new List<ITcSmTreeItem>();

			try
			{
				if (treeItem.ChildCount <= 0)
				{
					return children;
				}

				foreach (ITcSmTreeItem child in treeItem)
				{
					children.Add(child);
				}
			}
			catch (Exception)
			{
				return children;
			}

			return children;
		}

		private const int MaximumDepth = 64;
	}
}
