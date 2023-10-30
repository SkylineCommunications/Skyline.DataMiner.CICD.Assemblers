namespace Assemblers.ProtocolTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Org.XmlUnit.Builder;
    using Org.XmlUnit.Diff;

    using Skyline.DataMiner.CICD.Assemblers.Common;
    using Skyline.DataMiner.CICD.Assemblers.Protocol;
    using Skyline.DataMiner.CICD.Parsers.Common.VisualStudio.Projects;
    using Skyline.DataMiner.CICD.Parsers.Common.Xml;
    using Skyline.DataMiner.CICD.Parsers.Protocol.VisualStudio;

    [TestClass]
    public class ProtocolBuilderTests
    {
        [TestMethod]
        public void TestProtocol()
        {
            var solutionFilePath = @"D:\TESTING\ConnectorSDKStyle\ConnectorSDKStyle.sln";
            ProtocolSolution solution = ProtocolSolution.Load(solutionFilePath);
			ProtocolBuilder builder = new ProtocolBuilder(solution);

            var buildResultItems = builder.Build();

            File.WriteAllText(@"D:\TESTING\resultProtocol.xml", buildResultItems.Document);
		}


        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_Basic()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", new Project("QAction_1", new[]{ new ProjectFile("QAction_1.cs", "using System;") }) },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_MultipleQActions()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
		<QAction id=""2"" encoding=""csharp"" name=""QAction 2"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"">
			<![CDATA[using System;]]>
		</QAction>
		<QAction id=""2"" encoding=""csharp"" name=""QAction 2"">
			<![CDATA[using System.Xml;]]>
		</QAction>
	</QActions>
</Protocol>";


            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", new Project("QAction_1", new[]{ new ProjectFile("QAction_1.cs", "using System;") }) },
                { "QAction_2", new Project("QAction_2", new[]{ new ProjectFile("QAction_2.cs", "using System.Xml;") }) },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_VersionHistory()
        {
            string originalProtocol = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no"" ?>
<Protocol>
	<VersionHistory>
		<Branches>
			<Branch id=""1"">
				<SystemVersions>
					<SystemVersion id=""0"">
						<MajorVersions>
							<MajorVersion id=""0"">
								<MinorVersions>
									<MinorVersion id=""1"">
										<Changes>
											<Change>Change1</Change>
											<Fix>Fix1</Fix>
											<NewFeature>NewFeature1</NewFeature>
										</Changes>
										<Date>2020-01-02</Date>
										<Provider>
											<Author>TWA</Author>
											<Company>Skyline Communications</Company>
										</Provider>
									</MinorVersion>
									<MinorVersion id=""10"">
										<Changes>
											<Change>Change2</Change>
										</Changes>
										<Date>2020-01-03</Date>
										<Provider>
											<Author>TWA</Author>
											<Company>Skyline Communications</Company>
										</Provider>
									</MinorVersion>
									<MinorVersion id=""11"">
										<Changes></Changes><!-- empty to detect IndexOutOfRange -->
										<Date>2020-01-04</Date>
										<Provider>
											<Author>TWA</Author>
											<Company>Skyline Communications</Company>
										</Provider>
									</MinorVersion>
								</MinorVersions>
							</MajorVersion>
						</MajorVersions>
					</SystemVersion>
				</SystemVersions>
			</Branch>
		</Branches>
	</VersionHistory>
</Protocol>";

            string expected = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no"" ?>
<!--

Revision History (auto generated):

DATE          VERSION     AUTHOR                         COMMENTS

02/01/2020    1.0.0.1     TWA, Skyline Communications    Fix: Fix1
                                                         Change: Change1
                                                         NF: NewFeature1
03/01/2020    1.0.0.10    TWA, Skyline Communications    Change: Change2
04/01/2020    1.0.0.11    TWA, Skyline Communications    
-->
<Protocol>
	<VersionHistory>
		<Branches>
			<Branch id=""1"">
				<SystemVersions>
					<SystemVersion id=""0"">
						<MajorVersions>
							<MajorVersion id=""0"">
								<MinorVersions>
									<MinorVersion id=""1"">
										<Changes>
											<Change>Change1</Change>
											<Fix>Fix1</Fix>
											<NewFeature>NewFeature1</NewFeature>
										</Changes>
										<Date>2020-01-02</Date>
										<Provider>
											<Author>TWA</Author>
											<Company>Skyline Communications</Company>
										</Provider>
									</MinorVersion>
									<MinorVersion id=""10"">
										<Changes>
											<Change>Change2</Change>
										</Changes>
										<Date>2020-01-03</Date>
										<Provider>
											<Author>TWA</Author>
											<Company>Skyline Communications</Company>
										</Provider>
									</MinorVersion>
									<MinorVersion id=""11"">
										<Changes></Changes><!-- empty to detect IndexOutOfRange -->
										<Date>2020-01-04</Date>
										<Provider>
											<Author>TWA</Author>
											<Company>Skyline Communications</Company>
										</Provider>
									</MinorVersion>
								</MinorVersions>
							</MajorVersion>
						</MajorVersions>
					</SystemVersion>
				</SystemVersions>
			</Branch>
		</Branches>
	</VersionHistory>
</Protocol>";

            var projects = new Dictionary<string, Project>();

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_MultipleFiles()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""3"" encoding=""csharp"" name=""QAction 3"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""3"" encoding=""csharp"" name=""QAction 3"">
			<![CDATA[using System;
//---------------------------------
// QAction_3.cs
//---------------------------------

//---------------------------------
// Class1.cs
//---------------------------------
class Class1 {}]]>
		</QAction>
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_3", new Project("QAction_3", new[]{
                    new ProjectFile("QAction_3.cs", "using System;"),
                    new ProjectFile("Class1.cs", "using System; class Class1 {}")
                }) },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_TargetNotEmpty()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"">
            <![CDATA[using System;]]>
        </QAction>
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", new Project("QAction_1", new[]{ new ProjectFile("QAction_1.cs", "using System.Xml;") }) },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            var exception = Assert.ThrowsException<AssemblerException>(() => builder.Build());

            Assert.AreEqual("Cannot replace QAction 1, because the target XML node is not empty!", exception.Message);
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_DllImports()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" dllImport=""System.Data.dll"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var projectFiles = new[] { new ProjectFile("QAction_1.cs", "using System;") };
            var references = new[] { new Reference("System.Data.dll"), new Reference("System.Xml.dll") };
            var project1 = new Project("QAction_1", projectFiles: projectFiles, references: references);

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", project1 },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_DllImports_NoDuplicate()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" dllImport=""System.Data.dll"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" dllImport=""System.Data.dll"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var projectFiles = new[] { new ProjectFile("QAction_1.cs", "using System;") };
            var references = new[] { new Reference("System.Data.dll"), new Reference("System.Xml.dll") };
            var project1 = new Project("QAction_1", projectFiles: projectFiles, references: references);

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", project1 },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_DllImports_ProjectReference()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" dllImport=""MyLibrary.dll"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var projectFiles = new[] { new ProjectFile("QAction_1.cs", "using System;") };
            var projectReferences = new[] { new ProjectReference("MyLibrary") };
            var project1 = new Project("QAction_1", projectFiles: projectFiles, projectReferences: projectReferences);

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", project1 },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_ClassLibrary()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""63000"" encoding=""csharp"" name=""** Auto-generated Class Library **"" options=""precompile"" />
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""63000"" encoding=""csharp"" name=""** Auto-generated Class Library **"" options=""precompile"">
			<![CDATA[namespace Skyline.DataMiner.Library { }]]>
		</QAction>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" dllImport=""System.Data.dll;[ProtocolName].[ProtocolVersion].QAction.63000.dll"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var project63000 = new Project("QAction_63000", new[] { new ProjectFile("QAction_63000.cs", "namespace Skyline.DataMiner.Library { }") });

            var projectFiles = new[] { new ProjectFile("QAction_1.cs", "using System;") };
            var references = new[] { new Reference("System.Data.dll"), new Reference("System.Xml.dll") };
            var projectReferences = new[] { new ProjectReference("QAction_63000") };
            var project1 = new Project("QAction_1", projectFiles: projectFiles, projectReferences: projectReferences, references: references);

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_63000", project63000 },
                { "QAction_1", project1 },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_ReferencedQAction()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" options=""precompile"" />
		<QAction id=""2"" encoding=""csharp"" name=""QAction 2"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" options=""precompile"">
			<![CDATA[using System;]]>
		</QAction>
		<QAction id=""2"" encoding=""csharp"" name=""QAction 2"" dllImport=""[ProtocolName].[ProtocolVersion].QAction.1.dll"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var project1 = new Project("QAction_1", new[] { new ProjectFile("QAction_1.cs", "using System;") });

            var projectReferences = new[] { new ProjectReference("QAction_1") };
            var project2 = new Project("QAction_2", projectFiles: new[] { new ProjectFile("QAction_2.cs", "using System;") }, projectReferences: projectReferences);

            var projects = new Dictionary<string, Project>()
                               {
                                   { "QAction_1", project1 },
                                   { "QAction_2", project2 },
                               };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_ReferencedQActionWithCustomName()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" options=""precompile;dllName=Common.dll"" />
		<QAction id=""2"" encoding=""csharp"" name=""QAction 2"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" options=""precompile;dllName=Common.dll"">
			<![CDATA[using System;]]>
		</QAction>
		<QAction id=""2"" encoding=""csharp"" name=""QAction 2"" dllImport=""[ProtocolName].[ProtocolVersion].Common.dll"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var project1 = new Project("QAction_1", new[] { new ProjectFile("QAction_1.cs", "using System;") });

            var projectReferences = new[] { new ProjectReference("QAction_1") };
            var project2 = new Project("QAction_2", projectFiles: new[] { new ProjectFile("QAction_2.cs", "using System;") }, projectReferences: projectReferences);

            var projects = new Dictionary<string, Project>()
                               {
                                   { "QAction_1", project1 },
                                   { "QAction_2", project2 },
                               };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_MissingQAction()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>();
            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);
            var exception = Assert.ThrowsException<AssemblerException>(() => builder.Build());

            Assert.AreEqual("Project with name 'QAction_1' could not be found!", exception.Message);
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_MissingName()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"">
			<![CDATA[using System;]]>
		</QAction>
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", new Project("QAction_1", new[]{ new ProjectFile("QAction_1.cs", "using System;") }) },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_JScriptQAction()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""jscript"" name=""QAction 1"">
 			<![CDATA[
				id:123 = ""test"";
			]]>
		</QAction>
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""jscript"" name=""QAction 1"">
 			<![CDATA[
				id:123 = ""test"";
			]]>
		</QAction>
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>();

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_ProtocolBuilder_SpecialCharacters()
        {
            string originalProtocol = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"" />
	</QActions>
</Protocol>";

            string expected = @"<Protocol>
	<QActions>
		<QAction id=""1"" encoding=""csharp"" name=""QAction 1"">
			<![CDATA[using Characterø;]]>
		</QAction>
	</QActions>
</Protocol>";

            var projects = new Dictionary<string, Project>()
            {
                { "QAction_1", new Project("QAction_1", new[]{ new ProjectFile("QAction_1.cs", "using Characterø;") }) },
            };

            ProtocolBuilder builder = new ProtocolBuilder(XmlDocument.Parse(originalProtocol), projects);

            string result = builder.Build().Document;

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }

        [TestMethod]
        public void ProtocolCompiler_Solution_Build()
        {
            // arrange
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.GetFullPath(Path.Combine(baseDir, @"TestFiles\Protocol\Solution1"));
            var path = Path.Combine(dir, "protocol.sln");

            var solution = ProtocolSolution.Load(path);

            // act
            ProtocolBuilder builder = new ProtocolBuilder(solution);
            var buildResultItems = builder.Build();

            string result = buildResultItems.Document;

            // check
            string expected = @"<Protocol xmlns=""http://www.skyline.be/protocol"">
	<QActions>
		<QAction id=""63000"" name=""** Auto-generated Class Library **"" encoding=""csharp"" options=""precompile"">
			<![CDATA[namespace QAction_63000
{
    public class QAction_63000
    {
    }
}
]]>
		</QAction>
		<QAction id=""1"" name=""QA1"" encoding=""csharp"" triggers=""1"" options=""precompile"" dllImport=""Newtonsoft.Json.dll;[ProtocolName].[ProtocolVersion].QAction.63000.dll"">
			<![CDATA[namespace QAction_1
{
    public class Class1
    {
    }
}
]]>
		</QAction>
		<QAction id=""2"" name=""QA2"" encoding=""csharp"" triggers=""2"" dllImport=""[ProtocolName].[ProtocolVersion].QAction.1.dll;[ProtocolName].[ProtocolVersion].QAction.63000.dll"">
			<![CDATA[namespace QAction_2
{
    public class QAction_2
    {
    }
}
]]>
		</QAction>
		<QAction id=""3"" name=""QA3"" encoding=""csharp"" triggers=""3"" dllImport=""[ProtocolName].[ProtocolVersion].QAction.63000.dll"">
			<![CDATA[
//---------------------------------
// QAction_3.cs
//---------------------------------
namespace QAction_3
{
    public class QAction_3
    {
    }
}

//---------------------------------
// Class1.cs
//---------------------------------
namespace QAction_3
{
    public class Class1
    {
    }
}

//---------------------------------
// SubDir\Class2.cs
//---------------------------------
namespace QAction_3
{
    public class Class2
    {
    }
}
]]>
		</QAction>
		<QAction id=""4"" name=""QA4"" encoding=""jscript"" triggers=""4"">
			<![CDATA[
				id:123 = ""test"";
			]]>
		</QAction>
    </QActions>
</Protocol>";

            Diff d = DiffBuilder.Compare(Input.FromString(expected))
                .WithTest(Input.FromString(result)).Build();

            Assert.IsFalse(d.HasDifferences(), d.ToString());
        }
    }
}
