import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const inputPath = 'D:/MyGitHub/ProjectA/Project-A-SquareTowerDefense/Assets/Resources/WavePerEnemies&Gold&Gem.xlsx';
const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);
console.log((await workbook.inspect({ kind: 'workbook,sheet,table', maxChars: 10000, tableMaxRows: 10, tableMaxCols: 12, tableMaxCellChars: 100 })).ndjson);
const sheetNames = ['Sheet1'];
for (const sheetName of sheetNames) {
  const sheet = workbook.worksheets.getItem(sheetName);
  console.log(`SHEET:${sheet.name}`);
  const used = sheet.getUsedRange();
  console.log(`USED:${used ? used.address : 'none'}`);
  if (used) {
    console.log('VALUES', JSON.stringify(used.values));
    console.log('FORMULAS', JSON.stringify(used.formulas));
    console.log((await workbook.inspect({ kind: 'region', sheetId: sheet.name, range: used.address, maxChars: 16000 })).ndjson);
    console.log((await workbook.inspect({ kind: 'formula', sheetId: sheet.name, range: used.address, maxChars: 8000, options: { maxResults: 100 } })).ndjson);
  }
  const preview = await workbook.render({ sheetName: sheet.name, autoCrop: 'all', scale: 1, format: 'png' });
  await fs.writeFile(`${sheet.name.replace(/[^A-Za-z0-9_-]/g,'_')}.png`, new Uint8Array(await preview.arrayBuffer()));
}
