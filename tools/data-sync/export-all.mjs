#!/usr/bin/env node
/**
 * export-all.mjs — entry point for `npm run data:sync`
 * Runs all data export scripts in sequence, then mirrors generated JSON to the
 * Unity Resources folder so Resources.Load("cards") works at runtime.
 */
import { copyFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT  = join(__dirname, '../..');
const GEN_DIR    = join(REPO_ROOT, 'Assets/_Project/Scripts/Data/Generated');
const RES_DIR    = join(REPO_ROOT, 'Assets/_Project/Data/Resources');

mkdirSync(RES_DIR, { recursive: true });

import './export-reference.mjs';
import './export-cards.mjs';

// Mirror generated JSON into the Unity Resources folder after generation completes.
// Using setTimeout(0) to run after the imported modules' top-level code finishes.
setTimeout(() => {
  // Mirror generated JSON into the Unity Resources folder.
  const generatedFiles = ['cards.json', 'cosmic-ages.json', 'correspondence.json', 'game-modes.json'];
  for (const f of generatedFiles) {
    copyFileSync(join(GEN_DIR, f), join(RES_DIR, f));
    console.log(`[sync] mirrored ${f} → Assets/_Project/Data/Resources/`);
  }

  // crucible-codex.json is hand-authored in Resources; copy it into Generated for consistency.
  copyFileSync(join(RES_DIR, 'crucible-codex.json'), join(GEN_DIR, 'crucible-codex.json'));
  console.log('[sync] mirrored crucible-codex.json → Assets/_Project/Scripts/Data/Generated/');
}, 0);
