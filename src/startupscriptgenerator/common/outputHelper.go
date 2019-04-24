// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

package common

import (
	"strings"
)

func AddScriptToExtractZippedOutput(scriptBuilder *strings.Builder, appFolder string) {
	zipFileName := "oryx_output.tar.gz"
	scriptBuilder.WriteString("zipFileName=" + zipFileName + "\n")
	scriptBuilder.WriteString("cd \"" + appFolder + "\"\n")
	scriptBuilder.WriteString("if [ -f \"$zipFileName\" ]; then\n")
	scriptBuilder.WriteString(
		"    echo \"Found '$zipFileName' under '" + appFolder + "'. Extracting it's contents into it...\"\n")
	scriptBuilder.WriteString("    tar -xzf \"$zipFileName\"\n")
	scriptBuilder.WriteString("    echo Done.\n")
	scriptBuilder.WriteString("    echo Deleting the file '$zipFileName'...\n")
	scriptBuilder.WriteString("    rm -f \"$zipFileName\"\n")
	scriptBuilder.WriteString("    echo \"Done.\"\n")
	scriptBuilder.WriteString("fi\n\n")
}

func AddScriptToCopyToDir(scriptBuilder *strings.Builder, srcDir string, destDir string) {
	scriptBuilder.WriteString("if [ -d \"" + destDir + "\" ]; then\n")
	scriptBuilder.WriteString("    echo Directory '" + destDir + "' already exists. Deleting it...\n")
	scriptBuilder.WriteString("    rm -rf \"" + destDir + "\"\n")
	scriptBuilder.WriteString("    echo \"Done.\"\n")
	scriptBuilder.WriteString("fi\n")
	scriptBuilder.WriteString("mkdir -p \"" + destDir + "\"\n")
	scriptBuilder.WriteString("echo Copying content from '" + srcDir + "' to directory '" + destDir + "'...\n")
	scriptBuilder.WriteString("cp -rf \"" + srcDir + "\"/* \"" + destDir + "\"\n")
	scriptBuilder.WriteString("echo \"Done.\"\n")
}
