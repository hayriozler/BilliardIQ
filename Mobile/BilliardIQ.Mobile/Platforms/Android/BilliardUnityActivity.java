package com.billiardiq.mobile;

import android.content.Intent;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

/**
 * Hosts the Unity game within its own task (launchMode="singleTask").
 *
 * The key problem with the stock UnityPlayerActivity:
 *   calling finish() triggers Unity's onDestroy() which calls System.exit(0),
 *   killing the entire MAUI process — the user sees the app close.
 *
 * Fix:
 *   Override finish() and onBackPressed() to call returnToMaui() instead.
 *   returnToMaui() explicitly brings the MAUI launcher activity to the
 *   foreground (FLAG_ACTIVITY_REORDER_TO_FRONT), then moves the Unity task
 *   to the back. moveTaskToBack(true) alone is not enough when Android has
 *   no previous foreground task to restore automatically.
 */
public class BilliardUnityActivity extends UnityPlayerActivity {

    /**
     * Intercept every finish() call (from Unity C# via JNI or from Android).
     * Do NOT call super.finish() — that triggers onDestroy() → System.exit(0).
     */
    @Override
    public void finish() {
        returnToMaui();
    }

    /**
     * Hardware / gesture back button — bring MAUI to front instead of quitting.
     */
    @Override
    public void onBackPressed() {
        returnToMaui();
    }

    /**
     * Called when the user launches a new game while Unity is already
     * backgrounded (task reuse via FLAG_ACTIVITY_NEW_TASK + singleTask).
     * Forwards the fresh game-data JSON to Unity's MauiBridge.StartGame().
     */
    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        String gameData = intent.getStringExtra("gameData");
        if (gameData != null && !gameData.isEmpty()) {
            UnityPlayer.UnitySendMessage("MauiBridge", "StartGame", gameData);
        }
    }

    /**
     * Explicitly brings the MAUI launcher activity to the foreground, then
     * moves the Unity task to the back so Unity stays alive for reuse.
     *
     * moveTaskToBack(true) alone does not reliably restore the MAUI task when
     * Android has nothing to surface automatically (e.g. after a cold start or
     * when the MAUI task was trimmed). Sending the launcher intent with
     * FLAG_ACTIVITY_REORDER_TO_FRONT guarantees MAUI comes to front.
     */
    private void returnToMaui() {
        Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        if (intent != null) {
            intent.addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT
                          | Intent.FLAG_ACTIVITY_SINGLE_TOP);
            startActivity(intent);
        }
        moveTaskToBack(true);
    }
}
