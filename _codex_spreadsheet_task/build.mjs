import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const root = 'D:/MyGitHub/ProjectA/Project-A-SquareTowerDefense';
const sourcePath = `${root}/Assets/Resources/WavePerEnemies&Gold&Gem.xlsx`;
const outputDir = `${root}/outputs/wave-reward-model`;
const outputPath = `${outputDir}/WavePerEnemies&Gold&Gem.xlsx`;
const previewPath = `${root}/_codex_spreadsheet_task/WavePerEnemies_Gold_Gem_horizontal_preview.png`;

function colLetter(n) {
  let s = '';
  while (n > 0) {
    const rem = (n - 1) % 26;
    s = String.fromCharCode(65 + rem) + s;
    n = Math.floor((n - 1) / 26);
  }
  return s;
}

const input = await FileBlob.load(sourcePath);
const workbook = await SpreadsheetFile.importXlsx(input);
const sheet = workbook.worksheets.getItem('Sheet1');
sheet.getRange('A1:AO100').clear({ applyTo: 'all' });
sheet.showGridLines = false;

sheet.getRange('A1').values = [['웨이브별 몬스터·골드·젬 및 티어 제작 계산표']];
sheet.getRange('A1:AO1').merge();
sheet.getRange('A1:AO1').format = { fill: '#17365D', font: { bold: true, color: '#FFFFFF', size: 16 }, horizontalAlignment: 'center', verticalAlignment: 'center' };
sheet.getRange('A1:AO1').format.rowHeight = 30;

const assumptionLabels = [['웨이브당 전투 턴', '사이클 1 기본 소환수 / 턴', '사이클당 웨이브 수', '일반 몬스터 골드 / 웨이브 배율', '중간보스 골드 / 기준 웨이브 배율', '중간보스 젬 / 기준 웨이브 배율', '최종보스 골드 / 웨이브 배율', '최종보스 젬 / 웨이브 배율', '보스 보상 사이클 배율', '중간보스 보상 기준 웨이브', '타워 기본 소환 시작 비용', '소환당 비용 증가']];
sheet.getRange('A3:L3').values = assumptionLabels;
sheet.getRange('A4:L4').values = [[5, 2, 10, 10, 50, 1, 100, 2, 1, 5, 50, 2]];
sheet.getRange('A3:L3').format = { fill: '#D9EAF7', font: { bold: true, color: '#17365D' }, horizontalAlignment: 'center', verticalAlignment: 'center', wrapText: true, borders: { preset: 'all', style: 'thin', color: '#9FBAD0' } };
sheet.getRange('A4:L4').format = { horizontalAlignment: 'center', verticalAlignment: 'center', borders: { preset: 'all', style: 'thin', color: '#D9D9D9' }, numberFormat: '#,##0' };
sheet.getRange('A3:L4').format.rowHeight = 36;

sheet.getRange('A6:B6').values = [['요약', '값']];
sheet.getRange('A6:B6').format = { fill: '#D9EAF7', font: { bold: true, color: '#17365D' }, borders: { preset: 'all', style: 'thin', color: '#9FBAD0' } };
sheet.getRange('A7:A14').values = [['1~39 일반 몬스터 수'], ['1~39 최종보스 수'], ['1~39 중간보스 수(보상 기준)'], ['1~39 골드 드롭 합계'], ['1~39 젬 드롭 합계'], ['1~40 골드 드롭 합계'], ['1~40 젬 드롭 합계'], ['1~39 최대 기본 타워 소환 수']];
sheet.getRange('A7:B14').format.borders = { preset: 'all', style: 'thin', color: '#D9D9D9' };
sheet.getRange('A7:A14').format.wrapText = true;
sheet.getRange('A7:A14').format.rowHeight = 25;

sheet.getRange('A16:D16').values = [['티어', '필요 기본 타워 수', '최대 제작 가능 수', '남는 기본 타워 수']];
sheet.getRange('A16:D16').format = { fill: '#1F4E78', font: { bold: true, color: '#FFFFFF' }, horizontalAlignment: 'center', verticalAlignment: 'center', wrapText: true, borders: { preset: 'all', style: 'thin', color: '#17365D' } };
sheet.getRange('A17:A21').values = [['브론즈'], ['실버'], ['골드'], ['미스릴'], ['다이아']];
sheet.getRange('B17:B21').values = [[1], [3], [9], [27], [81]];
sheet.getRange('C17:C21').formulas = [['=INT($B$14/B17)'], ['=INT($B$14/B18)'], ['=INT($B$14/B19)'], ['=INT($B$14/B20)'], ['=INT($B$14/B21)']];
sheet.getRange('D17:D21').formulas = [['=$B$14-C17*B17'], ['=$B$14-C18*B18'], ['=$B$14-C19*B19'], ['=$B$14-C20*B20'], ['=$B$14-C21*B21']];
sheet.getRange('A17:D21').format.borders = { preset: 'all', style: 'thin', color: '#D9D9D9' };
sheet.getRange('B17:D21').format.numberFormat = '#,##0';
sheet.getRange('A23:H23').merge();
sheet.getRange('A23').values = [['합성 계산은 기본 타워 3개 → 다음 티어 1개 기준입니다. 1~39웨이브 골드 전부를 소환에 사용한다고 가정하며, 타워 강화·판매·기타 지출은 제외합니다.']];
sheet.getRange('A23:H23').format = { font: { italic: true, color: '#666666' }, wrapText: true, verticalAlignment: 'center' };
sheet.getRange('A23:H23').format.rowHeight = 34;

const waveHeaderRow = 25;
const waveColumns = Array.from({ length: 40 }, (_, i) => colLetter(2 + i));
sheet.getRange(`A${waveHeaderRow}`).values = [['항목']];
sheet.getRange(`B${waveHeaderRow}:AO${waveHeaderRow}`).values = [Array.from({ length: 40 }, (_, i) => i + 1)];
sheet.getRange(`A${waveHeaderRow}:AO${waveHeaderRow}`).format = { fill: '#1F4E78', font: { bold: true, color: '#FFFFFF' }, horizontalAlignment: 'center', verticalAlignment: 'center', borders: { preset: 'all', style: 'thin', color: '#17365D' } };
sheet.getRange(`A${waveHeaderRow}:AO${waveHeaderRow}`).format.rowHeight = 26;

sheet.getRange('A26:A42').values = [['사이클'], ['사이클 배율'], ['웨이브 유형'], ['일반 몬스터 수'], ['최종보스 수'], ['중간보스 수(보상 기준)'], ['총 몬스터 수'], ['일반 몬스터 골드'], ['최종보스 골드'], ['중간보스 골드'], ['총 골드 드롭'], ['최종보스 젬'], ['중간보스 젬'], ['총 젬 드롭'], ['누적 골드 드롭'], ['웨이브 종료 시 최대 기본 타워 설치 수'], ['최대 설치 후 잔여 골드']];
sheet.getRange('A26:A42').format = { fill: '#EAF2F8', font: { bold: true, color: '#17365D' }, wrapText: true };

const matrix = [];
for (const c of waveColumns) {
  matrix.push([
    `=INT((${c}$25-1)/$C$4)+1`,
    `=${c}26`,
    `=IF(MOD(${c}$25,$C$4)=0,"최종보스",IF(MOD(${c}$25-1,$C$4)=$J$4-1,"중간보스 기준","일반"))`,
    `=IF(MOD(${c}$25,$C$4)=0,0,$B$4*${c}26*$A$4)`,
    `=IF(MOD(${c}$25,$C$4)=0,1,0)`,
    `=IF(MOD(${c}$25-1,$C$4)=$J$4-1,1,0)`,
    `=SUM(${c}29:${c}31)`,
    `=${c}29*${c}$25*$D$4`,
    `=${c}30*${c}$25*$G$4*${c}27`,
    `=${c}31*(INT((${c}$25-1)/$C$4)*$C$4+$J$4)*$E$4*${c}27`,
    `=SUM(${c}33:${c}35)`,
    `=${c}30*${c}$25*$H$4*${c}27`,
    `=${c}31*(INT((${c}$25-1)/$C$4)*$C$4+$J$4)*$F$4*${c}27`,
    `=SUM(${c}37:${c}38)`,
    `=SUM($B$36:${c}36)`,
    `=INT((-(2*$K$4-$L$4)+SQRT((2*$K$4-$L$4)^2+8*$L$4*${c}40))/(2*$L$4))`,
    `=${c}40-${c}41*(2*$K$4+(${c}41-1)*$L$4)/2`,
  ]);
}
const transposed = Array.from({ length: 17 }, (_, row) => matrix.map((col) => col[row]));
sheet.getRange('B26:AO42').formulas = transposed;
sheet.getRange('A26:AO42').format.borders = { preset: 'all', style: 'thin', color: '#D9E2F3' };
sheet.getRange('B26:AO42').format.horizontalAlignment = 'right';
sheet.getRange('B26:AO42').format.numberFormat = '#,##0';
sheet.getRange('B28:AO28').format.horizontalAlignment = 'center';

for (const c of waveColumns) {
  const wave = waveColumns.indexOf(c) + 1;
  if (wave % 10 === 0) sheet.getRange(`${c}25:${c}42`).format.fill = '#FFF2CC';
  if ((wave - 5) % 10 === 0) sheet.getRange(`${c}25:${c}42`).format.fill = '#FCE4D6';
}

sheet.getRange('B7:B14').formulas = [
  ['=SUM(B29:AN29)'], ['=SUM(B30:AN30)'], ['=SUM(B31:AN31)'], ['=SUM(B36:AN36)'],
  ['=SUM(B39:AN39)'], ['=SUM(B36:AO36)'], ['=SUM(B39:AO39)'],
  ['=INT((-(2*$K$4-$L$4)+SQRT((2*$K$4-$L$4)^2+8*$L$4*$B$10))/(2*$L$4))'],
];
sheet.getRange('B7:B14').format.numberFormat = '#,##0';

for (const [col, width] of Object.entries({ A: 24, B: 10, C: 10, D: 10, E: 10, F: 10, G: 10, H: 10, I: 10, J: 10, K: 10, L: 10 })) sheet.getRange(`${col}:${col}`).format.columnWidth = width;
for (const c of waveColumns) sheet.getRange(`${c}:${c}`).format.columnWidth = 9;
sheet.freezePanes.freezeRows(waveHeaderRow);

await workbook.recalculate();
console.log('SUMMARY_VALUES', JSON.stringify(sheet.getRange('A6:B14').values));
console.log('TIER_VALUES', JSON.stringify(sheet.getRange('A16:D21').values));
console.log('TIER_FORMULAS', JSON.stringify(sheet.getRange('A16:D21').formulas));
const errorScan = await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A', options: { useRegex: true, maxResults: 100 }, summary: 'formula error scan' });
console.log('ERROR_SCAN', errorScan.ndjson);

await fs.mkdir(outputDir, { recursive: true });
const preview = await workbook.render({ sheetName: 'Sheet1', autoCrop: 'all', scale: 1, format: 'png' });
await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
await fs.copyFile(outputPath, sourcePath);
console.log(`EXPORTED:${outputPath}`);
console.log(`UPDATED_SOURCE:${sourcePath}`);
