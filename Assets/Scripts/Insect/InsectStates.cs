using Fusion;
using UnityEngine;

public abstract class InsectState
{
    [SerializeField] protected MoveInsect insect;

    public InsectState(MoveInsect insect)
    {
        this.insect = insect;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }

    public virtual void Update() { }
    public virtual void FixedUpdateNetwork() { }

    public virtual bool CanCutThread() => true;
    public virtual bool IsParalised() => false;
    public virtual float GetSpeedMultiplier() => 1f;
}


public class NormalState : InsectState
{
    public NormalState(MoveInsect insect) : base(insect) { }

    public override void Enter()
    {
        Debug.Log("Entered Normal State");
    }
}

public class FastState : InsectState
{
    public FastState(MoveInsect insect) : base(insect) { }

    public override void Enter()
    {
        Debug.Log("Entered Fast State");
    }

    public override float GetSpeedMultiplier() => 2f;
}

public class SlowState : InsectState
{
    public SlowState(MoveInsect insect) : base(insect) { }

    public override void Enter()
    {
        Debug.Log("Entered Slow State");
    }

    public override float GetSpeedMultiplier() => 0.5f;
}

public class ParalyzedState : InsectState
{
    private float timer;
    private float duration = 10f;

    public ParalyzedState(MoveInsect insect) : base(insect) { }

    public override void Enter()
    {
        timer = duration;
        Debug.Log("Entered Paralyzed State");
    }

    public override void Update()
    {
        timer -= insect.Runner.DeltaTime;
        if (timer <= 0f)
            insect.State = new NormalState(insect);
    }

    public override bool IsParalised() => true;

    public override bool CanCutThread() => false;

    public override float GetSpeedMultiplier() => 0f;
}

public class CannotCutThreadState : InsectState
{
    private float timer;
    private float duration = 10f;

    public CannotCutThreadState(MoveInsect insect) : base(insect) { }

    public override void Enter()
    {
        timer = duration;
        Debug.Log("Entered Cannot Cut Thread State");
    }

    public override void Update()
    {
        timer -= insect.Runner.DeltaTime;
        if (timer <= 0f)
            insect.State = new NormalState(insect);
    }

    public override bool CanCutThread() => false;
}