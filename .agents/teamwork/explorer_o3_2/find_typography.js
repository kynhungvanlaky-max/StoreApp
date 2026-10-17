const fs = require('fs');
const path = require('path');

function walk(dir) {
    let results = [];
    const list = fs.readdirSync(dir);
    list.forEach(file => {
        const fullPath = path.join(dir, file);
        const stat = fs.statSync(fullPath);
        if (stat && stat.isDirectory()) {
            if (!file.includes('bin') && !file.includes('obj') && !file.includes('node_modules')) {
                results = results.concat(walk(fullPath));
            }
        } else if (file.endsWith('.css') || file.endsWith('.razor') || file.endsWith('.cshtml')) {
            results.push(fullPath);
        }
    });
    return results;
}

const files = walk('c:/Users/kynhu/Documents/DATN/datn/src');
console.log('Total files found:', files.length);

const results = [];

files.forEach(f => {
    const content = fs.readFileSync(f, 'utf8');
    
    // Check for letter-spacing in css
    if (f.endsWith('.css')) {
        const matches = [...content.matchAll(/([^{};]+)\{([^}]+)\}/g)];
        for (const m of matches) {
            const selector = m[1].trim();
            const body = m[2];
            if (body.includes('letter-spacing') || selector.includes('h1') || selector.includes('h2') || selector.includes('h3')) {
                const lsMatch = body.match(/letter-spacing\s*:\s*([^;]+)/);
                const lhMatch = body.match(/line-height\s*:\s*([^;]+)/);
                if (lsMatch || lhMatch || selector.match(/\bh[1-6]\b/)) {
                    results.push({
                        file: path.relative('c:/Users/kynhu/Documents/DATN/datn', f),
                        selector: selector.length > 80 ? selector.slice(0, 80) + '...' : selector,
                        letterSpacing: lsMatch ? lsMatch[1].trim() : null,
                        lineHeight: lhMatch ? lhMatch[1].trim() : null
                    });
                }
            }
        }
    }

    // Check for tracking- or text-[9px] in razor or cshtml
    if (f.endsWith('.razor') || f.endsWith('.cshtml')) {
        const lines = content.split('\n');
        lines.forEach((line, idx) => {
            if (line.includes('tracking-') || line.includes('text-[9px]') || line.includes('text-[10px]') || line.includes('h1') || line.includes('h2')) {
                // track occurrences
                const tr = line.match(/tracking-[\w\d\[\]\.\-]+/g);
                const t9 = line.match(/text-\[9px\]/g);
                if (tr || t9) {
                    results.push({
                        file: path.relative('c:/Users/kynhu/Documents/DATN/datn', f),
                        line: idx + 1,
                        tracking: tr,
                        text9: t9,
                        snippet: line.trim().slice(0, 120)
                    });
                }
            }
        });
    }
});

fs.writeFileSync('c:/Users/kynhu/Documents/DATN/datn/.agents/teamwork/explorer_o3_2/typography_scan.json', JSON.stringify(results, null, 2));
console.log('Saved', results.length, 'entries to typography_scan.json');
