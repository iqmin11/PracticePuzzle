using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class FSM : MonoBehaviour
{
    public class State
    {
        public string Name;
        public System.Action Start;
        public System.Action Update;
        public System.Action End;
    };

    private void Awake()
    {
        AllState = new Dictionary<string, State>();
    }

    // Update is called once per frame
    public void Update()
    {
        if(CurState == null)
        {
            return;
        }

        CurState.Update();
    }

    public void CreateState(string StateName, System.Action StateStart, System.Action StateUpdate, System.Action StateEnd)
    {
        if (FindState(StateName) != null)
        {
            return;
        }

        AllState.Add(StateName, new FSM.State());
        FSM.State Temp = AllState[StateName];
        Temp.Name = StateName;
        Temp.Start = StateStart;
        Temp.Update = StateUpdate;
        Temp.End = StateEnd;
    }

    public void ChangeState(string StateName)
    {
        if (null != CurState)
        {
            if (null != CurState.End)
            {
                CurState.End();
            }
        }

        CurState = FindState(StateName);

        if (CurState == null)
        {
            return;
        }

        CurState.Start();
    }

    private State FindState(string StateName)
    {
        if(AllState.ContainsKey(StateName))
        {
            return AllState[StateName];
        }

        return null;
    }

    private Dictionary<string, State> AllState;
    private State CurState;
}
