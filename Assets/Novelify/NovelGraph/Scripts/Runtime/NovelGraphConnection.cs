using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    public struct NovelGraphConnection
    {
        public NovelGraphConnectionPort inputPort;
        public NovelGraphConnectionPort outputPort;

        public NovelGraphConnection(NovelGraphConnectionPort input, NovelGraphConnectionPort output)
        {
            inputPort = input;
            outputPort = output;
        }

        public NovelGraphConnection(string inputPortId, int inputPortIndex,
            string outputPortId, int outputPortIndex)
        {
            inputPort = new NovelGraphConnectionPort(inputPortId, inputPortIndex);
            outputPort = new NovelGraphConnectionPort(outputPortId, outputPortIndex);
        }
    }

    [System.Serializable]
    public struct NovelGraphConnectionPort
    {
        public string nodeId;
        public int portIndex;

        public NovelGraphConnectionPort(string id, int index)
        {
            this.nodeId = id;
            this.portIndex = index;
        }
    }
}
