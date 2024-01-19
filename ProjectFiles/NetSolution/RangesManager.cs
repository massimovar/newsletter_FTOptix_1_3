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
using FTOptix.S7TiaProfinet;
using FTOptix.CommunicationDriver;
using FTOptix.MicroController;
using FTOptix.CODESYS;
using static System.Formats.Asn1.AsnWriter;
#endregion

public class RangesManager : BaseNetLogic
{
    private Trend _trend;
    private Store _store;
    private IUANode _trendPens;
    private IUANode _ranges;
    private IUANode _rangesContainer;
    private ReferencesObserver _referencesObserver;
    private IEventRegistration referencesEventRegistration;

    public override void Start()
    {
        _trend = InformationModel.Get<Trend>(LogicObject.GetVariable("MyTrend").Value);
        _store = InformationModel.Get<Store>(_trend.Model);
        _trendPens = _trend.Get("Pens");
        _ranges = _trend.Get("TimeRanges");
        _rangesContainer = InformationModel.Get(LogicObject.GetVariable("TimeRangesContainer").Value);

        _referencesObserver = new ReferencesObserver(_ranges, _trendPens, _rangesContainer, _store);
        referencesEventRegistration = _ranges.RegisterEventObserver(_referencesObserver, EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved);
    }

    public override void Stop()
    {
        if (referencesEventRegistration == null) return;
        referencesEventRegistration.Dispose();
    }

    private class ReferencesObserver : IReferenceObserver
    {
        private IUANode uiContainer;
        private Store store;
        private IUANode pens;

        public ReferencesObserver(IUANode rangesNode, IUANode pens, IUANode uiContainer, Store store)
        {
            this.uiContainer = uiContainer;
            this.store = store;
            this.pens = pens;
            rangesNode.Children.ToList().ForEach(CreateRangeUI);
        }

        private void CreateRangeUI(IUANode rangeNode)
        {
            TimeRange range = (TimeRange)(rangeNode as IUAVariable).Value.Value;
            var trendWidgetInstance = InformationModel.Make<TrendRangeWidget>(rangeNode.BrowseName);
            trendWidgetInstance.GetVariable("RangeStartDate").Value = range.StartTime;
            trendWidgetInstance.GetVariable("RangeEndDate").Value = range.EndTime;
            var timeSpan = range.EndTime - range.StartTime;
            trendWidgetInstance.GetVariable("Timespan").Value = timeSpan.TotalMilliseconds;
            uiContainer.Add(trendWidgetInstance);
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            CreateRangeUI(targetNode);
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            var uiRange = uiContainer.Get(targetNode.BrowseName);
            uiRange?.Delete();
        }
    }
}
