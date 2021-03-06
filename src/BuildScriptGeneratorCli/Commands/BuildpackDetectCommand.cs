// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.Common;

namespace Microsoft.Oryx.BuildScriptGeneratorCli
{
    [Command("buildpack-detect", Description = "Determines whether Oryx can be applied as a buildpack to " +
        "an app in the current working directory.")]
    internal class BuildpackDetectCommand : CommandBase
    {
        // CodeDetectFail @ https://github.com/buildpack/lifecycle/blob/master/detector.go
        public const int DetectorFailCode = 100;

        [Argument(0, Description = "The source directory.")]
        public string SourceDir { get; set; }

        [Option("--platform-dir <dir>", CommandOptionType.SingleValue, Description = "Platform directory path.")]
        public string PlatformDir { get; set; }

        [Option("--plan-path <path>", CommandOptionType.SingleValue, Description = "Build plan TOML path.")]
        public string PlanPath { get; set; }

        internal override bool IsValidInput(IServiceProvider serviceProvider, IConsole console)
        {
            var result = true;
            var options = serviceProvider.GetRequiredService<IOptions<BuildScriptGeneratorOptions>>().Value;
            var logger = serviceProvider.GetService<ILogger<BuildpackDetectCommand>>();

			// Set from ConfigureBuildScriptGeneratorOptions
            if (!Directory.Exists(options.SourceDir))
            {
                logger.LogError("Could not find the source directory {srcDir}", options.SourceDir);
                console.Error.WriteLine($"Error: Could not find the source directory '{options.SourceDir}'.");
                result = false;
            }

			if (!string.IsNullOrWhiteSpace(PlanPath))
			{
				PlanPath = Path.GetFullPath(PlanPath);
	            if (!File.Exists(PlanPath))
	            {
	                logger?.LogError("Could not find build plan file {planPath}", PlanPath);
	                console.Error.WriteLine($"Error: Could not find build plan file '{PlanPath}'.");
	                result = false;
	            }
			}

			if (!string.IsNullOrWhiteSpace(PlatformDir))
			{
				PlatformDir = Path.GetFullPath(PlatformDir);
	            if (!Directory.Exists(PlatformDir))
	            {
	                logger?.LogError("Could not find platform directory {platformDir}", PlatformDir);
	                console.Error.WriteLine($"Error: Could not find platform directory '{PlatformDir}'.");
	                result = false;
	            }
			}

            return result;
        }

        internal override void ConfigureBuildScriptGeneratorOptions(BuildScriptGeneratorOptions options)
        {
            BuildScriptGeneratorOptionsHelper.ConfigureBuildScriptGeneratorOptions(
                options,
                SourceDir,
                destinationDir: null,
                intermediateDir: null,
                language: null,
                languageVersion: null,
                scriptOnly: false,
                properties: null);
        }

        internal override int Execute(IServiceProvider serviceProvider, IConsole console)
        {
            var generator = serviceProvider.GetRequiredService<IBuildScriptGenerator>();

            var options = serviceProvider.GetRequiredService<IOptions<BuildScriptGeneratorOptions>>().Value;
            var env = serviceProvider.GetRequiredService<CliEnvironmentSettings>();
            var repo = serviceProvider.GetRequiredService<ISourceRepoProvider>().GetSourceRepo();

            var ctx = BuildScriptGenerator.CreateContext(options, env, repo);
            var compatPlats = generator.GetCompatiblePlatforms(ctx);

            if (compatPlats != null && compatPlats.Any())
            {
                console.WriteLine("# Detected platforms:");
                console.WriteLine(string.Join(' ', compatPlats.Select(pair => $"{pair.Item1.Name}=\"{pair.Item2}\"")));
                return ProcessConstants.ExitSuccess;
            }

            return DetectorFailCode;
        }
    }
}
