﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltarDialogTestRunner : StateListenerBehaviour
{
    [SerializeField] string dialogToRun = "Test1";

    [SerializeField] bool runOnStart;
    [SerializeField] TestDialogVisualizer dialogVisualizer;

    protected override void OnRealStart()
    {
        if (runOnStart)
            Run();
    }

    [Zenject.Inject] ProgressionHandler progression;

    [NaughtyAttributes.Button(null, NaughtyAttributes.EButtonEnableMode.Playmode)]
    public void Run()
    {

        var collection = MiroParser.LoadTreesAsAltarTreeCollection();

        if (collection != null)
        {
            var node = MiroParser.FindDialogWithName(collection, dialogToRun);
            if (node != null)
            {
                StartCoroutine(RunRoutine(collection, node));
            }
            else
            {
                Debug.LogError("TestDialog not found");
            }
        }
        else
        {
            Debug.LogError("Failed to load AltarTreeCollection");
        }
    }

    public IEnumerator RunRoutine(AltarTreeCollection collection, AltarBaseNode node)
    {
        Debug.Log("NodeDebugRunner Start");
        var prog = (progression == null) ? (IDialogPropertiesHandler)new TestPropertiesHandler() : progression;
        INodeServiceProvider provider = new TestAltarDialogServiceProvider(dialogVisualizer, prog, collection);
        NodeResult result = NodeResult.Wait;
        dialogVisualizer.StartDialog();
        while (node != null)
        {
            if (node is IConditionalNode conditionalNode)
            {
                if (!conditionalNode.ConditionPassed(provider))
                {
                    Debug.LogError("NodeDebugRunner stopped from failed conditions " + node.ToDebugString());
                    yield break;
                }
            }

            if (node is IStartableNode startableNode)
            {
                result = startableNode.Start(provider);
            }

            if (result == NodeResult.Wait)
            {
                if (node is ITickingNode tickingNode)
                {
                    while (result == NodeResult.Wait)
                    {
                        yield return null;
                        result = tickingNode.Tick(provider);
                    }
                }
            }

            if (result == NodeResult.Error)
            {
                Debug.LogError("NodeDebugRunner exited with Error");
                yield break;
            }
            else if ((int)result < node.Children.Length && (int)result >= 0)
            {
                if (node is IEndableNode endableNode)
                {
                    endableNode.OnEnd(provider);
                }

                node = node.Children[(int)result];
                result = NodeResult.Wait;
            }
            else
            {
                if (node is IEndableNode endableNode)
                    endableNode.OnEnd(provider);

                if (node.Children != null && node.Children.Length > 0)
                {
                    node = node.Children[0];
                }
                else
                {
                    Debug.Log("Result: " + result + ", Node: " + node + " " + node.ToDebugString());
                    node = null;
                }
                result = NodeResult.Wait;
            }
        }

        dialogVisualizer.EndDialog();
        Debug.Log("NodeDebugRunner Finish");
    }


    public class TestAltarDialogServiceProvider : INodeServiceProvider
    {
        IDialogVisualizer visualizer;
        IDialogPropertiesHandler properties;
        AltarTreeCollection treeCollection;

        public TestAltarDialogServiceProvider(IDialogVisualizer vis, IDialogPropertiesHandler prop, AltarTreeCollection treeCollection)
        {
            visualizer = vis;
            properties = prop;
            this.treeCollection = treeCollection;
        }

        public IDialogVisualizer DialogVisualizer => visualizer;
        public IDialogPropertiesHandler Properties => properties;

        public AltarTreeCollection AltarTreeCollection => treeCollection;
    }

    public class TestPropertiesHandler : IDialogPropertiesHandler
    {
        public void FireEvent(string @event)
        {
            Debug.Log("EventFired: " + @event);
        }

        public bool GetVariable(string name)
        {
            return true;
        }

        public void SetVariable(string variableName, bool variableState)
        {
        }
    }
}