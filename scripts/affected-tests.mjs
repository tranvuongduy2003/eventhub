#!/usr/bin/env node
/**
 * Map a changed file → verification steps for the agent harness.
 * v1: layer heuristics + .graph/index.json (extend with full dependency graph later).
 *
 * Usage: node scripts/affected-tests.mjs <absolute-or-relative-file-path>
 * stdout: JSON { skip, steps[], error? }
 */

import { readFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

function loadGraph() {
  try {
    return JSON.parse(readFileSync(join(repoRoot, '.graph', 'index.json'), 'utf8'));
  } catch {
    return { version: 1, layers: {}, skipPatterns: [] };
  }
}

function toPosix(p) {
  return p.replace(/\\/g, '/');
}

function relPath(filePath) {
  const abs = resolve(repoRoot, filePath);
  return toPosix(relative(repoRoot, abs));
}

function matchesAny(rel, patterns) {
  return patterns.some((pattern) => {
    const re = new RegExp(
      '^' +
        pattern
          .replace(/\*\*/g, '.*')
          .replace(/\*/g, '[^/]*')
          .replace(/\./g, '\\.') +
        '$',
    );
    return re.test(rel);
  });
}

function featureSegment(rel, layerPrefix) {
  const rest = rel.slice(layerPrefix.length);
  const seg = rest.split('/')[0];
  return seg && !seg.includes('.') ? seg : null;
}

function buildSteps(rel, graph) {
  const steps = [];

  if (rel.startsWith('web/') && /\.(tsx?|jsx?)$/.test(rel)) {
    steps.push({ kind: 'eslint', file: rel });
    return steps;
  }

  if (rel.endsWith('.cs')) {
    steps.push({ kind: 'dotnet-format', file: rel });

    for (const [prefix, cfg] of Object.entries(graph.layers ?? {})) {
      if (!rel.startsWith(prefix)) continue;

      const feature =
        cfg.namespaceFromSegment === true ? featureSegment(rel, prefix) : null;

      if (cfg.postEditAction === 'test' && cfg.testProject) {
        const step = { kind: 'dotnet-test', project: cfg.testProject };
        if (feature) step.filter = `FullyQualifiedName~${feature}`;
        steps.push(step);
      } else if (cfg.postEditAction === 'build' && cfg.buildProject) {
        steps.push({ kind: 'dotnet-build', project: cfg.buildProject });
      }
      break;
    }
  }

  for (const [prefix, cfg] of Object.entries(graph.layers ?? {})) {
    if (!rel.startsWith(prefix)) continue;
    if (cfg.postEditAction === 'test' && cfg.testCommand) {
      steps.push({ kind: 'shell-test', command: cfg.testCommand });
    }
    break;
  }

  return steps;
}

function main() {
  const inputPath = process.argv[2];
  if (!inputPath) {
    console.log(JSON.stringify({ skip: true, steps: [], error: 'missing file path' }));
    process.exit(0);
  }

  const graph = loadGraph();
  const rel = relPath(inputPath);

  if (matchesAny(rel, graph.skipPatterns ?? [])) {
    console.log(JSON.stringify({ skip: true, steps: [], rel }));
    process.exit(0);
  }

  const steps = buildSteps(rel, graph);
  if (steps.length === 0) {
    console.log(JSON.stringify({ skip: true, steps: [], rel }));
    process.exit(0);
  }

  console.log(JSON.stringify({ skip: false, steps, rel }));
}

main();
