import { readFile } from "node:fs/promises";

const jscadSource = new URL("../../src/Client/BeamHeadJscad.js", import.meta.url).href;

/// Loads the existing browser ESM boundary as ESM under all supported Node versions.
export async function load(url, context, nextLoad) {
    if (url === jscadSource) {
        return { format: "module", source: await readFile(new URL(url), "utf8"), shortCircuit: true };
    }
    return nextLoad(url, context);
}
