using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/////////////////
////   WIP   ////
/////////////////
[ExecuteInEditMode]
public class BasePuzzleObject : BaseStaticObject
{
    private void Awake()
    {

    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    protected override GameObject CustomiseAddSide(GameObject Side)
    {
        BasePuzzleSide newSide = Side.AddComponent<BasePuzzleSide>();
        return Side;
    }
}
