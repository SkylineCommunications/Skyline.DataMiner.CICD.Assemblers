namespace Skyline.DataMiner.CICD.Assemblers.Common
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using NuGet.Common;
	using NuGet.Configuration;
	using NuGet.Packaging;
	using NuGet.ProjectModel;
	using NuGet.Protocol.Core.Types;

	using Skyline.DataMiner.CICD.Common.NuGet;
	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Loggers;
	using Skyline.DataMiner.CICD.Parsers.Common.VisualStudio.Projects;

	/// <summary>
	/// Processes the assemblies of the NuGet packages referenced in a project.
	/// </summary>
	/// <remarks>This class uses the project.assets.json file to determine the unified NuGet packages that should be referenced.
	/// The project references are not processed using this file. They are processed via the protocol solution object.
	/// </remarks>
	public class ProjectAssetsProcessor
	{
		private readonly ILogCollector logCollector;
		private readonly ISettings settings;
		private readonly ILogger nuGetLogger;
		private readonly ICollection<SourceRepository> repositories;

		private readonly IFileSystem _fileSystem = FileSystem.Instance;


		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectAssetsProcessor"/> class.
		/// </summary>
		/// <param name="solutionDirectoryPath">Directory path of the solution.</param>
		public ProjectAssetsProcessor(string solutionDirectoryPath)
		{
			nuGetLogger = NullLogger.Instance;

			// Start with the lowest settings. It will automatically look at the other NuGet.config files it can find on the default locations
			settings = Settings.LoadDefaultSettings(root: solutionDirectoryPath);

			NuGetRootPath = SettingsUtility.GetGlobalPackagesFolder(settings);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PackageReferenceProcessor"/> class.
		/// </summary>
		/// <param name="logCollector">The log collector.</param>
		/// <param name="solutionDirectoryPath">Directory path of the solution (to find the NuGet.config)</param>
		/// <exception cref="ArgumentNullException"><paramref name="logCollector"/> is <see langword="null"/>.</exception>
		public ProjectAssetsProcessor(ILogCollector logCollector, string solutionDirectoryPath) : this(solutionDirectoryPath)
		{
			this.logCollector = logCollector ?? throw new ArgumentNullException(nameof(logCollector));

			foreach (SourceRepository sourceRepository in repositories)
			{
				LogDebug($"Source: [{sourceRepository.PackageSource?.Name}] {sourceRepository.PackageSource?.Source}");
			}

			LogDebug($"NuGet Root Path: {NuGetRootPath}");
		}

		/// <summary>
		/// Gets the NuGet root path.
		/// </summary>
		/// <value>The NuGet root path.</value>
		public string NuGetRootPath { get; }

		/// <summary>
		/// Processes the NuGet packages.
		/// </summary>
		/// <param name="project">The project to process.</param>
		/// <param name="targetFrameworkMoniker">The target framework moniker.</param>
		/// <param name="defaultIncludedFilesNuGetPackages">Specifies the NuGet package IDs that are included by default.</param>
		/// <returns>The assembly info of the processed packages.</returns>
		/// <exception cref="InvalidOperationException">Cannot find the package with the identity.</exception>
		public NuGetPackageAssemblyData Process(Project project, string targetFrameworkMoniker, IReadOnlyCollection<string> defaultIncludedFilesNuGetPackages)
		{
			var nugetPackageAssemblyData = new NuGetPackageAssemblyData();

			var projectAssetsFilePath = Path.Combine(Path.GetDirectoryName(project.Path), "obj", "project.assets.json");

			if(!File.Exists(projectAssetsFilePath))
			{
				// Projects that do not use any NuGet packages and do not reference any project that uses NuGet packages will not have a project.assets.json file.
				return nugetPackageAssemblyData;
			}

			var lockFileFormat = new LockFileFormat();
			var assetsFileContent = File.ReadAllText(projectAssetsFilePath);
			var assetsFile = lockFileFormat.Parse(assetsFileContent, "In Memory");

			var target = assetsFile.Targets.FirstOrDefault(t => t.Name == targetFrameworkMoniker);

			if(target == null)
			{
				throw new InvalidOperationException($"Could not find target '{targetFrameworkMoniker}' in project.assets.json file '{projectAssetsFilePath}'.");
			}

			foreach (var library in target.Libraries)
			{
				if (String.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
				{
					ProcessPackage(library, defaultIncludedFilesNuGetPackages, nugetPackageAssemblyData);
				}
				
				// Do not process items of type "project", project references are processed in builder already.
			}

			return nugetPackageAssemblyData;
		}

		private void ProcessPackage(LockFileTargetLibrary library, IReadOnlyCollection<string> defaultIncludedFilesNuGetPackages, NuGetPackageAssemblyData nugetPackageAssemblyData)
		{
			// This is a NuGet package.
			if (!defaultIncludedFilesNuGetPackages.Contains(library.Name) &&
				!String.Equals(library.Name, "StyleCop.Analyzers", StringComparison.OrdinalIgnoreCase) &&
				!library.Name.StartsWith("Skyline.DataMiner.Dev"))
			{
				if (ShouldSkipPackage(library)) return;

				Console.WriteLine(library.Name);
				ProcessLibItemsOfPackage(library, nugetPackageAssemblyData);
				ProcessFrameworkItemsOfPackage(library, nugetPackageAssemblyData);
			}
		}

		/// <summary>
		/// Checks whether packages that are part of dev packs were overridden by another package
		/// </summary>
		/// <param name="library"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private bool ShouldSkipPackage(LockFileTargetLibrary library)
		{
			// In dev packs, the Newtonsoft.Json and SharpZipLib packages were added as dependencies but only for runtime..
			if(String.Equals(library.Name, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
			{
				var assemblyFound = library.CompileTimeAssemblies.FirstOrDefault(a => a.Path.EndsWith(".dll"));

				if (assemblyFound == null)
					return true;
			}
			else if(String.Equals(library.Name, "SharpZipLib", StringComparison.OrdinalIgnoreCase))
			{
				var assemblyFound = library.CompileTimeAssemblies.FirstOrDefault(a => a.Path.EndsWith(".dll"));

				if (assemblyFound == null)
					return true;
			}

			return false;
		}

		private void ProcessLibItemsOfPackage(LockFileTargetLibrary library, NuGetPackageAssemblyData nugetPackageAssemblyData)
		{
			string packageKey = library.Name.ToLower() + "\\" + library.Version.ToString().ToLower();

			foreach (var runtimeAssembly in library.RuntimeAssemblies)
			{
				// Currently, only lib items are supported.
				if(runtimeAssembly.Path.StartsWith("lib/"))
				{
					string assemblyName = runtimeAssembly.Path.Substring(runtimeAssembly.Path.LastIndexOf('/') + 1);
					string dllImportEntry = packageKey + "\\" + runtimeAssembly.Path.Replace("/", "\\");

					nugetPackageAssemblyData.ProcessedAssemblies.Add(assemblyName);

					string fullPath = null;
					string dllImportValue;
					bool isFilePackage = false;

					if (library.Name.StartsWith(DevPackHelper.FilesPrefix))
					{
						// Full path is not set as it should not be included.
						dllImportValue = assemblyName;
						isFilePackage = true;
					}
					else
					{
						fullPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(NuGetRootPath, library.Name.ToLower(), library.Version.ToString().ToLower(), runtimeAssembly.Path));
						dllImportValue = _fileSystem.Path.Combine(dllImportEntry); // fileInfo.Name
					}

					var packageAssemblyReference = new PackageAssemblyReference(dllImportValue, fullPath, isFilePackage);

					// Needs to be added as a reference in the dllImport attribute/script references.
					nugetPackageAssemblyData.DllImportNugetAssemblyReferences.Add(packageAssemblyReference);

					if (!library.Name.StartsWith(DevPackHelper.FilesPrefix))
					{
						// Needs to be provided in the package to install.
						nugetPackageAssemblyData.NugetAssemblies.Add(packageAssemblyReference);
					}
				}
			}
		}

		private void ProcessFrameworkItemsOfPackage(LockFileTargetLibrary library, NuGetPackageAssemblyData nugetPackageAssemblyData)
		{
			// Framework references are items that are that are part of the targeted .NET framework. These are specified in the nuspec file.
			// See: https://docs.microsoft.com/en-us/nuget/reference/nuspec#framework-assembly-references
			var frameworkItems = library.FrameworkReferences;

			nugetPackageAssemblyData.ProcessedAssemblies.AddRange(frameworkItems);
			nugetPackageAssemblyData.DllImportFrameworkAssemblyReferences.AddRange(frameworkItems);
		}

		private void LogDebug(string message)
		{
			logCollector?.ReportDebug($"ProjectAssetsProcessor|{message}");
		}
	}
}