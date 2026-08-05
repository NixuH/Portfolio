using Game.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Commands.CommandsData;
using static Pathfinding.Util.RetainedGizmos;

public class Build_Command : Game.Commands.Command
{
    private GameObject buildingInConstruction;
    private readonly BuildKey buildKey;

    public Build_Command(
        GameObject caster,
        CommandsData data,
        BuildKey buildKey)
        : base(
            caster,
            data,
            CallType.Active,
            TargetType.Position,
            CommandType.Build)
    {
        this.buildKey = buildKey;
    }

    protected override void OnStart()
    {
        var buildData = commandData.GetBuildData(buildKey);

        var targetPosition =
                new Vector3(
                    Mathf.Floor(startPosition.x),
                    Mathf.Round(startPosition.y),
                    Mathf.Floor(startPosition.z)) 
                + buildData.prefab
                    .GetComponent<BuildingParams>()
                    .offset;

        var targetRotation = Quaternion.Euler(
            0,
            Mathf.Round(startRotation.eulerAngles.y / 90f) * 90f,
            0);

        buildingInConstruction =
            UnityEngine.Object.Instantiate(
                buildData.prefab,
                targetPosition,
                targetRotation);

        // Temporary demonstration of the command waiting lifecycle. Cammand Sends debug log onFinish
        Wait(
            new Queue<Func<IEnumerator>>(
                new[] { new Func<IEnumerator>(() => WaitSecondsCoroutine(3)) } 
            ),
            WaitFailure.Error,
            AfterWaitBehaviour.Finish);

        buildingInConstruction
            .GetComponent<Building>()
            .StartConstruction();
    }


    protected override void OnFinish()
    {
        Debug.Log("BuildCommandFinished");
    }

    protected override void OnCancel() { }

    protected override void OnError() { }

    protected override void Cleaning() { }

    protected override void Execute() { }

    protected override void OnAction(Vector3 targetPos, Quaternion targetRot) { }

    protected override void OnAction(GameObject target) { }
}