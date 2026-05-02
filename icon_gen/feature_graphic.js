const sharp = require('sharp');
const path  = require('path');
const out   = path.join(__dirname, '../BilliardIQ.Mobile/Resources/AppIcon/feature_graphic_1024x500.png');

const svg = `<?xml version="1.0" encoding="UTF-8"?>
<svg width="1024" height="500" viewBox="0 0 1024 500" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <!-- Background gradient -->
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%"   stop-color="#0A1520"/>
      <stop offset="100%" stop-color="#0F2318"/>
    </linearGradient>

    <!-- Table felt -->
    <radialGradient id="felt" cx="50%" cy="50%" r="70%">
      <stop offset="0%"   stop-color="#1E3B1E"/>
      <stop offset="100%" stop-color="#122012"/>
    </radialGradient>

    <!-- Ball gradients -->
    <radialGradient id="wb" cx="35%" cy="30%" r="65%">
      <stop offset="0%"   stop-color="#ffffff"/>
      <stop offset="65%"  stop-color="#e0e0e0"/>
      <stop offset="100%" stop-color="#b0b0b0"/>
    </radialGradient>
    <radialGradient id="yb" cx="35%" cy="30%" r="65%">
      <stop offset="0%"   stop-color="#FFE566"/>
      <stop offset="60%"  stop-color="#F5C400"/>
      <stop offset="100%" stop-color="#B08800"/>
    </radialGradient>
    <radialGradient id="rb" cx="35%" cy="30%" r="65%">
      <stop offset="0%"   stop-color="#FF7070"/>
      <stop offset="60%"  stop-color="#E02020"/>
      <stop offset="100%" stop-color="#900000"/>
    </radialGradient>

    <!-- Glow -->
    <filter id="glow" x="-30%" y="-30%" width="160%" height="160%">
      <feGaussianBlur stdDeviation="12" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>
    <filter id="softglow" x="-20%" y="-20%" width="140%" height="140%">
      <feGaussianBlur stdDeviation="6" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>

    <!-- Text shadow -->
    <filter id="tshadow" x="-5%" y="-5%" width="110%" height="130%">
      <feDropShadow dx="0" dy="3" stdDeviation="6" flood-color="#000000" flood-opacity="0.6"/>
    </filter>
  </defs>

  <!-- Background -->
  <rect width="1024" height="500" fill="url(#bg)"/>

  <!-- Subtle table felt area (right side) -->
  <ellipse cx="780" cy="250" rx="360" ry="260" fill="url(#felt)" opacity="0.5"/>

  <!-- Table border frame -->
  <rect x="490" y="40" width="490" height="420" rx="28" fill="none" stroke="#4A3728" stroke-width="14" opacity="0.7"/>
  <rect x="503" y="53" width="464" height="394" rx="22" fill="none" stroke="#6B5040" stroke-width="3" opacity="0.5"/>

  <!-- Decorative grid lines on table -->
  <line x1="735" y1="53" x2="735" y2="447" stroke="#1E3B1E" stroke-width="1" opacity="0.5"/>
  <line x1="503" y1="250" x2="967" y2="250" stroke="#1E3B1E" stroke-width="1" opacity="0.5"/>

  <!-- Pocket circles -->
  <circle cx="503" cy="53"  r="14" fill="#0A1520" stroke="#4A3728" stroke-width="3" opacity="0.8"/>
  <circle cx="967" cy="53"  r="14" fill="#0A1520" stroke="#4A3728" stroke-width="3" opacity="0.8"/>
  <circle cx="503" cy="447" r="14" fill="#0A1520" stroke="#4A3728" stroke-width="3" opacity="0.8"/>
  <circle cx="967" cy="447" r="14" fill="#0A1520" stroke="#4A3728" stroke-width="3" opacity="0.8"/>
  <circle cx="503" cy="250" r="10" fill="#0A1520" stroke="#4A3728" stroke-width="3" opacity="0.8"/>
  <circle cx="967" cy="250" r="10" fill="#0A1520" stroke="#4A3728" stroke-width="3" opacity="0.8"/>

  <!-- White ball (cue ball) -->
  <circle cx="720" cy="200" r="72" fill="url(#wb)" filter="url(#softglow)"/>
  <ellipse cx="698" cy="178" rx="18" ry="11" fill="white" opacity="0.7"/>

  <!-- Yellow ball -->
  <circle cx="620" cy="330" r="56" fill="url(#yb)" filter="url(#softglow)"/>
  <ellipse cx="604" cy="314" rx="13" ry="8" fill="white" opacity="0.55"/>

  <!-- Red ball -->
  <circle cx="840" cy="330" r="56" fill="url(#rb)" filter="url(#softglow)"/>
  <ellipse cx="824" cy="314" rx="13" ry="8" fill="white" opacity="0.55"/>

  <!-- Left section: app name + tagline -->
  <!-- Accent line -->
  <rect x="60" y="148" width="5" height="90" rx="2" fill="#4CAF50"/>

  <!-- App name -->
  <text x="82" y="210"
        font-family="Arial Black, Arial, sans-serif"
        font-size="72"
        font-weight="900"
        fill="white"
        filter="url(#tshadow)"
        letter-spacing="-1">Billiard</text>
  <text x="82" y="280"
        font-family="Arial Black, Arial, sans-serif"
        font-size="72"
        font-weight="900"
        fill="#4CAF50"
        filter="url(#tshadow)"
        letter-spacing="-1">IQ</text>

  <!-- Tagline -->
  <text x="82" y="330"
        font-family="Arial, sans-serif"
        font-size="22"
        fill="#90CAF9"
        letter-spacing="1"
        font-weight="400">Smart 3-Cushion Billiards Tracker</text>

  <!-- Feature pills -->
  <rect x="82"  y="360" width="130" height="34" rx="17" fill="#1E3A5F" opacity="0.9"/>
  <text x="147" y="382" text-anchor="middle" font-family="Arial,sans-serif" font-size="14" fill="#90CAF9">OCR Scoring</text>

  <rect x="224" y="360" width="140" height="34" rx="17" fill="#1E3A5F" opacity="0.9"/>
  <text x="294" y="382" text-anchor="middle" font-family="Arial,sans-serif" font-size="14" fill="#90CAF9">Career Stats</text>

  <rect x="376" y="360" width="110" height="34" rx="17" fill="#1E3A5F" opacity="0.9"/>
  <text x="431" y="382" text-anchor="middle" font-family="Arial,sans-serif" font-size="14" fill="#90CAF9">Offline</text>
</svg>`;

sharp(Buffer.from(svg))
  .resize(1024, 500)
  .png()
  .toFile(out)
  .then(i => console.log('Feature graphic created:', out, JSON.stringify(i)))
  .catch(e => console.error(e));
