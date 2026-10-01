export function requestFullscreen() {
    const el = document.documentElement;
    if (!document.fullscreenElement && el.requestFullscreen) {
        el.requestFullscreen().catch(() => {});
    }
}

export function isFullscreen() {
    return !!document.fullscreenElement;
}

let fireworksState = null;

const fireworkPalettes = {
    1: ["#ffffff", "#f2f2f2", "#ffd54a", "#c9202c"],
    2: ["#f5c32c", "#ffe27a", "#ffffff", "#c9202c"]
};

export function stopFireworks() {
    if (!fireworksState) {
        return;
    }

    cancelAnimationFrame(fireworksState.frame);
    clearTimeout(fireworksState.timer);
    fireworksState.canvas.remove();
    fireworksState = null;
}

export function startFireworks(slot) {
    stopFireworks();

    const panel = document.querySelector(`.player-panel[data-slot="${slot}"]`);
    const area = panel ? panel.getBoundingClientRect() : { left: 0, top: 0, width: window.innerWidth, height: window.innerHeight };
    const palette = fireworkPalettes[slot] || fireworkPalettes[1];

    const canvas = document.createElement("canvas");
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    canvas.style.cssText = "position:fixed;inset:0;width:100%;height:100%;pointer-events:none;z-index:9999";
    document.body.appendChild(canvas);
    const ctx = canvas.getContext("2d");

    const rockets = [];
    const sparks = [];
    let lastLaunch = 0;
    const startedAt = performance.now();
    const launchFor = 7000;

    const launch = () => {
        rockets.push({
            x: area.left + area.width * (0.15 + Math.random() * 0.7),
            y: area.top + area.height,
            targetY: area.top + area.height * (0.15 + Math.random() * 0.45),
            speed: 9 + Math.random() * 4,
            color: palette[Math.floor(Math.random() * palette.length)]
        });
    };

    const explode = (rocket) => {
        const count = 70 + Math.floor(Math.random() * 40);
        for (let i = 0; i < count; i++) {
            const angle = (Math.PI * 2 * i) / count;
            const speed = 1.5 + Math.random() * 4.5;
            sparks.push({
                x: rocket.x,
                y: rocket.y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                life: 1,
                decay: 0.012 + Math.random() * 0.012,
                color: Math.random() < 0.7 ? rocket.color : palette[Math.floor(Math.random() * palette.length)]
            });
        }
    };

    const frame = (now) => {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        if (now - startedAt < launchFor && now - lastLaunch > 320) {
            launch();
            lastLaunch = now;
        }

        for (let i = rockets.length - 1; i >= 0; i--) {
            const rocket = rockets[i];
            rocket.y -= rocket.speed;
            ctx.fillStyle = rocket.color;
            ctx.beginPath();
            ctx.arc(rocket.x, rocket.y, 3, 0, Math.PI * 2);
            ctx.fill();
            if (rocket.y <= rocket.targetY) {
                explode(rocket);
                rockets.splice(i, 1);
            }
        }

        for (let i = sparks.length - 1; i >= 0; i--) {
            const spark = sparks[i];
            spark.x += spark.vx;
            spark.y += spark.vy;
            spark.vy += 0.045;
            spark.vx *= 0.99;
            spark.life -= spark.decay;
            if (spark.life <= 0) {
                sparks.splice(i, 1);
                continue;
            }

            ctx.globalAlpha = Math.max(spark.life, 0);
            ctx.fillStyle = spark.color;
            ctx.beginPath();
            ctx.arc(spark.x, spark.y, 2.4, 0, Math.PI * 2);
            ctx.fill();
        }

        ctx.globalAlpha = 1;

        if (now - startedAt > launchFor && rockets.length === 0 && sparks.length === 0) {
            stopFireworks();
            return;
        }

        fireworksState.frame = requestAnimationFrame(frame);
    };

    fireworksState = { canvas, frame: 0, timer: setTimeout(stopFireworks, 15000) };
    fireworksState.frame = requestAnimationFrame(frame);
}
