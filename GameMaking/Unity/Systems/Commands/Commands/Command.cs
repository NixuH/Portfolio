using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Commands
{
    /// <summary>
    /// Base class for every unit/building command (Move, Attack, Build, ...).
    /// Handles the shared lifecycle (Start -> [NeedAction] -> Execute -> [Wait] -> Finish/Cancel/Error)
    /// and lets callers hook extra behaviour (VFX, sound, animation) without subclassing,
    /// via the callOnX action queues.
    /// </summary>
    public abstract class Command : IDisposable
    {
        private static readonly Dictionary<TargetType, int> TargetLayers = new()
        {
            { TargetType.Position, LayerMask.GetMask("Ground") },
            { TargetType.Unit, LayerMask.GetMask("GroundUnit", "AirUnit") },
            { TargetType.GroundUnit, LayerMask.GetMask("GroundUnit") },
            { TargetType.AirUnit, LayerMask.GetMask("AirUnit") },
            { TargetType.Building, LayerMask.GetMask("Building") },
        };

        #region Panel / identity data

        public readonly Sprite icon;
        public readonly CallType callType;
        public readonly CommandType commandType;
        public readonly TargetType targetType;

        #endregion

        #region State

        protected readonly GameObject caster;
        protected readonly CommandsData commandData;

        protected CommandState state;
        protected AfterWaitBehaviour afterWaitBehaviour;

        protected Vector3 startPosition, targetPosition;
        protected Quaternion startRotation, targetRotation;

        /// <summary>Current lifecycle state.</summary>
        public CommandState State => state;

        /// <summary>
        /// True once the command has finished / been canceled / errored out unrecoverably.
        /// </summary>
        public bool IsDone { get; private set; }

        #endregion

        #region Hooks

        protected readonly List<Action> onStartCallbacks;
        protected readonly List<Action> onWaitCallbacks;
        protected readonly List<Action> onResumeCallbacks;
        protected readonly List<Action> onCancelCallbacks;
        protected readonly List<Action> onErrorCallbacks;
        protected readonly List<Action> onFinishCallbacks;
        protected readonly List<Action> onCompleteCallbacks;

        #endregion

        private Coroutine _waitCoroutineHandle;
        private bool _disposed;

        protected Command(
            GameObject caster,
            CommandsData commandsData,
            CallType callType,
            TargetType targetType,
            CommandType commandType,
            Sprite icon = null,
            List<Action> onStartCallbacks = null,
            List<Action> onFinishCallbacks = null,
            List<Action> onWaitCallbacks = null,
            List<Action> onResumeCallbacks = null,
            List<Action> onErrorCallbacks = null,
            List<Action> onCancelCallbacks = null,
            List<Action> onCompleteCallbacks = null)
        {
            state = CommandState.None;

            this.caster = caster;
            this.commandData = commandsData;
            this.callType = callType;
            this.targetType = targetType;
            this.commandType = commandType;
            this.icon = icon;

            this.onStartCallbacks = onStartCallbacks ?? new List<Action>();
            this.onWaitCallbacks = onWaitCallbacks ?? new List<Action>();
            this.onResumeCallbacks = onResumeCallbacks ?? new List<Action>();
            this.onFinishCallbacks = onFinishCallbacks ?? new List<Action>();
            this.onErrorCallbacks = onErrorCallbacks ?? new List<Action>();
            this.onCancelCallbacks = onCancelCallbacks ?? new List<Action>();
            this.onCompleteCallbacks = onCompleteCallbacks ?? new List<Action>();
        }

        public int TargetLayer => TargetLayers.TryGetValue(targetType, out var mask) ? mask : 0;

        #region Lifecycle

        /// <summary>
        /// Initializes the command. Normally called internally the first time the caster ticks it,
        /// but can be called manually to start immediately. Doing so bypasses the caster's own
        /// bookkeeping, so only do it if you know the caster won't also try to start it.
        /// </summary>
        public void Start(Vector3 startPos, Quaternion startRot)
        {
            startPosition = startPos;
            startRotation = startRot;

            RunCallbacks(onStartCallbacks);

            try
            {
                OnStart();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Exception in OnStart: {e}");
                Error();
                return;
            }

            if (state == CommandState.None)
                state = callType == CallType.NeedAction ? CommandState.NeedAction : CommandState.Idle;

            Tick(); // Execute immediately rather than waiting for the next external tick.
        }

        public void Cancel()
        {
            StopWaitCoroutine();
            RunCallbacks(onCancelCallbacks);

            state = CommandState.Canceled;

            try
            {
                OnCancel();
            }
            catch (Exception e)
            {
                state = CommandState.Error;
                Debug.LogError($"[{GetType().Name}] Exception in OnCancel: {e}");
            }

            Complete();
        }

        public void Error()
        {
            StopWaitCoroutine();
            state = CommandState.Error;
            RunCallbacks(onErrorCallbacks);

            try
            {
                OnError();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Exception in OnError: {e}");
                state = CommandState.Error;
            }

            // If OnError() does not change the state or throws an exception, complete the command.
            if (state == CommandState.Error)
            {
                Complete();
            }
        }

        /// <summary>Called when the player targets this command with a ground position.</summary>
        public CommandState Action(Vector3 targetPos, Quaternion targetRot)
        {
            if (targetType != TargetType.Position)
            {
                Debug.LogError($"[{GetType().Name}] This command doesn't accept a ground target.");
                return state;
            }

            try
            {
                OnAction(targetPos, targetRot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Exception in OnAction: {e}");
                Error();
            }

            return state;
        }

        /// <summary>Called when the player targets this command with a GameObject.</summary>
        public CommandState Action(GameObject target)
        {
            try
            {
                OnAction(target);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Exception in OnAction: {e}");
                Error();
            }

            return state;
        }

        /// <summary>
        /// Puts the command into a waiting state, optionally running a queue of coroutine
        /// actions before automatically resuming.
        /// </summary>
        public void Wait(Queue<Func<IEnumerator>> actions = null,
            WaitFailure waitFailureMode = WaitFailure.Continue,
            AfterWaitBehaviour afterWaitBehaviour = AfterWaitBehaviour.Continue)
        {
            if (state == CommandState.Waiting)
                return;

            state = CommandState.Waiting;
            this.afterWaitBehaviour = afterWaitBehaviour;

            RunCallbacks(onWaitCallbacks);

            OnWait(actions, waitFailureMode);
        }

        public void Resume()
        {
            RunCallbacks(onResumeCallbacks);

            switch (afterWaitBehaviour)
            {
                case AfterWaitBehaviour.Continue:
                    state = callType == CallType.NeedAction ? CommandState.NeedAction : CommandState.Idle;
                    break;
                case AfterWaitBehaviour.Cancel:
                    Cancel();
                    break;
                case AfterWaitBehaviour.Finish:
                    Finish(startPosition, startRotation);
                    break;
            }
        }

        /// <summary>Called when the command finishes naturally (as opposed to being canceled).</summary>
        public void Finish(Vector3 targetPos, Quaternion targetRot)
        {
            StopWaitCoroutine();
            state = CommandState.Finished;
            targetPosition = targetPos;
            targetRotation = targetRot;

            RunCallbacks(onFinishCallbacks);

            try
            {
                OnFinish();
                Complete();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Exception in OnFinish: {e}");
                Error();
            }
        }

        /// <summary>
        /// Final step after finish/cancel/error handling - marks the command as done so its
        /// owner can safely remove it from its queue, and notifies any listeners.
        /// </summary>
        protected void Complete()
        {
            if (IsDone)
                return;

            IsDone = true;
            RunCallbacks(onCompleteCallbacks);
        }

        public CommandState Tick()
        {
            try
            {
                switch (state)
                {
                    case CommandState.Idle:
                    case CommandState.NeedAction:
                        Execute();
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Exception in Execute: {e}");
                state = CommandState.Error;
                Error();
            }

            return state;
        }

        #endregion

        #region Waiting / coroutines

        /// <summary>
        /// Runs a queue of coroutine-returning actions in sequence, then resumes automatically.
        /// If no actions are given, the command stays in the Waiting state until something else
        /// calls <see cref="Resume"/> (e.g. an animation event or a timer).
        /// </summary>
        protected virtual void OnWait(Queue<Func<IEnumerator>> actions, WaitFailure waitFailureMode)
        {
            if (actions == null || actions.Count == 0)
                return;

            if (caster == null || !caster.TryGetComponent(out CommandCaster cc))
            {
                Debug.LogError($"[{GetType().Name}] Caster must have a CommandCaster component to run wait actions.");
                return;
            }

            _waitCoroutineHandle = cc.StartCoroutine(WaitCoroutine(actions, waitFailureMode));
        }

        private IEnumerator WaitCoroutine(Queue<Func<IEnumerator>> actions, WaitFailure waitFailureMode)
        {
            try
            {
                while (actions.Count > 0)
                {
                    if (state is CommandState.Canceled or CommandState.Error or CommandState.Finished)
                        yield break;

                    var action = actions.Dequeue();
                    var failed = false;

                    if (caster == null || !caster.TryGetComponent(out CommandCaster cc))
                    {
                        failed = true;
                    }
                    else
                    {
                        yield return cc.StartCoroutine(SafeExecute(action, () => failed = true));
                    }

                    if (failed && waitFailureMode == WaitFailure.Error)
                    {
                        Error();
                        yield break;
                    }
                }

                if (state == CommandState.Waiting)
                    Resume();
            }
            finally
            {
                _waitCoroutineHandle = null;
            }
        }

        private static IEnumerator SafeExecute(Func<IEnumerator> action, Action onFail)
        {
            IEnumerator routine;
            try
            {
                routine = action();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception in wait action: {e}");
                onFail?.Invoke();
                yield break;
            }

            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = routine.MoveNext();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception in wait action coroutine: {e}");
                    onFail?.Invoke();
                    yield break;
                }

                if (!hasNext)
                    yield break;

                yield return routine.Current;
            }
        }

        protected void StopWaitCoroutine()
        {
            if (_waitCoroutineHandle == null)
                return;

            if (caster != null && caster.TryGetComponent(out CommandCaster cc))
                cc.Stop(_waitCoroutineHandle);

            _waitCoroutineHandle = null;
        }

        protected IEnumerator WaitSecondsCoroutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        #endregion

        #region AddToCallbacks

        /// <summary>
        /// Adds a callback that will be invoked when the command reaches this lifecycle stage.
        /// </summary>
        /// <param name="callback">
        /// Action to execute when the event occurs.
        /// </param>
        /// <returns>
        /// An action that removes the callback from the command.
        /// Calling the returned action multiple times has no effect.
        /// </returns>
        private static Action AddCallback(List<Action> list, Action callback)
        {
            list.Add(callback);

            bool removed = false;

            return () =>
            {
                if (removed)
                    return;

                removed = true;
                list.Remove(callback);
            };
        }

        /// <summary>
        /// Adds an action that is executed when the command starts.
        /// </summary>
        /// <param name="callback">
        /// Callback invoked after the command starts but before <see cref="OnStart"/> finishes.
        /// </param>
        /// <returns>
        /// A cleanup action that removes this callback.
        /// </returns>
        public Action AddOnStartCallback(Action callback) => AddCallback(onStartCallbacks, callback);

        /// <summary>
        /// Adds an action executed when the command enters the waiting state.
        /// </summary>
        /// <returns>A cleanup action that removes this callback.</returns>
        public Action AddOnWaitCallback(Action callback) => AddCallback(onWaitCallbacks, callback);

        /// <summary>
        /// Adds an action executed when the command leaves the waiting state.
        /// </summary>
        /// <returns>A cleanup action that removes this callback.</returns>
        public Action AddOnResumeCallback(Action callback) => AddCallback(onResumeCallbacks, callback);

        /// <summary>
        /// Adds an action executed when the command enters the cancel state.
        /// </summary>
        /// <returns>A cleanup action that removes this callback.</returns>
        public Action AddOnCancelCallback(Action callback) => AddCallback(onCancelCallbacks, callback);

        /// <summary>
        /// Adds an action executed when the command enters the error state.
        /// </summary>
        /// <returns>A cleanup action that removes this callback.</returns>
        public Action AddOnErrorCallback(Action callback) => AddCallback(onErrorCallbacks, callback);

        /// <summary>
        /// Adds an action executed when the command enters the finish state.
        /// </summary>
        /// <returns>A cleanup action that removes this callback.</returns>
        public Action AddOnFinishCallback(Action callback) => AddCallback(onFinishCallbacks, callback);

        /// <summary>
        /// Adds an action invoked when the command lifecycle ends, regardless of the final outcome.
        /// </summary>
        /// <param name="callback">Action executed on completion.</param>
        /// <returns>A cleanup action that removes the callback.</returns>
        public Action AddOnCompleteCallback(Action callback) => AddCallback(onCompleteCallbacks, callback);

        #endregion

        #region Cleanup / IDisposable

        private static void RunCallbacks(List<Action> callbacksList)
        {
            if (callbacksList.Count == 0)
                return;

            var callbacks = callbacksList.ToArray();

            foreach(var action in callbacks)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Callback exception: {e}");
                }
            }
        }

        protected virtual void ClearCallbackQueues()
        {
            onStartCallbacks.Clear();
            onWaitCallbacks.Clear();
            onResumeCallbacks.Clear();
            onCancelCallbacks.Clear();
            onErrorCallbacks.Clear();
            onFinishCallbacks.Clear();
            onCompleteCallbacks.Clear();
        }

        /// <summary>
        /// Releases the command's resources. Must be called explicitly once the command is done.
        /// There is no finalizer, so a missed call will leak the wait-coroutine handle and skip cleanup.
        /// </summary>
        public void Dispose()
        {
            // No finalizer here on purpose: it would need to touch StopWaitCoroutine() -> Unity API,
            // which must only run on the main thread, never from the GC finalizer thread.

            if (_disposed)
                return;

            StopWaitCoroutine();

            try
            {
                if (!IsDone)
                    Complete();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[{GetType().Name}] Exception during Dispose: {e}"
                    );
            }

            ClearCallbackQueues();
            Cleaning();

            _disposed = true;
        }

        #endregion

        #region Abstract API for subclasses

        protected abstract void OnStart();

        /// <summary>If the error is recoverable, this should move the command back to Idle; otherwise call Cancel().</summary>
        protected abstract void OnError();
        protected abstract void OnCancel();
        protected abstract void OnFinish();
        protected abstract void Execute();

        /// <summary>Called when the player performs an action targeting a GameObject.</summary>
        protected abstract void OnAction(GameObject target);

        /// <summary>Called when the player performs an action targeting a ground position.</summary>
        protected abstract void OnAction(Vector3 targetPos, Quaternion targetRot);

        /// <summary>Cleanup logic run once, when the command is disposed (after finish or cancel).</summary>
        protected abstract void Cleaning();

        #endregion
    }
}