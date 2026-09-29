export function requestFullscreen() {
    const el = document.documentElement;
    if (!document.fullscreenElement && el.requestFullscreen) {
        el.requestFullscreen().catch(() => {});
    }
}

export function isFullscreen() {
    return !!document.fullscreenElement;
}
