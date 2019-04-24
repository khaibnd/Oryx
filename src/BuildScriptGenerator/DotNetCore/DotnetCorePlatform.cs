﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;

namespace Microsoft.Oryx.BuildScriptGenerator.DotNetCore
{
    /// <summary>
    /// .NET Core platform.
    /// </summary>
    [BuildProperty(Constants.ZipAllOutputBuildPropertyKey, Constants.ZipAllOutputBuildPropertyKeyDocumentation)]
    internal class DotnetCorePlatform : IProgrammingPlatform
    {
        private readonly IDotnetCoreVersionProvider _versionProvider;
        private readonly IAspNetCoreWebAppProjectFileProvider _aspNetCoreWebAppProjectFileProvider;
        private readonly IEnvironmentSettingsProvider _environmentSettingsProvider;
        private readonly ILogger<DotnetCorePlatform> _logger;
        private readonly DotnetCoreLanguageDetector _detector;

        public DotnetCorePlatform(
            IDotnetCoreVersionProvider versionProvider,
            IAspNetCoreWebAppProjectFileProvider aspNetCoreWebAppProjectFileProvider,
            IEnvironmentSettingsProvider environmentSettingsProvider,
            ILogger<DotnetCorePlatform> logger,
            DotnetCoreLanguageDetector detector)
        {
            _versionProvider = versionProvider;
            _aspNetCoreWebAppProjectFileProvider = aspNetCoreWebAppProjectFileProvider;
            _environmentSettingsProvider = environmentSettingsProvider;
            _logger = logger;
            _detector = detector;
        }

        public string Name => DotnetCoreConstants.LanguageName;

        public IEnumerable<string> SupportedLanguageVersions => _versionProvider.SupportedDotNetCoreVersions;

        public LanguageDetectorResult Detect(ISourceRepo sourceRepo)
        {
            return _detector.Detect(sourceRepo);
        }

        public BuildScriptSnippet GenerateBashBuildScriptSnippet(BuildScriptGeneratorContext context)
        {
            var buildProperties = new Dictionary<string, string>();
            (string projectFile, string publishDir) = GetProjectFileAndPublishDir(context.SourceRepo);
            if (string.IsNullOrEmpty(projectFile) || string.IsNullOrEmpty(publishDir))
            {
                return null;
            }

            string startupFileName = null;
            var projectFileContent = context.SourceRepo.ReadFile(projectFile);
            var projFileDoc = XDocument.Load(new StringReader(projectFileContent));
            var assemblyNameElement = projFileDoc.XPathSelectElement(DotnetCoreConstants.AssemblyNameXPathExpression);
            if (assemblyNameElement == null)
            {
                var name = Path.GetFileNameWithoutExtension(projectFile);
                startupFileName = $"{name}.dll";
            }
            else
            {
                startupFileName = $"{assemblyNameElement.Value}.dll";
            }

            buildProperties[DotnetCoreConstants.StartupFileName] = startupFileName;
            bool zipAllOutput = ShouldZipAllOutput(context);
            buildProperties[ManifestFilePropertyKeys.ZipAllOutput] = zipAllOutput.ToString().ToLowerInvariant();

            _environmentSettingsProvider.TryGetAndLoadSettings(out var environmentSettings);

            var templateProperties = new DotNetCoreBashBuildSnippetProperties
            {
                ProjectFile = projectFile,
                PublishDirectory = publishDir,
                BuildProperties = buildProperties,
                BenvArgs = $"dotnet={context.DotnetCoreVersion}",
                DirectoriesToExcludeFromCopyToIntermediateDir = GetDirectoriesToExcludeFromCopyToIntermediateDir(
                    context),
                PreBuildScriptPath = environmentSettings?.PreBuildScriptPath,
                PostBuildScriptPath = environmentSettings?.PostBuildScriptPath,
                ManifestFileName = Constants.ManifestFileName,
                ZipAllOutput = zipAllOutput,
            };
            var script = TemplateHelpers.Render(
                TemplateHelpers.TemplateResource.DotNetCoreSnippet,
                templateProperties,
                _logger);
            return new BuildScriptSnippet { BashBuildScriptSnippet = script, IsFullScript = true };
        }

        public bool IsCleanRepo(ISourceRepo repo)
        {
            (_, string expectedPublishDir) = GetProjectFileAndPublishDir(repo);
            return !repo.DirExists(expectedPublishDir);
        }

        public string GenerateBashRunScript(RunScriptGeneratorOptions runScriptGeneratorOptions)
        {
            throw new System.NotImplementedException();
        }

        public bool IsEnabled(BuildScriptGeneratorContext scriptGeneratorContext)
        {
            return scriptGeneratorContext.EnableDotNetCore;
        }

        public bool IsEnabledForMultiPlatformBuild(BuildScriptGeneratorContext scriptGeneratorContext)
        {
            // A user has the power to either enable or disable multi-platform builds entirely.
            // However if user enables it, ASP.NET Core platform still explicitly opts out of it.
            return false;
        }

        public void SetRequiredTools(
            ISourceRepo sourceRepo,
            string targetPlatformVersion,
            IDictionary<string, string> toolsToVersion)
        {
            Debug.Assert(toolsToVersion != null, $"{nameof(toolsToVersion)} must not be null.");
            if (!string.IsNullOrWhiteSpace(targetPlatformVersion))
            {
                toolsToVersion["dotnet"] = targetPlatformVersion;
            }
        }

        public void SetVersion(BuildScriptGeneratorContext context, string version)
        {
            context.DotnetCoreVersion = version;
        }

        public IEnumerable<string> GetDirectoriesToExcludeFromCopyToBuildOutputDir(
            BuildScriptGeneratorContext scriptGeneratorContext)
        {
            var dirs = new List<string>();
            dirs.Add("obj");
            dirs.Add("bin");
            return dirs;
        }

        public IEnumerable<string> GetDirectoriesToExcludeFromCopyToIntermediateDir(
            BuildScriptGeneratorContext scriptGeneratorContext)
        {
            var dirs = new List<string>();
            dirs.Add(".git");
            dirs.Add("obj");
            dirs.Add("bin");
            dirs.Add(DotnetCoreConstants.OryxOutputPublishDirectory);
            return dirs;
        }

        private static bool ShouldZipAllOutput(BuildScriptGeneratorContext context)
        {
            return BuildPropertiesHelper.IsTrue(
                Constants.ZipAllOutputBuildPropertyKey,
                context,
                valueIsRequired: false);
        }

        private (string projFile, string publishDir) GetProjectFileAndPublishDir(ISourceRepo repo)
        {
            var projectFile = _aspNetCoreWebAppProjectFileProvider.GetProjectFile(repo);
            if (string.IsNullOrEmpty(projectFile))
            {
                return (null, null);
            }

            var publishDir = Path.Combine(
                new FileInfo(projectFile).Directory.FullName,
                DotnetCoreConstants.OryxOutputPublishDirectory);
            return (projectFile, publishDir);
        }
    }
}