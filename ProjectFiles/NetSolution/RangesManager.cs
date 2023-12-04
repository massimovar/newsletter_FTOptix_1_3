#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.EventLogger;
using FTOptix.HMIProject;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.DataLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Core;
using System.Linq;
#endregion

public class RangesManager : BaseNetLogic
{
    private TrendRangeWidget _trendRangesWidget;
    private Trend _myTrend;
    private IUANode ranges;
    private IEventRegistration referencesEventRegistration;

    public override void Start()
    {
        _trendRangesWidget = Project.Current.Get<TrendRangeWidget>("UI/Screens/Widget/TrendRangeWidget");
        _myTrend = InformationModel.Get<Trend>(LogicObject.GetVariable("MyTrend").Value);
        ranges = _myTrend.Get("TimeRanges");
        ReferencesObserver referencesObserver = new ReferencesObserver(ranges);

        referencesEventRegistration = ranges.RegisterEventObserver(referencesObserver, EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved);
    }

    public override void Stop()
    {
        if (referencesEventRegistration == null) return;
        referencesEventRegistration.Dispose();
    }

    private class ReferencesObserver : IReferenceObserver
    {
        public ReferencesObserver(IUANode rangesNode)
        {
            rangesNode.Children.ToList().ForEach(CreateRangeUI);
        }

        private void CreateRangeUI(IUANode rangeNode)
        {
            Log.Info(rangeNode.BrowseName);
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            Log.Info("Added");
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            
        }
    }
}
