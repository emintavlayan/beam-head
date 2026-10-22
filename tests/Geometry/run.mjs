import { spawnSync } from "node:child_process";
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";

const directory = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(directory, "../..");
const output = path.join(directory, "output");

/// Runs the actual Fable-compiled geometry suite with production JSCAD dependencies.
function run(command, arguments_) {
    const result = spawnSync(command, arguments_, { cwd: root, stdio: "inherit" });
    if (result.error) throw result.error;
    if (result.status !== 0) process.exit(result.status ?? 1);
}

run("dotnet", ["fable", path.join(directory, "Jscad", "Geometry.Jscad.Tests.fsproj"), "--outDir", output]);
run("node", ["--loader", pathToFileURL(path.join(directory, "jscad-loader.mjs")).href, path.join(output, "Jscad.Tests.js")]);
