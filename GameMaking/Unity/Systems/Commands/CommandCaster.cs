using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Commands
{
    /// <summary>
    /// Drives a GameObject's commands: owns the currently active command, ticks passive
    /// (always-on) commands every physics step, and forwards player input (Action) to
    /// whichever command is currently waiting for it.
    /// </summary>
    public class CommandCaster : MonoBehaviour
    {
        public Quaternion targetRot = Quaternion.identity;

        private readonly List<Command> passiveCommands = new();

        private readonly List<Command> onCompleteCommands = new();

        private Command currentCommand;

        public Command CurrentCommand => currentCommand;

        #region Coroutine helpers (commands run their "wait" coroutines through the caster)

        public Coroutine Run(IEnumerator coroutine) => StartCoroutine(coroutine);

        public void Stop(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        public void StopAll() => StopAllCoroutines();

        #endregion

        public void AddToOnCompleteCommand(Command cmd)
        {
            if (onCompleteCommands.Contains(cmd))
                return;

            onCompleteCommands.Add(cmd);

            cmd.AddOnCompleteCallback(() => RemoveFromOnCompleteCommand(cmd));
        }

        private void RemoveFromOnCompleteCommand(Command cmd)
        {
            onCompleteCommands.Remove(cmd);

            cmd.Dispose();
        }

        public void CancelCurrentCommand()
        {
            if (currentCommand == null)
                return;

            currentCommand.Cancel();
            currentCommand.Dispose();
            currentCommand = null;
        }

        public void SetCurrentCommand(Command command, Vector3 startPos, Quaternion startRot)
        {
            CancelCurrentCommand();
            currentCommand = command;
            command.Start(startPos, startRot);

            if (currentCommand != null && currentCommand.IsDone)
            {
                currentCommand.Dispose();
                currentCommand = null;
            }
        }

        public void AddPassiveCommand(Command command) => passiveCommands.Add(command);

        public void Action(ref RaycastHit hit)
        {
            if (currentCommand == null)
                return;

            if (currentCommand.targetType != TargetType.Position && hit.collider == null)
            {
                Debug.LogError($"[CommandCaster] '{currentCommand.commandType}' needs an object target but the raycast hit nothing.");
                return;
            }

            var state = currentCommand.targetType == TargetType.Position
                ? currentCommand.Action(hit.point, targetRot)
                : currentCommand.Action(hit.collider.gameObject);

            if (currentCommand.IsDone)
            {
                currentCommand.Dispose();
                currentCommand = null;
                targetRot = Quaternion.identity;
            }
            else if (state != CommandState.NeedAction)
            {
                AddToOnCompleteCommand(currentCommand);
                currentCommand = null;
                targetRot = Quaternion.identity;
            }
        }

        private void FixedUpdate()
        {
            TickCurrentCommand();
            TickPassiveCommands();
        }

        private void TickCurrentCommand()
        {
            if (currentCommand == null)
                return;

            currentCommand.Tick();

            if (currentCommand.IsDone)
            {
                currentCommand.Dispose();
                currentCommand = null;
            }
        }

        private void TickPassiveCommands()
        {
            for (int i = passiveCommands.Count - 1; i >= 0; i--)
            {
                var command = passiveCommands[i];
                command.Tick();

                if (command.IsDone)
                {
                    command.Dispose();
                    passiveCommands.RemoveAt(i);
                }
            }
        }
    }
}