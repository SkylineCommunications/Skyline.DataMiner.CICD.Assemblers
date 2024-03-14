namespace Skyline.DataMiner.CICD.Assemblers.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.IO;

    using Skyline.DataMiner.CICD.Assemblers.Common;
    using NuGet.Packaging.Core;
    using System.Threading.Tasks;
    using Skyline.DataMiner.CICD.Parsers.Common.VisualStudio.Projects;

    public static class AssemblyFilter
    {
        public static async Task<NuGetPackageAssemblyData> Filter(string targetFrameworkMoniker, PackageReferenceProcessor packageReferenceProcessor, BuildResultItems buildResultItems, HashSet<string> dllImports, IList<PackageIdentity> packageIdentities)
        {
            NuGetPackageAssemblyData nugetAssemblyData = await packageReferenceProcessor.ProcessAsync(packageIdentities, targetFrameworkMoniker,
                Skyline.DataMiner.CICD.Common.NuGet.DevPackHelper.ProtocolDevPackNuGetDependenciesIncludingTransitive).ConfigureAwait(false);

            ProcessFrameworkAssemblies(dllImports, nugetAssemblyData);
            ProcessLibAssemblies(buildResultItems, dllImports, nugetAssemblyData);

            return nugetAssemblyData;
        }

        private static void ProcessFrameworkAssemblies(HashSet<string> dllImports, NuGetPackageAssemblyData nugetAssemblyData)
        {
            foreach (var frameworkAssembly in nugetAssemblyData.DllImportFrameworkAssemblyReferences)
            {
                if (!Helper.QActionDefaultImportDLLs.Any(a => String.Equals(a, frameworkAssembly, StringComparison.OrdinalIgnoreCase)))
                {
                    dllImports.Add(frameworkAssembly);
                }
            }
        }

        private static void ProcessLibAssemblies(BuildResultItems buildResultItems, HashSet<string> dllImports, NuGetPackageAssemblyData nugetAssemblyData)
        {
            ProcessDllImportNuGetAssemblyReferences(dllImports, nugetAssemblyData);

            foreach (var dir in nugetAssemblyData.DllImportDirectoryReferences)
            {
                if (!Helper.QActionDefaultImportDLLs.Any(d => String.Equals(d, dir, StringComparison.OrdinalIgnoreCase)))
                {
                    dllImports.Add(dir);
                }
            }

            foreach (var libItem in nugetAssemblyData.NugetAssemblies)
            {
                // It's possible that frameworkassemblies like System.Net.Http exist and were added as a dependency earlier on.
                // If that's the case, we should ignore the NuGet adding the same thing.
                // "System.Net.Http.dll" for example.
                // Need to check dllImports

                if (!dllImports.Contains(Path.GetFileName(libItem.AssemblyPath)) && buildResultItems.Assemblies.FirstOrDefault(b => b.AssemblyPath == libItem.AssemblyPath) == null)
                {
                    buildResultItems.Assemblies.Add(libItem);
                }
            }
        }

        private static void ProcessDllImportNuGetAssemblyReferences(HashSet<string> dllImports, NuGetPackageAssemblyData nugetAssemblyData)
        {
            if (nugetAssemblyData.DllImportNugetAssemblyReferences.Count <= 0)
            {
                return;
            }

            HashSet<string> directoriesWithExplicitDllImport = new HashSet<string>();
            List<string> potentialRemainingDirectoryImports = new List<string>();

            // At this point it could be that there are multiple assemblies with the same name (if different NuGet packages contains the same assembly).
            // If this is the case, we select the highest version.
            Dictionary<string, List<PackageAssemblyReference>> assemblies = new Dictionary<string, List<PackageAssemblyReference>>();

            foreach (var libItem in nugetAssemblyData.DllImportNugetAssemblyReferences)
            {
                string assemblyName = Path.GetFileName(libItem.DllImport);

                if (assemblies.TryGetValue(assemblyName, out List<PackageAssemblyReference> entries))
                {
                    entries.Add(libItem);
                }
                else
                {
                    assemblies.Add(assemblyName, new List<PackageAssemblyReference> { libItem });
                }
            }

            foreach (var assembly in assemblies.Keys)
            {
                var packagesContainingAssembly = assemblies[assembly];

                if (packagesContainingAssembly.Count == 1)
                {
                    var libItem = packagesContainingAssembly[0];

                    dllImports.Add(libItem.DllImport);
                    directoriesWithExplicitDllImport.Add(libItem.DllImport.Substring(0, libItem.DllImport.Length - assembly.Length));
                }
                else
                {
                    Version mostRecentVersion = null;
                    PackageAssemblyReference selectedItem = null;

                    // Select most recent version.
                    foreach (var libItem in packagesContainingAssembly)
                    {
                        var version = AssemblyName.GetAssemblyName(libItem.AssemblyPath).Version;

                        if (mostRecentVersion == null || version > mostRecentVersion)
                        {
                            mostRecentVersion = version;
                            selectedItem = libItem;
                        }
                    }

                    if (selectedItem == null)
                    {
                        continue;
                    }

                    dllImports.Add(selectedItem.DllImport);
                    directoriesWithExplicitDllImport.Add(selectedItem.DllImport.Substring(0,
                        selectedItem.DllImport.Length - assembly.Length));

                    // Add other items that were not selected as hint directories.
                    foreach (var libItem in packagesContainingAssembly)
                    {
                        if (libItem != selectedItem)
                        {
                            string directoryPath = libItem.DllImport.Substring(0, libItem.DllImport.Length - assembly.Length);

                            potentialRemainingDirectoryImports.Add(directoryPath);
                        }
                    }
                }
            }

            foreach (var directoryPath in potentialRemainingDirectoryImports)
            {
                if (!directoriesWithExplicitDllImport.Contains(directoryPath))
                {
                    nugetAssemblyData.DllImportDirectoryReferences.Add(directoryPath);
                }
            }
        }

    }
}
