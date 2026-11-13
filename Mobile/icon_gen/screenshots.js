const sharp = require('sharp');
const path  = require('path');
const outDir = path.join(__dirname, '../BilliardIQ.Mobile/Resources/AppIcon/screenshots');
const fs = require('fs');
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

function save(svg, filename, w, h) {
    return sharp(Buffer.from(svg)).resize(w, h).png()
        .toFile(path.join(outDir, filename))
        .then(i => console.log(`✓ ${filename} (${i.width}x${i.height})`));
}

// ─── SCREEN 1: Home / Stats + Game List ──────────────────────────────────────
function screen1(W, H) {
    const sc = W / 1200; // scale factor
    const fs = n => n * sc;

const games = [
    { opp: "Ahmet Yılmaz",    loc: "Kadıköy Bilardo",  date: "28.04.2026", ps: 50, os: 38, hr: 8 },
    { opp: "Pierre Dupont",   loc: "Amsterdam Club",   date: "25.04.2026", ps: 40, os: 40, hr: 5 },
    { opp: "Mehmet Demir",    loc: "Beşiktaş Spor",    date: "22.04.2026", ps: 35, os: 50, hr: 4 },
    { opp: "Jan van der Berg", loc: "Rotterdam Salonu", date: "20.04.2026", ps: 50, os: 31, hr: 7 },
    { opp: "Ali Kaya",        loc: "Kadıköy Bilardo",  date: "17.04.2026", ps: 50, os: 44, hr: 6 },
    { opp: "Thomas Müller",   loc: "Istanbul Open",    date: "14.04.2026", ps: 28, os: 50, hr: 3 },
];

    let rows = '';
    games.forEach((g, i) => {
        const y = fs(408 + i * 155);
        const cardH = fs(140);
        const textColor = g.ps > g.os ? '#2E7D32' : g.ps < g.os ? '#C62828' : '#888';
        rows += `
  <rect x="${fs(20)}" y="${y}" width="${W - fs(40)}" height="${cardH}" rx="${fs(16)}" fill="white" filter="url(#shadow)"/>
  <rect x="${fs(20)}" y="${y}" width="${fs(6)}" height="${cardH}" rx="${fs(3)}" fill="${textColor}"/>
  <text x="${fs(42)}" y="${y + fs(44)}" font-family="Arial" font-size="${fs(30)}" fill="#111" font-weight="bold">${g.opp}</text>
  <text x="${W - fs(40)}" y="${y + fs(44)}" text-anchor="end" font-family="Arial Black" font-size="${fs(32)}" font-weight="bold">
    <tspan fill="#2E7D32">${g.ps}</tspan><tspan fill="#888"> – </tspan><tspan fill="#C62828">${g.os}</tspan>
  </text>
  <text x="${fs(42)}" y="${y + fs(84)}" font-family="Arial" font-size="${fs(24)}" fill="#888">${g.loc}</text>
  <text x="${fs(42)}" y="${y + fs(118)}" font-family="Arial" font-size="${fs(22)}" fill="#aaa">${g.date}</text>
  <text x="${W - fs(40)}" y="${y + fs(118)}" text-anchor="end" font-family="Arial" font-size="${fs(22)}" fill="#aaa">Serie: ${g.hr}</text>`;
    });

    const bottomY = H - fs(120);
    return `<?xml version="1.0" encoding="UTF-8"?>
<svg width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="statsBg" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0%" stop-color="#1A3050"/>
      <stop offset="100%" stop-color="#1E3A5F"/>
    </linearGradient>
    <filter id="shadow" x="-3%" y="-3%" width="106%" height="112%">
      <feDropShadow dx="0" dy="2" stdDeviation="4" flood-color="#00000018"/>
    </filter>
  </defs>
  <rect width="${W}" height="${H}" fill="#F0F0F0"/>
  <rect width="${W}" height="${fs(56)}" fill="#0F1923"/>
  <text x="${fs(48)}" y="${fs(38)}" font-family="Arial" font-size="${fs(26)}" fill="white" font-weight="bold">9:41</text>
  <text x="${W - fs(48)}" y="${fs(38)}" font-family="Arial" font-size="${fs(22)}" fill="white" text-anchor="end">● ● 100%</text>
  <rect x="0" y="${fs(56)}" width="${W}" height="${fs(80)}" fill="#0F1923"/>
  <text x="${fs(48)}" y="${fs(112)}" font-family="Arial Black, Arial" font-size="${fs(38)}" fill="white" font-weight="900">BilliardIQ</text>
  <rect x="${fs(20)}" y="${fs(156)}" width="${W - fs(40)}" height="${fs(200)}" rx="${fs(20)}" fill="url(#statsBg)"/>
  <text x="${W*0.12}" y="${fs(208)}" text-anchor="middle" font-family="Arial" font-size="${fs(24)}" fill="#90CAF9">Matches</text>
  <text x="${W*0.37}" y="${fs(208)}" text-anchor="middle" font-family="Arial" font-size="${fs(24)}" fill="#90CAF9">Average</text>
  <text x="${W*0.63}" y="${fs(208)}" text-anchor="middle" font-family="Arial" font-size="${fs(24)}" fill="#90CAF9">Best Avg</text>
  <text x="${W*0.88}" y="${fs(208)}" text-anchor="middle" font-family="Arial" font-size="${fs(24)}" fill="#90CAF9">High Run</text>
  <text x="${W*0.12}" y="${fs(275)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="white" font-weight="900">24</text>
  <text x="${W*0.37}" y="${fs(275)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="white" font-weight="900">1.423</text>
  <text x="${W*0.63}" y="${fs(275)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="#FFD54F" font-weight="900">1.857</text>
  <text x="${W*0.88}" y="${fs(275)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="white" font-weight="900">8</text>
  <line x1="${fs(30)}" y1="${fs(300)}" x2="${W-fs(30)}" y2="${fs(300)}" stroke="#2E4A6F" stroke-width="1.5"/>
  <text x="${fs(48)}" y="${fs(336)}" font-family="Arial" font-size="${fs(24)}" fill="#90CAF9">Last Match</text>
  <text x="${W-fs(48)}" y="${fs(336)}" text-anchor="end" font-family="Arial Black" font-size="${fs(28)}" fill="#81C784" font-weight="bold">Win</text>
  ${rows}
  <!-- Bottom bar -->
  <rect x="0" y="${bottomY}" width="${W}" height="${fs(120)}" fill="white"/>
  <line x1="0" y1="${bottomY}" x2="${W}" y2="${bottomY}" stroke="#E0E0E0" stroke-width="1"/>
  <!-- Camera button -->
  <circle cx="${W*0.22}" cy="${bottomY + fs(60)}" r="${fs(44)}" fill="#512BD4"/>
  <text x="${W*0.22}" y="${bottomY + fs(72)}" text-anchor="middle" font-family="Arial" font-size="${fs(40)}" fill="white">⬚</text>
  <!-- Plus button -->
  <circle cx="${W*0.78}" cy="${bottomY + fs(60)}" r="${fs(44)}" fill="#512BD4"/>
  <text x="${W*0.78}" y="${bottomY + fs(74)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="white" font-weight="900">+</text>
</svg>`;
}

// ─── SCREEN 2: New Game Form ─────────────────────────────────────────────────
function screen2(W, H) {
    const sc = W / 1200;
    const fs = n => n * sc;
    const field = (label, value, yTop, optional = false) => `
  <text x="${fs(48)}" y="${yTop + fs(34)}" font-family="Arial" font-size="${fs(26)}" fill="#888">${label}${optional ? ' (optional)' : ''}</text>
  <rect x="${fs(32)}" y="${yTop + fs(46)}" width="${W - fs(64)}" height="${fs(84)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${fs(60)}" y="${yTop + fs(100)}" font-family="Arial" font-size="${fs(32)}" fill="${value ? '#111' : '#BBB'}">${value || label}</text>`;

    return `<?xml version="1.0" encoding="UTF-8"?>
<svg width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <rect width="${W}" height="${H}" fill="#F2F2F2"/>
  <!-- Status bar -->
  <rect width="${W}" height="${fs(56)}" fill="#0F1923"/>
  <text x="${fs(48)}" y="${fs(38)}" font-family="Arial" font-size="${fs(26)}" fill="white">9:41</text>
  <text x="${W-fs(48)}" y="${fs(38)}" font-family="Arial" font-size="${fs(22)}" fill="white" text-anchor="end">● ● 100%</text>
  <!-- Title bar -->
  <rect x="0" y="${fs(56)}" width="${W}" height="${fs(88)}" fill="#0F1923"/>
  <text x="${fs(48)}" y="${fs(115)}" font-family="Arial" font-size="${fs(22)}" fill="#aaa">&#8592;</text>
  <text x="${W/2}" y="${fs(115)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(36)}" fill="white" font-weight="bold">Add New Game</text>

  <!-- Form content -->
  ${field('Opponent Name', 'Ahmet Yılmaz', fs(168))}
  ${field('Location', 'Kadıköy Bilardo', fs(318))}

  <!-- Date -->
  <text x="${fs(48)}" y="${fs(468 + 34)}" font-family="Arial" font-size="${fs(26)}" fill="#888">Date</text>
  <rect x="${fs(32)}" y="${fs(468 + 46)}" width="${W - fs(64)}" height="${fs(84)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${fs(60)}" y="${fs(468 + 100)}" font-family="Arial" font-size="${fs(32)}" fill="#111">28.04.2026</text>

  <!-- Score section -->
  <text x="${fs(48)}" y="${fs(668)}" font-family="Arial Black" font-size="${fs(30)}" fill="#333" font-weight="bold">Score</text>
  <rect x="${fs(32)}" y="${fs(690)}" width="${(W - fs(64)) * 0.44}" height="${fs(120)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${fs(32) + (W-fs(64))*0.22}" y="${fs(730)}" text-anchor="middle" font-family="Arial" font-size="${fs(24)}" fill="#888">My Score</text>
  <text x="${fs(32) + (W-fs(64))*0.22}" y="${fs(790)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="#2E7D32" font-weight="900">50</text>
  <text x="${W/2}" y="${fs(790)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="#888">–</text>
  <rect x="${fs(32) + (W-fs(64))*0.56}" y="${fs(690)}" width="${(W-fs(64))*0.44}" height="${fs(120)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${fs(32) + (W-fs(64))*0.78}" y="${fs(730)}" text-anchor="middle" font-family="Arial" font-size="${fs(24)}" fill="#888">Opponent Score</text>
  <text x="${fs(32) + (W-fs(64))*0.78}" y="${fs(790)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(52)}" fill="#C62828" font-weight="900">38</text>

  <!-- Innings + HighRun -->
  <text x="${W*0.28}" y="${fs(860)}" text-anchor="middle" font-family="Arial" font-size="${fs(26)}" fill="#888">Highest Run</text>
  <rect x="${fs(32)}" y="${fs(874)}" width="${(W-fs(64))*0.44}" height="${fs(80)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${W*0.28}" y="${fs(928)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(38)}" fill="#111">8</text>
  <text x="${W*0.78}" y="${fs(860)}" text-anchor="middle" font-family="Arial" font-size="${fs(26)}" fill="#888">Innings</text>
  <rect x="${fs(32) + (W-fs(64))*0.56}" y="${fs(874)}" width="${(W-fs(64))*0.44}" height="${fs(80)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${W*0.78}" y="${fs(928)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(38)}" fill="#111">35</text>

  <!-- Ball selection -->
  <text x="${fs(48)}" y="${fs(1010)}" font-family="Arial Black" font-size="${fs(30)}" fill="#333" font-weight="bold">Select Ball</text>
  <!-- White ball -->
  <rect x="${fs(48)}" y="${fs(1026)}" width="${fs(160)}" height="${fs(140)}" rx="${fs(16)}" fill="white" stroke="#4CAF50" stroke-width="3"/>
  <circle cx="${fs(128)}" cy="${fs(1096)}" r="${fs(48)}" fill="#EFEFEF"/>
  <ellipse cx="${fs(112)}" cy="${fs(1080)}" rx="${fs(12)}" ry="${fs(7)}" fill="white" opacity="0.7"/>
  <!-- Yellow ball -->
  <rect x="${fs(240)}" y="${fs(1026)}" width="${fs(160)}" height="${fs(140)}" rx="${fs(16)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <circle cx="${fs(320)}" cy="${fs(1096)}" r="${fs(48)}" fill="#F5C400"/>
  <ellipse cx="${fs(304)}" cy="${fs(1080)}" rx="${fs(12)}" ry="${fs(7)}" fill="white" opacity="0.55"/>

  <!-- Notes -->
  <text x="${fs(48)}" y="${fs(1228)}" font-family="Arial" font-size="${fs(26)}" fill="#888">Notes (optional)</text>
  <rect x="${fs(32)}" y="${fs(1244)}" width="${W-fs(64)}" height="${fs(120)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${fs(60)}" y="${fs(1314)}" font-family="Arial" font-size="${fs(28)}" fill="#bbb">Add a description or notes...</text>

  <!-- Scoreboard Photo section -->
  <text x="${fs(48)}" y="${fs(1432)}" font-family="Arial Black" font-size="${fs(30)}" fill="#333" font-weight="bold">Scoreboard Photo</text>
  <rect x="${fs(32)}" y="${fs(1450)}" width="${fs(110)}" height="${fs(110)}" rx="${fs(55)}" fill="#512BD4"/>
  <text x="${fs(87)}" y="${fs(1516)}" text-anchor="middle" font-family="Arial" font-size="${fs(52)}" fill="white">📷</text>

  <!-- OCR status banner -->
  <rect x="${fs(32)}" y="${fs(1580)}" width="${W-fs(64)}" height="${fs(64)}" rx="${fs(12)}" fill="#1E3A5F"/>
  <text x="${W/2}" y="${fs(1621)}" text-anchor="middle" font-family="Arial" font-size="${fs(26)}" fill="white">✓ My Score: 50  |  Opponent: 38  |  Innings: 35</text>

  <!-- Save button -->
  <rect x="${fs(32)}" y="${H - fs(160)}" width="${W-fs(64)}" height="${fs(100)}" rx="${fs(16)}" fill="#0B5D3B"/>
  <text x="${W/2}" y="${H - fs(98)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(38)}" fill="white" font-weight="bold">✓  Save</text>
</svg>`;
}

// ─── SCREEN 3: Player Profile ─────────────────────────────────────────────────
function screen3(W, H) {
    const sc = W / 1200;
    const fs = n => n * sc;
    const row = (label, value, y) => `
  <text x="${fs(48)}" y="${y + fs(30)}" font-family="Arial" font-size="${fs(24)}" fill="#888">${label}</text>
  <rect x="${fs(32)}" y="${y + fs(40)}" width="${W - fs(64)}" height="${fs(76)}" rx="${fs(12)}" fill="white" stroke="#E0E0E0" stroke-width="1.5"/>
  <text x="${fs(60)}" y="${y + fs(92)}" font-family="Arial" font-size="${fs(30)}" fill="${value.startsWith('Select') ? '#BBB' : '#111'}">${value}</text>`;

    return `<?xml version="1.0" encoding="UTF-8"?>
<svg width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <rect width="${W}" height="${H}" fill="#F2F2F2"/>
  <rect width="${W}" height="${fs(56)}" fill="#0F1923"/>
  <text x="${fs(48)}" y="${fs(38)}" font-family="Arial" font-size="${fs(26)}" fill="white">9:41</text>
  <text x="${W-fs(48)}" y="${fs(38)}" font-family="Arial" font-size="${fs(22)}" fill="white" text-anchor="end">● ● 100%</text>
  <rect x="0" y="${fs(56)}" width="${W}" height="${fs(88)}" fill="#0F1923"/>
  <text x="${W/2}" y="${fs(115)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(36)}" fill="white" font-weight="bold">Player Profile</text>

  <!-- Avatar circle -->
  <circle cx="${W/2}" cy="${fs(260)}" r="${fs(80)}" fill="#1E3A5F"/>
  <text x="${W/2}" y="${fs(282)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(72)}" fill="white">H</text>

  <!-- Player name label -->
  <text x="${W/2}" y="${fs(378)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(40)}" fill="#111" font-weight="bold">Hayri Özler</text>
  <text x="${W/2}" y="${fs(424)}" text-anchor="middle" font-family="Arial" font-size="${fs(28)}" fill="#888">Intermediate · Türkiye / İstanbul</text>

  <!-- Stats summary bar -->
  <rect x="${fs(32)}" y="${fs(458)}" width="${W-fs(64)}" height="${fs(110)}" rx="${fs(16)}" fill="#1E3A5F"/>
  <text x="${W*0.2}"  y="${fs(500)}" text-anchor="middle" font-family="Arial" font-size="${fs(22)}" fill="#90CAF9">Matches</text>
  <text x="${W*0.5}"  y="${fs(500)}" text-anchor="middle" font-family="Arial" font-size="${fs(22)}" fill="#90CAF9">Avg</text>
  <text x="${W*0.8}"  y="${fs(500)}" text-anchor="middle" font-family="Arial" font-size="${fs(22)}" fill="#90CAF9">Best Avg</text>
  <text x="${W*0.2}"  y="${fs(552)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(40)}" fill="white" font-weight="900">24</text>
  <text x="${W*0.5}"  y="${fs(552)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(40)}" fill="white" font-weight="900">1.423</text>
  <text x="${W*0.8}"  y="${fs(552)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(40)}" fill="#FFD54F" font-weight="900">1.857</text>

  <!-- Form fields -->
  ${row('Name',            'Hayri',             fs(594))}
  ${row('Surname',         'Özler',             fs(718))}
  ${row('Country',         'Türkiye',           fs(842))}
  ${row('City',            'İstanbul',          fs(966))}
  ${row('Club (optional)', 'İstanbul BC',       fs(1090))}
  ${row('Email',           'hozler@gmail.com',  fs(1214))}
  ${row('Language',        'Türkçe',            fs(1338))}
  ${row('Level',           'Intermediate',      fs(1462))}

  <!-- Update button -->
  <rect x="${fs(32)}" y="${H - fs(160)}" width="${W-fs(64)}" height="${fs(100)}" rx="${fs(16)}" fill="#0B5D3B"/>
  <text x="${W/2}" y="${H - fs(98)}" text-anchor="middle" font-family="Arial Black" font-size="${fs(38)}" fill="white" font-weight="bold">✎  Update</text>
</svg>`;
}

// ─── Generate all 6 screenshots ──────────────────────────────────────────────
async function run() {
    // 7-inch tablet  (1200 × 1920)
    await save(screen1(1200, 1920), '7inch_01_home.png',    1200, 1920);
    await save(screen2(1200, 1920), '7inch_02_newgame.png', 1200, 1920);
    await save(screen3(1200, 1920), '7inch_03_profile.png', 1200, 1920);

    // 10-inch tablet (1920 × 2560)
    await save(screen1(1920, 2560), '10inch_01_home.png',    1920, 2560);
    await save(screen2(1920, 2560), '10inch_02_newgame.png', 1920, 2560);
    await save(screen3(1920, 2560), '10inch_03_profile.png', 1920, 2560);

    console.log('\nAll screenshots saved to:', path.join(__dirname, '../BilliardIQ.Mobile/Resources/AppIcon/screenshots'));
}
run().catch(console.error);
