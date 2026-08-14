using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace TwinCAT.ProductivityTools.Integration.Tests.ViewModels
{
	/// <summary>
	/// Covers the states of the real time driver dialog that do not talk to a target.
	/// </summary>
	/// <remarks>
	/// Everything below happens before a single ADS telegram is sent, which is exactly where the
	/// dialog used to fail: an unparseable AmsNetId ended in a null reference, and the buttons
	/// stayed enabled while a request was running.
	/// </remarks>
	public class TcRteInstallViewModelTests
	{
		[Theory]
		[InlineData("")]
		[InlineData(null)]
		[InlineData("not an ams net id")]
		[InlineData("1.2.3")]
		public async Task ReportsAnUnusableTargetInsteadOfFailing(string target)
		{
			TcRteInstallViewModel viewModel = new TcRteInstallViewModel(target);

			await viewModel.InitializeAsync();

			viewModel.Status.Should().Contain("is not a valid AmsNetId");
			viewModel.Connections.Should().BeEmpty();
		}

		[Theory]
		[InlineData("")]
		[InlineData("not an ams net id")]
		public void DisablesBothCommandsForAnUnusableTarget(string target)
		{
			TcRteInstallViewModel viewModel = new TcRteInstallViewModel(target);

			viewModel.SearchCommand.CanExecute(null).Should().BeFalse();
			viewModel.InstallCommand.CanExecute(null).Should().BeFalse();
		}

		[Fact]
		public void KeepsInstallDisabledUntilAnAdapterIsSelected()
		{
			TcRteInstallViewModel viewModel = new TcRteInstallViewModel("1.2.3.4.5.6");

			viewModel.InstallCommand.CanExecute(null).Should().BeFalse();

			viewModel.SelectedItem = new LocalAreaConnection { Name = "LAN" };

			viewModel.InstallCommand.CanExecute(null).Should().BeTrue();
		}

		[Fact]
		public void RaisesCanExecuteChangedWhenTheSelectionChanges()
		{
			// The install button is bound to the command. Without the notification it stays greyed
			// out after the user picked an adapter.
			TcRteInstallViewModel viewModel = new TcRteInstallViewModel("1.2.3.4.5.6");

			bool raised = false;
			viewModel.InstallCommand.CanExecuteChanged += (sender, args) => raised = true;

			viewModel.SelectedItem = new LocalAreaConnection { Name = "LAN" };

			raised.Should().BeTrue();
		}

		[Fact]
		public void FallsBackToTheAmsNetIdWhenNoRouteMatches()
		{
			TcRteInstallViewModel viewModel = new TcRteInstallViewModel("1.2.3.4.5.6");

			viewModel.Target.Should().Be("1.2.3.4.5.6");
			viewModel.TargetName.Should().Be("1.2.3.4.5.6");
		}

		[Fact]
		public void HasNoNameForAnUnusableTarget()
		{
			new TcRteInstallViewModel("nonsense").TargetName.Should().BeEmpty();
		}

		[Fact]
		public async Task DoesNothingWhenInstallIsInvokedWithoutASelection()
		{
			// The command is bound to a button, but a keyboard shortcut or an automated run can
			// still invoke it while nothing is selected.
			TcRteInstallViewModel viewModel = new TcRteInstallViewModel("1.2.3.4.5.6");

			await viewModel.InstallCommand.ExecuteAsync(null);

			viewModel.Status.Should().BeEmpty();
			viewModel.IsBusy.Should().BeFalse();
		}
	}
}
