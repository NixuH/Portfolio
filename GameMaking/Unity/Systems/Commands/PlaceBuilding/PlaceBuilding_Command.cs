using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Commands
{
    public class PlaceBuilding_Command : Command
    {
        #region State

        private bool preview;
        private GameObject previewHoldingBuilding;
        private CommandsData.BuildData _buildData;
        private readonly CommandsData.BuildKey buildKey;

        private CommandsData.BuildData BuildData =>
            _buildData ??= commandData.GetBuildData(buildKey);

        #endregion

        #region Constructor

        public PlaceBuilding_Command(
            GameObject caster,
            CommandsData commandData,
            CallType callType,
            TargetType targetType,
            CommandsData.BuildKey buildkey)
            : base(
                caster,
                commandData,
                callType,
                targetType,
                CommandType.Build)
        {
            this.buildKey = buildkey;
        }

        #endregion

        #region Lifecycle overrides

        protected override void OnStart()
        {
            preview = caster.GetComponent<IsometricPlayerController>() != null;

            if (preview)
            {
                previewHoldingBuilding = UnityEngine.Object.Instantiate(BuildData.prefab);
                var renderer = previewHoldingBuilding.GetComponent<MeshRenderer>();
                renderer.gameObject.layer = LayerMask.NameToLayer("Preview");
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var color = renderer.material.color;
                renderer.material.color = new Color(color.r, color.g, color.b, 0.01f);
            }

            Debug.Log("Build command started.");
        }

        protected override void Execute()
        {
            if (!preview || previewHoldingBuilding == null)
                return;

            if (Physics.SphereCast(
                Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()),
                0.1f,
                out RaycastHit hit,
                1000f,
                LayerMask.GetMask("Ground", "Building")))
            {
                targetPosition =
                    new Vector3(
                        Mathf.Floor(hit.point.x),
                        Mathf.Round(hit.point.y),
                        Mathf.Floor(hit.point.z))
                    + previewHoldingBuilding.GetComponent<BuildingParams>().offset;

                previewHoldingBuilding.transform.position = targetPosition;

                if (!previewHoldingBuilding.activeSelf)
                    previewHoldingBuilding.SetActive(true);
            }
            else
            {
                previewHoldingBuilding.SetActive(false);
            }
        }

        protected override void OnAction(Vector3 targetPos, Quaternion targetRot)
        {
            var building = new Build_Command(caster, commandData, CommandsData.BuildKey.CommandCenter);
            caster.GetComponent<CommandCaster>().AddToOnCompleteCommand(building);
            building.Start(targetPos, targetRot);
            Finish(targetPos, targetRot);
        }

        protected override void OnAction(GameObject target)
        {
            Debug.LogError("PlaceBuilding_Command accepts only ground targets.");
        }

        protected override void OnFinish()
        {
            Debug.Log("Build designation finished.");
        }

        protected override void OnCancel()
        {
            Debug.Log("Build designation canceled.");
        }

        protected override void OnError()
        {
            Debug.LogError("Build designation failed.");
        }

        protected override void Cleaning()
        {
            if (previewHoldingBuilding != null)
                UnityEngine.Object.Destroy(previewHoldingBuilding);

            previewHoldingBuilding = null;
        }

        #endregion
    }
}