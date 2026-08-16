using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using TwinCAT.ProductivityTools.Build;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Build
{
	public class BuildArtifactCleanerTests : IDisposable
	{
		private readonly string root;

		public BuildArtifactCleanerTests()
		{
			root = Path.Combine(Path.GetTempPath(), "tcpt-clean-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
		}

		public void Dispose()
		{
			try
			{
				Directory.Delete(root, true);
			}
			catch (IOException) { }
		}

		private void File_(string relative, string content = "x")
		{
			string path = Path.Combine(root, relative);

			Directory.CreateDirectory(Path.GetDirectoryName(path));
			System.IO.File.WriteAllText(path, content);
		}

		[Fact]
		public void Deletes_the_boot_folder_and_keeps_the_sources()
		{
			File_(@"MyProject.tsproj");
			File_(@"MyPlc\MyPlc.plcproj");
			File_(@"MyPlc\POUs\MAIN.TcPOU");
			File_(@"_Boot\TwinCAT RT (x64)\Plc\Port_851.app");
			File_(@"MyPlc\_CompileInfo\MyPlc.compileinfo");

			BuildArtifactCleanResult result = new BuildArtifactCleaner().Clean(root);

			Directory.Exists(Path.Combine(root, "_Boot")).Should().BeFalse();
			Directory.Exists(Path.Combine(root, @"MyPlc\_CompileInfo")).Should().BeFalse();

			System.IO.File.Exists(Path.Combine(root, "MyProject.tsproj")).Should().BeTrue();
			System.IO.File.Exists(Path.Combine(root, @"MyPlc\POUs\MAIN.TcPOU")).Should().BeTrue();

			result.DeletedAnything.Should().BeTrue();
			result.FailedEntries.Should().BeEmpty();
		}

		[Fact]
		public void Deletes_the_generated_files_of_a_plc_project()
		{
			File_(@"MyPlc\MyPlc.tpy");
			File_(@"MyPlc\LineIDs.dbg");
			File_(@"MyPlc\MyPlc.plcproj");

			new BuildArtifactCleaner().Clean(root);

			System.IO.File.Exists(Path.Combine(root, @"MyPlc\MyPlc.tpy")).Should().BeFalse();
			System.IO.File.Exists(Path.Combine(root, @"MyPlc\LineIDs.dbg")).Should().BeFalse();
			System.IO.File.Exists(Path.Combine(root, @"MyPlc\MyPlc.plcproj")).Should().BeTrue();
		}

		[Fact]
		public void Keeps_the_project_directory_itself()
		{
			File_(@"MyProject.tsproj");

			new BuildArtifactCleaner().Clean(root);

			Directory.Exists(root).Should().BeTrue();
		}

		[Fact]
		public void Reports_nothing_for_a_directory_that_does_not_exist()
		{
			BuildArtifactCleanResult result = new BuildArtifactCleaner().Clean(
				Path.Combine(root, "gone")
			);

			result.DeletedAnything.Should().BeFalse();
			result.FailedEntries.Should().BeEmpty();
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Reports_nothing_for_a_missing_directory(string directory)
		{
			new BuildArtifactCleaner().Clean(directory).DeletedAnything.Should().BeFalse();
		}

		[Fact]
		public void Reports_an_entry_it_could_not_delete_instead_of_throwing()
		{
			File_(@"_Boot\locked.app");

			using (
				new FileStream(
					Path.Combine(root, @"_Boot\locked.app"),
					FileMode.Open,
					FileAccess.Read,
					FileShare.None
				)
			)
			{
				BuildArtifactCleanResult result = new BuildArtifactCleaner().Clean(root);

				result
					.FailedEntries.Should()
					.HaveCount(
						2,
						"both the locked file and the directory that holds it stay behind"
					);
				result.ToString().Should().Contain("could not be deleted");
			}
		}
	}
}
