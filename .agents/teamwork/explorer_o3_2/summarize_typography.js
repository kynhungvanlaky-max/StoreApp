const fs = require('fs');

const data = JSON.parse(fs.readFileSync('c:/Users/kynhu/Documents/DATN/datn/.agents/teamwork/explorer_o3_2/typography_scan.json', 'utf8'));

console.log('=== 1. CSS RULES WITH LETTER-SPACING OR HEADINGS ===');
const cssEntries = data.filter(d => d.selector);
cssEntries.forEach(e => {
    if (e.letterSpacing || e.lineHeight || e.selector.match(/\bh[1-6]\b/)) {
        console.log(`[${e.file}] ${e.selector}`);
        if (e.letterSpacing) console.log(`   letter-spacing: ${e.letterSpacing}`);
        if (e.lineHeight) console.log(`   line-height: ${e.lineHeight}`);
    }
});

console.log('\n=== 2. TEXT-[9PX] OCCURRENCES ===');
const text9 = data.filter(d => d.text9);
console.log(`Found ${text9.length} occurrences of text-[9px]:`);
text9.forEach(e => {
    console.log(`- ${e.file}:${e.line} -> ${e.snippet}`);
});

console.log('\n=== 3. TRACKING USAGES IN RAZOR/CSHTML ===');
const trackingCounts = {};
const trackingEntries = data.filter(d => d.tracking);
trackingEntries.forEach(e => {
    e.tracking.forEach(t => {
        trackingCounts[t] = (trackingCounts[t] || 0) + 1;
    });
});
console.log('Tracking class frequencies:', trackingCounts);

console.log('\n=== 4. TRACKING-WIDEST OCCURRENCES ===');
const widest = trackingEntries.filter(e => e.tracking.includes('tracking-widest'));
console.log(`Found ${widest.length} occurrences of tracking-widest:`);
widest.forEach(e => {
    console.log(`- ${e.file}:${e.line} -> ${e.snippet}`);
});
