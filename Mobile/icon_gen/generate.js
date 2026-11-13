const sharp = require('sharp');
const path = require('path');

const iconDir = path.join(__dirname, '../BilliardIQ.Mobile/Resources/AppIcon');
const outDir  = path.join(__dirname, '../BilliardIQ.Mobile/Resources/AppIcon');

// Combined SVG: background + foreground merged into one 512x512 image
const combinedSvg = `<?xml version="1.0" encoding="UTF-8"?>
<svg width="512" height="512" viewBox="0 0 512 512" xmlns="http://www.w3.org/2000/svg">
  <!-- Background: dark with billiard table -->
  <rect width="512" height="512" fill="#0F1923"/>
  <rect x="45" y="45" width="422" height="422" rx="36" fill="#1A2E1A"/>
  <rect x="45" y="45" width="422" height="422" rx="36" fill="none" stroke="#4A3728" stroke-width="27"/>
  <rect x="58" y="58" width="396" height="396" rx="27" fill="none" stroke="#6B5040" stroke-width="5"/>

  <!-- Foreground: white cue ball + yellow + red balls -->
  <!-- White cue ball -->
  <defs>
    <radialGradient id="wb" cx="38%" cy="32%" r="60%">
      <stop offset="0%" stop-color="white"/>
      <stop offset="70%" stop-color="#e8e8e8"/>
      <stop offset="100%" stop-color="#c0c0c0"/>
    </radialGradient>
    <radialGradient id="yb" cx="38%" cy="32%" r="60%">
      <stop offset="0%" stop-color="#FFE566"/>
      <stop offset="60%" stop-color="#F5C400"/>
      <stop offset="100%" stop-color="#C89000"/>
    </radialGradient>
    <radialGradient id="rb" cx="38%" cy="32%" r="60%">
      <stop offset="0%" stop-color="#FF6B6B"/>
      <stop offset="60%" stop-color="#E02020"/>
      <stop offset="100%" stop-color="#A00000"/>
    </radialGradient>
  </defs>
  <circle cx="256" cy="224" r="101" fill="url(#wb)"/>
  <ellipse cx="224" cy="193" rx="25" ry="16" fill="white" opacity="0.8"/>
  <!-- Yellow ball -->
  <circle cx="166" cy="357" r="70" fill="url(#yb)"/>
  <ellipse cx="148" cy="336" rx="16" ry="10" fill="white" opacity="0.6"/>
  <!-- Red ball -->
  <circle cx="346" cy="357" r="70" fill="url(#rb)"/>
  <ellipse cx="328" cy="336" rx="16" ry="10" fill="white" opacity="0.6"/>
</svg>`;

sharp(Buffer.from(combinedSvg))
  .resize(512, 512)
  .png()
  .toFile(path.join(outDir, 'store_icon_512.png'))
  .then(info => console.log('✓ store_icon_512.png created:', info))
  .catch(err => console.error('Error:', err));
