using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityRonin.Core.Combat;
using UnityRonin.Runtime.Combat;
using UnityRonin.Runtime.Input;
using UnityRonin.Runtime.Player;

namespace UnityRonin.Runtime.Game
{
    /// <summary>
    /// Drives the Samurai through <see cref="VirtualJoystick"/>'s and <see cref="SwipeInputReader"/>'s
    /// real IPointerDownHandler/IDragHandler methods (the same ones a touchscreen calls), used by
    /// <see cref="RoninAutopilot"/> (the "-demo" tour: verified self-play, PASS/FAIL, quits).
    /// </summary>
    internal static class SelfPlayInput
    {
        private const float DragRadiusPixels = 140f;

        public static IEnumerator WalkToward(VirtualJoystick joystick, SamuraiLocomotion locomotion,
            Transform cameraYawSource, Transform player, Transform target, DuelPresenter duelPresenter,
            float timeoutSeconds, string logTag)
        {
            float elapsed = 0f;
            float nextLog = 0f;
            Vector2 origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.25f);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = origin };
            joystick.OnPointerDown(pointer);

            while (elapsed < timeoutSeconds && !duelPresenter.IsActive)
            {
                Vector3 toTarget = target.position - player.position;
                toTarget.y = 0f;

                if (elapsed >= nextLog)
                {
                    Debug.Log($"[UnityRonin] {logTag} walk t={elapsed:F1} playerPos={player.position} dist={toTarget.magnitude:F2} stick={joystick.Current.X:F2},{joystick.Current.Y:F2} horiz={locomotion.DebugLastHorizontal} vel={locomotion.DebugControllerVelocity} grounded={locomotion.DebugIsGrounded} lastDt={locomotion.DebugLastDeltaTime:F4} frames={locomotion.DebugFrameCount} collide={locomotion.DebugLastCollisionFlags} fps={1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0}");
                    nextLog += 2f;
                }

                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Vector3 worldDir = toTarget.normalized;
                    float yaw = cameraYawSource.eulerAngles.y * Mathf.Deg2Rad;
                    // Inverse of CameraRelativeMotion.ToWorldDirection: rotate the desired world heading
                    // back into stick space by the camera's yaw.
                    float stickX = worldDir.x * Mathf.Cos(yaw) - worldDir.z * Mathf.Sin(yaw);
                    float stickY = worldDir.x * Mathf.Sin(yaw) + worldDir.z * Mathf.Cos(yaw);
                    pointer.position = origin + new Vector2(stickX, stickY) * DragRadiusPixels;
                    joystick.OnDrag(pointer);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            joystick.OnPointerUp(pointer);
        }

        public static IEnumerator FightDuel(DuelPresenter duelPresenter, SwipeInputReader swipeReader,
            bool alwaysLose, Action<int> onRanTooLong = null)
        {
            int beats = 0;
            while (duelPresenter.IsActive && beats < 100)
            {
                var phase = duelPresenter.PeekCurrentPhase;
                var beat = duelPresenter.PeekCurrentBeat;
                yield return new WaitForSeconds(0.1f); // let the telegraph/riposte prompt show before answering it
                if (!duelPresenter.IsActive) break;

                // A riposte window is deliberately declined (no swipe, safe timeout): it only needs to prove
                // the slow-time wiring fires correctly, not gamble this pass's deterministic win/loss outcome
                // on its real risk/reward (the Core tests already cover every branch of that directly).
                if (phase != DuelPhase.PlayerRiposte && beat.HasValue)
                {
                    var correct = CorrectResponse(beat.Value);
                    var symbol = alwaysLose ? Perpendicular(correct) : correct;
                    yield return Swipe(swipeReader, symbol);
                }
                beats++;
                yield return null;
            }

            if (beats >= 100) onRanTooLong?.Invoke(beats);
        }

        // Mirrors DuelDirector's own scoring: a cutting Attack must be answered with the perpendicular
        // shape (a cross-block), a Thrust attack must be met with a Thrust of its own, and a Guard must be
        // answered by matching the open shape, cut or thrust alike.
        private static GestureSymbol CorrectResponse(DuelBeat beat) =>
            beat.EnemyAction == DuelAction.Attack && beat.Symbol != GestureSymbol.Thrust
                ? Perpendicular(beat.Symbol)
                : beat.Symbol;

        // A general "give me any different symbol" helper — used both for the perpendicular cross-block
        // and (via CorrectResponse's callers) to pick a deliberately wrong answer.
        private static GestureSymbol Perpendicular(GestureSymbol symbol) => symbol switch
        {
            GestureSymbol.Horizontal => GestureSymbol.Vertical,
            GestureSymbol.Vertical => GestureSymbol.Horizontal,
            GestureSymbol.Rising => GestureSymbol.Falling,
            GestureSymbol.Falling => GestureSymbol.Rising,
            GestureSymbol.Thrust => GestureSymbol.Horizontal,
            _ => GestureSymbol.Horizontal
        };

        public static IEnumerator Swipe(SwipeInputReader swipeReader, GestureSymbol symbol)
        {
            Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 delta = symbol switch
            {
                GestureSymbol.Horizontal => new Vector2(200f, 0f),
                GestureSymbol.Vertical => new Vector2(0f, 200f),
                GestureSymbol.Rising => new Vector2(200f, 200f),
                GestureSymbol.Falling => new Vector2(200f, -200f),
                GestureSymbol.Thrust => Vector2.zero,
                _ => Vector2.zero
            };

            var pointer = new PointerEventData(EventSystem.current) { pointerId = 1, position = start };
            swipeReader.OnPointerDown(pointer);
            pointer.position = start + delta;
            swipeReader.OnPointerUp(pointer);
            yield return null;
        }
    }
}
