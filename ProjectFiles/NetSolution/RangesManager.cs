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
using System.Collections.Generic;
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
        private IUANode _uiContainer;
        private Store _store;
        private IUANode _pens;
        private IUANode _rangesNode;
        private List<IUANode> _trendTimeRanges;

        public ReferencesObserver(IUANode rangesNode, IUANode pens, IUANode uiContainer, Store store)
        {
            this._uiContainer = uiContainer;
            this._store = store;
            this._pens = pens;
            this._rangesNode = rangesNode;
            _trendTimeRanges = new List<IUANode>();

            rangesNode.Children.ToList().ForEach(CreateRangeUI);

        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            AddTimeRange(targetNode);
            CreateRangesUI();
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            RemoveTimeRange(targetNode);
            CreateRangesUI();
        }

        private void CreateRangesUI()
        {
            _uiContainer.Children.Clear();
            foreach (var tr in _trendTimeRanges)
            {
                CreateRangeUI(tr);
            }
        }

        private void CreateRangeUI(IUANode rangeNode)
        {
            TimeRange range = (TimeRange)(rangeNode as IUAVariable).Value.Value;
            var startTime = range.StartTime;
            var endTime = range.EndTime;
            var trendWidgetInstance = InformationModel.Make<TrendRangeWidget>(rangeNode.BrowseName);
            trendWidgetInstance.GetVariable("RangeStartDate").Value = startTime;
            trendWidgetInstance.GetVariable("RangeEndDate").Value = endTime;
            var timeSpan = range.EndTime - range.StartTime;
            trendWidgetInstance.GetVariable("Timespan").Value = timeSpan.TotalMilliseconds;
            _uiContainer.Add(trendWidgetInstance);
        }

        private void AddTimeRange(IUANode targetNode)
        {
            _trendTimeRanges.Add(targetNode);
            SortTimeRanges();
        }

        private void RemoveTimeRange(IUANode targetNode)
        {
            var trToRemove = ((FTOptix.Core.TimeRange)((UAManagedCore.UAVariable)targetNode).Value.Value);
            var tr = _trendTimeRanges.FirstOrDefault(tr =>
                ((FTOptix.Core.TimeRange)((UAManagedCore.UAVariable)tr).Value.Value).StartTime == trToRemove.StartTime
                &&
                ((FTOptix.Core.TimeRange)((UAManagedCore.UAVariable)tr).Value.Value).EndTime == trToRemove.EndTime);

            _trendTimeRanges.Remove(tr);
            SortTimeRanges();
        }

        private void SortTimeRanges()
        {
            _trendTimeRanges.Sort(
                (tr1, tr2) =>
                    DateTime.Compare(
                        ((FTOptix.Core.TimeRange)((UAManagedCore.UAVariable)tr1).Value.Value).StartTime,
                        ((FTOptix.Core.TimeRange)((UAManagedCore.UAVariable)tr2).Value.Value).StartTime
                ));
        }
    }
}
