#!/usr/bin/env node
/**
 * Fails if any locale is missing keys present in en-US (master).
 * Usage: node apps/admin/scripts/check-i18n-keys.mjs
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const localesRoot = path.resolve(__dirname, "../src/i18n/locales");
const master = "en-US";
const locales = fs.readdirSync(localesRoot).filter((name) => fs.statSync(path.join(localesRoot, name)).isDirectory());

function flatten(obj, prefix = "") {
  /** @type {string[]} */
  const keys = [];
  for (const [key, value] of Object.entries(obj)) {
    const next = prefix ? `${prefix}.${key}` : key;
    if (value && typeof value === "object" && !Array.isArray(value)) {
      keys.push(...flatten(value, next));
    } else {
      keys.push(next);
    }
  }
  return keys;
}

function loadNamespace(locale, nsFile) {
  const raw = fs.readFileSync(path.join(localesRoot, locale, nsFile), "utf8");
  return JSON.parse(raw);
}

const masterDir = path.join(localesRoot, master);
const namespaces = fs.readdirSync(masterDir).filter((f) => f.endsWith(".json"));

/** @type {string[]} */
const problems = [];

for (const locale of locales) {
  if (locale === master) continue;
  for (const nsFile of namespaces) {
    const localePath = path.join(localesRoot, locale, nsFile);
    if (!fs.existsSync(localePath)) {
      problems.push(`[${locale}] missing file ${nsFile}`);
      continue;
    }
    const masterKeys = new Set(flatten(loadNamespace(master, nsFile)));
    const localeKeys = new Set(flatten(loadNamespace(locale, nsFile)));
    for (const key of masterKeys) {
      if (!localeKeys.has(key)) {
        problems.push(`[${locale}/${nsFile}] missing key: ${key}`);
      }
    }
  }
}

if (problems.length) {
  console.error(`i18n key check failed (${problems.length} issue(s)):`);
  for (const line of problems.slice(0, 80)) console.error(`  ${line}`);
  if (problems.length > 80) console.error(`  …and ${problems.length - 80} more`);
  process.exit(1);
}

console.log(`i18n key check OK — ${locales.length} locales, ${namespaces.length} namespaces vs ${master}.`);
