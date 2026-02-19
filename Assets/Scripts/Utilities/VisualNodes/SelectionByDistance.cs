using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RenamedFrom("SelectionByDistanceNode")]
public class SelectionByDistance : Unit
{
    protected override void Definition()
    {
        Begin = ControlInput("Begin", ACT);
        After = ControlOutput("After");
        InputDistance = ValueInput<float>("InputDistance", 0);
        PreviousValue = ValueInput<float>("PreviousValue", 0);
        OutputValue = ValueOutput<float>("OutputValue");

        for (int i = 0; i < rangeCount; i++) rangePorts.Add(ValueInput<float>($"Range {i}"));
    }

    [PortLabelHidden, DoNotSerialize] public ControlInput Begin;
    [DoNotSerialize] public ValueInput InputDistance;
    [DoNotSerialize] public ValueInput PreviousValue;
    [DoNotSerialize] public List<ValueInput> rangePorts = new();

    [PortLabelHidden, DoNotSerialize] public ControlOutput After;
    [DoNotSerialize] public ValueOutput OutputValue;

    [DoNotSerialize, Inspectable, UnitHeaderInspectable("Ranges")]
    public int rangeCount
    {
        get => _rangeCount;
        set => _rangeCount = Mathf.Clamp(value, 0, 10);
    }
    [SerializeAs(nameof(rangeCount))]
    private int _rangeCount;


    ControlOutput ACT(Flow flow)
    {
        int prevValue = flow.GetValue<int>(PreviousValue);

        int i = rangePorts.Count;

        while (i > 0 && flow.GetValue<float>(rangePorts[i - 1]) < flow.GetValue<float>(InputDistance)) i--;

        flow.SetValue(OutputValue, i);
        return prevValue != i ? After : null;
    }
}
