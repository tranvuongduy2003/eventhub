#!/usr/bin/env node
/**
 * Export or verify the committed OpenAPI YAML contract from the API build output.
 *
 * Usage (from repo root):
 *   node scripts/openapi/sync-contract.mjs          # regenerate contracts/openapi/api.v1.yaml
 *   node scripts/openapi/sync-contract.mjs --check  # fail if YAML is out of date (CI)
 */

import { createRequire } from 'node:module'
import { execSync } from 'node:child_process'
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const scriptDirectory = dirname(fileURLToPath(import.meta.url))
const repositoryRoot = join(scriptDirectory, '../..')
const buildJsonPath = join(repositoryRoot, 'contracts/openapi/.build/api-v1.json')
const committedYamlPath = join(repositoryRoot, 'contracts/openapi/api.v1.yaml')
const checkOnly = process.argv.includes('--check')

const requireFromWeb = createRequire(join(repositoryRoot, 'web/package.json'))
const { parse, stringify } = requireFromWeb('yaml')

function buildOpenApiJson() {
  execSync(
    'dotnet build src/Api/EventHub.Api.csproj -c Release -p:OpenApiGenerateDocuments=true',
    { cwd: repositoryRoot, stdio: 'inherit' },
  )
}

function loadDocumentFromJson(path) {
  return JSON.parse(readFileSync(path, 'utf8'))
}

function loadDocumentFromYaml(path) {
  const raw = readFileSync(path, 'utf8')
  const withoutHeader = raw.replace(/^#.*\n/gm, '').trimStart()
  return parse(withoutHeader)
}

function sortKeys(value) {
  if (Array.isArray(value)) {
    return value.map(sortKeys)
  }

  if (value !== null && typeof value === 'object') {
    return Object.keys(value)
      .sort()
      .reduce((sorted, key) => {
        sorted[key] = sortKeys(value[key])
        return sorted
      }, {})
  }

  return value
}

function canonicalize(document) {
  return JSON.stringify(sortKeys(document))
}

function writeYaml(document) {
  const yamlBody = stringify(document, {
    lineWidth: 0,
    sortMapEntries: true,
  })

  writeFileSync(
    committedYamlPath,
    `# Generated from EventHub.Api (OpenAPI v1). Do not edit by hand — run:\n` +
      `#   yarn --cwd web api:export\n` +
      `${yamlBody}`,
    'utf8',
  )
}

mkdirSync(dirname(buildJsonPath), { recursive: true })
buildOpenApiJson()

const builtDocument = loadDocumentFromJson(buildJsonPath)

if (checkOnly) {
  const committedDocument = loadDocumentFromYaml(committedYamlPath)
  const builtCanonical = canonicalize(builtDocument)
  const committedCanonical = canonicalize(committedDocument)

  if (builtCanonical !== committedCanonical) {
    console.error(
      'OpenAPI contract is out of date. Regenerate and commit contracts/openapi/api.v1.yaml:\n' +
        '  yarn --cwd web api:export',
    )
    process.exit(1)
  }

  console.log('OpenAPI contract is in sync with the API.')
  process.exit(0)
}

writeYaml(builtDocument)
console.log(`Wrote ${committedYamlPath}`)
