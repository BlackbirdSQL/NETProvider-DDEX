// Microsoft.SqlServer.ConnectionDlg.UI, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.SqlServer.ConnectionDlg.UI.BrowsePageViewModel

using System.Diagnostics;

using BlackbirdSql.Core;
using BlackbirdSql.Core.Diagnostics;
using BlackbirdSql.Core.Diagnostics.Enums;
using BlackbirdSql.Core.Events;
using BlackbirdSql.Core.Interfaces;
using BlackbirdSql.Common.Model;





namespace BlackbirdSql.Wpf.Model;


public class BrowsePageViewModel : ViewModelBase
{
    protected const string C_KeySelectedConnection = "SelectedConnection";

    protected static new DescriberDictionary _Describers;

    private readonly ConnectionPropertySectionViewModel _connectionPropertyViewModel;

    public ConnectionInfo SelectedConnection
    {
        get { return (ConnectionInfo)GetProperty(C_KeySelectedConnection); }
        set { SetProperty(C_KeySelectedConnection, value); }
    }



    public ConnectionPropertySectionViewModel ConnectionPropertyViewModel => _connectionPropertyViewModel;

    protected Traceable Trace { get; set; }

    public BrowsePageViewModel(BrowsePageViewModel rhs) : base(rhs.ParentViewModel, rhs)
    {
        Trace = new(rhs.Trace.DependencyManager);
        _connectionPropertyViewModel = rhs.ConnectionPropertyViewModel;

        if (_connectionPropertyViewModel != null && rhs.Channel != null)
        {
            _Channel = rhs.Channel;
            _Channel.SelectedConnectionChanged -= UpdateSelectedConnection;
            _Channel.SelectedConnectionChanged += UpdateSelectedConnection;
        }
    }

    public BrowsePageViewModel(IBDependencyManager dependencyManager, ConnectionPropertySectionViewModel connectionPropertyViewModel, IBEventsChannel channel)
        : base()
    {
        Cmd.CheckForNull(connectionPropertyViewModel, "connectionPropertyViewModel");
        Cmd.CheckForNull(channel, "channel");
        // Cmd.CheckForNull(dependencyManager, "dependencyManager");
        Trace = new Traceable(dependencyManager);
        if (connectionPropertyViewModel != null && channel != null)
        {
            _connectionPropertyViewModel = connectionPropertyViewModel;
            channel.SelectedConnectionChanged -= UpdateSelectedConnection;
            channel.SelectedConnectionChanged += UpdateSelectedConnection;
        }
    }


    protected static new void CreateAndPopulatePropertySet(DescriberDictionary describers = null)
    {
        if (_Describers == null)
        {
            _Describers = new();

            // Initializers for property sets are held externally for this class
            ViewModelBase.CreateAndPopulatePropertySet(_Describers);

            _Describers.Add(C_KeySelectedConnection, typeof(object), null);
        }

        // If null then this was a call from our own .ctor so no need to pass anything back
        describers?.AddRange(_Describers);

    }

    public override IBPropertyAgent Copy()
    {
        return new BrowsePageViewModel(this);
    }

    public void UpdateSelectedConnection(object sender, SelectedConnectionChangedEventArgs e)
    {
        Trace.AssertTraceEvent(e != null, TraceEventType.Error, EnUiTraceId.UiInfra, "event argument is null");
        if (e != null)
        {
            IBPropertyAgent browseInfo = e.ConnectionInfo;
            Trace.AssertTraceEvent(browseInfo != null, TraceEventType.Error, EnUiTraceId.UiInfra, "browseInfo is null");
            if (browseInfo != null)
            {
                ConnectionInfo connectionInfo = new(browseInfo.Channel, (ConnectionInfo)browseInfo);
                _connectionPropertyViewModel.UpdateConnectionProperty(connectionInfo);
            }
        }
    }
}