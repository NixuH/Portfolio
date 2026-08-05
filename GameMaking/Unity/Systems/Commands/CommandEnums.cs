namespace Game.Commands
{
    /// <summary>Current stage of a Command's life.</summary>
    public enum CommandState
    {
        None,
        Idle,
        NeedAction,
        Waiting,
        Error,
        Canceled,
        Finished
    }

    /// <summary>How the command starts running.</summary>
    public enum CallType
    {
        /// <summary>Starts running right away.</summary>
        Active,
        /// <summary>Waits for player input (a target) before it runs.</summary>
        NeedAction,
        /// <summary>Never ticked directly. Reacts to game events instead.</summary>
        Passive,
        /// <summary>Started by an outside event (e.g. OnAttack).</summary>
        OnEvent
    }

    /// <summary>What kind of target a command needs.</summary>
    public enum TargetType
    {
        Position,
        Unit,
        GroundUnit,
        AirUnit,
        Building
    }

    /// <summary>What to do if a queued wait-action fails.</summary>
    public enum WaitFailure
    {
        Continue,
        Error
    }

    /// <summary>What a command does after waiting ends.</summary>
    public enum AfterWaitBehaviour
    {
        Continue,
        Cancel,
        Finish
    }

    /// <summary>Category used for UI, filters, and cooldown groups.</summary>
    public enum CommandType
    {
        Attack,
        Move,
        Build,
        Destroy,
        Summon,
        Passive
    }
}