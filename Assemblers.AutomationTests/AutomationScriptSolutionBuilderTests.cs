﻿namespace Assemblers.AutomationTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using FluentAssertions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Skyline.DataMiner.CICD.Assemblers.Automation;
    using Skyline.DataMiner.CICD.Parsers.Automation.VisualStudio;

    [TestClass]
    public class AutomationScriptSolutionBuilderTests
    {
        #region Solution 1 (Basic)

        [TestMethod]
        public async Task AutomationScriptCompiler_Solution1_BuildAsync()
        {
            // arrange
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution1"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            // act
            var result = await builder.BuildAsync().ConfigureAwait(false);

            // check
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }

        #endregion

        #region Solution 2 (Scripts in subfolders)

        [TestMethod]
        public async Task AutomationScriptCompiler_Solution2_BuildAsync()
        {
            // arrange
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution2"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            // act
            var result = await builder.BuildAsync().ConfigureAwait(false);

            // check
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }

        #endregion

        #region Solution 3 (SharedProject)
        [TestMethod]
        public async Task AutomationScriptCompiler_Solution3_BuildAsync()
        {
            // arrange
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution3"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            // act
            var result = await builder.BuildAsync().ConfigureAwait(false);

            // check
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            Assert.IsTrue(result.Single(x => x.Key.Name == "Script_1").Value.Document.Contains("namespace SharedProject"));
        }

        #endregion

        #region Solution 4

        [TestMethod]
        public async Task AutomationScriptCompilerUsingNuGetPackages_Solution4_SaveCompiledScriptAsync()
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution4"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            var result = await builder.BuildAsync().ConfigureAwait(false);

            Assert.AreEqual(1, result.Count, "Expected 1 script in build result.");

            var firstItem = result.FirstOrDefault();
            string automationScriptXml = firstItem.Value.Document;

            var document = XDocument.Parse(automationScriptXml);
            XNamespace ns = document.Root.GetDefaultNamespace();

            var referenceNodes = document
                ?.Element(ns + "DMSScript")
                ?.Element(ns + "Script")
                ?.Element(ns + "Exe")
                ?.Elements(ns + "Param");

            Assert.IsNotNull(referenceNodes);

            List<string> generatedEntries = new List<string>();
            foreach (var referenceNode in referenceNodes)
            {
                generatedEntries.Add(referenceNode.Value);
            }

            List<string> expectedEntries = new List<string>
            {
                @"C:\Skyline DataMiner\ProtocolScripts\DllImport\advancedstringbuilder\0.1.0\lib\net45\AdvancedStringBuilder.dll",
                @"C:\Skyline DataMiner\ProtocolScripts\DllImport\newtonsoft.json\12.0.1\lib\net45\Newtonsoft.Json.dll",
                @"C:\Skyline DataMiner\ProtocolScripts\DllImport\newtonsoft.json.bson\1.0.2\lib\net45\Newtonsoft.Json.Bson.dll"
            };

            generatedEntries.Should().BeEquivalentTo(expectedEntries);
        }

        #endregion

        [TestMethod]
        public async Task AutomationScriptCompilerUsingNuGetPackages_Solution5_SaveCompiledScriptAsync()
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution5"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            var result = await builder.BuildAsync().ConfigureAwait(false);

            Assert.AreEqual(1, result.Count);

            var firstItem = result.FirstOrDefault();
            string automationScriptXml = firstItem.Value.Document;

            var document = XDocument.Parse(automationScriptXml);
            XNamespace ns = document.Root.GetDefaultNamespace();

            var referenceNodes = document
                ?.Element(ns + "DMSScript")
                ?.Element(ns + "Script")
                ?.Element(ns + "Exe")
                ?.Elements(ns + "Param");

            Assert.IsNotNull(referenceNodes);

            List<string> generatedEntries = new List<string>();
            foreach (var referenceNode in referenceNodes)
            {
                generatedEntries.Add(referenceNode.Value);
            }

            List<string> expectedEntries = new List<string>
            {
                @"C:\Skyline DataMiner\ProtocolScripts\MyCustomDll.dll",
            };

            generatedEntries.Should().BeEquivalentTo(expectedEntries);
        }

        #region SpecialChar

        [TestMethod]
        public async Task AutomationScriptSolutionBuilder_BuildAsync_SpecialCharacters()
        {
            string expectedLine = "engine.GenerateInformation(\"test ›\");";

            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\SpecialChar"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);
            
            var result = await builder.BuildAsync().ConfigureAwait(false);

            Assert.AreEqual(1, result.Count, "Expected 1 script in build result.");

            var firstItem = result.FirstOrDefault();
            string automationScriptXml = firstItem.Value.Document;

            // Doing this way as trying to figure out why newlines where giving issues was not worth it.
            var line = automationScriptXml.Substring(automationScriptXml.IndexOf("engine."), expectedLine.Length);

            line.Should().BeEquivalentTo(expectedLine);
        }

        #endregion

        /// <summary>
        /// Test for Automation script that refers to a library exe block from another script.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AutomationScriptCompiler_Solution6_BuildAsync()
        {
            // arrange
            string expectedResult = @"<DMSScript options=""272"">
	<Name>SimpleScript</Name>
	<Description />
	<Type>Automation</Type>
	<Author>SKYLINE2\Pedro</Author>
	<CheckSets>FALSE</CheckSets>
	<Folder />
	<Protocols>
	</Protocols>
	<Memory>
	</Memory>
	<Parameters>
	</Parameters>
	<Script>
		<Exe id=""2"" type=""csharp"">
			<Value>
				<![CDATA[using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Skyline.DataMiner.Automation;
using MyUtils;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name=""engine"">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
			engine.GenerateInformation(Utils.MakeUppercase(""test""));
	}
}]]>
			</Value>
			<Message />
			<Param type=""scriptRef"">Utils:MyLibrary</Param>
		</Exe>
	</Script>
</DMSScript>
";

            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution6"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            // act
            var result = await builder.BuildAsync().ConfigureAwait(false);

            // check
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(expectedResult, result.Single(x => x.Key.Name == "SimpleScript").Value.Document);
        }

        /// <summary>
        /// Test for Automation script that refers to a library exe block defined in the same script.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AutomationScriptCompiler_Solution7_BuildAsync()
        {
            // arrange
            string expectedResult = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<DMSScript options=""272"" xmlns=""http://www.skyline.be/automation"">
	<Name>LibraryInOwnScript</Name>
	<Description />
	<Type>Automation</Type>
	<Author>SKYLINE2\Pedro</Author>
	<CheckSets>FALSE</CheckSets>
	<Folder />

	<Protocols>
	</Protocols>

	<Memory>
	</Memory>

	<Parameters>
	</Parameters>

	<Script>
		<Exe id=""1"" type=""csharp"">
			<Value>
				<![CDATA[using MyUtils;

using Skyline.DataMiner.Automation;

public class Script
{
	public void Run(Engine engine)
	{
		engine.GenerateInformation(Utils.MakeUppercase(""test""));
	}
}]]>
			</Value>
			<!--<Param type=""debug"">true</Param>-->
			<Message />
			<Param type=""scriptRef"">[AutomationScriptName]:MyLibrary</Param>
		</Exe>
		<Exe id=""2"" type=""csharp"">
			<Value>
				<![CDATA[namespace MyUtils
{
	public static class Utils
	{
		public static string MakeUppercase(string input)
		{
			return input.ToUpper();
		}
	}
}]]>
			</Value>
			<!--<Param type=""debug"">true</Param>-->
			<Message />
			<Param type=""preCompile"">true</Param>
			<Param type=""libraryName"">MyLibrary</Param>
		</Exe>
	</Script>
</DMSScript>";

            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution7"));
            var path = Path.Combine(dir, "AutomationScript.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            // act
            var result = await builder.BuildAsync().ConfigureAwait(false);

            // check
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expectedResult, result[0].Value.Document);
        }
        
        [TestMethod]
        public async Task AutomationScriptCompilerUsingNuGetPackages_Solution8_SrmAsync()
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Solutions\Solution8"));
            var path = Path.Combine(dir, "Solution8.sln");

            var solution = AutomationScriptSolution.Load(path);
            var builder = new AutomationScriptSolutionBuilder(solution);

            var result = await builder.BuildAsync().ConfigureAwait(false);

            Assert.AreEqual(1, result.Count, "Expected 1 script in build result.");

            var firstItem = result.FirstOrDefault();
            string automationScriptXml = firstItem.Value.Document;

            var document = XDocument.Parse(automationScriptXml);
            XNamespace ns = document.Root.GetDefaultNamespace();

            var referenceNodes = document
                ?.Element(ns + "DMSScript")
                ?.Element(ns + "Script")
                ?.Element(ns + "Exe")
                ?.Elements(ns + "Param");

            Assert.IsNotNull(referenceNodes);

            List<string> generatedEntries = new List<string>();
            foreach (var referenceNode in referenceNodes)
            {
                generatedEntries.Add(referenceNode.Value);
            }

            List<string> expectedEntries = new List<string>
            {
                @"Microsoft.CSharp.dll",
                @"System.Data.dll",
                @"System.Data.DataSetExtensions.dll",
                @"System.Drawing.dll",
                @"System.IO.Compression.FileSystem.dll",
                @"System.Runtime.Caching.dll",
                @"System.Runtime.Serialization.dll",
                @"System.Xml.Linq.dll",

                @"C:\Skyline DataMiner\ProtocolScripts\DllImport\SRM\SLSRMLibrary.dll",
                @"C:\Skyline DataMiner\ProtocolScripts\DllImport\SRM\SLDijkstraSearch.dll",
                @"C:\Skyline DataMiner\ProtocolScripts\DllImport\SRM\Skyline.DataMiner.Core.SRM.Utils.IAS.dll"
            };

            generatedEntries.Should().BeEquivalentTo(expectedEntries);
        }
    }
}
